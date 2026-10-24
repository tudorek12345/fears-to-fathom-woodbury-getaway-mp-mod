using System;
using WoodburySpectatorSync.Coop;

namespace WoodburySpectatorSync.Diagnostics
{
    // Opt-in fixture changes only transmitted appearances, never saved player settings.
    internal sealed class CharacterAppearanceProbe
    {
        private readonly bool _enabled = Environment.GetEnvironmentVariable("WSS_CHARACTER_PROBE") == "1";
        private int _session;
        private double _liveAt = -1;
        public CharacterProfile Select(bool host, CharacterProfile normal, int session, bool liveGameplay, double now)
        {
            if(!_enabled) return normal;
            if(_session!=session) { _session=session; _liveAt=-1; }
            if(liveGameplay && _liveAt<0) _liveAt=now;
            bool changed=_liveAt>=0 && now-_liveAt>=5;
            CharacterProfile.TryParse(host
                ? (changed?"1:2:4:0:2:7:2:2:3":"1:2:3:0:1:1:2:2:2")
                : (changed?"2:4:0:2:0:6:3:3:2":"2:4:2:2:2:4:3:3:1"),out var profile);
            return profile;
        }
    }
}
