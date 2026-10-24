using System.Collections.Concurrent;

// Configuration/log sinks only; tests compile and execute the production transports.
namespace BepInEx { public static class Paths { public static string PluginPath; } }
namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        public readonly ConcurrentQueue<string> Lines = new ConcurrentQueue<string>();
        public void LogInfo(object value) => Lines.Enqueue(value.ToString());
        public void LogWarning(object value) => Lines.Enqueue(value.ToString());
        public void LogError(object value) => Lines.Enqueue(value.ToString());
    }
}
namespace WoodburySpectatorSync.Config
{
    public enum CoopTransport { Lan, Spacewar }
    public sealed class Entry<T> { public T Value; public Entry(T value) { Value = value; } }
    public sealed class Settings
    {
        public Entry<CoopTransport> Transport = new Entry<CoopTransport>(CoopTransport.Lan);
        public Entry<string> HostBindIP = new Entry<string>("127.0.0.1"), SpectatorHostIP = new Entry<string>("127.0.0.1");
        public Entry<int> HostPort = new Entry<int>(0), UdpPort = new Entry<int>(0), SendHz = new Entry<int>(20);
        public Entry<bool> UdpEnabled = new Entry<bool>(false);
        public Entry<ulong> SpacewarLobbyId = new Entry<ulong>(900);
    }
}
