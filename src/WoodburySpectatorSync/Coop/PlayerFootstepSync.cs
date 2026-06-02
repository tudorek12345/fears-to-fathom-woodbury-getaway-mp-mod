using System;
using System.Globalization;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class PlayerFootstepSync
    {
        private const string FootstepEventKind = "PlayerFootstep";
        private const string FootstepEventId = "Coop.PlayerFootstep";
        private const float RemoteMinDistance = 0.8f;
        private const float RemoteMaxDistance = 13f;

        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly string _role;
        private readonly Action<string> _sessionLogWrite;

        private bool _hasLocalSample;
        private Vector3 _lastLocalPosition;
        private float _lastLocalTime;
        private float _nextLocalStepTime;
        private float _localDistance;
        private int _localSeq;
        private int _lastRemoteSeq;
        private int _localStepCount;
        private int _remoteStepCount;
        private long _lastRemoteStepMs;
        private long _lastLocalStepMs;
        private GameObject _playbackRoot;
        private AudioSource _playbackSource;
        private AudioClip[] _cachedClips;
        private float _nextClipRefreshTime;

        public PlayerFootstepSync(ManualLogSource logger, Settings settings, string role, Action<string> sessionLogWrite)
        {
            _logger = logger;
            _settings = settings;
            _role = string.IsNullOrEmpty(role) ? "peer" : role;
            _sessionLogWrite = sessionLogWrite;
        }

        public string Summary
        {
            get
            {
                if (!IsEnabled())
                {
                    return "Footsteps: off";
                }

                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return "Footsteps: local=" + _localStepCount +
                       " remote=" + _remoteStepCount +
                       " rx=" + FormatAge(nowMs, _lastRemoteStepMs);
            }
        }

        public void UpdateLocal(
            int sessionId,
            int generation,
            string sceneName,
            byte playerId,
            Vector3 position,
            PlayerInputState input,
            Func<SceneEventState, bool> sendEvent)
        {
            if (!IsEnabled() || !IsSceneAllowed(sceneName) || sendEvent == null)
            {
                ResetLocal();
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (!_hasLocalSample)
            {
                _hasLocalSample = true;
                _lastLocalPosition = position;
                _lastLocalTime = now;
                return;
            }

            var dt = Mathf.Max(0.001f, now - _lastLocalTime);
            var delta = position - _lastLocalPosition;
            delta.y = 0f;
            var distance = delta.magnitude;
            var speed = distance / dt;
            _lastLocalPosition = position;
            _lastLocalTime = now;

            var inputMagnitude = Mathf.Abs(input.MoveX) + Mathf.Abs(input.MoveY);
            if ((inputMagnitude < 0.1f && speed < 0.32f) || !IsGrounded(position))
            {
                _localDistance = Mathf.Max(0f, _localDistance - dt * 0.75f);
                return;
            }

            var sprinting = input.Sprint && input.MoveY > 0.1f;
            var crouching = input.Crouch;
            var interval = crouching ? 0.58f : (sprinting ? 0.3f : 0.43f);
            var requiredDistance = crouching ? 0.42f : (sprinting ? 0.78f : 0.56f);
            _localDistance += distance;
            if (now < _nextLocalStepTime && _localDistance < requiredDistance)
            {
                return;
            }

            _nextLocalStepTime = now + interval;
            _localDistance = 0f;
            var volume = Mathf.Clamp01((speed - 0.25f) / 3.2f);
            if (sprinting) volume = Mathf.Max(volume, 0.78f);
            if (crouching) volume *= 0.55f;
            if (volume < 0.15f) volume = 0.15f;

            var flags = 0;
            if (crouching) flags |= 1;
            if (sprinting) flags |= 2;
            flags |= 4;

            var state = new SceneEventState
            {
                SessionId = sessionId,
                Generation = generation,
                EventSeq = ++_localSeq,
                UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SceneName = sceneName ?? string.Empty,
                EventId = FootstepEventId,
                EventKind = FootstepEventKind,
                TargetPath = string.Empty,
                Payload = BuildPayload(position, _role),
                IntValue = playerId,
                FloatValue = volume,
                Flags = flags
            };

            if (sendEvent(state))
            {
                _localStepCount++;
                _lastLocalStepMs = state.UnixTimeMs;
            }
        }

        public bool TryHandleRemote(SceneEventState state, Transform fallbackAnchor)
        {
            if (!IsEnabled() || !IsFootstepEvent(state))
            {
                return false;
            }

            if (state.EventSeq <= _lastRemoteSeq)
            {
                return true;
            }

            _lastRemoteSeq = state.EventSeq;
            if (!TryParsePosition(state.Payload, out var position))
            {
                if (fallbackAnchor == null)
                {
                    return true;
                }
                position = fallbackAnchor.position;
            }

            PlayAt(position, Mathf.Clamp01(state.FloatValue));
            _remoteStepCount++;
            _lastRemoteStepMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return true;
        }

        public void Shutdown()
        {
            if (_playbackRoot != null)
            {
                UnityEngine.Object.Destroy(_playbackRoot);
                _playbackRoot = null;
                _playbackSource = null;
            }
        }

        private void PlayAt(Vector3 position, float volume)
        {
            var clip = ResolveFootstepClip();
            if (clip == null)
            {
                return;
            }

            EnsurePlaybackSource();
            if (_playbackSource == null)
            {
                return;
            }

            _playbackSource.transform.position = position;
            _playbackSource.PlayOneShot(clip, Mathf.Clamp01(volume) * ResolveVolume());
        }

        private AudioClip ResolveFootstepClip()
        {
            if (_cachedClips != null && _cachedClips.Length > 0 && Time.realtimeSinceStartup < _nextClipRefreshTime)
            {
                return _cachedClips[UnityEngine.Random.Range(0, _cachedClips.Length)];
            }

            _nextClipRefreshTime = Time.realtimeSinceStartup + 5f;
            _cachedClips = null;
            var controllers = UnityEngine.Object.FindObjectsOfType<FirstPersonController>();
            foreach (var controller in controllers)
            {
                if (controller == null || controller.GroundTypes == null)
                {
                    continue;
                }

                foreach (var ground in controller.GroundTypes)
                {
                    if (ground != null && ground.footstepSounds != null && ground.footstepSounds.Length > 0)
                    {
                        _cachedClips = ground.footstepSounds;
                        return _cachedClips[UnityEngine.Random.Range(0, _cachedClips.Length)];
                    }
                }
            }

            return null;
        }

        private void EnsurePlaybackSource()
        {
            if (_playbackSource != null)
            {
                UpdatePlaybackSource();
                return;
            }

            _playbackRoot = new GameObject("WSS_RemoteFootsteps_" + _role);
            UnityEngine.Object.DontDestroyOnLoad(_playbackRoot);
            _playbackSource = _playbackRoot.AddComponent<AudioSource>();
            _playbackSource.playOnAwake = false;
            UpdatePlaybackSource();
        }

        private void UpdatePlaybackSource()
        {
            if (_playbackSource == null) return;
            _playbackSource.spatialBlend = 1f;
            _playbackSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _playbackSource.minDistance = RemoteMinDistance;
            _playbackSource.maxDistance = RemoteMaxDistance;
            _playbackSource.volume = ResolveVolume();
        }

        public static bool IsFootstepEvent(SceneEventState state)
        {
            return string.Equals(state.EventKind, FootstepEventKind, StringComparison.Ordinal) ||
                   string.Equals(state.EventId, FootstepEventId, StringComparison.Ordinal);
        }

        private static string BuildPayload(Vector3 position, string role)
        {
            return "role=" + Escape(role) +
                   ";x=" + position.x.ToString("R", CultureInfo.InvariantCulture) +
                   ";y=" + position.y.ToString("R", CultureInfo.InvariantCulture) +
                   ";z=" + position.z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool TryParsePosition(string payload, out Vector3 position)
        {
            position = Vector3.zero;
            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            var x = 0f;
            var y = 0f;
            var z = 0f;
            var hasX = false;
            var hasY = false;
            var hasZ = false;
            var parts = payload.Split(';');
            foreach (var part in parts)
            {
                var equals = part.IndexOf('=');
                if (equals <= 0) continue;
                var key = part.Substring(0, equals);
                var value = part.Substring(equals + 1);
                float parsed;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    continue;
                }

                if (string.Equals(key, "x", StringComparison.Ordinal))
                {
                    x = parsed;
                    hasX = true;
                }
                else if (string.Equals(key, "y", StringComparison.Ordinal))
                {
                    y = parsed;
                    hasY = true;
                }
                else if (string.Equals(key, "z", StringComparison.Ordinal))
                {
                    z = parsed;
                    hasZ = true;
                }
            }

            if (!hasX || !hasY || !hasZ)
            {
                return false;
            }

            position = new Vector3(x, y, z);
            return true;
        }

        private bool IsEnabled()
        {
            return _settings != null &&
                   _settings.CoopFootstepSyncEnabled != null &&
                   _settings.CoopFootstepSyncEnabled.Value;
        }

        private float ResolveVolume()
        {
            return _settings != null && _settings.CoopFootstepVolume != null
                ? Mathf.Clamp(_settings.CoopFootstepVolume.Value, 0f, 2f)
                : 0.72f;
        }

        private static bool IsSceneAllowed(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return !string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(sceneName, "Disclaimer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGrounded(Vector3 position)
        {
            return Physics.Raycast(position + Vector3.up * 0.35f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
        }

        private void ResetLocal()
        {
            _hasLocalSample = false;
            _localDistance = 0f;
            _nextLocalStepTime = 0f;
        }

        private static string FormatAge(long nowMs, long lastMs)
        {
            if (lastMs <= 0) return "n/a";
            var delta = Math.Max(0, nowMs - lastMs);
            return delta >= 1000 ? (delta / 1000f).ToString("0.0") + "s" : delta + "ms";
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace(";", "_").Replace("=", "_");
        }
    }
}
