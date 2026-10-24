using System;
using System.Globalization;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    // Catalog indices only. No remote files, asset paths or arbitrary renderer names.
    public struct CharacterProfile : IEquatable<CharacterProfile>
    {
        public byte Face, Skin, Hair, HairColor, Outfit, Top, Pants, Boots, Accessory;
        public static readonly string[][] Choices = {
            new[] { "Oval", "Angular", "Round", "Long" },
            new[] { "Porcelain", "Sand", "Honey", "Bronze", "Umber", "Espresso" },
            new[] { "Cropped", "Side part", "Bob", "Beanie", "Shaved" },
            new[] { "Espresso", "Chestnut", "Copper", "Blond", "Silver" },
            new[] { "Field jacket", "Puffer", "Hoodie" },
            new[] { "Pine", "Rust", "Midnight", "Sand", "Plum", "Slate", "Snow", "Ochre" },
            new[] { "Charcoal", "Denim", "Olive", "Khaki", "Brown", "Black" },
            new[] { "Leather", "Black", "Sand", "Burgundy" },
            new[] { "None", "Glasses", "Scarf", "Beard" }
        };
        public static readonly string[] Labels = { "Face", "Skin tone", "Hair", "Hair color", "Outerwear", "Jacket color", "Trousers", "Boots", "Accessory" };
        public static CharacterProfile Default => new CharacterProfile { Skin=1, Hair=1, HairColor=1, Top=0, Pants=1 };
        public byte Get(int index)
        {
            switch(index) { case 0:return Face; case 1:return Skin; case 2:return Hair; case 3:return HairColor; case 4:return Outfit; case 5:return Top; case 6:return Pants; case 7:return Boots; case 8:return Accessory; default:throw new ArgumentOutOfRangeException(nameof(index)); }
        }
        public void Set(int index, byte value)
        {
            switch(index) { case 0:Face=value;break;case 1:Skin=value;break;case 2:Hair=value;break;case 3:HairColor=value;break;case 4:Outfit=value;break;case 5:Top=value;break;case 6:Pants=value;break;case 7:Boots=value;break;case 8:Accessory=value;break;default:throw new ArgumentOutOfRangeException(nameof(index)); }
        }
        public bool IsValid { get { for(int i=0;i<Choices.Length;i++) if(Get(i)>=Choices[i].Length) return false; return true; } }
        public bool Equals(CharacterProfile other) { for(int i=0;i<Choices.Length;i++) if(Get(i)!=other.Get(i)) return false; return true; }
        public override bool Equals(object other) => other is CharacterProfile profile && Equals(profile);
        public override int GetHashCode() { int hash=17; for(int i=0;i<Choices.Length;i++) hash=unchecked(hash*31+Get(i)); return hash; }
        public override string ToString() { var values=new string[Choices.Length]; for(int i=0;i<values.Length;i++) values[i]=Get(i).ToString(CultureInfo.InvariantCulture); return string.Join(":",values); }
        public static bool TryParse(string value, out CharacterProfile profile)
        {
            profile=Default; if(value==null || value.Length>40) return false;
            var fields=value.Split(':'); if(fields.Length!=Choices.Length) return false;
            var candidate=new CharacterProfile();
            for(int i=0;i<fields.Length;i++) { if(!byte.TryParse(fields[i],NumberStyles.None,CultureInfo.InvariantCulture,out byte part)) return false; candidate.Set(i,part); }
            if(!candidate.IsValid) return false; profile=candidate; return true;
        }
    }

    // A profile is owned by the sender of this handshaken two-player connection.
    // It survives scene changes, but never a session change. Reliable periodic replay
    // repairs initial snapshot queue churn without sending textures or frame-rate traffic.
    internal sealed class CharacterProfileChannel
    {
        private int _session;
        private uint _sent, _received;
        private double _next;
        private CharacterProfile _last;
        private string _localText;
        private CharacterProfile _local = CharacterProfile.Default;
        public CharacterProfile Local(string text)
        { if(_localText!=text) { _localText=text; CharacterProfile.TryParse(text,out _local); } return _local; }
        public CharacterProfile Remote { get; private set; } = CharacterProfile.Default;
        public void Reset(int session)
        { if(_session==session) return; _session=session; _sent=_received=0; _next=0; Remote=CharacterProfile.Default; }
        public CharacterAppearanceMessage Next(int session, CharacterProfile local, double now)
        {
            Reset(session); if(session<=0 || !local.IsValid || (_sent!=0 && local.Equals(_last) && now<_next)) return null;
            _last=local; _next=now+3; if(++_sent==0) ++_sent;
            return new CharacterAppearanceMessage(session,_sent,local);
        }
        public bool Accept(int session, CharacterAppearanceMessage message)
        {
            if(session<=0 || message.SessionId!=session || message.Revision==0 || !message.Profile.IsValid) return false;
            Reset(session);
            if(_received!=0 && unchecked((int)(message.Revision-_received))<=0) return false;
            _received=message.Revision; Remote=message.Profile; return true;
        }
    }

    internal sealed class CharacterDraft
    {
        public CharacterProfile Value { get; private set; }
        private CharacterProfile _saved;
        public bool Changed => !Value.Equals(_saved);
        public void Begin(CharacterProfile saved) { _saved=Value=saved.IsValid?saved:CharacterProfile.Default; }
        public void Cycle(int slot,int direction) { var next=Value; next.Set(slot,(byte)((next.Get(slot)+direction%CharacterProfile.Choices[slot].Length+CharacterProfile.Choices[slot].Length)%CharacterProfile.Choices[slot].Length)); Value=next; }
        public void Reset() { Value=CharacterProfile.Default; }
        public void Discard() { Value=_saved; }
        public CharacterProfile Apply() { _saved=Value; return Value; }
    }
}
