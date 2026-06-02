using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class VoiceChatSync
    {
        private const int SampleRate = 16000;
        private const int FrameSamples = 640;
        private const int MicLoopSeconds = 2;
        private const int MaxFramesPerUpdate = 3;
        private const float RemoteLevelDecaySeconds = 0.6f;
        private const byte HostRole = 1;
        private const byte ClientRole = 2;

        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly string _role;
        private readonly Action<string> _sessionLogWrite;

        private string _device;
        private AudioClip _micClip;
        private int _lastMicPosition;
        private int _voiceSeq;
        private bool _micUnavailableLogged;
        private bool _micStartedLogged;
        private float[] _sampleBuffer;
        private byte[] _encodedBuffer;
        private GameObject _playbackRoot;
        private AudioSource _playbackSource;
        private int _lastRemoteSeq;
        private float _lastRemoteFrameTime;
        private long _lastRemoteFrameMs;
        private Texture2D _panelTexture;
        private Texture2D _barTexture;
        private GUIStyle _labelStyle;
        private GUIStyle _smallStyle;

        public VoiceChatSync(ManualLogSource logger, Settings settings, string role, Action<string> sessionLogWrite)
        {
            _logger = logger;
            _settings = settings;
            _role = string.IsNullOrEmpty(role) ? "peer" : role;
            _sessionLogWrite = sessionLogWrite;
            _sampleBuffer = new float[FrameSamples];
            _encodedBuffer = new byte[FrameSamples];
        }

        public float LocalLevel01 { get; private set; }
        public float RemoteLevel01 { get; private set; }
        public bool RemoteIsLoud => RemoteLevel01 >= ShoutThreshold && Time.realtimeSinceStartup - _lastRemoteFrameTime <= RemoteLevelDecaySeconds;

        public string Summary
        {
            get
            {
                if (!IsEnabled())
                {
                    return "Voice: off";
                }

                var rxAge = _lastRemoteFrameMs > 0
                    ? (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastRemoteFrameMs).ToString()
                    : "-";
                return "Voice: local=" + FormatLevel(LocalLevel01) +
                       " remote=" + FormatLevel(RemoteLevel01) +
                       " rx=" + rxAge + "ms" +
                       " loud=" + (RemoteIsLoud ? "yes" : "no");
            }
        }

        private float SendThreshold
        {
            get
            {
                return _settings != null && _settings.CoopVoiceSendThreshold != null
                    ? Mathf.Clamp01(_settings.CoopVoiceSendThreshold.Value)
                    : 0.012f;
            }
        }

        private float ShoutThreshold
        {
            get
            {
                return _settings != null && _settings.CoopVoiceShoutThreshold != null
                    ? Mathf.Clamp01(_settings.CoopVoiceShoutThreshold.Value)
                    : 0.42f;
            }
        }

        public void UpdateLocalCapture(
            int sessionId,
            int generation,
            string sceneName,
            byte senderRole,
            Func<VoiceFrameState, bool> sendFrame)
        {
            DecayRemoteLevel();

            if (!IsEnabled() || !IsSceneVoiceAllowed(sceneName) || sendFrame == null)
            {
                StopMic();
                LocalLevel01 = 0f;
                return;
            }

            if (!EnsureMicStarted())
            {
                LocalLevel01 = 0f;
                return;
            }

            var position = Microphone.GetPosition(_device);
            if (position < 0 || _micClip == null)
            {
                return;
            }

            var available = position >= _lastMicPosition
                ? position - _lastMicPosition
                : (_micClip.samples - _lastMicPosition) + position;

            var frames = 0;
            while (available >= FrameSamples && frames < MaxFramesPerUpdate)
            {
                _micClip.GetData(_sampleBuffer, _lastMicPosition);
                _lastMicPosition = (_lastMicPosition + FrameSamples) % _micClip.samples;
                available -= FrameSamples;
                frames++;

                var level = EncodeFrame(_sampleBuffer, _encodedBuffer);
                LocalLevel01 = Mathf.Lerp(LocalLevel01, level, 0.55f);
                if (level < SendThreshold)
                {
                    continue;
                }

                var payload = new byte[FrameSamples];
                Buffer.BlockCopy(_encodedBuffer, 0, payload, 0, FrameSamples);
                var state = new VoiceFrameState
                {
                    SessionId = sessionId,
                    Generation = generation,
                    VoiceSeq = ++_voiceSeq,
                    UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SceneName = sceneName ?? string.Empty,
                    SenderRole = senderRole,
                    SampleRate = SampleRate,
                    SampleCount = FrameSamples,
                    Level = (byte)Mathf.Clamp(Mathf.RoundToInt(level * 255f), 0, 255),
                    Flags = level >= ShoutThreshold ? (byte)1 : (byte)0,
                    Pcm8 = payload
                };
                sendFrame(state);
            }
        }

        public void HandleRemoteFrame(VoiceFrameState state, Transform anchor)
        {
            if (!IsEnabled() || state.Pcm8 == null || state.Pcm8.Length == 0)
            {
                return;
            }

            if (state.VoiceSeq <= _lastRemoteSeq)
            {
                return;
            }

            _lastRemoteSeq = state.VoiceSeq;
            _lastRemoteFrameTime = Time.realtimeSinceStartup;
            _lastRemoteFrameMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            RemoteLevel01 = Mathf.Clamp01(state.Level / 255f);

            EnsurePlaybackSource(anchor);
            if (_playbackSource == null)
            {
                return;
            }

            if (anchor != null)
            {
                _playbackSource.transform.position = anchor.position;
            }

            var sampleCount = Mathf.Clamp(state.SampleCount, 1, state.Pcm8.Length);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = ((state.Pcm8[i] / 255f) * 2f) - 1f;
            }

            var sampleRate = Mathf.Clamp(state.SampleRate, 8000, 48000);
            var clip = AudioClip.Create("WSS-RemoteVoice", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            _playbackSource.PlayOneShot(clip, ResolveVolume());
            UnityEngine.Object.Destroy(clip, (sampleCount / (float)sampleRate) + 0.75f);
        }

        public void UpdatePlaybackAnchor(Transform anchor)
        {
            DecayRemoteLevel();
            if (_playbackSource != null && anchor != null)
            {
                _playbackSource.transform.position = anchor.position;
            }
        }

        public void DrawHud(string title)
        {
            if (!IsEnabled() ||
                _settings == null ||
                _settings.CoopVoiceHudEnabled == null ||
                !_settings.CoopVoiceHudEnabled.Value ||
                !IsSceneVoiceAllowed(SceneManager.GetActiveScene().name))
            {
                return;
            }

            EnsureHudStyles();
            var rect = new Rect(18f, Mathf.Max(80f, Screen.height - 104f), 226f, 70f);
            var previousColor = GUI.color;
            var previousDepth = GUI.depth;
            GUI.depth = -1000;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 18f), string.IsNullOrEmpty(title) ? "Proximity voice" : title, _labelStyle);
            DrawBar(rect.x + 72f, rect.y + 31f, 142f, 8f, LocalLevel01, "Mic");
            DrawBar(rect.x + 72f, rect.y + 49f, 142f, 8f, RemoteLevel01, "Peer");
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Shutdown()
        {
            StopMic();
            if (_playbackRoot != null)
            {
                UnityEngine.Object.Destroy(_playbackRoot);
                _playbackRoot = null;
                _playbackSource = null;
            }
        }

        private bool IsEnabled()
        {
            return _settings != null &&
                   _settings.CoopVoiceChatEnabled != null &&
                   _settings.CoopVoiceChatEnabled.Value;
        }

        private static bool IsSceneVoiceAllowed(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return !string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(sceneName, "Disclaimer", StringComparison.OrdinalIgnoreCase);
        }

        private bool EnsureMicStarted()
        {
            if (_micClip != null && Microphone.IsRecording(_device))
            {
                return true;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                if (!_micUnavailableLogged)
                {
                    _micUnavailableLogged = true;
                    Log("Co-op voice " + _role + ": no microphone devices found");
                }
                return false;
            }

            _device = Microphone.devices[0];
            try
            {
                _micClip = Microphone.Start(_device, true, MicLoopSeconds, SampleRate);
                _lastMicPosition = 0;
                if (!_micStartedLogged)
                {
                    _micStartedLogged = true;
                    Log("Co-op voice " + _role + ": microphone started device=" + _device + " sampleRate=" + SampleRate);
                }
                return _micClip != null;
            }
            catch (Exception ex)
            {
                if (!_micUnavailableLogged)
                {
                    _micUnavailableLogged = true;
                    Log("Co-op voice " + _role + ": microphone start failed reason=" + ex.Message);
                }
                _micClip = null;
                return false;
            }
        }

        private void StopMic()
        {
            if (_micClip == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(_device) && Microphone.IsRecording(_device))
                {
                    Microphone.End(_device);
                }
            }
            catch { }

            _micClip = null;
            _lastMicPosition = 0;
            LocalLevel01 = 0f;
        }

        private float EncodeFrame(float[] samples, byte[] output)
        {
            var peak = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = Mathf.Clamp(samples[i], -1f, 1f);
                var abs = Mathf.Abs(sample);
                if (abs > peak) peak = abs;
                output[i] = (byte)Mathf.Clamp(Mathf.RoundToInt((sample * 0.5f + 0.5f) * 255f), 0, 255);
            }

            return Mathf.Clamp01(peak * 2.5f);
        }

        private void EnsurePlaybackSource(Transform anchor)
        {
            if (_playbackSource != null)
            {
                UpdatePlaybackSettings();
                return;
            }

            _playbackRoot = new GameObject("WSS_RemoteVoice_" + _role);
            UnityEngine.Object.DontDestroyOnLoad(_playbackRoot);
            if (anchor != null)
            {
                _playbackRoot.transform.position = anchor.position;
            }
            _playbackSource = _playbackRoot.AddComponent<AudioSource>();
            _playbackSource.playOnAwake = false;
            UpdatePlaybackSettings();
        }

        private void UpdatePlaybackSettings()
        {
            if (_playbackSource == null) return;
            _playbackSource.spatialBlend = 1f;
            _playbackSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _playbackSource.minDistance = ResolveMinDistance();
            _playbackSource.maxDistance = ResolveMaxDistance();
            _playbackSource.volume = ResolveVolume();
        }

        private float ResolveVolume()
        {
            return _settings != null && _settings.CoopVoiceVolume != null
                ? Mathf.Clamp(_settings.CoopVoiceVolume.Value, 0f, 2f)
                : 0.85f;
        }

        private float ResolveMinDistance()
        {
            return _settings != null && _settings.CoopVoiceMinDistance != null
                ? Mathf.Clamp(_settings.CoopVoiceMinDistance.Value, 0.1f, 50f)
                : 1.2f;
        }

        private float ResolveMaxDistance()
        {
            var min = ResolveMinDistance();
            return _settings != null && _settings.CoopVoiceMaxDistance != null
                ? Mathf.Max(min + 0.5f, Mathf.Clamp(_settings.CoopVoiceMaxDistance.Value, 1f, 100f))
                : 18f;
        }

        private void DecayRemoteLevel()
        {
            if (_lastRemoteFrameTime <= 0f) return;
            var age = Time.realtimeSinceStartup - _lastRemoteFrameTime;
            if (age > RemoteLevelDecaySeconds)
            {
                RemoteLevel01 = Mathf.Lerp(RemoteLevel01, 0f, Mathf.Clamp01((age - RemoteLevelDecaySeconds) * 3f));
            }
        }

        private void EnsureHudStyles()
        {
            if (_panelTexture != null) return;

            _panelTexture = CreateTexture(new Color(0.015f, 0.016f, 0.018f, 0.72f));
            _barTexture = CreateTexture(Color.white);
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.78f, 0.55f, 0.98f) },
                clipping = TextClipping.Clip
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.84f, 0.86f, 0.88f, 0.95f) },
                clipping = TextClipping.Clip
            };
        }

        private void DrawBar(float x, float y, float width, float height, float value, string label)
        {
            GUI.Label(new Rect(x - 58f, y - 5f, 52f, 16f), label, _smallStyle);
            GUI.color = new Color(0f, 0f, 0f, 0.44f);
            GUI.DrawTexture(new Rect(x, y, width, height), _barTexture);
            GUI.color = value >= ShoutThreshold
                ? new Color(1f, 0.12f, 0.04f, 0.95f)
                : new Color(1f, 0.52f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, Mathf.Clamp01(value) * width, height), _barTexture);
            GUI.color = Color.white;
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void Log(string line)
        {
            _logger?.LogInfo(line);
            _sessionLogWrite?.Invoke(line);
        }

        private static string FormatLevel(float value)
        {
            return Mathf.Clamp01(value).ToString("0.00");
        }

        public static byte HostSenderRole => HostRole;
        public static byte ClientSenderRole => ClientRole;
    }
}
