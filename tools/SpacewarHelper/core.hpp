#pragma once
#include <cstdint>
#include <array>
#include <atomic>
#include <algorithm>
#include <deque>
#include <mutex>
#include <stdexcept>
#include <string>
#include <vector>

namespace woodbury {
constexpr uint16_t WireVersion = 6;
constexpr size_t MaxPacket = 256 * 1024, MaxFrame = MaxPacket + 1024, MaxQueue = 8 * 1024 * 1024;
enum class Op : uint16_t {
    Init=1, Host=2, Join=3, Leave=4, Friends=5, Invite=6, Send=7, Shutdown=8, ResetPeer=9, BlockPeer=10,
    Ready=100, Lobby=101, Packet=102, Friend=103, FriendsEnd=104, Invitation=105,
    PeerLeft=106, Error=107, Status=108, Ack=109, ConnectionLost=110, PeerMetrics=111, RelayStatus=112
};
using Bytes = std::vector<uint8_t>;
struct Frame {
    Op op{}; uint32_t request{}; bool reliable{true}; Bytes data;
    size_t size() const { return 13 + data.size(); }
};
struct Writer {
    Bytes bytes;
    void number(uint64_t value, size_t width) { for (size_t i=0;i<width;++i) bytes.push_back(static_cast<uint8_t>(value>>(8*i))); }
    void text(const std::string& value) {
        if (value.size()>4096) throw std::runtime_error("IPC string exceeds 4096 bytes");
        number(value.size(),4); bytes.insert(bytes.end(),value.begin(),value.end());
    }
};
struct Reader {
    const Bytes& bytes; size_t pos{};
    uint64_t number(size_t width) {
        if (width>8 || width>bytes.size()-pos) throw std::runtime_error("Truncated IPC payload");
        uint64_t value=0; for(size_t i=0;i<width;++i) value |= uint64_t(bytes[pos++])<<(8*i); return value;
    }
    std::string text() {
        auto size=number(4);
        if(size>4096 || size>bytes.size()-pos) throw std::runtime_error("Invalid IPC string length");
        std::string result(bytes.begin()+pos,bytes.begin()+pos+static_cast<size_t>(size)); pos+=static_cast<size_t>(size);
        if(result.find('\0')!=std::string::npos) throw std::runtime_error("NUL in IPC string");
        return result;
    }
    void end() const { if(pos!=bytes.size()) throw std::runtime_error("Trailing IPC payload"); }
};
inline uint32_t frameLength(const Bytes& prefix) {
    Reader r{prefix}; auto size=static_cast<uint32_t>(r.number(4)); r.end();
    if(size<9 || size>MaxFrame) throw std::runtime_error("Invalid IPC frame size");
    return size;
}
// .NET Framework's redirected StandardInput writer can emit a UTF-8 BOM
// when the Windows input code page is UTF-8, even through BaseStream.
// Accept that preamble only at process startup; all later framing stays strict.
template<class ReadExact>
uint32_t inputFrameLength(ReadExact read, bool& first) {
    auto prefix=read(4);
    bool startup=first; first=false;
    if(startup && prefix.size()==4 && prefix[0]==0xef && prefix[1]==0xbb && prefix[2]==0xbf) {
        auto tail=read(3);
        Bytes length{prefix[3]}; length.insert(length.end(),tail.begin(),tail.end());
        return frameLength(length);
    }
    return frameLength(prefix);
}
inline Frame decode(const Bytes& body) {
    if(body.size()<9 || body.size()>MaxFrame) throw std::runtime_error("Invalid IPC frame size");
    Reader r{body};
    if(r.number(2)!=WireVersion) throw std::runtime_error("Incompatible IPC version");
    Frame f; f.op=static_cast<Op>(r.number(2)); f.request=static_cast<uint32_t>(r.number(4));
    auto flags=r.number(1); if(flags>1) throw std::runtime_error("Invalid IPC flags");
    f.reliable=flags==1; f.data.assign(body.begin()+r.pos,body.end()); return f;
}
inline Bytes encode(const Frame& f) {
    if(f.data.size()>MaxFrame-9) throw std::runtime_error("Invalid IPC frame size");
    Writer w; w.number(f.data.size()+9,4); w.number(WireVersion,2); w.number(static_cast<uint16_t>(f.op),2);
    w.number(f.request,4); w.number(f.reliable?1:0,1); w.bytes.insert(w.bytes.end(),f.data.begin(),f.data.end()); return w.bytes;
}
class Queue {
    std::deque<Frame> frames_; size_t bytes_{}; std::mutex mutex_;
public:
    bool push(Frame f) {
        std::lock_guard<std::mutex> lock(mutex_);
        if(f.size()>MaxQueue) throw std::runtime_error("Frame exceeds IPC queue capacity");
        if(bytes_+f.size()>MaxQueue && !f.reliable) return false;
        for(auto it=frames_.begin();bytes_+f.size()>MaxQueue && it!=frames_.end();) {
            if(!it->reliable) { bytes_-=it->size(); it=frames_.erase(it); } else ++it;
        }
        if(bytes_+f.size()>MaxQueue) throw std::runtime_error("Reliable IPC queue exceeded 8 MiB");
        bytes_+=f.size(); frames_.push_back(std::move(f)); return true;
    }
    bool pop(Frame& f) {
        std::lock_guard<std::mutex> lock(mutex_);
        if(frames_.empty()) return false;
        f=std::move(frames_.front()); frames_.pop_front(); bytes_-=f.size(); return true;
    }
};

// Main-thread Steam outbox. Reliable messages retain their order. If Steam's
// reliable window is full, current unreliable poses may still pass that head;
// stale queued poses are discarded rather than delivered seconds late.
enum class SendResult { Accepted, Congested, Failed };
class SendBacklog {
    std::deque<Frame> frames_; size_t bytes_{};
public:
    size_t bytes() const { return bytes_; }
    bool empty() const { return frames_.empty(); }
    void clear() { frames_.clear(); bytes_=0; }
    bool push(const Frame& f) {
        if(bytes_+f.size()>MaxQueue && !f.reliable) return false;
        for(auto it=frames_.begin();bytes_+f.size()>MaxQueue && it!=frames_.end();) {
            if(!it->reliable) { bytes_-=it->size(); it=frames_.erase(it); } else ++it;
        }
        if(bytes_+f.size()>MaxQueue) throw std::runtime_error("Reliable Steam send queue exceeded 8 MiB");
        frames_.push_back(f); bytes_+=f.size(); return true;
    }
    template<class Sender> SendResult drain(Sender sender, size_t budget=64) {
        while(budget-- && !frames_.empty()) {
            auto result=sender(frames_.front());
            if(result==SendResult::Congested && frames_.front().reliable) {
                size_t disposable=0;
                for(const auto& frame:frames_) if(!frame.reliable) ++disposable;
                size_t skip=disposable>budget?disposable-budget:0;
                auto it=frames_.begin(); ++it;
                while(it!=frames_.end()) {
                    if(it->reliable) { ++it; continue; }
                    if(skip) --skip;
                    else if(budget) {
                        auto transient=sender(*it);
                        if(transient==SendResult::Failed) return transient;
                        --budget;
                    }
                    bytes_-=it->size(); it=frames_.erase(it);
                }
                return result;
            }
            if(result==SendResult::Failed) return result;
            bytes_-=frames_.front().size(); frames_.pop_front();
        }
        return SendResult::Accepted;
    }
};
inline bool validLobby(const std::string& magic, const std::string& protocol, const std::string& mod,
    const std::string& expectedProtocol, const std::string& expectedMod, uint64_t originalHost, uint64_t owner, int members) {
    return magic=="tudorek12345.woodbury.spacewar.v1" && protocol==expectedProtocol && mod==expectedMod &&
        originalHost!=0 && originalHost==owner && members>=1 && members<=2;
}
inline bool eligible(uint64_t candidate, uint64_t self, uint64_t owner, uint64_t selectedPeer, uint64_t allowOnly,
    bool hosting, bool member) {
    return member && candidate!=0 && candidate!=self &&
        (hosting ? (allowOnly==0 || candidate==allowOnly) : candidate==owner && candidate==selectedPeer);
}

// Steam can call its diagnostic hook on a service thread while holding a
// networking lock. Fixed storage and try-lock keep the callback nonblocking:
// no allocation, IO, Steam calls, or waiting for the consumer.
struct DiagnosticLine {
    static constexpr size_t TextCapacity = 2048;
    int level{};
    std::array<char, TextCapacity> text{};
};
class DiagnosticQueue {
    static constexpr size_t Capacity = 64;
    std::array<DiagnosticLine, Capacity> lines_{};
    size_t head_{}, count_{};
    std::mutex mutex_;
    std::atomic<uint32_t> dropped_{0};
public:
    void push(int level, const char* text) noexcept {
        try {
            std::unique_lock<std::mutex> lock(mutex_, std::try_to_lock);
            if(!lock.owns_lock() || count_==Capacity) { ++dropped_; return; }
            auto& line=lines_[(head_+count_)%Capacity];
            line.level=level;
            size_t length=0;
            if(text) for(; length+1<line.text.size() && text[length]; ++length)
                line.text[length]=(text[length]=='\r' || text[length]=='\n')?' ':text[length];
            line.text[length]='\0';
            ++count_;
        } catch(...) { ++dropped_; }
    }
    bool pop(DiagnosticLine& line) noexcept {
        try {
            std::unique_lock<std::mutex> lock(mutex_, std::try_to_lock);
            if(!lock.owns_lock() || !count_) return false;
            line=lines_[head_]; head_=(head_+1)%Capacity; --count_; return true;
        } catch(...) { return false; }
    }
    uint32_t takeDropped() noexcept { return dropped_.exchange(0); }
};

// This policy belongs to one helper lifetime. A failed new connection may
// change the default for FUTURE sessions once, but must not reset successful
// guests or remove the direct option while Steam relays are unavailable.
class RoutePolicy {
    bool relayOnly_{}, recoveryAttempted_{};
    std::vector<uint64_t> established_;
public:
    explicit RoutePolicy(bool relayOnly) :relayOnly_(relayOnly) {}
    bool relayOnly() const { return relayOnly_; }
    void connected(uint64_t peer) {
        if(std::find(established_.begin(),established_.end(),peer)==established_.end()) established_.push_back(peer);
    }
    void closed(uint64_t peer) {
        established_.erase(std::remove(established_.begin(),established_.end(),peer),established_.end());
    }
    template<class Configure> bool recover(uint64_t peer, bool rendezvousFailure, bool localRelayReady, bool peerRelayReady, Configure configure) {
        if(relayOnly_ || recoveryAttempted_ || !rendezvousFailure || !localRelayReady || !peerRelayReady ||
            std::find(established_.begin(),established_.end(),peer)!=established_.end()) return false;
        recoveryAttempted_=true;
        if(!configure()) return false;
        relayOnly_=true; return true;
    }
};

// Pure timing policy: refresh negative relay availability only while peers
// are trying to connect, at most once every 30 seconds for the helper lifetime.
class RelayRefreshPolicy {
    int64_t nextMilliseconds_{};
public:
    bool shouldRefresh(bool hasPeers, bool unavailable, int64_t nowMilliseconds) {
        if(!hasPeers || !unavailable || nowMilliseconds<nextMilliseconds_) return false;
        nextMilliseconds_=nowMilliseconds+30000; return true;
    }
};
}
