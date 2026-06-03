using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class GenericNpcEntry
    {
        public readonly string IdPrefix;
        public readonly string Kind;
        public readonly string TypeName;
        public readonly bool Multiple;

        public GenericNpcEntry(string idPrefix, string kind, string typeName, bool multiple = false)
        {
            IdPrefix = idPrefix ?? string.Empty;
            Kind = kind ?? string.Empty;
            TypeName = typeName ?? string.Empty;
            Multiple = multiple;
        }
    }

    internal sealed class GenericNpcRecord
    {
        public readonly string Id;
        public readonly string Kind;
        public readonly Component Component;
        public readonly string Path;

        public GenericNpcRecord(string id, string kind, Component component, string path)
        {
            Id = id ?? string.Empty;
            Kind = kind ?? string.Empty;
            Component = component;
            Path = path ?? string.Empty;
        }
    }

    internal sealed class GenericNpcRegistry
    {
        private readonly ManualLogSource _logger;
        private readonly Action<string> _sessionLogWrite;
        private readonly string _side;
        private readonly string _scenePrefix;
        private readonly GenericNpcEntry[] _entries;
        private readonly Dictionary<string, GenericNpcRecord> _records = new Dictionary<string, GenericNpcRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _lastAppliedSeqById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _lastAppliedMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _nextMissingLogMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _lastActiveById = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector3> _smoothedPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, Quaternion> _smoothedRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private readonly HashSet<string> _loggedPaths = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _suppressedLogged = new HashSet<string>(StringComparer.Ordinal);
        private string _lastSceneName = string.Empty;
        private float _lastRefreshTime = -10f;

        public GenericNpcRegistry(
            ManualLogSource logger,
            Action<string> sessionLogWrite,
            string side,
            string scenePrefix,
            GenericNpcEntry[] entries)
        {
            _logger = logger;
            _sessionLogWrite = sessionLogWrite;
            _side = string.IsNullOrEmpty(side) ? "client" : side;
            _scenePrefix = scenePrefix ?? string.Empty;
            _entries = entries ?? new GenericNpcEntry[0];
        }

        public IEnumerable<GenericNpcRecord> Records
        {
            get { return _records.Values; }
        }

        public void Refresh(string sceneName, bool force = false)
        {
            sceneName = sceneName ?? string.Empty;
            if (string.IsNullOrEmpty(_scenePrefix) ||
                sceneName.IndexOf(_scenePrefix, StringComparison.OrdinalIgnoreCase) < 0)
            {
                _records.Clear();
                _lastSceneName = sceneName;
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (!force &&
                string.Equals(_lastSceneName, sceneName, StringComparison.Ordinal) &&
                now - _lastRefreshTime < 1f)
            {
                return;
            }

            _lastSceneName = sceneName;
            _lastRefreshTime = now;
            _records.Clear();

            for (var i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.TypeName)) continue;

                var found = FindLoadedComponentsByTypeName(entry.TypeName);
                var suffixCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var foundIndex = 0; foundIndex < found.Count; foundIndex++)
                {
                    var component = found[foundIndex];
                    if (component == null || component.transform == null) continue;

                    var path = NetPath.GetPath(component.transform);
                    var id = entry.Multiple
                        ? entry.IdPrefix + "/" + StableComponentSuffix(component, suffixCounts)
                        : entry.IdPrefix;
                    if (_records.ContainsKey(id)) continue;

                    _records[id] = new GenericNpcRecord(id, entry.Kind, component, path);
                    if (_loggedPaths.Add(id + "|" + path))
                    {
                        LogInfo("NPCRegistry found npcId=" + id +
                                " path=" + (string.IsNullOrEmpty(path) ? "-" : path) +
                                " type=" + component.GetType().Name +
                                " critical=" + BoolText(component.gameObject.activeInHierarchy));
                    }

                    if (!entry.Multiple)
                    {
                        break;
                    }
                }
            }
        }

        public bool TryBuildState(
            GenericNpcRecord record,
            int sessionId,
            int generation,
            int npcSeq,
            long nowMs,
            Vector3 velocity,
            out NpcBrainState state)
        {
            state = default(NpcBrainState);
            if (record == null || record.Component == null || record.Component.transform == null)
            {
                return false;
            }

            var transform = record.Component.transform;
            var active = record.Component.gameObject.activeInHierarchy;
            var visible = active && HasVisibleRenderer(transform);
            var animator = transform.GetComponentInChildren<Animator>(true);
            var nav = transform.GetComponentInChildren<NavMeshAgent>(true);
            var navDestination = Vector3.zero;
            var remaining = 0f;
            var navEnabled = false;
            var navMoving = false;
            if (nav != null)
            {
                navEnabled = nav.enabled && nav.gameObject.activeInHierarchy;
                if (navEnabled)
                {
                    try
                    {
                        navDestination = nav.destination;
                        remaining = nav.remainingDistance;
                        navMoving = nav.velocity.sqrMagnitude > 0.0025f || remaining > 0.05f;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            var animStateHash = 0;
            var animNextStateHash = 0;
            var animTransition = false;
            var animNormalizedTime = 0f;
            var animSpeed = 1f;
            if (animator != null && animator.layerCount > 0)
            {
                try
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    animStateHash = info.fullPathHash != 0 ? info.fullPathHash : info.shortNameHash;
                    animNormalizedTime = info.normalizedTime;
                    animTransition = animator.IsInTransition(0);
                    animSpeed = animator.speed;
                    if (animTransition)
                    {
                        animNextStateHash = animator.GetNextAnimatorStateInfo(0).fullPathHash;
                    }
                }
                catch (Exception)
                {
                }
            }

            var stateName = ReadFirstFieldText(record.Component, "state", "currentState", "currentConvo");
            if (string.IsNullOrEmpty(stateName)) stateName = active ? "Active" : "Inactive";
            var sequence = ReadSceneSequence();
            var moving = navMoving || velocity.sqrMagnitude > 0.0025f || HasBoolField(record.Component, true, "moving", "run", "go");
            var signature = record.Id + "|" + active + "|" + visible + "|" + stateName + "|" +
                            sequence + "|" + animStateHash + "|" + animNextStateHash + "|" + moving;

            state = new NpcBrainState
            {
                SessionId = sessionId,
                Generation = generation,
                NpcSeq = npcSeq,
                UnixTimeMs = nowMs,
                SceneName = SceneManager.GetActiveScene().name,
                NpcId = record.Id,
                NpcKind = record.Kind,
                Active = active,
                Visible = visible,
                Critical = active,
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = velocity,
                StateHash = StableHash(signature),
                StateName = stateName,
                Phase = stateName,
                Sequence = sequence,
                TargetPath = string.Empty,
                TargetPosition = Vector3.zero,
                HasNavAgent = nav != null,
                NavAgentEnabled = navEnabled,
                NavDestination = navDestination,
                RemainingDistance = remaining,
                IsMoving = moving,
                HasAnimator = animator != null,
                AnimStateHash = animStateHash,
                AnimNextStateHash = animNextStateHash,
                AnimTransition = animTransition,
                AnimNormalizedTime = animNormalizedTime,
                AnimSpeed = animSpeed,
                ScriptedFlags = active ? 1 : 0
            };
            return true;
        }

        public bool ApplyState(NpcBrainState state, bool snapshot)
        {
            if (string.IsNullOrEmpty(state.NpcId)) return false;
            Refresh(SceneManager.GetActiveScene().name);

            if (!snapshot &&
                _lastAppliedSeqById.TryGetValue(state.NpcId, out var lastSeq) &&
                state.NpcSeq <= lastSeq)
            {
                LogInfo("NPCBrain stale drop npcId=" + state.NpcId +
                        " gotSeq=" + state.NpcSeq +
                        " lastSeq=" + lastSeq);
                return true;
            }

            if ((!_records.TryGetValue(state.NpcId, out var record) || record.Component == null) &&
                !TryResolveFallbackRecord(state, out record))
            {
                LogMissingThrottled(state);
                if (!state.Critical)
                {
                    _lastAppliedSeqById[state.NpcId] = state.NpcSeq;
                    _lastAppliedMsById[state.NpcId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    return true;
                }
                return false;
            }

            SuppressLocalBrain(record);
            var transform = record.Component.transform;
            if (state.Active && !transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(true);
            }

            var targetPosition = PredictNpcTargetPosition(state);
            var targetRotation = state.Rotation;
            var activeToggle = _lastActiveById.TryGetValue(state.NpcId, out var lastActive) && lastActive != state.Active;
            if (!snapshot && !activeToggle && _smoothedPositions.TryGetValue(state.NpcId, out var currentPosition))
            {
                var alpha = 1f - Mathf.Exp(-18f * Mathf.Max(Time.unscaledDeltaTime, Time.deltaTime, 0.016f));
                currentPosition = Vector3.Distance(currentPosition, targetPosition) > 4f
                    ? targetPosition
                    : Vector3.Lerp(currentPosition, targetPosition, alpha);
                var currentRotation = _smoothedRotations.TryGetValue(state.NpcId, out var storedRotation)
                    ? storedRotation
                    : transform.rotation;
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, alpha);
                transform.position = currentPosition;
                transform.rotation = currentRotation;
                _smoothedPositions[state.NpcId] = currentPosition;
                _smoothedRotations[state.NpcId] = currentRotation;
            }
            else
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                _smoothedPositions[state.NpcId] = targetPosition;
                _smoothedRotations[state.NpcId] = targetRotation;
            }

            ApplyVisibility(transform, state.Active && (state.Visible || ShouldForceVisibleNpcState(state)));
            ApplyAnimatorState(transform, state);
            if (!state.Active && transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(false);
            }

            _lastAppliedSeqById[state.NpcId] = state.NpcSeq;
            _lastAppliedMsById[state.NpcId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastActiveById[state.NpcId] = state.Active;
            return true;
        }

        public void SuppressLocalBrain()
        {
            Refresh(SceneManager.GetActiveScene().name);
            foreach (var record in _records.Values)
            {
                SuppressLocalBrain(record);
            }
        }

        public string BuildOverlaySummary(string label)
        {
            var active = "-";
            var newestAge = -1L;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var record in _records.Values)
            {
                if (record == null || record.Component == null) continue;
                if (record.Component.gameObject.activeInHierarchy)
                {
                    active = record.Kind;
                }
                if (_lastAppliedMsById.TryGetValue(record.Id, out var appliedMs))
                {
                    var age = Math.Max(0, nowMs - appliedMs);
                    newestAge = newestAge < 0 ? age : Math.Min(newestAge, age);
                }
            }

            return label + ": active=" + active + " age=" + (newestAge >= 0 ? newestAge + "ms" : "-");
        }

        private static List<Component> FindLoadedComponentsByTypeName(string typeName)
        {
            var results = new List<Component>();
            if (string.IsNullOrEmpty(typeName)) return results;
            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.gameObject == null) continue;
                var scene = behaviour.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
                {
                    results.Add(behaviour);
                }
            }
            results.Sort((left, right) => string.CompareOrdinal(NetPath.GetPath(left.transform), NetPath.GetPath(right.transform)));
            return results;
        }

        private void SuppressLocalBrain(GenericNpcRecord record)
        {
            if (record == null || record.Component == null) return;
            var transform = record.Component.transform;
            var navAgents = transform.GetComponentsInChildren<NavMeshAgent>(true);
            for (var i = 0; i < navAgents.Length; i++)
            {
                if (navAgents[i] != null) navAgents[i].enabled = false;
            }

            var pathAgents = transform.GetComponentsInChildren<NavmeshPathAgent>(true);
            for (var i = 0; i < pathAgents.Length; i++)
            {
                if (pathAgents[i] != null) pathAgents[i].enabled = false;
            }

            var behaviours = transform.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null) continue;
                var name = behaviour.GetType().Name;
                if (name.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Hiker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Hobo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Chef", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Deer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Janitor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Worker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Cop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Stranger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("IdleMovement", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("NPC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("FoldingGuy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    behaviour.enabled = false;
                }
            }

            if (_suppressedLogged.Add(record.Id))
            {
                LogInfo("NPCBrain client suppress local AI npcId=" + record.Id);
            }
        }

        private static void ApplyVisibility(Transform transform, bool visible)
        {
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = visible;
            }
        }

        private static bool ShouldForceVisibleNpcState(NpcBrainState state)
        {
            if (!state.Active || !string.Equals(state.NpcId, "Pizzeria/Mike/Main", StringComparison.Ordinal))
            {
                return false;
            }

            var stateName = state.StateName ?? string.Empty;
            if (stateName.IndexOf("InCar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stateName.IndexOf("WaitingInCar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }

        private static void ApplyAnimatorState(Transform transform, NpcBrainState state)
        {
            if (!state.HasAnimator || state.AnimStateHash == 0) return;
            var animator = transform.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.layerCount <= 0) return;
            animator.enabled = true;
            animator.speed = state.AnimSpeed <= 0f ? 1f : state.AnimSpeed;
            try
            {
                if (state.AnimTransition && state.AnimNextStateHash != 0)
                {
                    animator.CrossFade(state.AnimNextStateHash, 0.08f, 0, Mathf.Repeat(state.AnimNormalizedTime, 1f));
                }
                else
                {
                    var current = animator.GetCurrentAnimatorStateInfo(0);
                    var currentHash = current.fullPathHash != 0 ? current.fullPathHash : current.shortNameHash;
                    if (currentHash != state.AnimStateHash)
                    {
                        animator.Play(state.AnimStateHash, 0, Mathf.Repeat(state.AnimNormalizedTime, 1f));
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static bool HasVisibleRenderer(Transform transform)
        {
            if (transform == null) return false;
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            return false;
        }

        private static string ReadFirstFieldText(object target, params string[] names)
        {
            if (target == null || names == null) return string.Empty;
            var type = target.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var field = type.GetField(names[i], System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null) continue;
                var value = field.GetValue(target);
                if (value != null) return value.ToString();
            }
            return string.Empty;
        }

        private static bool HasBoolField(object target, bool desired, params string[] names)
        {
            if (target == null || names == null) return false;
            var type = target.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var field = type.GetField(names[i], System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(target) == desired)
                {
                    return true;
                }
            }
            return false;
        }

        private static string ReadSceneSequence()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            try
            {
                if (sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var gm = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                    return gm != null ? gm.currentPlayerState.ToString() : "Pizzeria";
                }
                if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var gm = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
                    return gm != null ? ReadFirstFieldText(gm, "currentPlayerState", "currentState", "phoneUIState") : "RoadTrip";
                }
                if (sceneName.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var gm = UnityEngine.Object.FindObjectOfType<OfficeLayoutGameManager>();
                    return gm != null ? ReadFirstFieldText(gm, "currentPlayerState", "currentState") : "Office";
                }
                if (sceneName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sceneName.IndexOf("Lot", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var gm = UnityEngine.Object.FindObjectOfType<ParkingLotGameManager>();
                    return gm != null ? ReadFirstFieldText(gm, "currentPlayerState", "currentState") : "ParkingLot";
                }
            }
            catch (Exception)
            {
            }
            return sceneName;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = 23;
                text = text ?? string.Empty;
                for (var i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }
                return hash;
            }
        }

        private static string StablePathSuffix(string path)
        {
            if (string.IsNullOrEmpty(path)) return "unknown";
            return path.Replace(" ", "_").Replace("[", "").Replace("]", "").Replace("/", ".");
        }

        private bool TryResolveFallbackRecord(NpcBrainState state, out GenericNpcRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(state.NpcId))
            {
                return false;
            }

            var incoming = NormalizeNpcId(state.NpcId);
            foreach (var pair in _records)
            {
                if (pair.Value == null || pair.Value.Component == null) continue;
                var recordId = NormalizeNpcId(pair.Key);
                if (string.Equals(recordId, incoming, StringComparison.Ordinal) ||
                    incoming.EndsWith(recordId, StringComparison.Ordinal) ||
                    recordId.EndsWith(incoming, StringComparison.Ordinal))
                {
                    record = pair.Value;
                    _records[state.NpcId] = record;
                    return true;
                }

                var objectName = NormalizeNpcId(pair.Value.Component.gameObject != null ? pair.Value.Component.gameObject.name : string.Empty);
                if (!string.IsNullOrEmpty(objectName) && incoming.IndexOf(objectName, StringComparison.Ordinal) >= 0)
                {
                    record = pair.Value;
                    _records[state.NpcId] = record;
                    return true;
                }
            }

            return false;
        }

        private static Vector3 PredictNpcTargetPosition(NpcBrainState state)
        {
            if (state.Velocity.sqrMagnitude < 0.0001f)
            {
                return state.Position;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var ageSeconds = Mathf.Clamp((nowMs - state.UnixTimeMs) / 1000f, 0f, 0.18f);
            return state.Position + state.Velocity * ageSeconds;
        }

        private void LogMissingThrottled(NpcBrainState state)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_nextMissingLogMsById.TryGetValue(state.NpcId, out var nextMs) && nowMs < nextMs)
            {
                return;
            }

            _nextMissingLogMsById[state.NpcId] = nowMs + (state.Critical ? 2000 : 8000);
            LogInfo("NPCBrain missing npcId=" + state.NpcId +
                    " critical=" + BoolText(state.Critical) +
                    " pending=" + (state.Critical ? "yes" : "no") +
                    " scene=" + SceneManager.GetActiveScene().name);
        }

        private static string StableComponentSuffix(Component component, Dictionary<string, int> suffixCounts)
        {
            if (component == null) return "unknown";

            var typeName = component.GetType().Name;
            var objectName = component.gameObject != null ? component.gameObject.name : "unknown";
            var suffix = SanitizeIdPart(typeName + "." + objectName);
            if (suffixCounts == null)
            {
                return suffix;
            }

            suffixCounts.TryGetValue(suffix, out var count);
            suffixCounts[suffix] = count + 1;
            return count <= 0 ? suffix : suffix + "." + count;
        }

        private static string SanitizeIdPart(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                {
                    chars[i] = '_';
                }
            }
            return new string(chars).Trim('_');
        }

        private static string NormalizeNpcId(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var chars = new List<char>(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = char.ToLowerInvariant(value[i]);
                if (char.IsLetterOrDigit(c))
                {
                    chars.Add(c);
                }
            }
            while (chars.Count > 0 && char.IsDigit(chars[chars.Count - 1]))
            {
                chars.RemoveAt(chars.Count - 1);
            }
            return new string(chars.ToArray());
        }

        private static string BoolText(bool value)
        {
            return value ? "yes" : "no";
        }

        private void LogInfo(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _logger?.LogInfo(message);
            _sessionLogWrite?.Invoke(message);
        }
    }
}
