using System;
using System.Linq;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Net;

static class CharacterRegression
{
    public static void Run(Action<bool,string> expect)
    {
        var original=CharacterProfile.Default;
        foreach(var scene in SceneCatalog.GameplayScenes.Concat(new[]{"Typing Shooter Final scene"})) expect(SceneCatalog.HasPlayerAvatar(scene),"custom avatar retains gameplay world "+scene);
        foreach(var scene in new[]{"MainMenu","CreditsScene","Disclaimer",null,"OfficeLayoutBackup"}) expect(!SceneCatalog.HasPlayerAvatar(scene),"no avatar in utility or unknown scene");
        for(int slot=0;slot<CharacterProfile.Choices.Length;slot++)
        {
            for(byte value=0;value<CharacterProfile.Choices[slot].Length;value++)
            {
                var profile=original; profile.Set(slot,value);
                expect(CharacterProfile.TryParse(profile.ToString(),out var saved) && saved.Equals(profile),"catalog setting roundtrip "+slot+":"+value);
                var packet=Protocol.BuildCharacterAppearance(new CharacterAppearanceMessage(72,9,profile));
                expect(packet.Length<32 && Protocol.TryParsePayload(packet,out var decoded,out _,out _) && decoded is CharacterAppearanceMessage message && message.SessionId==72 && message.Revision==9 && message.Profile.Equals(profile),"appearance wire roundtrip "+slot+":"+value);
            }
            var invalid=original; invalid.Set(slot,(byte)CharacterProfile.Choices[slot].Length);
            expect(!invalid.IsValid && !CharacterProfile.TryParse(invalid.ToString(),out _),"catalog rejects out of range "+slot);
            var corrupt=Protocol.BuildCharacterAppearance(new CharacterAppearanceMessage(72,9,original));
            corrupt[corrupt.Length-9+slot]=255;
            expect(!Protocol.TryParsePayload(corrupt,out _,out _,out _),"wire rejects catalog corruption "+slot);
        }
        foreach(var bad in new[]{null,"","0:1",new string('9',50),"-1:1:1:1:0:0:1:0:0","0: 1:1:1:0:0:1:0:0","0:1:1:1:0:0:1:0:0:0"})
            expect(!CharacterProfile.TryParse(bad,out var fallback) && fallback.Equals(original),"bad saved appearance falls back");
        var bytes=Protocol.BuildCharacterAppearance(new CharacterAppearanceMessage(72,9,original));
        for(int i=0;i<bytes.Length;i++) expect(!Protocol.TryParsePayload(bytes.Take(i).ToArray(),out _,out _,out _),"truncated appearance "+i);
        expect(!Protocol.TryParsePayload(bytes.Concat(new byte[]{0}).ToArray(),out _,out _,out _),"appearance rejects trailing bytes");
        var sender=new CharacterProfileChannel(); var receiver=new CharacterProfileChannel();
        expect(sender.Next(0,original,0)==null,"disconnected appearance is silent");
        var first=sender.Next(72,original,0);
        expect(first!=null && receiver.Accept(72,first),"initial appearance delivered");
        expect(sender.Next(72,original,2.9)==null,"unchanged appearance throttled");
        expect(!receiver.Accept(72,first),"duplicate appearance rejected");
        var changed=original; changed.Outfit=2; changed.Face=3;
        var update=sender.Next(72,changed,1);
        expect(update!=null && receiver.Accept(72,update) && receiver.Remote.Equals(changed),"live change sent without waiting for periodic resend");
        expect(!receiver.Accept(72,first) && receiver.Remote.Equals(changed),"old appearance cannot roll back a live edit");
        expect(sender.Next(72,changed,4)!=null,"periodic resend repairs queue churn");
        receiver.Reset(72); expect(receiver.Remote.Equals(changed),"same session preserves appearance across scene reload");
        receiver.Reset(73); expect(receiver.Remote.Equals(original) && !receiver.Accept(73,update),"new session rejects old look");
        expect(receiver.Accept(73,sender.Next(73,changed,4.1)),"new session immediately republishes appearance");
        receiver.Reset(0); expect(receiver.Remote.Equals(original),"disconnect clears partner appearance");
        expect(!receiver.Accept(73,new CharacterAppearanceMessage(73,0,changed)),"zero revision rejected");
        expect(receiver.Accept(74,new CharacterAppearanceMessage(74,uint.MaxValue,changed)) && receiver.Accept(74,new CharacterAppearanceMessage(74,1,original)),"revision rollover supported");
        expect(!receiver.Accept(74,new CharacterAppearanceMessage(74,uint.MaxValue,changed)),"pre-rollover replay rejected");
        expect(sender.Local(changed.ToString()).Equals(changed) && sender.Local("invalid").Equals(original),"cached settings refresh and recover");
        var draft=new CharacterDraft(); draft.Begin(original); draft.Cycle(0,-1);
        expect(draft.Changed && draft.Value.Face==3 && original.Face==0,"draft wraps without mutating saved look");
        draft.Discard(); expect(!draft.Changed && draft.Value.Equals(original),"discard restores saved look");
        draft.Begin(changed); draft.Reset(); expect(draft.Changed && draft.Value.Equals(original),"reset is an unapplied edit");
        draft.Discard(); expect(draft.Value.Equals(changed),"reset can be discarded");
        draft.Cycle(8,1); var committed=draft.Apply(); expect(!draft.Changed && committed.Accessory==1,"apply commits current draft");
        draft.Cycle(4,1); draft.Discard(); expect(draft.Value.Equals(committed),"subsequent discard respects last apply");
    }
}
