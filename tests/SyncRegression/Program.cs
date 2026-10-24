using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Net;
using WoodburySpectatorSync.UI;

static class Program
{
    static int checks;
    static void Expect(bool ok,string name) { if (!ok) throw new Exception(name); checks++; }
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--probe")
        {
            try
            {
                var clock=System.Diagnostics.Stopwatch.StartNew();
                using(var stream=new SpacewarStream(args[1],true,0,Console.WriteLine))
                {
                    try { stream.WaitForPeer(()=>clock.Elapsed.TotalSeconds<15); }
                    catch(OperationCanceledException) { }
                    Console.WriteLine("Native lobby ID: " + stream.LobbyId);
                    Environment.ExitCode=stream.LobbyId==0?2:0;
                }
            }
            catch(Exception ex) { Console.WriteLine("Native probe: " + ex.Message); Environment.ExitCode=2; }
            return;
        }
        try { Run(args[0]); Console.WriteLine("PASS: " + checks + " pose/protocol/limb/Steam stream checks."); }
        catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode=1; }
    }
    static PlayerTransformState Pose(uint sequence=1) => new PlayerTransformState
    {
        PlayerId=1, SessionId=7, Generation=4, Sequence=sequence,
        Position=new Vector3(1,2,3), Rotation=Quaternion.identity,
        CameraPosition=new Vector3(1,3.6f,3), CameraRotation=new Quaternion(0,0.6f,0,0.8f),
        PoseFlags=PlayerPoseFlags.Valid | PlayerPoseFlags.Crouching | PlayerPoseFlags.LeftHand | PlayerPoseFlags.RightHand,
        LocalVelocity=new Vector3(-1,0,2), LeftHandPosition=new Vector3(-0.3f,1.2f,0.4f), LeftHandRotation=Quaternion.identity,
        RightHandPosition=new Vector3(0.3f,1.3f,0.5f), RightHandRotation=Quaternion.identity
    };
    static void Run(string fake)
    {
        TransportRegression.Run(fake, Expect);
        CharacterRegression.Run(Expect);
        AnimationAndMenu();
        StoryAndLookup();
        var presentation = new ScenePresentationFrame {
            health=8,music=2,
            nodes=new[] { new ScenePresentationFrame.Node {
                path="Typing Shooter Final scene/Player[0]", active=true, position=new Vector3(1,2,3),
                rotation=Quaternion.identity, scale=new Vector3(1,1,1), animation=1234,time=0.25f,
                texts=new[] { new ScenePresentationFrame.TextValue {type="TMPro.TextMeshProUGUI",text="Score: 42"} },
                tints=new[] { new ScenePresentationFrame.TintValue {type="UnityEngine.UI.Image",color=new Color(1,0,0,0.5f)} },
                hasAudio=true,playing=true,hasCamera=true,cameraEnabled=true,audioTime=12,volume=0.7f,fieldOfView=65
            } },
            words=new[] { new ScenePresentationFrame.Word {target="Typing Shooter Final scene/Enemy[0]",text="alien",progress=2} }
        };
        var encoded=presentation.Encode();
        Expect(ScenePresentationFrame.Decode(encoded).Encode()==encoded,"presentation arrays/text/color/poses/animation/audio/camera/words round trip");
        var bytes=Convert.FromBase64String(encoded);
        for(int i=0;i<bytes.Length;i++) Expect(RejectPresentation(Convert.ToBase64String(bytes.Take(i).ToArray())),"truncated presentation at "+i);
        Expect(RejectPresentation(Convert.ToBase64String(bytes.Concat(new byte[]{0}).ToArray())),"presentation trailing bytes rejected");
        var corrupt=(byte[])bytes.Clone();corrupt[0]=2;
        Expect(RejectPresentation(Convert.ToBase64String(corrupt)),"unknown presentation schema rejected");
        corrupt=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(9000),0,corrupt,12,4);
        Expect(RejectPresentation(Convert.ToBase64String(corrupt)),"oversize presentation node count rejected");
        corrupt=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(4),0,corrupt,8,4);
        Expect(RejectPresentation(Convert.ToBase64String(corrupt)),"unknown minigame music state rejected");
        presentation.words[0].progress=99; Expect(RejectPresentation(presentation.Encode()),"presentation invalid word progress rejected");
        presentation.words[0].progress=2; presentation.nodes[0].position.x=float.NaN;
        Expect(RejectPresentation(presentation.Encode()),"presentation non-finite transform rejected");
        Expect(RejectPresentation("{\"health\":10}"),"incomplete Unity JSON cannot pass as presentation");
        foreach (var scene in SceneCatalog.GameplayScenes)
        {
            string adapter = SceneCatalog.AdapterFor(scene);
            Expect(adapter.Length > 0, scene + " has a sync adapter");
            int entered = 0, emitted = 0, applied = 0;
            var host = new DelegateHostSceneAdapter(adapter, s => SceneCatalog.Matches(adapter, s), () => entered++, _ => emitted++);
            var client = new DelegateClientSceneAdapter(adapter, s => SceneCatalog.Matches(adapter, s), () => entered++, (k,v,d) => { applied++; return k == "state" && v == 42; });
            Expect(host.MatchesScene(scene) && client.MatchesScene(scene), scene + " symmetric routing");
            Expect(!host.MatchesScene("MainMenu") && !client.MatchesScene(scene + "Backup"), scene + " rejects utility/substring collisions");
            host.OnSceneEnter(); client.OnSceneEnter(); host.EmitHostSnapshot(123);
            Expect(client.TryApplyStoryFlag("state",42,true) && entered == 2 && emitted == 1 && applied == 1, scene + " entry/snapshot/apply dispatch");
        }
        Expect(SceneCatalog.AdapterFor(null) == "" && SceneCatalog.AdapterFor("Unknown") == "", "unknown scenes do not claim sync");
        Expect(SceneCatalog.AdapterFor("CabinDarkScene") == "Cabin", "legacy dark scene alias retained");
        Expect(SceneCatalog.AdapterFor("Typing Shooter Final scene") == "TypingShooter" && SceneCatalog.HasOfficeWorld("Typing Shooter Final scene"), "additive typing retains Office sync");
        Expect(SceneCatalog.AdapterFor("CreditsScene") == "Credits" && !SceneCatalog.HasOfficeWorld("CreditsScene"), "credits routing is separate");
        var p=Pose(); Message message; string error; ProtocolParseErrorCode code;
        byte[] payload=Protocol.BuildPlayerTransform(p);
        Expect(Protocol.TryParsePayload(payload,out message,out error),"pose parses");
        var round=((PlayerTransformMessage)message).State;
        Expect(Protocol.BuildPlayerTransform(round).SequenceEqual(payload),"all pose fields round trip exactly");
        Expect(Protocol.TryParsePayload(Protocol.BuildPong(p),out message,out error),"pong parses pose");
        Expect(Protocol.BuildPlayerTransform(((PongMessage)message).Transform).SequenceEqual(payload),"pong preserves all fields");
        for (int i=0;i<payload.Length;i++) Expect(!Protocol.TryParsePayload(payload.Take(i).ToArray(),out message,out error),"truncated pose at "+i);
        Expect(!Protocol.TryParsePayload(payload.Concat(new byte[]{0}).ToArray(),out message,out error),"trailing pose bytes rejected");
        payload[4]=5;
        Expect(!Protocol.TryParsePayload(payload,out message,out code,out error) && code==ProtocolParseErrorCode.UnsupportedVersion,"v5 rejects cleanly");
        p=Pose(); p.Position.x=float.NaN;
        Expect(!Protocol.TryParsePayload(Protocol.BuildPlayerTransform(p),out message,out error),"NaN position rejected");
        p=Pose();p.RightHandPosition.x=999;
        Expect(!PlayerPoseRules.IsValid(p),"unbounded reach rejected");
        p=Pose();p.CameraRotation=new Quaternion(); Expect(!PlayerPoseRules.IsValid(p),"zero rotation rejected");
        p=Pose();p.PoseFlags=(PlayerPoseFlags)65535;Expect(!PlayerPoseRules.IsValid(p),"unknown flags rejected");
        var gate=new PlayerPoseStream();
        Expect(gate.Accept(Pose(3),1,7,4),"first accepted");
        Expect(!gate.Accept(Pose(3),1,7,4),"TCP/UDP duplicate rejected");
        Expect(!gate.Accept(Pose(2),1,7,4),"reordered packet rejected");
        Expect(!gate.Accept(Pose(4),0,7,4),"wrong player rejected");
        Expect(!gate.Accept(Pose(4),1,8,4),"wrong session rejected");
        Expect(!gate.Accept(Pose(4),1,7,5),"wrong scene rejected");
        Expect(gate.Accept(Pose(4),1,7,4),"bad packets do not poison sequence");
        gate.Reset(); Expect(gate.Accept(Pose(uint.MaxValue),1,7,4) && gate.Accept(Pose(0),1,7,4),"sequence wraps");
        p=Pose();p.Generation=5;Expect(gate.Accept(p,1,7,5),"new scene resets sequence");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Seated|PlayerPoseFlags.Airborne,10,"Run")=="Sit","vehicle motion does not walk");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Airborne,0,null)=="Jump","airborne takes priority");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Crouching,0,null)=="Crouch","crouch idle");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Crouching,1,null)=="CrouchWalk","crouch walk");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Valid,0.15f,"Idle")=="Idle" && PlayerPoseRules.SelectMotion(PlayerPoseFlags.Valid,0.15f,"Walk")=="Walk","walk hysteresis");
        Expect(PlayerPoseRules.SelectMotion(PlayerPoseFlags.Sprinting,2,null)=="Run","sprint state honored");
        foreach (var target0 in new[]{new Vector3(0.4f,0,0.2f),new Vector3(100,100,100),Vector3.zero,Vector3.up})
        {
            var target=target0;var joint=LimbPoseMath.JointPosition(Vector3.zero,ref target,Vector3.up,0.4f,0.3f);
            Expect(Math.Abs(joint.magnitude-0.4f)<0.001,"upper limb length preserved");
            Expect(Math.Abs((target-joint).magnitude-0.3f)<0.001,"lower limb length preserved");
        }
        var sampler=new PlayerMotionSampler();
        sampler.Sample(Vector3.zero,0,true); Vector3 velocity=Vector3.zero;
        for(int i=1;i<=100;i++) velocity=sampler.Sample(new Vector3(i*0.002f,0,0),i*0.001f,false);
        Expect(Math.Abs(velocity.x-2f)<0.01f,"1000 FPS samples retain movement speed");
        Expect(sampler.Sample(new Vector3(50,0,0),0.11f,false).sqrMagnitude==0,"teleport does not create sprint velocity");
        Expect(sampler.Sample(new Vector3(51,0,0),1f,false).sqrMagnitude==0,"long gaps reset velocity");
        Expect(sampler.Sample(new Vector3(51.1f,0,0),1.05f,true).sqrMagnitude==0,"seat/source changes reset velocity");
        var frame=Protocol.BuildFrame(Protocol.BuildPlayerTransform(Pose()));
        using(var partial=new ChunkStream(frame)) Expect(TcpFraming.TryReadFrame(partial,new byte[4],out payload)==FrameReadResult.Success,"partial stream reads reassemble");
        using(var partial=new MemoryStream(frame.Take(frame.Length-1).ToArray())) Expect(TcpFraming.TryReadFrame(partial,new byte[4],out payload)==FrameReadResult.Disconnected,"truncated frame disconnects");
        using(var partial=new MemoryStream(BitConverter.GetBytes(Protocol.MaxPayloadBytes+1))) Expect(TcpFraming.TryReadFrame(partial,new byte[4],out payload)==FrameReadResult.PayloadTooLarge,"oversize frame rejected before allocation");
        foreach(string mode in new[]{"echo","spoof"})
        {
            Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR",mode);
            using(var stream=new SpacewarStream(fake,true,0,null))
            {
                stream.WaitForPeer(()=>true);Expect(stream.LobbyId==900,"lobby exposed");
                byte[] large=Protocol.BuildFrame(Enumerable.Range(0,200000).Select(i=>(byte)i).ToArray());
                stream.Write(large,0,large.Length);
                var task=Task.Run(()=> { byte[] data;var result=TcpFraming.TryReadFrame(stream,new byte[4],out data);return result==FrameReadResult.Success && data.SequenceEqual(large.Skip(4)); });
                Expect(task.Wait(5000) && task.Result,"Steam chunks reassemble and foreign senders ignored: "+mode);
            }
        }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","echo");
        using(var stream=new SpacewarStream(fake,false,900,null)) { stream.WaitForPeer(()=>true); Expect(stream.LobbyId==900,"join validates original host"); }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","crowded");
        bool rejected=false;try { using(var stream=new SpacewarStream(fake,true,0,null))stream.WaitForPeer(()=>true); } catch(IOException) { rejected=true; }
        Expect(rejected,"third player rejected");
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","disconnect");
        using(var stream=new SpacewarStream(fake,true,0,null))
        {
            stream.WaitForPeer(()=>true);stream.Write(frame,0,frame.Length);
            var task=Task.Run(()=> { try { stream.Read(new byte[1],0,1);return false; }catch(IOException) { return true; } });
            Expect(task.Wait(5000) && task.Result,"peer departure unblocks reader");
        }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","lobby-only");
        using(var stream=new SpacewarStream(fake,true,0,null))
        {
            var timer=System.Diagnostics.Stopwatch.StartNew(); bool cancelled=false;
            try { stream.WaitForPeer(()=>timer.ElapsedMilliseconds<700); } catch(OperationCanceledException) { cancelled=true; }
            Expect(cancelled && stream.LobbyId==900,"lobby membership alone never reports packet connection");
        }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","route-delay");
        using(var stream=new SpacewarStream(fake,false,900,null))
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();stream.WaitForPeer(()=>timer.ElapsedMilliseconds<5000);
            Expect(timer.ElapsedMilliseconds>=350,"delayed Steam route waits for received packets");
        }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR","route-error");
        string failureStatus=""; bool failed=false;
        using(var stream=new SpacewarStream(fake,false,900,s=>failureStatus=s))
        {
            try { stream.WaitForPeer(()=>true); } catch(IOException ex) { failed=ex.Message.Contains("fixture route failure 5008"); }
            Expect(failed && failureStatus.Contains("fixture route failure 5008"),"route failure keeps helper reason instead of reporting connected");
        }
        Environment.SetEnvironmentVariable("WSS_FAKE_SPACEWAR",null);
    }

    enum StoryStage { Before, After }
    sealed class StoryFixture { public bool completed; public int count; public StoryStage stage; public string ignored = "safe"; }
    static void StoryAndLookup()
    {
        var schema=new ScalarStorySchema(typeof(StoryFixture),"completed","count","stage");
        var host=new StoryFixture {completed=true,count=42,stage=StoryStage.After}; var peer=new StoryFixture();
        Expect(schema.IsComplete,"story schema binds explicit scalar fields");
        for(int i=0;i<schema.Names.Length;i++)
        {
            Expect(schema.Read(host,i,out var value),"read native story scalar "+i);
            var frame=Protocol.BuildStoryFlag(schema.Names[i],value);
            Protocol.TryParsePayload(frame,out var parsed,out _,out _); var flag=(StoryFlagMessage)parsed;
            Expect(schema.Apply(peer,flag.Key,flag.Value),"story scalar crosses real wire codec "+i);
        }
        Expect(peer.completed && peer.count==42 && peer.stage==StoryStage.After,"story values applied to peer");
        Expect(!schema.Apply(peer,"completed",9) && peer.completed,"invalid story bool rejected without mutation");
        Expect(!schema.Apply(peer,"stage",9) && peer.stage==StoryStage.After,"invalid story enum rejected without mutation");
        Expect(!schema.Apply(peer,"ignored",0) && peer.ignored=="safe","unlisted native fields cannot be changed");
        Expect(!new ScalarStorySchema(typeof(StoryFixture),"missing").IsComplete,"missing game fields are diagnosed");
        var cache=new BatchLookupCache();int calls=0;Func<object> find=()=>{calls++;return new object();};object first;
        using(cache.Begin())
        {
            first=cache.Get(find); using(cache.Begin()) Expect(ReferenceEquals(first,cache.Get(find)),"nested sync shares one lookup");
            Expect(ReferenceEquals(first,cache.Get(find)) && calls==1,"inner scope retains outer lookup");
            cache.Invalidate();Expect(!ReferenceEquals(first,cache.Get(find)) && calls==2,"spawn invalidation refreshes lookup");
        }
        using(cache.Begin()) Expect(!ReferenceEquals(first,cache.Get(find)) && calls==3,"next update refreshes lookup");
        using(cache.Begin()) { Expect(cache.Get<string>(()=>null)==null,"missing lookup cached");Expect(cache.Get<string>(()=>"appeared")==null,"negative lookup stable within batch"); }
        using(cache.Begin()) Expect(cache.Get<string>(()=>"appeared")=="appeared","activation visible next batch");
        try { using(cache.Begin()) { cache.Get(find); throw new IOException(); } } catch(IOException) {}
        var before=calls;cache.Get(find);cache.Get(find);Expect(calls==before+2,"exception disposal does not leak cache scope");
    }

    static void AnimationAndMenu()
    {
        var airborne = new PlayerAirborneSampler();
        Expect(!airborne.Sample(false,false,0,true), "inactive controller does not imply jump");
        Expect(!airborne.Sample(false,true,0,false), "stationary uninitialized grounding does not imply jump");
        Expect(airborne.Sample(false,true,4,false), "upward motion starts airborne animation");
        Expect(airborne.Sample(false,true,0,false), "jump apex retains airborne animation");
        Expect(!airborne.Sample(true,true,0,false), "landing clears airborne animation");
        Expect(airborne.Sample(false,true,-2,false), "falling starts airborne animation");
        Expect(!airborne.Sample(false,false,-2,false), "cinematic movement cannot animate a jump");
        Expect(!airborne.Sample(false,true,0,true), "scene/source reset clears airborne history");
        var idle = CharacterAnimation.Evaluate(0,0,0,0,0,0,0,0);
        Expect(Math.Abs(CharacterAnimation.GroundCorrection(0,-0.14f,0.1f,1)-0.14f)<0.0001f, "planted foot correction prevents crouch sinking");
        Expect(CharacterAnimation.GroundCorrection(0,0.2f,0.4f,1)==-0.2f, "standing baseline follows lower foot");
        Expect(CharacterAnimation.GroundCorrection(0,-0.3f,0,0)==0, "seated and airborne poses are not grounded artificially");
        var breath = CharacterAnimation.Evaluate(0,0,0,0.8f,0,0,0,0);
        Expect(idle.LeftThigh.x == 0 && idle.LeftKnee.x == 0, "idle has no walking legs");
        Expect(breath.Chest.x > idle.Chest.x && breath.HipOffset.y > idle.HipOffset.y, "idle breathing moves torso and hips");
        var walk = CharacterAnimation.Evaluate(2,0,1.5707963f,0,0,0,0,0);
        var run = CharacterAnimation.Evaluate(5,0,1.5707963f,0,0,0,0,1);
        Expect(walk.LeftThigh.x > 20 && walk.RightThigh.x < -20, "walk alternates legs");
        Expect(walk.LeftArm.x < 0 && walk.RightArm.x > 0, "arms counter-swing legs");
        Expect(walk.LeftKnee.x == 0 && walk.RightKnee.x > 30, "swinging knee lifts independently");
        Expect(run.LeftThigh.x > walk.LeftThigh.x && run.RightKnee.x > walk.RightKnee.x && run.LeftElbow.x < walk.LeftElbow.x, "run has stronger stride, knees and bent arms");
        var back = CharacterAnimation.Evaluate(-2,0,1.5707963f,0,0,0,0,0);
        Expect(back.LeftThigh.x < 0 && back.RightThigh.x > 0, "backward movement reverses stride");
        var side = CharacterAnimation.Evaluate(0,2,1.5707963f,0,0,0,0,0);
        Expect(side.LeftThigh.x == 0 && side.LeftThigh.z < -20 && side.RightThigh.z > 20, "strafe moves legs sideways");
        var crouch = CharacterAnimation.Evaluate(0,0,0,0,1,0,0,0);
        Expect(crouch.HipOffset.y < -0.25f && crouch.LeftKnee.x > 60 && crouch.LeftThigh.x < -30, "crouch bends knees and lowers body");
        var sit = CharacterAnimation.Evaluate(10,10,1.5f,0,1,1,1,1);
        Expect(sit.LeftThigh.x == -78 && sit.RightThigh.x == -78 && sit.LeftKnee.x == 82 && sit.HipOffset.y == 0, "seating overrides movement/crouch/airborne");
        var jump = CharacterAnimation.Evaluate(3,0,1.5707963f,0,0,0,1,0);
        var jumpOtherPhase = CharacterAnimation.Evaluate(3,0,4.71f,0,0,0,1,0);
        Expect(jump.LeftKnee.x == 55 && jump.LeftThigh.x == jumpOtherPhase.LeftThigh.x, "airborne tucks legs without walking in air");
        Expect(CharacterAnimation.CycleRate(0) == 0 && CharacterAnimation.CycleRate(-1) == 0 && CharacterAnimation.CycleRate(30) <= 15, "cycle rate stops and is bounded");
        for (int i=0; i<60; i++)
        {
            var frame = CharacterAnimation.Evaluate(i%7-3,i%5-2,i*0.7f,i*0.1f,(i%3)*0.5f,(i%4)*0.33f,(i%6)*0.2f,(i%2));
            Expect(Math.Abs(frame.LeftThigh.x)<140 && Math.Abs(frame.RightKnee.x)<160 && Math.Abs(frame.HipOffset.y)<0.32f, "blended animation stays bounded " + i);
        }
        Expect(CoopMenuValidation.TryValidate(false,true,"127.0.0.1","27055","","Player",out int port,out ulong lobby,out _) && port==27055, "native LAN join validates");
        Expect(CoopMenuValidation.TryValidate(false,true,"::1","65535","","Player",out _,out _,out _), "IPv6 and max port accepted");
        foreach(var bad in new[]{"0","65536","-1","abc",""}) Expect(!CoopMenuValidation.TryValidate(false,true,"host",bad,"","Player",out _,out _,out _), "invalid port " + bad);
        foreach(var bad in new[]{"","http://host","host name","host\nname"}) Expect(!CoopMenuValidation.TryValidate(false,true,bad,"27055","","Player",out _,out _,out _), "invalid address");
        Expect(CoopMenuValidation.TryValidate(true,true,"","","109775243987654321","Player",out _,out lobby,out _) && lobby==109775243987654321UL, "Steam lobby ID preserves 64 bits");
        foreach(var bad in new[]{"0","-1","18446744073709551616",""}) Expect(!CoopMenuValidation.TryValidate(true,true,"","",bad,"Player",out _,out _,out _), "invalid lobby ID");
        Expect(CoopMenuValidation.TryValidate(true,false,"","","","Player",out _,out _,out _), "Spacewar host does not need lobby ID");
        foreach(var name in new[]{"","  ",new string('a',33)}) Expect(!CoopMenuValidation.TryValidate(true,false,"","","",name,out _,out _,out _), "invalid player name");
    }

    static bool RejectPresentation(string payload)
    {
        try { ScenePresentationFrame.Decode(payload); return false; }
        catch(FormatException) { return true; }
        catch(InvalidDataException) { return true; }
        catch(IOException) { return true; }
    }
    sealed class ChunkStream : MemoryStream
    {
        public ChunkStream(byte[] data):base(data){}
        public override int Read(byte[] b,int offset,int count)=>base.Read(b,offset,Math.Min(3,count));
    }
}
