using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Net;

static class TransportRegression
{
    static void Until(Func<bool> done) { var timer=Stopwatch.StartNew(); while(!done()) { if(timer.ElapsedMilliseconds>5000) throw new Exception("Transport wait timed out"); Thread.Sleep(10); } }
    public static void Run(string fake, Action<bool,string> expect)
    {
        var root=Path.Combine(Path.GetTempPath(),"WoodburyTransport-"+Guid.NewGuid().ToString("N"));
        var helper=Path.Combine(root,"Spacewar"); Directory.CreateDirectory(helper);
        foreach(var file in Directory.GetFiles(fake)) File.Copy(file,Path.Combine(helper,Path.GetFileName(file)));
        BepInEx.Paths.PluginPath=root;
        var settings=new Settings(); settings.Transport.Value=CoopTransport.Spacewar;
        var logger=new ManualLogSource();
        try
        {
            Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","route-error");
            var client=new CoopClient(logger,settings); client.Connect();
            try
            {
                Until(()=>!client.IsRunning);
                expect(!client.IsConnected && client.LastDisconnectReason.Contains("fixture route failure 5008"),"client retains Spacewar join failure");
                Thread.Sleep(200);
                expect(client.Status.Contains("fixture route failure 5008"),"client cleanup does not erase failure");
            } finally { client.Disconnect(); }
            var server=new CoopServer(logger,settings); server.Start();
            try
            {
                Until(()=>!server.IsRunning);
                expect(!server.IsClientConnected && server.ConnectionStatus.Contains("fixture route failure 5008"),"host retains Spacewar join failure");
            } finally { server.Stop(); }
            Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","disconnect");
            client=new CoopClient(logger,settings); client.Connect();
            try
            {
                Until(()=>client.IsConnected); client.Enqueue(new PingMessage()); Until(()=>!client.IsRunning);
                expect(client.LastDisconnectReason.Contains("peer disconnected"),"established Spacewar client departure has a reason");
                Thread.Sleep(200); expect(!client.IsRunning,"expired Spacewar lobby is not silently retried");
            } finally { client.Disconnect(); }
            server=new CoopServer(logger,settings); server.Start();
            try
            {
                Until(()=>server.IsClientConnected); server.Enqueue(new PingMessage()); Until(()=>!server.IsRunning);
                expect(server.LastDisconnectReason.Contains("peer disconnected"),"established Spacewar host departure has a reason");
            } finally { server.Stop(); }

            settings.Transport.Value=CoopTransport.Lan;
            var reservation=new TcpListener(IPAddress.Loopback,0); reservation.Start();
            settings.HostPort.Value=((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
            server=new CoopServer(logger,settings); server.Start();
            try
            {
                for(int round=0;round<12;round++)
                {
                    Stream old;
                    using(var first=new TcpClient("127.0.0.1",settings.HostPort.Value))
                    {
                        Until(()=>server.IsClientConnected);
                        old=(Stream)typeof(CoopServer).GetField("_stream",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(server);
                    }
                    Until(()=>!server.IsClientConnected);
                    using(var next=new TcpClient("127.0.0.1",settings.HostPort.Value))
                    {
                        next.ReceiveTimeout=3000; Until(()=>server.IsClientConnected);
                        // Reproduce a late cleanup callback from the previous receiver.
                        typeof(CoopServer).GetMethod("DisconnectClient",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(server,new object[]{old,"late prior session failure"});
                        server.Enqueue(new StoryFlagMessage("reconnect",round));
                        byte[] payload; var result=TcpFraming.TryReadFrame(next.GetStream(),new byte[4],out payload);
                        expect(result==FrameReadResult.Success && Protocol.TryParsePayload(payload,out var message,out var error,out var detail) && message is StoryFlagMessage flag && flag.Value==round,"old receiver cannot kick replacement peer "+round);
                    }
                    Until(()=>!server.IsClientConnected);
                }
                client=new CoopClient(logger,settings); client.Connect();
                try
                {
                    Until(()=>server.IsClientConnected && client.IsConnected);
                    var hostLook=CharacterProfile.Default; hostLook.Outfit=2; hostLook.Top=7;
                    var clientLook=CharacterProfile.Default; clientLook.Face=3; clientLook.Hair=4; clientLook.Accessory=1;
                    server.Enqueue(new CharacterAppearanceMessage(44,1,hostLook));
                    client.Enqueue(new CharacterAppearanceMessage(44,2,clientLook));
                    Message receivedByHost=null, receivedByClient=null;
                    Until(()=>server.TryDequeueIncoming(out receivedByHost));
                    Until(()=>client.TryDequeue(out receivedByClient));
                    expect(receivedByHost is CharacterAppearanceMessage fromClient && fromClient.Profile.Equals(clientLook) && fromClient.Revision==2,"production client sends appearance over real TCP");
                    expect(receivedByClient is CharacterAppearanceMessage fromHost && fromHost.Profile.Equals(hostLook) && fromHost.Revision==1,"production host sends appearance over real TCP");
                }
                finally { client.Disconnect(); }
            } finally { server.Stop(); }
        }
        finally { Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR",null); }
    }
}
