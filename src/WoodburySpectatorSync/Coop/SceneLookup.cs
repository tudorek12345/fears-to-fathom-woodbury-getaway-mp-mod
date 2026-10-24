using System;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class SceneLookup
    {
        private static readonly BatchLookupCache Cache = new BatchLookupCache();
        private static float _nextReport;
        public static IDisposable Begin() => Cache.Begin();
        public static void Invalidate() => Cache.Invalidate();
        public static T First<T>() where T : UnityEngine.Object => Cache.Get(() => UnityEngine.Object.FindObjectOfType<T>());
        public static T[] All<T>() where T : UnityEngine.Object => Cache.Get(() => UnityEngine.Object.FindObjectsOfType<T>());
        public static void Report(Action<string> log)
        {
            if (Environment.GetEnvironmentVariable("WSS_PARITY_PROBE") != "1" || Time.unscaledTime < _nextReport) return;
            _nextReport = Time.unscaledTime + 10f;
            log?.Invoke("SceneLookup requests=" + Cache.Requests + " searches=" + Cache.Searches + " reused=" + (Cache.Requests - Cache.Searches));
        }
    }
}
