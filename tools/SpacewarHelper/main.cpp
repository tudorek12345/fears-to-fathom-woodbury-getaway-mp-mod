#include "core.hpp"
#include <windows.h>
#include <steam/steam_api.h>
// CSteamID return-by-value differs between MSVC and MinGW. The SDK's flat
// exports return uint64 and keep these calls ABI-compatible with Valve's DLL.
#include <steam/steam_api_flat.h>
#include <steam/isteamnetworkingmessages.h>
#include <steam/isteamnetworkingutils.h>
#include <atomic>
#include <chrono>
#include <iostream>
#include <thread>
#include <map>
#include <set>
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <io.h>
#if defined(__MINGW32__)
#include <pthread.h>
#endif

using namespace woodbury;
using Clock = std::chrono::steady_clock;

namespace {
DiagnosticQueue sdkDiagnostics;
void steamDiagnostic(ESteamNetworkingSocketsDebugOutputType level, const char* message) {
    sdkDiagnostics.push(static_cast<int>(level),message);
}
void drainSteamDiagnostics() {
    DiagnosticLine line;
    for(int i=0;i<16 && sdkDiagnostics.pop(line);++i)
        std::cerr<<"Steam SDK ["<<line.level<<"] "<<line.text.data()<<'\n';
    const auto dropped=sdkDiagnostics.takeDropped();
    if(dropped) std::cerr<<"Steam SDK diagnostics suppressed: "<<dropped<<" (bounded queue)\n";
}
Bytes readExact(HANDLE pipe, size_t count) {
    Bytes data(count); size_t offset=0;
    while(offset<count) {
        DWORD read=0;
        if(!ReadFile(pipe,data.data()+offset,static_cast<DWORD>(count-offset),&read,nullptr) || !read)
            throw std::runtime_error("IPC input closed");
        offset+=read;
    }
    return data;
}
void writeExact(HANDLE pipe, const Bytes& bytes) {
    size_t offset=0;
    while(offset<bytes.size()) {
        DWORD written=0;
        if(!WriteFile(pipe,bytes.data()+offset,static_cast<DWORD>(bytes.size()-offset),&written,nullptr) || !written)
            throw std::runtime_error("IPC output closed");
        offset+=written;
    }
}
HANDLE isolateIpcOutput() {
    // The Steam SDK may print native assertions to stdout. Preserve a private
    // copy of the parent's binary pipe, then send every normal stdout path to
    // stderr before Steam is initialized. One stray byte corrupts framing.
    HANDLE original=GetStdHandle(STD_OUTPUT_HANDLE);
    HANDLE diagnostics=GetStdHandle(STD_ERROR_HANDLE);
    if(!original || original==INVALID_HANDLE_VALUE || !diagnostics || diagnostics==INVALID_HANDLE_VALUE)
        throw std::runtime_error("Missing Spacewar IPC or diagnostic output pipe");
    HANDLE ipc=nullptr;
    if(!DuplicateHandle(GetCurrentProcess(),original,GetCurrentProcess(),&ipc,0,FALSE,DUPLICATE_SAME_ACCESS))
        throw std::runtime_error("Could not duplicate Spacewar IPC output pipe");
    std::cout.flush(); std::fflush(stdout);
    if(!SetStdHandle(STD_OUTPUT_HANDLE,diagnostics) || _dup2(_fileno(stderr),_fileno(stdout))!=0 ||
        !SetStdHandle(STD_OUTPUT_HANDLE,diagnostics)) {
        CloseHandle(ipc);
        throw std::runtime_error("Could not isolate Spacewar IPC output from native logs");
    }
    return ipc;
}
uint64_t parseId(const std::string& value) {
    if(value.empty() || value.size()>20) return 0;
    for(char c:value) if(c<'0'||c>'9') return 0;
    try { return std::stoull(value); } catch(...) { return 0; }
}
class Bridge {
    Queue& output_;
    std::string protocol_, mod_, fatal_;
    uint64_t allowOnly_, self_, lobby_{}, owner_{}, peer_{}, invitation_{};
    uint32_t request_{};
    bool hosting_{}, pending_{}, cancelled_{}, hasDeferred_{};
    Frame deferred_;
    struct PeerSend { SendBacklog queue; Clock::time_point congested{}, retry{}; };
    std::map<uint64_t, PeerSend> sends_;
    std::vector<uint64_t> peers_;
    std::set<uint64_t> blocked_;
    RoutePolicy routing_;
    RelayRefreshPolicy relayRefresh_;
    Clock::time_point deadline_, nextCheck_{}, nextNetworkCheck_{}, nextNetworkReport_{};
    std::string networkStatus_;
    std::string publishedRelay_;
    bool relayEmitted_{};
    int lastRelayAvailability_{}, lastRelayConfig_{}, lastRelayReachability_{};
    std::string lastRelayDetail_;
    CCallResult<Bridge,LobbyCreated_t> created_;
    CCallResult<Bridge,LobbyEnter_t> entered_;
    STEAM_CALLBACK(Bridge, OnJoinRequest, GameLobbyJoinRequested_t);
    STEAM_CALLBACK(Bridge, OnInvite, LobbyInvite_t);
    STEAM_CALLBACK(Bridge, OnMembers, LobbyChatUpdate_t);
    STEAM_CALLBACK(Bridge, OnLobbyData, LobbyDataUpdate_t);
    STEAM_CALLBACK(Bridge, OnSessionRequest, SteamNetworkingMessagesSessionRequest_t);
    STEAM_CALLBACK(Bridge, OnSessionFailed, SteamNetworkingMessagesSessionFailed_t);

    bool member(uint64_t id) const {
        if(!lobby_) return false;
        const CSteamID lobby(lobby_);
        for(int i=0;i<SteamMatchmaking()->GetNumLobbyMembers(lobby);++i)
            if(SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(SteamMatchmaking(),lobby.ConvertToUint64(),i)==id) return true;
        return false;
    }
    bool accepts(uint64_t id) const {
        return blocked_.count(id)==0 && eligible(id,self_,owner_,peer_,allowOnly_,hosting_,member(id));
    }
    void closePeer(uint64_t id) {
        sends_.erase(id);
        routing_.closed(id);
        if(id) { SteamNetworkingIdentity identity; identity.SetSteamID64(id); SteamNetworkingMessages()->CloseSessionWithUser(identity); }
    }
    void closePeer() {
        for(auto id:peers_) closePeer(id);
        sends_.clear(); peers_.clear();
        networkStatus_.clear(); nextNetworkReport_=Clock::time_point{};
        peer_=0;
    }
    void state() {
        Writer w; w.number(lobby_,8); w.number(owner_,8); w.number(peer_,8);
        w.number(peers_.size(),1); for(auto id:peers_) w.number(id,8);
        emit(Op::Lobby,request_,std::move(w.bytes));
    }
    SteamRelayNetworkStatus_t relayStatus(bool report, const char* context) {
        SteamRelayNetworkStatus_t relay{};
        SteamNetworkingUtils()->GetRelayNetworkStatus(&relay);
        relay.m_debugMsg[sizeof(relay.m_debugMsg)-1]='\0';
        if(report) std::cerr<<"Steam relay diagnostics: context="<<context
            <<" availability="<<static_cast<int>(relay.m_eAvail)
            <<" config="<<static_cast<int>(relay.m_eAvailNetworkConfig)
            <<" anyRelay="<<static_cast<int>(relay.m_eAvailAnyRelay)
            <<" measuring="<<relay.m_bPingMeasurementInProgress
            <<" detail="<<relay.m_debugMsg<<'\n';
        return relay;
    }
    void networkStatus() {
        if(Clock::now()<nextNetworkCheck_) return;
        nextNetworkCheck_=Clock::now()+std::chrono::seconds(1);
        std::string status;
        const bool report=Clock::now()>=nextNetworkReport_;
        if(report) nextNetworkReport_=Clock::now()+std::chrono::seconds(10);
        const auto relay=relayStatus(report,"poll");
        size_t detailLength=0;
        while(detailLength<sizeof(relay.m_debugMsg) && relay.m_debugMsg[detailLength]) ++detailLength;
        const std::string relayDetail(relay.m_debugMsg,detailLength);
        if(!relayEmitted_ || lastRelayAvailability_!=relay.m_eAvail || lastRelayConfig_!=relay.m_eAvailNetworkConfig ||
            lastRelayReachability_!=relay.m_eAvailAnyRelay || lastRelayDetail_!=relayDetail) {
            Writer w; w.number(static_cast<uint32_t>(relay.m_eAvail),4);
            w.number(static_cast<uint32_t>(relay.m_eAvailNetworkConfig),4);
            w.number(static_cast<uint32_t>(relay.m_eAvailAnyRelay),4); w.text(relayDetail);
            emit(Op::RelayStatus,0,std::move(w.bytes));
            relayEmitted_=true; lastRelayAvailability_=relay.m_eAvail;
            lastRelayConfig_=relay.m_eAvailNetworkConfig; lastRelayReachability_=relay.m_eAvailAnyRelay;
            lastRelayDetail_=relayDetail;
        }
        const std::string relayAvailability=relay.m_eAvail==k_ESteamNetworkingAvailability_Current?"ready":(relay.m_eAvail<0?"unavailable":"checking");
        if(lobby_ && publishedRelay_!=relayAvailability) {
            SteamMatchmaking()->SetLobbyMemberData(CSteamID(lobby_),"woodbury_relay",relayAvailability.c_str());
            publishedRelay_=relayAvailability;
        }
        bool primaryConnected=false, pendingPeer=false;
        for(auto id:peers_) {
            SteamNetworkingIdentity identity; identity.SetSteamID64(id);
            SteamNetConnectionInfo_t info{}; SteamNetConnectionRealTimeStatus_t quick{};
            auto connection=SteamNetworkingMessages()->GetSessionConnectionInfo(identity,&info,&quick);
            if(connection==k_ESteamNetworkingConnectionState_Connected) routing_.connected(id);
            else pendingPeer=true;
            if(connection==k_ESteamNetworkingConnectionState_Connected && quick.m_nPing>=0 && quick.m_nPing<=10000) {
                Writer metric; metric.number(id,8); metric.number(static_cast<uint32_t>(quick.m_nPing),4);
                metric.number((info.m_nFlags & k_nSteamNetworkConnectionInfoFlags_Relayed)?1:0,1);
                metric.number(static_cast<uint32_t>(std::max(0,quick.m_cbPendingReliable)),4);
                metric.number(static_cast<uint32_t>(std::max(0,quick.m_cbSentUnackedReliable)),4);
                auto queue=sends_.find(id); metric.number(queue==sends_.end()?0:queue->second.queue.bytes(),4);
                emit(Op::PeerMetrics,request_,std::move(metric.bytes),false);
            }
            if(report) {
                auto queued=sends_.find(id);
                std::cerr<<"Peer network: peer="<<id<<" state="<<static_cast<int>(connection)
                    <<" route="<<((info.m_nFlags & k_nSteamNetworkConnectionInfoFlags_Relayed)?"relay":"direct/unknown")
                    <<" pingMs="<<quick.m_nPing<<" pendingReliable="<<quick.m_cbPendingReliable
                    <<" unackedReliable="<<quick.m_cbSentUnackedReliable
                    <<" helperQueued="<<(queued==sends_.end()?0:queued->second.queue.bytes())<<'\n';
            }
            if(id!=peer_) continue;
            primaryConnected=connection==k_ESteamNetworkingConnectionState_Connected;
            if(connection==k_ESteamNetworkingConnectionState_Connected)
                status=(info.m_nFlags & k_nSteamNetworkConnectionInfoFlags_Relayed) ? "Steam relay connected" : "Steam direct connection established";
            else if(connection==k_ESteamNetworkingConnectionState_FindingRoute) status="Finding a Steam route to the other player";
            else if(connection==k_ESteamNetworkingConnectionState_Connecting) status="Establishing Steam peer connection";
            else status="Waiting for Steam peer handshake";
        }
        const auto nowMilliseconds=std::chrono::duration_cast<std::chrono::milliseconds>(Clock::now().time_since_epoch()).count();
        if(relayRefresh_.shouldRefresh(pendingPeer,relay.m_eAvail<0,nowMilliseconds)) {
            SteamNetworkingUtils()->InitRelayNetworkAccess();
            std::cerr<<"Refreshing unavailable Steam relay access; existing peer sessions preserved\n";
        }
        if(peer_) {
            if(status.empty()) status="Waiting for Steam peer handshake";
        } else {
            status=relay.m_eAvail==k_ESteamNetworkingAvailability_Current ? "Steam relay network ready" :
                (relay.m_eAvail<0?"Steam relay unavailable; check local Steam network access":"Preparing Steam relay access");
        }
        if(peer_ && relay.m_eAvail<0 && !primaryConnected) status+=" | Steam relay unavailable; check local Steam network access";
        status+=routing_.relayOnly()?" | New connections: relay only":" | Auto route";
        if(status==networkStatus_) return;
        networkStatus_=status; Writer w; w.text(status); emit(Op::Status,request_,std::move(w.bytes));
        std::cerr<<status<<'\n';
    }
    void connectionLost(uint64_t id, const char* context, int result, const SteamNetConnectionInfo_t* info=nullptr) {
        std::cerr<<"Steam peer failure: "<<context<<" result="<<result<<" peer="<<id;
        if(info) std::cerr<<" endReason="<<info->m_eEndReason<<" detail="<<info->m_szEndDebug;
        std::cerr<<'\n';
        const bool rendezvous=info && info->m_eEndReason==k_ESteamNetConnectionEnd_Misc_P2P_Rendezvous;
        const auto relay=relayStatus(true,"peer failure");
        const char* peerRelay=lobby_?SteamMatchmaking()->GetLobbyMemberData(CSteamID(lobby_),CSteamID(id),"woodbury_relay"):"";
        const bool peerRelayReady=std::strcmp(peerRelay,"ready")==0;
        std::cerr<<"Steam route recovery eligibility: peer="<<id<<" peerRelay="<<(peerRelay[0]?peerRelay:"unknown")<<'\n';
        const bool recovering=routing_.recover(id,rendezvous,relay.m_eAvail==k_ESteamNetworkingAvailability_Current,peerRelayReady,[] {
            // Valve locks ICE_Enable into each initialized connection. This
            // changes only future sessions; do not close any other guest.
            const bool configured=SteamNetworkingUtils()->SetGlobalConfigValueInt32(
                k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,k_nSteamNetworkingConfig_P2P_Transport_ICE_Enable_Disable);
            if(!configured) std::cerr<<"Steam rejected automatic relay-only recovery configuration; retaining automatic routing\n";
            return configured;
        });
        if(recovering) std::cerr<<"Steam route recovery: future sessions use relay only after initial 5008; established guests preserved. Relay/VPN reachability is still required.\n";
        std::string failure;
        if(!rendezvous) failure=std::string("Steam peer connection lost (")+context+", code "+std::to_string(result)+").";
        else if(recovering) failure="Steam packet route failed (5008). The next connection will try relay only; reachable Steam relays are still required.";
        else if(relay.m_eAvail<0) failure="Steam packet route failed (5008): local Steam relay access is unavailable. Check this PC's Steam network access.";
        else if(std::strcmp(peerRelay,"unavailable")==0) failure="Steam packet route failed (5008): the other player's helper reports Steam relay access unavailable. Check their Steam network access.";
        else failure="Steam could not negotiate a packet route (5008). Check Steam network access on both PCs; lobby membership alone is not a connection.";
        error(request_,failure);
        Writer w; w.number(id,8); emit(Op::ConnectionLost,request_,std::move(w.bytes)); closePeer(id);
    }
    bool validate() {
        if(!lobby_) return false;
        CSteamID lobby(lobby_);
        auto host=parseId(SteamMatchmaking()->GetLobbyData(lobby,"host"));
        auto owner=SteamAPI_ISteamMatchmaking_GetLobbyOwner(SteamMatchmaking(),lobby.ConvertToUint64());
        return validLobby(SteamMatchmaking()->GetLobbyData(lobby,"woodbury"), SteamMatchmaking()->GetLobbyData(lobby,"protocol"),
            SteamMatchmaking()->GetLobbyData(lobby,"mod"),protocol_,mod_,host,owner,SteamMatchmaking()->GetNumLobbyMembers(lobby)) &&
            host==owner_ && (hosting_ ? host==self_ : host!=self_) && member(self_);
    }
    void checkMembers() {
        if(!lobby_) return;
        if(!hosting_ && (!member(owner_) || SteamAPI_ISteamMatchmaking_GetLobbyOwner(SteamMatchmaking(),lobby_)!=owner_)) {
            error(request_,"Spacewar host left; host migration is not supported",true); leave(); return;
        }
        if(!validate()) { error(request_,"Lobby metadata, membership or original host changed",true); leave(); return; }
        std::vector<uint64_t> selected;
        for(int i=0;i<SteamMatchmaking()->GetNumLobbyMembers(CSteamID(lobby_));++i) {
            auto id=SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(SteamMatchmaking(),lobby_,i);
            if(blocked_.count(id)==0 && id!=self_ && (hosting_ ? (!allowOnly_ || id==allowOnly_) : id==owner_)) selected.push_back(id);
        }
        std::sort(selected.begin(),selected.end());
        if(peers_!=selected) {
            for(auto id:peers_) if(std::find(selected.begin(),selected.end(),id)==selected.end()) {
                closePeer(id); Writer w; w.number(id,8); emit(Op::PeerLeft,request_,std::move(w.bytes));
            }
            peers_=std::move(selected); peer_=peers_.empty()?0:peers_.front(); state();
        }
    }
    void onCreated(LobbyCreated_t* result, bool failure) {
        pending_=false;
        if(cancelled_) {
            if(!failure && result->m_eResult==k_EResultOK) SteamMatchmaking()->LeaveLobby(CSteamID(result->m_ulSteamIDLobby));
            cancelled_=false; return;
        }
        if(failure || result->m_eResult!=k_EResultOK) { error(request_,"Steam lobby creation failed (result "+std::to_string(static_cast<int>(result->m_eResult))+")",true); return; }
        lobby_=result->m_ulSteamIDLobby; owner_=self_;
        CSteamID lobby(lobby_);
        bool ok=SteamMatchmaking()->SetLobbyData(lobby,"woodbury","tudorek12345.woodbury.spacewar.v1");
        ok=SteamMatchmaking()->SetLobbyData(lobby,"protocol",protocol_.c_str()) && ok;
        ok=SteamMatchmaking()->SetLobbyData(lobby,"mod",mod_.c_str()) && ok;
        ok=SteamMatchmaking()->SetLobbyData(lobby,"host",std::to_string(self_).c_str()) && ok;
        if(!ok) { error(request_,"Could not publish lobby metadata",true); leave(); return; }
        state();
    }
    void onEntered(LobbyEnter_t* result, bool failure) {
        pending_=false;
        if(cancelled_) {
            if(!failure && result->m_EChatRoomEnterResponse==k_EChatRoomEnterResponseSuccess) SteamMatchmaking()->LeaveLobby(CSteamID(result->m_ulSteamIDLobby));
            cancelled_=false; return;
        }
        if(failure || result->m_EChatRoomEnterResponse!=k_EChatRoomEnterResponseSuccess) {
            error(request_,"Lobby unavailable, full, or access denied (response "+std::to_string(result->m_EChatRoomEnterResponse)+")",true); return;
        }
        lobby_=result->m_ulSteamIDLobby;
        owner_=parseId(SteamMatchmaking()->GetLobbyData(CSteamID(lobby_),"host"));
        if(!validate()) { error(request_,"Incompatible lobby, mod, protocol or host",true); leave(); return; }
        checkMembers();
    }
public:
    Bridge(Queue& output, uint16_t protocol, std::string mod, uint64_t allowOnly, bool relayOnly)
        :output_(output),protocol_(std::to_string(protocol)),mod_(std::move(mod)),allowOnly_(allowOnly),self_(SteamAPI_ISteamUser_GetSteamID(SteamUser())),routing_(relayOnly) {}
    ~Bridge() { leave(); }
    void emit(Op op, uint32_t request, Bytes data={}, bool reliable=true) {
        if(!fatal_.empty()) return;
        // Never unwind a queue-overflow exception across a Steam callback ABI boundary.
        try { output_.push(Frame{op,request,reliable,std::move(data)}); }
        catch(const std::exception& e) { fatal_=e.what(); }
    }
    void error(uint32_t request,const std::string& text,bool fatal=false) {
        Writer w; w.number(fatal?1:0,1); w.text(text); emit(Op::Error,request,std::move(w.bytes));
    }
    void leave() {
        // Keep pending call results alive long enough to leave a lobby created after cancellation.
        if(pending_) cancelled_=true;
        closePeer(); blocked_.clear(); invitation_=0;
        publishedRelay_.clear();
        if(lobby_) SteamMatchmaking()->LeaveLobby(CSteamID(lobby_));
        lobby_=owner_=0; request_=0;
    }
    bool command(const Frame& frame) {
        Reader r{frame.data};
        switch(frame.op) {
        case Op::Host:
            r.end();
            if(pending_) { cancelled_=true; deferred_=frame; hasDeferred_=true; std::cerr<<"Deferred Host until canceled lobby request completes\n"; break; }
            leave(); request_=frame.request; hosting_=true; pending_=true; deadline_=Clock::now()+std::chrono::seconds(25);
            created_.Set(SteamMatchmaking()->CreateLobby(k_ELobbyTypeFriendsOnly,2),this,&Bridge::onCreated); break;
        case Op::Join: {
            auto id=r.number(8); r.end();
            if(!CSteamID(id).IsLobby()) { error(frame.request,"Invalid lobby SteamID",true); break; }
            if(pending_) { cancelled_=true; deferred_=frame; hasDeferred_=true; std::cerr<<"Deferred Join until canceled lobby request completes\n"; break; }
            leave(); request_=frame.request; hosting_=false; pending_=true; deadline_=Clock::now()+std::chrono::seconds(25);
            entered_.Set(SteamMatchmaking()->JoinLobby(CSteamID(id)),this,&Bridge::onEntered); break;
        }
        case Op::Leave: r.end(); hasDeferred_=false; leave(); emit(Op::Ack,frame.request); break;
        case Op::ResetPeer: r.end(); closePeer(); checkMembers(); emit(Op::Ack,frame.request); break;
        case Op::BlockPeer: {
            auto id=r.number(8), block=r.number(1); r.end();
            if(!hosting_ || !id || id==self_ || block>1) { error(frame.request,"Invalid moderation target"); break; }
            if(block) { blocked_.insert(id); closePeer(id); } else blocked_.erase(id);
            checkMembers(); emit(Op::Ack,frame.request); break;
        }
        case Op::Friends:
            r.end();
            for(int i=0;i<SteamFriends()->GetFriendCount(k_EFriendFlagImmediate) && i<1024;++i) {
                CSteamID id(SteamAPI_ISteamFriends_GetFriendByIndex(SteamFriends(),i,k_EFriendFlagImmediate));
                Writer w; w.number(id.ConvertToUint64(),8); w.text(SteamFriends()->GetFriendPersonaName(id)); emit(Op::Friend,frame.request,std::move(w.bytes));
            }
            emit(Op::FriendsEnd,frame.request); break;
        case Op::Invite: {
            auto id=r.number(8); r.end();
            if(!hosting_ || !lobby_ || !validate() || !SteamMatchmaking()->InviteUserToLobby(CSteamID(lobby_),CSteamID(id)))
                error(frame.request,"Could not send Steam lobby invitation");
            else emit(Op::Ack,frame.request);
            break;
        }
        case Op::Send: {
            auto id=r.number(8); size_t size=r.bytes.size()-r.pos;
            if(size==0 || size>MaxPacket) throw std::runtime_error("Invalid outgoing packet size");
            if(!accepts(id)) { if(frame.reliable) error(frame.request,"Peer is no longer eligible"); break; }
            sends_[id].queue.push(frame);
            break;
        }
        case Op::Shutdown: r.end(); hasDeferred_=false; leave(); emit(Op::Ack,frame.request); return false;
        default: throw std::runtime_error("Unknown IPC command");
        }
        return true;
    }
    void tick() {
        if(!fatal_.empty()) throw std::runtime_error(fatal_);
        if(!SteamUser()->BLoggedOn()) throw std::runtime_error("Steam disconnected or signed out");
        if(!pending_ && hasDeferred_) {
            Frame next=std::move(deferred_); hasDeferred_=false; command(next);
        }
        if(pending_ && Clock::now()>deadline_) { error(request_,"Steam lobby operation timed out",true); leave(); }
        if(Clock::now()>=nextCheck_) { nextCheck_=Clock::now()+std::chrono::milliseconds(250); checkMembers(); }
        networkStatus();
        // Separate backlogs keep a slow/downloading guest from blocking the other.
        struct FailedPeer { uint64_t id; EResult reason; };
        std::vector<FailedPeer> failed;
        for(auto& entry:sends_) {
            auto id=entry.first; auto& pending=entry.second; EResult failedSend=k_EResultOK;
            if(Clock::now()<pending.retry || pending.queue.empty()) continue;
            auto result=pending.queue.drain([&](const Frame& frame) {
                Reader r{frame.data}; auto id=r.number(8);
                if(!accepts(id)) { failedSend=k_EResultAccessDenied; return SendResult::Failed; }
                SteamNetworkingIdentity identity; identity.SetSteamID64(id);
                int flags=frame.reliable ? k_nSteamNetworkingSend_Reliable : k_nSteamNetworkingSend_UnreliableNoDelay;
                EResult status=SteamNetworkingMessages()->SendMessageToUser(identity,r.bytes.data()+r.pos,
                    static_cast<uint32>(r.bytes.size()-r.pos),flags,0);
                if(status==k_EResultOK) return SendResult::Accepted;
                if(status==k_EResultLimitExceeded) return SendResult::Congested;
                if(status==k_EResultNoConnection || frame.reliable) { failedSend=status; return SendResult::Failed; }
                return SendResult::Accepted;
            });
            if(result==SendResult::Congested) {
                auto now=Clock::now();
                if(pending.congested==Clock::time_point{}) pending.congested=now;
                pending.retry=now+std::chrono::milliseconds(20);
                if(now-pending.congested>std::chrono::seconds(15)) {
                    error(request_,"Steam send queue remained congested for 15 seconds; reconnecting peer");
                    failed.push_back({id,k_EResultLimitExceeded});
                }
            } else {
                pending.congested=Clock::time_point{};
                if(result==SendResult::Failed) {
                    std::cerr<<"Send failed: peer="<<id<<" code="<<static_cast<int>(failedSend)<<'\n';
                    failed.push_back({id,failedSend});
                }
            }
        }
        for(const auto& failure:failed) {
            SteamNetworkingIdentity identity; identity.SetSteamID64(failure.id);
            SteamNetConnectionInfo_t info{};
            auto state=SteamNetworkingMessages()->GetSessionConnectionInfo(identity,&info,nullptr);
            std::cerr<<"Send failure route state="<<static_cast<int>(state)<<" code="<<static_cast<int>(failure.reason)<<'\n';
            connectionLost(failure.id,"send",static_cast<int>(failure.reason),&info);
        }
        SteamNetworkingMessage_t* messages[64];
        int count=SteamNetworkingMessages()->ReceiveMessagesOnChannel(0,messages,64);
        for(int i=0;i<count;++i) {
            // Release every native message even if validation or queue allocation throws.
            auto* msg=messages[i];
            try {
                auto id=msg->m_identityPeer.GetSteamID64();
                if(accepts(id) && msg->m_cbSize>0 && static_cast<size_t>(msg->m_cbSize)<=MaxPacket) {
                    Writer w; w.number(id,8);
                    const auto* bytes=static_cast<const uint8_t*>(msg->m_pData);
                    w.bytes.insert(w.bytes.end(),bytes,bytes+msg->m_cbSize);
                    emit(Op::Packet,request_,std::move(w.bytes),(msg->m_nFlags & k_nSteamNetworkingSend_Reliable)!=0);
                }
                msg->Release();
            } catch(...) { for(int j=i;j<count;++j) messages[j]->Release(); throw; }
        }
    }
};
void Bridge::OnJoinRequest(GameLobbyJoinRequested_t* event) {
    uint64_t id=event->m_steamIDLobby.ConvertToUint64();
    if(event->m_steamIDLobby.IsLobby() && id!=lobby_ && id!=invitation_) { invitation_=id; Writer w; w.number(id,8); emit(Op::Invitation,0,std::move(w.bytes)); }
}
void Bridge::OnInvite(LobbyInvite_t* event) {
    if(event->m_ulGameID!=480 || !CSteamID(event->m_ulSteamIDLobby).IsLobby() || event->m_ulSteamIDLobby==invitation_ || event->m_ulSteamIDLobby==lobby_) return;
    invitation_=event->m_ulSteamIDLobby; Writer w; w.number(invitation_,8); emit(Op::Invitation,0,std::move(w.bytes));
}
void Bridge::OnMembers(LobbyChatUpdate_t* event) { if(event->m_ulSteamIDLobby==lobby_) checkMembers(); }
void Bridge::OnLobbyData(LobbyDataUpdate_t* event) { if(event->m_ulSteamIDLobby==lobby_) checkMembers(); }
void Bridge::OnSessionRequest(SteamNetworkingMessagesSessionRequest_t* event) {
    const auto id=event->m_identityRemote.GetSteamID64();
    const bool allowed=accepts(id);
    const bool accepted=allowed && SteamNetworkingMessages()->AcceptSessionWithUser(event->m_identityRemote);
    std::cerr<<"Steam session request: peer="<<id<<" lobby="<<lobby_
        <<" eligible="<<allowed<<" accepted="<<accepted<<'\n';
    if(!allowed) SteamNetworkingMessages()->CloseSessionWithUser(event->m_identityRemote);
}
void Bridge::OnSessionFailed(SteamNetworkingMessagesSessionFailed_t* event) {
    auto id=event->m_info.m_identityRemote.GetSteamID64();
    if(!accepts(id)) return;
    SteamNetConnectionInfo_t current{};
    auto state=SteamNetworkingMessages()->GetSessionConnectionInfo(event->m_info.m_identityRemote,&current,nullptr);
    // Steam callbacks are asynchronous. A queued failure from the old session
    // must not close a replacement that is already connecting or connected.
    if(state!=k_ESteamNetworkingConnectionState_ProblemDetectedLocally && state!=k_ESteamNetworkingConnectionState_ClosedByPeer) {
        std::cerr<<"Ignoring stale Steam failure callback; current state="<<static_cast<int>(state)<<'\n'; return;
    }
    connectionLost(id,"session",current.m_eEndReason,&current);
}
}

int main(int argc,char** argv) {
    if(argc==2 && std::string(argv[1])=="--test-stdout-isolation") {
        HANDLE ipc=nullptr;
        try {
            ipc=isolateIpcOutput();
            std::cout<<"cpp stdout diagnostic\n"<<std::flush;
            std::printf("crt stdout diagnostic\n"); std::fflush(stdout);
            const char* native="win32 stdout diagnostic\n"; DWORD written=0;
            if(!WriteFile(GetStdHandle(STD_OUTPUT_HANDLE),native,static_cast<DWORD>(std::strlen(native)),&written,nullptr) || written!=std::strlen(native))
                throw std::runtime_error("Could not write native diagnostic");
            writeExact(ipc,encode(Frame{Op::Ack,0x12345678,true,{}}));
            CloseHandle(ipc); return 0;
        } catch(const std::exception& e) {
            std::cerr<<e.what()<<'\n'; if(ipc) CloseHandle(ipc); return 1;
        }
    }
    // A private process handles Steam. No global environment or game files are changed.
    if((argc!=3 && argc!=4) || std::string(argv[1])!="--parent" || (argc==4 && std::string(argv[3])!="--relay-only")) {
        std::cerr<<"Usage: Woodbury.Spacewar --parent <pid> [--relay-only]\n"; return 2;
    }
    const bool relayOnly=argc==4;
    auto parentId=parseId(argv[2]);
    if(!parentId || parentId>MAXDWORD) return 2;
    HANDLE parent=OpenProcess(SYNCHRONIZE,FALSE,static_cast<DWORD>(parentId));
    if(!parent) return 2;
    SetEnvironmentVariableA("SteamAppId","480"); SetEnvironmentVariableA("SteamGameId","480");
    Queue input,output; std::atomic<bool> stop{false};
    HANDLE in=GetStdHandle(STD_INPUT_HANDLE),out=nullptr;
    try { out=isolateIpcOutput(); }
    catch(const std::exception& e) { std::cerr<<e.what()<<'\n'; CloseHandle(parent); return 1; }
    auto reader=std::thread([&] {
        try { while(!stop) { auto prefix=readExact(in,4); auto size=frameLength(prefix); input.push(decode(readExact(in,size))); } }
        catch(const std::exception& e) { if(!stop) std::cerr<<e.what()<<'\n'; stop=true; }
    });
    auto writer=std::thread([&] {
        try { while(!stop) { Frame f; if(output.pop(f)) writeExact(out,encode(f)); else std::this_thread::sleep_for(std::chrono::milliseconds(2)); } }
        catch(const std::exception& e) { if(!stop) std::cerr<<e.what()<<'\n'; stop=true; }
    });
    bool initialized=false; int result=0;
    try {
        Frame init; bool gotInit=false; auto deadline=Clock::now()+std::chrono::seconds(10);
        while(!stop && Clock::now()<deadline && WaitForSingleObject(parent,0)==WAIT_TIMEOUT) {
            if(input.pop(init)) { gotInit=true; break; }
            std::this_thread::sleep_for(std::chrono::milliseconds(5));
        }
        if(!gotInit || init.op!=Op::Init || !init.reliable) throw std::runtime_error("Expected IPC initialization");
        Reader r{init.data}; auto protocol=static_cast<uint16_t>(r.number(2)); auto mod=r.text(); auto allowOnly=r.number(8); r.end();
        if(!protocol || mod.empty()) throw std::runtime_error("Missing mod or protocol version");
        SteamErrMsg initError{};
        const auto initResult=SteamAPI_InitEx(&initError);
        if(initResult!=k_ESteamAPIInitResult_OK)
            throw std::runtime_error(std::string("Steam initialization failed (")+std::to_string(static_cast<int>(initResult))+"): "+
                (initError[0] ? initError : "Start Steam under the same Windows account and verify App ID 480 availability"));
        initialized=true;
        if(SteamUtils()->GetAppID()!=480 || !SteamUser()->BLoggedOn() || !CSteamID(SteamAPI_ISteamUser_GetSteamID(SteamUser())).IsValid())
            throw std::runtime_error("Wrong Steam App ID or unavailable Steam user");
        if(!SteamNetworkingMessages() || !SteamNetworkingUtils()) throw std::runtime_error("Steam networking interface unavailable");
        SteamNetworkingUtils()->SetDebugOutputFunction(k_ESteamNetworkingSocketsDebugOutputType_Msg,steamDiagnostic);
        // Set before any Messages sessions are created. Keep established-session
        // failure detection unchanged; only initial direct/relay negotiation gets
        // more time on slow or VPN-mediated paths.
        if(!SteamNetworkingUtils()->SetGlobalConfigValueInt32(k_ESteamNetworkingConfig_TimeoutInitial,45000))
            throw std::runtime_error("Could not configure Steam initial connection timeout");
        // Automatic mode inherits Valve's defaults, including user privacy
        // preferences. Explicit relay-only disables ICE before any sessions.
        if(relayOnly && !SteamNetworkingUtils()->SetGlobalConfigValueInt32(
            k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,k_nSteamNetworkingConfig_P2P_Transport_ICE_Enable_Disable))
            throw std::runtime_error("Could not configure Steam relay-only routing");
        std::cerr<<"Steam initial route timeout: 45000 ms; mode="<<(relayOnly?"relay only":"automatic (Steam defaults, direct and relay)")<<'\n';
        SteamNetworkingUtils()->InitRelayNetworkAccess();
        Bridge bridge(output,protocol,mod,allowOnly,relayOnly);
        Writer ready; ready.number(480,4); ready.number(SteamAPI_ISteamUser_GetSteamID(SteamUser()),8);
        const std::string persona=SteamFriends()->GetPersonaName();
        if(persona.size()>512) throw std::runtime_error("Steam persona name exceeds 512 bytes");
        ready.text(persona);
        std::cerr<<"Initialized App ID 480, SteamID "<<SteamAPI_ISteamUser_GetSteamID(SteamUser())<<", IPC "<<WireVersion<<'\n';
        bridge.emit(Op::Ready,init.request,std::move(ready.bytes));
        while(!stop && WaitForSingleObject(parent,0)==WAIT_TIMEOUT) {
            SteamAPI_RunCallbacks();
            drainSteamDiagnostics();
            Frame f; bool running=true;
            for(int i=0;i<128 && input.pop(f);++i) if(!bridge.command(f)) { running=false; break; }
            if(!running) break;
            bridge.tick(); std::this_thread::sleep_for(std::chrono::milliseconds(5));
        }
    } catch(const std::exception& e) {
        drainSteamDiagnostics();
        std::cerr<<e.what()<<'\n'; result=1;
        try { Writer w; w.number(1,1); w.text(e.what()); output.push(Frame{Op::Error,0,true,std::move(w.bytes)}); } catch(...) {}
        std::this_thread::sleep_for(std::chrono::milliseconds(50));
    }
    if(initialized) SteamAPI_Shutdown();
    drainSteamDiagnostics();
    stop=true;
    // A worker can pass its stop check immediately before entering ReadFile/WriteFile.
    // Repeat cancellation until it exits so that single-cancel race cannot strand the helper.
    auto finish=[](std::thread& worker) {
        auto deadline=Clock::now()+std::chrono::seconds(2);
#if defined(__MINGW32__)
        HANDLE handle=pthread_gethandle(worker.native_handle());
#else
        HANDLE handle=worker.native_handle();
#endif
        while(WaitForSingleObject(handle,5)!=WAIT_OBJECT_0) {
            CancelSynchronousIo(handle);
            if(Clock::now()>deadline) return false;
        }
        worker.join(); return true;
    };
    if(!finish(reader) || !finish(writer)) ExitProcess(static_cast<UINT>(result));
    CloseHandle(out);
    CloseHandle(parent);
    return result;
}
