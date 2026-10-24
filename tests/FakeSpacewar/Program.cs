using System;
using System.IO;
using WoodburySpectatorSync.Net;

static class Program
{
    static void Main()
    {
        var input = Console.OpenStandardInput(); var output = Console.OpenStandardOutput();
        string mode = Environment.GetEnvironmentVariable("WSS_FAKE_SPACEWAR");
        int sends = 0;
        try
        {
            for (;;)
            {
                var frame = SpacewarWire.Read(input);
                if (frame.Op == SpacewarOp.Init)
                    Send(output, SpacewarOp.Ready, 1, SpacewarWire.Data(w => { w.Write(480u); w.Write(100UL); SpacewarWire.Text(w, "Fixture"); }));
                else if (frame.Op == SpacewarOp.Host || frame.Op == SpacewarOp.Join)
                {
                    bool host = frame.Op == SpacewarOp.Host;
                    Send(output, SpacewarOp.Lobby, 2, SpacewarWire.Data(w =>
                    {
                        w.Write(900UL); w.Write(host ? 100UL : 200UL); w.Write(200UL);
                        w.Write((byte)(mode == "crowded" ? 2 : 1)); w.Write(200UL);
                        if (mode == "crowded") w.Write(300UL);
                    }));
                }
                else if (frame.Op == SpacewarOp.Send)
                {
                    sends++;
                    if (mode == "lobby-only") continue;
                    if (mode == "route-delay" && sends == 1) System.Threading.Thread.Sleep(400);
                    if (mode == "route-error") { Send(output, SpacewarOp.Error, 2, SpacewarWire.Data(w => {w.Write((byte)1); SpacewarWire.Text(w,"fixture route failure 5008");})); continue; }
                    if (mode == "disconnect" && sends > 1) { Send(output, SpacewarOp.PeerLeft, 2, BitConverter.GetBytes(200UL)); continue; }
                    if (mode == "spoof") Send(output, SpacewarOp.Packet, 2, SpacewarWire.Data(w => { w.Write(300UL); w.Write(new byte[] { 99, 99 }); }));
                    Send(output, SpacewarOp.Packet, 2, frame.Data);
                }
            }
        }
        catch (EndOfStreamException) { }
    }
    static void Send(Stream stream, SpacewarOp op, uint request, byte[] data) =>
        SpacewarWire.Write(stream, new SpacewarFrame { Op = op, Request = request, Data = data });
}
