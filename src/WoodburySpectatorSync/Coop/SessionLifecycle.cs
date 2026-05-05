using System;
using System.Collections.Generic;
using BepInEx.Logging;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class SessionLifecycle
    {
        private const float PreLiveLogIntervalSeconds = 5f;

        private readonly string _role;
        private readonly ManualLogSource _logger;
        private readonly Action<string> _sessionLog;
        private readonly Dictionary<string, long> _preLiveCounts = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _nextPreLiveLogAt = new Dictionary<string, float>(StringComparer.Ordinal);

        private SessionState _state = SessionState.Disconnected;
        private int _currentSessionId;
        private int _currentGeneration;
        private long _lastTransitionMs;

        public SessionLifecycle(string role, ManualLogSource logger, Action<string> sessionLog)
        {
            _role = string.IsNullOrEmpty(role) ? "session" : role;
            _logger = logger;
            _sessionLog = sessionLog;
        }

        public SessionState State { get { return _state; } }
        public int CurrentSessionId { get { return _currentSessionId; } }
        public int CurrentGeneration { get { return _currentGeneration; } }
        public bool IsLive { get { return _state == SessionState.Live; } }

        public bool IsAtLeast(SessionState floor)
        {
            return (int)_state >= (int)floor;
        }

        public bool TryTransition(SessionState next, string reason, string sceneName, int generation, int sessionId, long nowMs)
        {
            if (next == _state)
            {
                _currentGeneration = generation;
                _currentSessionId = sessionId;
                return false;
            }

            if (!IsLegalTransition(_state, next))
            {
                Log("Co-op session " + _role + ": illegal transition " + _state + " -> " + next +
                    " reason=" + FormatValue(reason) +
                    " scene=" + FormatValue(sceneName) +
                    " gen=" + generation +
                    " sid=" + sessionId);
                return false;
            }

            var previous = _state;
            var dt = _lastTransitionMs > 0 ? Math.Max(0, nowMs - _lastTransitionMs) : 0;
            _state = next;
            _currentGeneration = generation;
            _currentSessionId = sessionId;
            _lastTransitionMs = nowMs;

            var line = "Co-op session " + _role + ": " + previous + " -> " + next +
                       " reason=" + FormatValue(reason) +
                       " scene=" + FormatValue(sceneName) +
                       " gen=" + generation +
                       " sid=" + sessionId +
                       " dt=" + dt + "ms";
            Log(line);

            if (next == SessionState.Disconnected)
            {
                _preLiveCounts.Clear();
                _nextPreLiveLogAt.Clear();
            }

            return true;
        }

        public bool TryTransition(SessionState next, string reason, long nowMs)
        {
            return TryTransition(next, reason, string.Empty, _currentGeneration, _currentSessionId, nowMs);
        }

        public void SetSessionId(int sessionId)
        {
            _currentSessionId = sessionId;
        }

        public void SetGeneration(int generation)
        {
            _currentGeneration = generation;
        }

        public void ForceDisconnect(string reason, long nowMs)
        {
            TryTransition(SessionState.Disconnected, reason, string.Empty, _currentGeneration, _currentSessionId, nowMs);
        }

        public void NoteDrop(string category, string detail, float nowSeconds)
        {
            MessageType type;
            if (!Enum.TryParse(category ?? string.Empty, out type))
            {
                type = MessageType.PlayerInput;
            }

            NotePreLive("TCP", "drop", type, detail, string.Empty, _currentGeneration, _currentSessionId, nowSeconds);
        }

        public void NotePreLive(string transport, string disposition, MessageType type, string reason, string sceneName, int generation, int sessionId, float nowSeconds)
        {
            var key = (transport ?? "?") + "|" + (disposition ?? "drop") + "|" + type + "|" + (reason ?? string.Empty);
            long count;
            _preLiveCounts.TryGetValue(key, out count);
            count++;
            _preLiveCounts[key] = count;

            float nextLogAt;
            if (_nextPreLiveLogAt.TryGetValue(key, out nextLogAt) && nowSeconds < nextLogAt)
            {
                return;
            }

            _nextPreLiveLogAt[key] = nowSeconds + PreLiveLogIntervalSeconds;
            var line = "Co-op session " + _role + ": " +
                       FormatValue(transport) + " pre-Live " + FormatValue(disposition) +
                       " type=" + type +
                       " count=" + count +
                       " state=" + _state +
                       " scene=" + FormatValue(sceneName) +
                       " gen=" + generation +
                       " sid=" + sessionId;
            if (!string.IsNullOrEmpty(reason))
            {
                line += " reason=" + reason;
            }
            Log(line);
        }

        private void Log(string line)
        {
            if (_logger != null) _logger.LogInfo(line);
            if (_sessionLog != null) _sessionLog(line);
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static bool IsLegalTransition(SessionState from, SessionState to)
        {
            if (to == SessionState.Disconnected) return true;
            switch (from)
            {
                case SessionState.Disconnected:
                    return to == SessionState.Connecting;
                case SessionState.Connecting:
                    return to == SessionState.Hello;
                case SessionState.Hello:
                    return to == SessionState.Connected;
                case SessionState.Connected:
                    return to == SessionState.SceneSyncing;
                case SessionState.SceneSyncing:
                    return to == SessionState.SceneReady || to == SessionState.Reconnecting;
                case SessionState.SceneReady:
                    return to == SessionState.SnapshotApplying || to == SessionState.SceneSyncing;
                case SessionState.SnapshotApplying:
                    return to == SessionState.Live || to == SessionState.Reconnecting || to == SessionState.SceneSyncing;
                case SessionState.Live:
                    return to == SessionState.Reconnecting || to == SessionState.SceneSyncing;
                case SessionState.Reconnecting:
                    return to == SessionState.Connecting;
                default:
                    return false;
            }
        }
    }
}
