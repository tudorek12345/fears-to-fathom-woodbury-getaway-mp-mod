#include "core.hpp"
#include <functional>
#include <iostream>
#include <thread>
#include <cstring>
using namespace woodbury;
static int count=0;
static void check(bool condition,const char* name) { if(!condition) throw std::runtime_error(name); ++count; }
static void rejects(const std::function<void()>& action,const char* name) {
    bool rejected=false; try { action(); } catch(const std::exception&) { rejected=true; } check(rejected,name);
}
int main() {
    try {
        Frame original{Op::Send,0x12345678,true,{1,2,3}};
        auto bytes=encode(original);
        check(bytes==Bytes({12,0,0,0,6,0,7,0,0x78,0x56,0x34,0x12,1,1,2,3}),"canonical C#/native wire vector");
        auto frame=decode(Bytes(bytes.begin()+4,bytes.end()));
        check(frame.op==original.op && frame.request==original.request && frame.data==original.data && frame.reliable,"wire roundtrip");
        rejects([] { frameLength({0xff,0xff,0xff,0x7f}); },"oversized prefix rejected before allocation");
        rejects([] { frameLength({8,0,0,0}); },"undersized frame rejected");
        rejects([] { frameLength({9,0}); },"truncated prefix rejected");
        rejects([] { decode({1,0,7,0,0,0,0,0,1}); },"version mismatch rejected");
        rejects([] { decode({5,0,7,0,0,0,0,0,1}); },"previous helper wire version rejected");
        rejects([] { decode({5,0,7,0,0,0,0,0,2}); },"invalid flags rejected");
        Writer relayPayload; relayPayload.number(static_cast<uint32_t>(-100),4);
        relayPayload.number(100,4); relayPayload.number(static_cast<uint32_t>(-10),4); relayPayload.text("No relay reachable");
        auto relayFrame=encode(Frame{Op::RelayStatus,0,true,relayPayload.bytes});
        auto relayDecoded=decode(Bytes(relayFrame.begin()+4,relayFrame.end()));
        Reader relayReader{relayDecoded.data};
        check(relayDecoded.op==Op::RelayStatus && relayDecoded.request==0 &&
            relayReader.number(4)==static_cast<uint32_t>(-100) && relayReader.number(4)==100 &&
            relayReader.number(4)==static_cast<uint32_t>(-10) && relayReader.text()=="No relay reachable",
            "relay status preserves signed availability values and explanation");
        relayReader.end();
        rejects([] { Writer w; w.text(std::string(4097,'a')); },"oversized strings rejected");
        rejects([] { Bytes b{8,0,0,0,1}; Reader r{b}; r.text(); },"truncated string rejected");
        Queue queue;
        Frame bulk{Op::Packet,1,false,Bytes(MaxPacket)};
        for(int i=0;i<31;++i) check(queue.push(bulk),"unreliable queue initially accepts packets");
        check(!queue.push(bulk),"unreliable overflow dropped");
        bulk.reliable=true; check(queue.push(bulk),"reliable data evicts unreliable data");
        Queue reliable;
        for(int i=0;i<31;++i) reliable.push(bulk);
        rejects([&] { reliable.push(bulk); },"reliable overflow fails explicitly");
        SendBacklog backlog;
        Frame first{Op::Send,10,true,{1}}, second{Op::Send,11,true,{2}};
        backlog.push(first); backlog.push(second);
        size_t queued=backlog.bytes(); int attempts=0;
        check(backlog.drain([&](const Frame& f) { ++attempts; check(f.request==10,"congestion retains the oldest reliable packet"); return SendResult::Congested; })==SendResult::Congested,
            "Steam limit exceeded is retryable");
        check(attempts==1 && backlog.bytes()==queued,"congestion neither drops nor reorders reliable sends");
        std::vector<uint32_t> delivered;
        backlog.drain([&](const Frame& f) { delivered.push_back(f.request); return SendResult::Accepted; });
        check(delivered==std::vector<uint32_t>({10,11}) && backlog.empty(),"retry delivers each reliable packet once in order");
        backlog.push(Frame{Op::Send,12,false,{3}}); backlog.push(second);
        attempts=0;
        backlog.drain([&](const Frame& f) { ++attempts; return f.reliable?SendResult::Accepted:SendResult::Congested; });
        check(attempts==2 && backlog.empty(),"congested unreliable update cannot block reliable data");
        backlog.push(first);
        backlog.push(Frame{Op::Send,12,false,{3}});
        backlog.push(second);
        backlog.push(Frame{Op::Send,13,false,{4}});
        backlog.push(Frame{Op::Send,14,false,{5}});
        delivered.clear();
        check(backlog.drain([&](const Frame& f) {
            delivered.push_back(f.request);
            return f.reliable?SendResult::Congested:SendResult::Accepted;
        },3)==SendResult::Congested && delivered==std::vector<uint32_t>({10,13,14}),
            "a blocked reliable send hid current unreliable poses behind stale ones");
        delivered.clear();
        backlog.drain([&](const Frame& f) { delivered.push_back(f.request); return SendResult::Accepted; });
        check(delivered==std::vector<uint32_t>({10,11}) && backlog.empty(),
            "transient bypass changed reliable ordering or retained stale poses");
        backlog.push(first); backlog.clear();
        check(backlog.empty() && backlog.bytes()==0,"peer reset removes pending old-session sends");
        for(int i=0;i<31;++i) backlog.push(bulk);
        rejects([&] { backlog.push(bulk); },"Steam reliable outbox remains bounded to eight MiB");
        const std::string magic="tudorek12345.woodbury.spacewar.v1";
        check(validLobby(magic,"27","0.1.8","27","0.1.8",100,100,2),"matching lobby accepted");
        check(!validLobby("other","27","0.1.8","27","0.1.8",100,100,2),"unrelated 480 lobby rejected");
        check(!validLobby(magic,"26","0.1.8","27","0.1.8",100,100,2),"protocol mismatch rejected");
        check(!validLobby(magic,"27","other","27","0.1.8",100,100,2),"mod mismatch rejected");
        check(!validLobby(magic,"27","0.1.8","27","0.1.8",100,101,1),"owner migration rejected");
        check(!validLobby(magic,"27","0.1.8","27","0.1.8",100,100,3),"third lobby member rejected");
        check(!validLobby(magic,"27","0.1.8","27","0.1.8",100,100,4),"four lobby members rejected");
        check(!validLobby(magic,"27","0.1.8","27","0.1.8",100,100,5),"fifth lobby member rejected");
        check(eligible(201,100,100,200,0,true,true),"host accepts second authenticated guest");
        check(eligible(200,100,100,200,200,true,true),"host accepts selected allowlisted member");
        check(!eligible(201,100,100,200,200,true,true),"third party rejected");
        check(!eligible(200,100,100,200,201,true,true),"allowlist mismatch rejected");
        check(!eligible(200,100,100,200,0,true,false),"departed member rejected");
        check(eligible(100,200,100,100,0,false,true),"client accepts original host");
        check(!eligible(300,200,100,300,0,false,true),"client rejects non-host peer");

        DiagnosticQueue diagnostics;
        DiagnosticLine line;
        diagnostics.push(4,"first\r\nline"); diagnostics.push(3,"second");
        check(diagnostics.pop(line) && line.level==4 && std::strcmp(line.text.data(),"first  line")==0,"diagnostics normalize multiline text");
        check(diagnostics.pop(line) && line.level==3 && std::strcmp(line.text.data(),"second")==0,"diagnostics preserve FIFO order");
        check(!diagnostics.pop(line),"empty diagnostic queue is nonblocking");
        const std::string longText(10000,'x'); diagnostics.push(2,longText.c_str());
        check(diagnostics.pop(line) && std::strlen(line.text.data())==DiagnosticLine::TextCapacity-1,"diagnostic text has a fixed bound and terminator");
        diagnostics.push(1,nullptr);
        check(diagnostics.pop(line) && line.text[0]=='\0',"null diagnostic text is harmless");
        for(int i=0;i<100;++i) diagnostics.push(3,"bounded");
        int drained=0; while(diagnostics.pop(line)) ++drained;
        check(drained==64 && diagnostics.takeDropped()==36 && diagnostics.takeDropped()==0,"diagnostic overflow is bounded and counted once");
        auto produce=[&] { for(int i=0;i<2000;++i) diagnostics.push(3,"threaded"); };
        std::thread firstProducer(produce), secondProducer(produce);
        firstProducer.join(); secondProducer.join(); drained=0;
        while(diagnostics.pop(line)) ++drained;
        check(drained<=64 && drained+diagnostics.takeDropped()==4000,"concurrent callbacks neither block on capacity nor lose drop accounting");

        int configured=0;
        auto configure=[&] { ++configured; return true; };
        RoutePolicy automatic(false);
        check(!automatic.relayOnly(),"automatic mode preserves Steam defaults");
        check(!automatic.recover(100,false,true,true,configure) && configured==0,"ordinary peer failures do not change routing");
        check(!automatic.recover(100,true,false,true,configure) && configured==0,"unavailable local relay preserves the direct option");
        check(!automatic.recover(100,true,true,false,configure) && configured==0,"unavailable or unknown remote relay preserves the direct option");
        automatic.connected(200);
        check(!automatic.recover(200,true,true,true,configure) && configured==0,"established peer failure does not trigger initial routing recovery");
        automatic.closed(100);
        check(!automatic.recover(200,true,true,true,configure) && configured==0,"closing another guest preserves established-peer policy");
        check(automatic.recover(100,true,true,true,configure) && configured==1 && automatic.relayOnly(),"first initial rendezvous failure can select relay for future sessions");
        automatic.closed(100);
        check(!automatic.recover(100,true,true,true,configure) && !automatic.recover(300,true,true,true,configure) && configured==1,
            "relay recovery is deduplicated across retries and peers for helper lifetime");
        RoutePolicy explicitRelay(true);
        check(explicitRelay.relayOnly() && !explicitRelay.recover(100,true,true,true,configure) && configured==1,"explicit relay mode never reconfigures on a failure");
        RoutePolicy rejectedConfig(false); int rejectedCalls=0;
        auto rejectConfig=[&] { ++rejectedCalls; return false; };
        check(!rejectedConfig.recover(100,true,true,true,rejectConfig) && !rejectedConfig.relayOnly(),"rejected SDK configuration does not claim relay mode");
        check(!rejectedConfig.recover(100,true,true,true,rejectConfig) && rejectedCalls==1,"rejected recovery does not spin on later failures");
        RoutePolicy newSession(false); newSession.connected(100); newSession.closed(100);
        check(newSession.recover(100,true,true,true,configure),"a freshly closed peer session starts a fresh eligibility record");
        RelayRefreshPolicy refresh;
        check(!refresh.shouldRefresh(false,true,0) && !refresh.shouldRefresh(true,false,0),"relay refresh requires a peer and negative availability");
        check(refresh.shouldRefresh(true,true,0) && !refresh.shouldRefresh(true,true,29999) && refresh.shouldRefresh(true,true,30000),
            "unavailable relay refreshes at most once per thirty seconds");
        check(!refresh.shouldRefresh(false,true,60000) && !refresh.shouldRefresh(true,false,60000) && refresh.shouldRefresh(true,true,60000),
            "healthy or idle checks do not consume a future retry");
        std::cout<<"PASS: "<<count<<" native core checks\n"; return 0;
    } catch(const std::exception& error) { std::cerr<<"FAIL: "<<error.what()<<'\n'; return 1; }
}
