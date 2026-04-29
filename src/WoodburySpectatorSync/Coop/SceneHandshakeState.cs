using System;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class SceneHandshakeState
    {
        public int Generation { get; private set; }
        public int ReadyGeneration { get; private set; } = -1;
        public bool SnapshotSentForReadyGeneration { get; private set; }
        public bool AwaitingReady { get; private set; }
        public string SceneName { get; private set; } = string.Empty;
        public long LastSceneRequestMs { get; private set; }
        public long LastSceneReadyMs { get; private set; }

        public void Begin(int generation, string sceneName, bool awaitingReady, long nowMs)
        {
            Generation = generation;
            ReadyGeneration = -1;
            SnapshotSentForReadyGeneration = false;
            AwaitingReady = awaitingReady;
            SceneName = sceneName ?? string.Empty;
            LastSceneRequestMs = nowMs;
            LastSceneReadyMs = 0;
        }

        public void MarkSceneRequest(long nowMs)
        {
            LastSceneRequestMs = nowMs;
            if (ReadyGeneration != Generation)
            {
                AwaitingReady = true;
            }
        }

        public bool AcceptReady(string sceneName, long nowMs)
        {
            if (!string.Equals(sceneName ?? string.Empty, SceneName, StringComparison.Ordinal))
            {
                return false;
            }

            if (ReadyGeneration == Generation)
            {
                LastSceneReadyMs = nowMs;
                AwaitingReady = false;
                return false;
            }

            ReadyGeneration = Generation;
            SnapshotSentForReadyGeneration = false;
            LastSceneReadyMs = nowMs;
            AwaitingReady = false;
            return true;
        }

        public bool TryMarkSnapshotSentForReadyGeneration()
        {
            if (ReadyGeneration != Generation)
            {
                return false;
            }

            if (SnapshotSentForReadyGeneration)
            {
                return false;
            }

            SnapshotSentForReadyGeneration = true;
            return true;
        }

        public void ResetReady()
        {
            ReadyGeneration = -1;
            SnapshotSentForReadyGeneration = false;
            AwaitingReady = false;
            LastSceneRequestMs = 0;
            LastSceneReadyMs = 0;
        }
    }
}
