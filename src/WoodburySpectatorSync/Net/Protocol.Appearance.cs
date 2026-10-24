using System;
using System.IO;
using WoodburySpectatorSync.Coop;

namespace WoodburySpectatorSync.Net
{
    public sealed class CharacterAppearanceMessage : Message
    {
        public readonly int SessionId;
        public readonly uint Revision;
        public readonly CharacterProfile Profile;
        public CharacterAppearanceMessage(int session, uint revision, CharacterProfile profile)
        { Type=MessageType.CharacterAppearance; SessionId=session; Revision=revision; Profile=profile; }
    }
    public static partial class Protocol
    {
        public static byte[] BuildCharacterAppearance(CharacterAppearanceMessage message)
        {
            if(message.SessionId<=0 || message.Revision==0 || !message.Profile.IsValid) throw new ArgumentException("Invalid character appearance");
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream))
            {
                WriteHeader(writer,MessageType.CharacterAppearance);
                writer.Write(message.SessionId); writer.Write(message.Revision);
                for(int i=0;i<CharacterProfile.Choices.Length;i++) writer.Write(message.Profile.Get(i));
                return stream.ToArray();
            }
        }
        private static CharacterAppearanceMessage ReadCharacterAppearance(BinaryReader reader)
        {
            int session=reader.ReadInt32(); uint revision=reader.ReadUInt32(); var profile=new CharacterProfile();
            for(int i=0;i<CharacterProfile.Choices.Length;i++) profile.Set(i,reader.ReadByte());
            if(session<=0 || revision==0 || !profile.IsValid || reader.BaseStream.Position!=reader.BaseStream.Length) throw new InvalidDataException("Invalid character appearance");
            return new CharacterAppearanceMessage(session,revision,profile);
        }
    }
}
