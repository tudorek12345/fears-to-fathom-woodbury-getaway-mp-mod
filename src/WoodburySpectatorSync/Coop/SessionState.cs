namespace WoodburySpectatorSync.Coop
{
    internal enum SessionState
    {
        Disconnected = 0,
        Connecting = 1,
        Hello = 2,
        Connected = 3,
        SceneSyncing = 4,
        SceneReady = 5,
        SnapshotApplying = 6,
        Live = 7,
        Reconnecting = 8
    }
}
