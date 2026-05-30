using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class PizzeriaMediaSync
    {
        public const string KeyPrefix = "Media.";

        private const string TvPrefix = KeyPrefix + "TV";
        private const string RadioPrefix = KeyPrefix + "Radio";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 23;
            var tvs = FindTvs();
            for (var i = 0; i < tvs.Count; i++)
            {
                EmitTv(fullPrefix + TvPrefix + i + ".", tvs[i], emit, ref hash);
            }

            var radios = FindRadios();
            for (var i = 0; i < radios.Count; i++)
            {
                EmitRadio(fullPrefix + RadioPrefix + i + ".", radios[i], emit, ref hash);
            }

            return hash;
        }

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            SuppressLocalMediaBrains(logger);

            if (fieldName.StartsWith(TvPrefix, StringComparison.Ordinal))
            {
                return TryApplyTvFlag(fieldName.Substring(TvPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(RadioPrefix, StringComparison.Ordinal))
            {
                return TryApplyRadioFlag(fieldName.Substring(RadioPrefix.Length), value, logger);
            }

            return true;
        }

        private static void EmitTv(string prefix, PizzeriaTV tv, Action<string, int> emit, ref int hash)
        {
            if (tv == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var videoPlayer = GetFieldObject(tv, "videoPlayer");
            var videoTimerMs = Mathf.Max(0, Mathf.RoundToInt(GetFieldValue<float>(tv, "videoTimer") * 1000f));
            var currentLengthMs = Mathf.Max(0, Mathf.RoundToInt(GetFieldValue<float>(tv, "currentVideolength") * 1000f));
            var currentClip = GetVideoClip(videoPlayer);
            var newsClip = GetFieldObject(tv, "newsClip");

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", tv.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", tv.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "On", tv.isTurnedOn ? 1 : 0, emit, ref hash);
            Emit(prefix + "AdvertIndex", GetFieldValue<int>(tv, "currentAdvertIndex"), emit, ref hash);
            Emit(prefix + "IsNews", ReferenceEquals(currentClip, newsClip) ? 1 : 0, emit, ref hash);
            Emit(prefix + "ClipHash", StableObjectNameHash(currentClip), emit, ref hash);
            Emit(prefix + "TimerMs", videoTimerMs, emit, ref hash);
            Emit(prefix + "LengthMs", currentLengthMs, emit, ref hash);
            Emit(prefix + "VideoPlaying", IsVideoPlaying(videoPlayer) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TvLight", IsObjectActive(GetFieldObject(tv, "tvLight")) ? 1 : 0, emit, ref hash);
        }

        private static void EmitRadio(string prefix, PizzeriaRadio radio, Action<string, int> emit, ref int hash)
        {
            if (radio == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var audio = GetFieldValue<AudioSource>(radio, "radioAudioSource");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", radio.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", radio.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Index", GetFieldValue<int>(radio, "index"), emit, ref hash);
            Emit(prefix + "PreviousIndex", GetFieldValue<int>(radio, "previousIndex"), emit, ref hash);
            Emit(prefix + "Playing", audio != null && audio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "TimeMs", audio != null ? Mathf.Max(0, Mathf.RoundToInt(audio.time * 1000f)) : 0, emit, ref hash);
            Emit(prefix + "ClipHash", audio != null ? StableObjectNameHash(audio.clip) : 0, emit, ref hash);
        }

        private static bool TryApplyTvFlag(string localKey, int value, ManualLogSource logger)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;

            var tvs = FindTvs();
            if (index < 0 || index >= tvs.Count) return false;
            var tv = tvs[index];
            if (tv == null) return false;

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                tv.gameObject.SetActive(value != 0);
                return true;
            }

            tv.enabled = false;

            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "On", StringComparison.Ordinal))
            {
                ApplyTvOn(tv, value != 0);
                return true;
            }

            if (string.Equals(name, "AdvertIndex", StringComparison.Ordinal))
            {
                SetFieldValue(tv, "currentAdvertIndex", value);
                ApplyTvClip(tv);
                return true;
            }

            if (string.Equals(name, "IsNews", StringComparison.Ordinal))
            {
                ApplyTvClip(tv, forceNews: value != 0);
                return true;
            }

            if (string.Equals(name, "ClipHash", StringComparison.Ordinal))
            {
                ApplyTvClipByHash(tv, value, logger);
                return true;
            }

            if (string.Equals(name, "TimerMs", StringComparison.Ordinal))
            {
                var seconds = Mathf.Max(0f, value / 1000f);
                SetFieldValue(tv, "videoTimer", seconds);
                SetVideoTime(GetFieldObject(tv, "videoPlayer"), seconds);
                return true;
            }

            if (string.Equals(name, "LengthMs", StringComparison.Ordinal))
            {
                SetFieldValue(tv, "currentVideolength", Mathf.Max(0f, value / 1000f));
                return true;
            }

            if (string.Equals(name, "VideoPlaying", StringComparison.Ordinal))
            {
                ApplyVideoPlayback(GetFieldObject(tv, "videoPlayer"), value != 0 && tv.isTurnedOn);
                return true;
            }

            if (string.Equals(name, "TvLight", StringComparison.Ordinal))
            {
                SetObjectActive(GetFieldObject(tv, "tvLight"), value != 0 && tv.isTurnedOn);
                return true;
            }

            return true;
        }

        private static bool TryApplyRadioFlag(string localKey, int value, ManualLogSource logger)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;

            var radios = FindRadios();
            if (index < 0 || index >= radios.Count) return false;
            var radio = radios[index];
            if (radio == null) return false;

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                radio.gameObject.SetActive(value != 0);
                return true;
            }

            radio.enabled = false;

            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "Index", StringComparison.Ordinal))
            {
                SetFieldValue(radio, "index", value);
                ApplyRadioTrack(radio, value, logger);
                return true;
            }

            if (string.Equals(name, "PreviousIndex", StringComparison.Ordinal))
            {
                SetFieldValue(radio, "previousIndex", value);
                return true;
            }

            if (string.Equals(name, "Playing", StringComparison.Ordinal))
            {
                ApplyAudioPlayback(GetFieldValue<AudioSource>(radio, "radioAudioSource"), value != 0);
                return true;
            }

            if (string.Equals(name, "TimeMs", StringComparison.Ordinal))
            {
                var audio = GetFieldValue<AudioSource>(radio, "radioAudioSource");
                if (audio != null) audio.time = Mathf.Max(0f, value / 1000f);
                return true;
            }

            if (string.Equals(name, "ClipHash", StringComparison.Ordinal))
            {
                ApplyRadioClipByHash(radio, value, logger);
                return true;
            }

            return true;
        }

        private static void SuppressLocalMediaBrains(ManualLogSource logger)
        {
            var tvs = FindTvs();
            for (var i = 0; i < tvs.Count; i++)
            {
                if (tvs[i] != null) tvs[i].enabled = false;
            }

            var radios = FindRadios();
            for (var i = 0; i < radios.Count; i++)
            {
                if (radios[i] != null) radios[i].enabled = false;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Pizzeria media client brain suppressed tvs=" + tvs.Count +
                               " radios=" + radios.Count);
            }
        }

        private static void ApplyTvOn(PizzeriaTV tv, bool isOn)
        {
            tv.isTurnedOn = isOn;
            var renderer = GetFieldValue<MeshRenderer>(tv, "meshRenderer");
            if (renderer != null)
            {
                renderer.enabled = isOn;
                var material = GetFieldValue<Material>(tv, isOn ? "tvOnMaterial" : "tvOffMaterial");
                if (material != null) renderer.material = material;
            }

            var audio = GetFieldValue<AudioSource>(tv, "tvAudioSource");
            if (audio != null)
            {
                audio.mute = !isOn;
                if (isOn && !audio.isPlaying) audio.Play();
            }

            SetObjectActive(GetFieldObject(tv, "tvLight"), isOn);
            ApplyVideoPlayback(GetFieldObject(tv, "videoPlayer"), isOn);
            if (isOn) ApplyTvClip(tv);
        }

        private static void ApplyTvClip(PizzeriaTV tv, bool? forceNews = null)
        {
            if (tv == null) return;
            var useNews = forceNews.HasValue && forceNews.Value;
            object clip = null;
            if (useNews)
            {
                clip = GetFieldObject(tv, "newsClip");
            }
            else
            {
                var clips = GetFieldObject(tv, "advertClips") as IList;
                if (clips != null && clips.Count > 0)
                {
                    var index = Mathf.Clamp(GetFieldValue<int>(tv, "currentAdvertIndex"), 0, clips.Count - 1);
                    SetFieldValue(tv, "currentAdvertIndex", index);
                    clip = clips[index];
                }
            }

            if (clip == null) return;
            SetVideoClip(GetFieldObject(tv, "videoPlayer"), clip);
            var length = GetObjectLength(clip);
            if (length > 0f) SetFieldValue(tv, "currentVideolength", length);
            if (tv.isTurnedOn) ApplyVideoPlayback(GetFieldObject(tv, "videoPlayer"), true);
        }

        private static void ApplyTvClipByHash(PizzeriaTV tv, int expectedHash, ManualLogSource logger)
        {
            if (expectedHash == 0) return;
            var newsClip = GetFieldObject(tv, "newsClip");
            if (StableObjectNameHash(newsClip) == expectedHash)
            {
                ApplyTvClip(tv, forceNews: true);
                return;
            }

            var clips = GetFieldObject(tv, "advertClips") as IList;
            if (clips != null)
            {
                for (var i = 0; i < clips.Count; i++)
                {
                    if (StableObjectNameHash(clips[i]) != expectedHash) continue;
                    SetFieldValue(tv, "currentAdvertIndex", i);
                    ApplyTvClip(tv, forceNews: false);
                    return;
                }
            }

            logger?.LogWarning("Pizzeria media TV clip hash missing path=" +
                               NetPath.GetPath(tv.transform) +
                               " hash=" + expectedHash);
        }

        private static void ApplyRadioTrack(PizzeriaRadio radio, int index, ManualLogSource logger)
        {
            var tracks = GetFieldValue<AudioClip[]>(radio, "tracks");
            if (tracks == null || tracks.Length == 0) return;
            index = Mathf.Clamp(index, 0, tracks.Length - 1);
            SetFieldValue(radio, "index", index);

            var audio = GetFieldValue<AudioSource>(radio, "radioAudioSource");
            if (audio == null) return;
            audio.clip = tracks[index];
            SetFieldValue(radio, "currentClipLength", tracks[index] != null ? tracks[index].length : 0f);
            if (!audio.isPlaying) audio.Play();
        }

        private static void ApplyRadioClipByHash(PizzeriaRadio radio, int expectedHash, ManualLogSource logger)
        {
            if (expectedHash == 0) return;
            var tracks = GetFieldValue<AudioClip[]>(radio, "tracks");
            if (tracks == null) return;
            for (var i = 0; i < tracks.Length; i++)
            {
                if (StableObjectNameHash(tracks[i]) != expectedHash) continue;
                ApplyRadioTrack(radio, i, logger);
                return;
            }

            logger?.LogWarning("Pizzeria media radio clip hash missing path=" +
                               NetPath.GetPath(radio.transform) +
                               " hash=" + expectedHash);
        }

        private static List<PizzeriaTV> FindTvs()
        {
            var list = new List<PizzeriaTV>(UnityEngine.Object.FindObjectsOfType<PizzeriaTV>());
            list.Sort((left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
            return list;
        }

        private static List<PizzeriaRadio> FindRadios()
        {
            var list = new List<PizzeriaRadio>(UnityEngine.Object.FindObjectsOfType<PizzeriaRadio>());
            list.Sort((left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
            return list;
        }

        private static bool TryParseIndexedKey(string value, out int index, out string name)
        {
            index = -1;
            name = string.Empty;
            if (string.IsNullOrEmpty(value) || value[0] != '0' && !char.IsDigit(value[0]))
            {
                return false;
            }

            var dot = value.IndexOf('.');
            if (dot <= 0) return false;
            if (!int.TryParse(value.Substring(0, dot), out index)) return false;
            name = value.Substring(dot + 1);
            return true;
        }

        private static object GetVideoClip(object videoPlayer)
        {
            if (videoPlayer == null) return null;
            return GetPropertyValue(videoPlayer, "clip");
        }

        private static void SetVideoClip(object videoPlayer, object clip)
        {
            if (videoPlayer == null || clip == null) return;
            SetPropertyValue(videoPlayer, "clip", clip);
        }

        private static bool IsVideoPlaying(object videoPlayer)
        {
            if (videoPlayer == null) return false;
            var value = GetPropertyValue(videoPlayer, "isPlaying");
            return value is bool playing && playing;
        }

        private static void ApplyVideoPlayback(object videoPlayer, bool playing)
        {
            if (videoPlayer == null) return;
            InvokeNoArg(videoPlayer, playing ? "Play" : "Pause");
        }

        private static void SetVideoTime(object videoPlayer, float seconds)
        {
            if (videoPlayer == null) return;
            SetPropertyValue(videoPlayer, "time", (double)seconds);
        }

        private static void ApplyAudioPlayback(AudioSource audio, bool playing)
        {
            if (audio == null) return;
            if (playing)
            {
                if (!audio.isPlaying) audio.Play();
            }
            else
            {
                audio.Stop();
            }
        }

        private static bool IsObjectActive(object target)
        {
            var go = GetGameObject(target);
            return go != null && go.activeSelf;
        }

        private static void SetObjectActive(object target, bool active)
        {
            var go = GetGameObject(target);
            if (go != null) go.SetActive(active);
        }

        private static GameObject GetGameObject(object target)
        {
            if (target is GameObject go) return go;
            if (target is Component component) return component.gameObject;
            return null;
        }

        private static float GetObjectLength(object target)
        {
            if (target == null) return 0f;
            var value = GetPropertyValue(target, "length");
            if (value is double doubleValue) return (float)doubleValue;
            if (value is float floatValue) return floatValue;
            return 0f;
        }

        private static int StableObjectNameHash(object target)
        {
            if (target == null) return 0;
            string name;
            if (target is UnityEngine.Object unityObject)
            {
                name = unityObject.name;
            }
            else
            {
                name = target.ToString();
            }

            unchecked
            {
                var hash = 2166136261u;
                if (!string.IsNullOrEmpty(name))
                {
                    for (var i = 0; i < name.Length; i++)
                    {
                        hash ^= char.ToUpperInvariant(name[i]);
                        hash *= 16777619u;
                    }
                }

                return (int)hash;
            }
        }

        private static void Emit(string key, int value, Action<string, int> emit, ref int hash)
        {
            emit(key, value);
            unchecked
            {
                hash = hash * 31 + (key != null ? key.GetHashCode() : 0);
                hash = hash * 31 + value;
            }
        }

        private static object GetFieldObject(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;
            var field = GetField(target.GetType(), fieldName);
            if (field == null) return null;

            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            var value = GetFieldObject(target, fieldName);
            if (value is T typed) return typed;
            return default(T);
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return;
            var field = GetField(target.GetType(), fieldName);
            if (field == null) return;

            try
            {
                field.SetValue(target, value);
            }
            catch
            {
            }
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName)) return null;
            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                return property != null ? property.GetValue(target, null) : null;
            }
            catch
            {
                return null;
            }
        }

        private static void SetPropertyValue(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrEmpty(propertyName)) return;
            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property != null && property.CanWrite) property.SetValue(target, value, null);
            }
            catch
            {
            }
        }

        private static void InvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            try
            {
                var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                if (method != null) method.Invoke(target, null);
            }
            catch
            {
            }
        }

        private static FieldInfo GetField(Type type, string name)
        {
            var key = (type != null ? type.FullName : string.Empty) + "." + name;
            if (FieldCache.TryGetValue(key, out var field))
            {
                return field;
            }

            var current = type;
            while (current != null)
            {
                field = current.GetField(name, FieldFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    FieldCache[key] = field;
                    return field;
                }

                current = current.BaseType;
            }

            FieldCache[key] = null;
            return null;
        }
    }
}
