using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class CabinStoryParitySync
    {
        public const string KeyPrefix = "StoryParity.";
        private static readonly ScalarStorySchema[] Schemas = {
            new ScalarStorySchema(typeof(CabinGameManager), "carIsInParkingLot", "coldNightSub", "playerTriedCrossingMapEnd", "mikeStartsTalkingAsPassenger", "isPhoneRinging"),
            new ScalarStorySchema(typeof(CabinHouseManager), "mikeExitedTruck", "hostHiddenFromWindow", "gettingInForTheFirstTime", "lockedJumpscare", "insideJumpscareConvoTrigger"),
            new ScalarStorySchema(typeof(CabinUIManager), "disableIntro", "disableHikerIntro"),
            new ScalarStorySchema(typeof(ToiletManager), "canPee", "peeingDone", "textSent"),
            new ScalarStorySchema(typeof(HostEndGame), "playerSawDavidOnce", "firePlaceConvoDone", "hitCount", "finalHit", "handleBreakPremature", "micShouted", "phoneCallDone", "stoolWasPutDown", "tableFlipped", "tableFlipped2", "isSofaMoved", "closeJumpscare"),
            new ScalarStorySchema(typeof(MikeEndGame), "stopShouting", "canBreakDoor", "brokeDoor")
        };
        private int _mask;
        private float _nextLog;
        private static UnityEngine.Object Root(CabinGameManager game, int index)
        {
            switch (index)
            {
                case 0: return game;
                case 1: return game.cabinHouseManager;
                case 2: return SceneLookup.First<CabinUIManager>();
                case 3: return game.cabinHouseManager != null ? game.cabinHouseManager.toiletManager : null;
                case 4: return game.hostEndGame;
                case 5: return game.mikeEnd;
                default: return null;
            }
        }
        public void Emit(CabinGameManager game, Action<string,int> emit, Action<string> log)
        {
            if (game == null) return;
            int mask = 0;
            for (int i=0;i<Schemas.Length;i++) if (Root(game,i) != null) mask |= 1 << i;
            emit(KeyPrefix + "Mask", mask);
            for (int i=0;i<Schemas.Length;i++)
            {
                var target=Root(game,i); if (target == null) continue;
                for (int n=0;n<Schemas[i].Names.Length;n++)
                    if (Schemas[i].Read(target,n,out var value)) emit(KeyPrefix+i+"."+Schemas[i].Names[n],value);
            }
            bool complete=Digest(game,mask,out int hash,out int count);
            if (complete) emit(KeyPrefix+"Digest",hash);
            Report("host",complete,count,hash,log);
        }
        public bool Apply(CabinGameManager game, string key, int value, Action<string> log)
        {
            if (game == null || !key.StartsWith(KeyPrefix,StringComparison.Ordinal)) return false;
            string name=key.Substring(KeyPrefix.Length);
            if (name=="Mask") { if (value<0 || value>=(1<<Schemas.Length)) return false; _mask=value; return true; }
            if (name=="Digest")
            {
                bool complete=Digest(game,_mask,out int actual,out int count);
                bool match=complete && actual==value;
                Report("client",match,count,actual,log);
                return match;
            }
            int dot=name.IndexOf('.');
            if (dot<=0 || !int.TryParse(name.Substring(0,dot),out int index) || index<0 || index>=Schemas.Length) return false;
            var target=Root(game,index);
            return target != null && Schemas[index].Apply(target,name.Substring(dot+1),value);
        }
        private static bool Digest(CabinGameManager game,int mask,out int hash,out int count)
        {
            hash=ScalarStorySchema.Hash(17,mask); count=0;
            // Validate bindings for late-spawned story actors too, even when this
            // checkpoint has not instantiated their scene references yet.
            foreach (var schema in Schemas) if (!schema.IsComplete) return false;
            for(int i=0;i<Schemas.Length;i++)
            {
                if ((mask & (1<<i))==0) continue;
                var target=Root(game,i); if (target==null || !Schemas[i].IsComplete) return false;
                for(int n=0;n<Schemas[i].Names.Length;n++)
                {
                    if (!Schemas[i].Read(target,n,out int value)) return false;
                    hash=ScalarStorySchema.Hash(hash,value); count++;
                }
            }
            return true;
        }
        private void Report(string role,bool match,int count,int hash,Action<string> log)
        {
            if (Environment.GetEnvironmentVariable("WSS_PARITY_PROBE")!="1" || Time.unscaledTime<_nextLog) return;
            _nextLog=Time.unscaledTime+5;
            log?.Invoke("StoryParity role="+role+" scene="+SceneManager.GetActiveScene().name+" result="+(match?"PASS":"FAIL")+" fields="+count+" hash="+hash);
        }
    }
}
