using System;

namespace WoodburySpectatorSync.Coop
{
    internal interface ICoopSceneAdapter
    {
        string Name { get; }
        bool MatchesScene(string sceneName);
        void OnSceneEnter();
    }

    internal interface ICoopHostSceneAdapter : ICoopSceneAdapter
    {
        void EmitHostSnapshot(long nowMs);
    }

    internal interface ICoopClientSceneAdapter : ICoopSceneAdapter
    {
        bool TryApplyStoryFlag(string key, int value, bool allowDefer);
    }

    internal sealed class DelegateHostSceneAdapter : ICoopHostSceneAdapter
    {
        private readonly Func<string, bool> _match;
        private readonly Action _onSceneEnter;
        private readonly Action<long> _emitHostSnapshot;

        public DelegateHostSceneAdapter(string name, Func<string, bool> match, Action onSceneEnter, Action<long> emitHostSnapshot)
        {
            Name = name ?? string.Empty;
            _match = match ?? (_ => false);
            _onSceneEnter = onSceneEnter ?? (() => { });
            _emitHostSnapshot = emitHostSnapshot ?? (_ => { });
        }

        public string Name { get; }

        public bool MatchesScene(string sceneName)
        {
            return _match(sceneName ?? string.Empty);
        }

        public void OnSceneEnter()
        {
            _onSceneEnter();
        }

        public void EmitHostSnapshot(long nowMs)
        {
            _emitHostSnapshot(nowMs);
        }
    }

    internal sealed class DelegateClientSceneAdapter : ICoopClientSceneAdapter
    {
        private readonly Func<string, bool> _match;
        private readonly Action _onSceneEnter;
        private readonly Func<string, int, bool, bool> _applyStoryFlag;

        public DelegateClientSceneAdapter(string name, Func<string, bool> match, Action onSceneEnter, Func<string, int, bool, bool> applyStoryFlag)
        {
            Name = name ?? string.Empty;
            _match = match ?? (_ => false);
            _onSceneEnter = onSceneEnter ?? (() => { });
            _applyStoryFlag = applyStoryFlag ?? ((_, __, ___) => false);
        }

        public string Name { get; }

        public bool MatchesScene(string sceneName)
        {
            return _match(sceneName ?? string.Empty);
        }

        public void OnSceneEnter()
        {
            _onSceneEnter();
        }

        public bool TryApplyStoryFlag(string key, int value, bool allowDefer)
        {
            return _applyStoryFlag(key ?? string.Empty, value, allowDefer);
        }
    }
}
