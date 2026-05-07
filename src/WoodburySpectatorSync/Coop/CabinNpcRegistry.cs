using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class CabinNpcRegistry
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int FlagCritical = 1;
        private const int FlagRenderable = 2;
        private const int FlagGo = 4;
        private const int FlagMoving = 8;
        private const int FlagReachedPos = 16;
        private const int FlagFollowingHost = 32;
        private const int FlagInteractable = 64;
        private const int FlagVisibleToPlayer = 128;
        private const int FlagCanSeePlayer = 256;

        private static readonly Entry[] Entries =
        {
            new Entry("Cabin/Mike/Main", "Mike", "mikeCabin", "MikeCabin"),
            new Entry("Cabin/Mike/Fishing", "Mike", "mikeFishing", "MikeFishing"),
            new Entry("Cabin/Mike/Cook", "Mike", "mikeController", "MikeCabinCookController"),
            new Entry("Cabin/Mike/PostEating", "Mike", "mikePostEating", "MikePostEating"),
            new Entry("Cabin/Mike/AfterHiding", "Mike", "mikeAfterHiding", "MikeAfterHiding"),
            new Entry("Cabin/Mike/Rizzler", "Mike", "mikeRizzlerController", "MikeRizzlerController"),
            new Entry("Cabin/Mike/End", "Mike", "mikeEnd", "MikeEndGame"),
            new Entry("Cabin/Hiker/Bridge", "Hiker", "cabinHiker", "CabinHiker"),
            new Entry("Cabin/Hiker/Window", "Hiker", "hikerCabinController", "HikerCabinController"),
            new Entry("Cabin/Host/Hiding", "Host", "hostHiding", "HostDuringHiding"),
            new Entry("Cabin/Host/FixingSink", "Host", "hostFixingSink", "HostFixingSink"),
            new Entry("Cabin/Host/EndGame", "Host", "hostEndGame", "HostEndGame"),
            new Entry("Cabin/Nora/End", "Nora", "noraEnd", "Nora"),
            new Entry("Cabin/Cat/EndGame", "Cat", "catEndGame", "CatEndGame")
        };

        private static readonly HashSet<string> DecisionScriptNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MikeCabin",
            "MikeCabinCookController",
            "MikeFishing",
            "MikePostEating",
            "MikeAfterHiding",
            "MikeRizzlerController",
            "MikeEndGame",
            "CabinHiker",
            "HikerCabinController",
            "HostDuringHiding",
            "HostFixingSink",
            "HostEndGame",
            "Nora",
            "CatEndGame",
            "NavmeshPathAgent"
        };

        private readonly ManualLogSource _logger;
        private readonly Action<string> _sessionLogWrite;
        private readonly string _side;
        private readonly Dictionary<string, CabinNpcRecord> _records = new Dictionary<string, CabinNpcRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _loggedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _suppressedLogged = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _missingNextLogTimes = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _lastAppliedSeqById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _lastAppliedMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _lastApplyLogHashById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _lastApplyLogMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector3> _smoothedPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, Quaternion> _smoothedRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private string _lastSceneName = string.Empty;
        private float _lastRefreshTime = -10f;
        private int _lastMissingCount;
        private int _lastCriticalMissingCount;

        public CabinNpcRegistry(ManualLogSource logger, Action<string> sessionLogWrite, string side)
        {
            _logger = logger;
            _sessionLogWrite = sessionLogWrite;
            _side = string.IsNullOrEmpty(side) ? "client" : side;
        }

        public IEnumerable<CabinNpcRecord> Records
        {
            get { return _records.Values; }
        }

        public int MissingCount
        {
            get { return _lastMissingCount; }
        }

        public int CriticalMissingCount
        {
            get { return _lastCriticalMissingCount; }
        }

        public void Refresh(CabinGameManager manager, string sceneName, bool force = false)
        {
            if (manager == null || string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("Cabin", StringComparison.OrdinalIgnoreCase) < 0)
            {
                _records.Clear();
                _lastSceneName = sceneName ?? string.Empty;
                _lastMissingCount = 0;
                _lastCriticalMissingCount = 0;
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
            _lastMissingCount = 0;
            _lastCriticalMissingCount = 0;

            foreach (var entry in Entries)
            {
                var component = ResolveComponent(manager, entry);
                if (component == null)
                {
                    _records.Remove(entry.Id);
                    if (!IsExpectedToResolve(entry.Id, manager))
                    {
                        continue;
                    }

                    var criticalMissing = IsCritical(entry.Id, manager, null, false);
                    _lastMissingCount++;
                    if (criticalMissing) _lastCriticalMissingCount++;
                    MaybeLogMissing(entry.Id, criticalMissing);
                    continue;
                }

                var path = NetPath.GetPath(component.transform);
                var active = component.gameObject.activeInHierarchy;
                var critical = IsCritical(entry.Id, manager, component, active);
                var record = new CabinNpcRecord(entry.Id, entry.Kind, entry.ManagerFieldName, entry.TypeName, component, path, critical);
                _records[entry.Id] = record;

                if (!_loggedPaths.TryGetValue(entry.Id, out var loggedPath) ||
                    !string.Equals(loggedPath, path, StringComparison.Ordinal))
                {
                    _loggedPaths[entry.Id] = path ?? string.Empty;
                    LogInfo("NPCRegistry found npcId=" + entry.Id +
                            " path=" + (string.IsNullOrEmpty(path) ? "-" : path) +
                            " type=" + component.GetType().Name +
                            " critical=" + BoolText(critical));
                }
            }
        }

        public bool TryBuildState(
            CabinGameManager manager,
            string npcId,
            int sessionId,
            int generation,
            int npcSeq,
            long nowMs,
            Vector3 velocity,
            out NpcBrainState state)
        {
            state = default(NpcBrainState);
            if (manager == null || !_records.TryGetValue(npcId, out var record) || record.Component == null)
            {
                return false;
            }

            var component = record.Component;
            var transform = component.transform;
            var active = component.gameObject.activeInHierarchy;
            var visible = active && HasVisibleRenderer(transform);
            var stateName = ReadFirstFieldText(component, "state", "currentState", "nextAnimationState", "currentConvo");
            var phase = ReadFirstFieldText(component, "phase", "currentAnimationState", "currentAxiousIdleState", "currentAngryIdleState");
            if (string.IsNullOrEmpty(stateName)) stateName = active ? "Active" : "Inactive";
            if (string.IsNullOrEmpty(phase)) phase = stateName;

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
                        navDestination = transform.position;
                    }
                }
            }

            var animator = transform.GetComponentInChildren<Animator>(true);
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
                    animStateHash = info.fullPathHash;
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

            var sequence = manager.CurrentSequence.ToString();
            var target = ResolveTarget(component);
            var targetPath = target != null ? NetPath.GetPath(target) : string.Empty;
            var targetPosition = target != null ? target.position : Vector3.zero;
            var critical = IsCritical(record.Id, manager, component, active);
            var scriptedFlags = BuildScriptedFlags(component, critical, visible);
            var isMoving = navMoving || velocity.sqrMagnitude > 0.0025f || HasBoolField(component, true, "moving", "go", "followingHost");
            var signature = record.Id + "|" + record.Kind + "|" + active + "|" + visible + "|" +
                            stateName + "|" + phase + "|" + sequence + "|" + targetPath + "|" +
                            animStateHash + "|" + animNextStateHash + "|" + scriptedFlags + "|" + isMoving;

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
                Critical = critical,
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = velocity,
                StateHash = StableHash(signature),
                StateName = stateName,
                Phase = phase,
                Sequence = sequence,
                TargetPath = targetPath,
                TargetPosition = targetPosition,
                HasNavAgent = nav != null,
                NavAgentEnabled = navEnabled,
                NavDestination = navDestination,
                RemainingDistance = remaining,
                IsMoving = isMoving,
                HasAnimator = animator != null,
                AnimStateHash = animStateHash,
                AnimNextStateHash = animNextStateHash,
                AnimTransition = animTransition,
                AnimNormalizedTime = animNormalizedTime,
                AnimSpeed = animSpeed,
                ScriptedFlags = scriptedFlags
            };
            return true;
        }

        public bool ApplyState(NpcBrainState state, bool snapshot)
        {
            if (string.IsNullOrEmpty(state.NpcId))
            {
                return false;
            }

            if (!snapshot &&
                _lastAppliedSeqById.TryGetValue(state.NpcId, out var lastSeq) &&
                state.NpcSeq <= lastSeq)
            {
                LogInfo("NPCBrain stale drop npcId=" + state.NpcId +
                        " gotSeq=" + state.NpcSeq +
                        " lastSeq=" + lastSeq);
                return true;
            }

            if (!_records.TryGetValue(state.NpcId, out var record) || record.Component == null)
            {
                MaybeLogMissing(state.NpcId, state.Critical);
                return false;
            }

            SuppressLocalBrain(record);
            var transform = record.Component.transform;
            if (state.Active && !transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(true);
            }

            var targetPosition = state.Position;
            var targetRotation = state.Rotation;
            var deferMotionToAi = ShouldDeferMotionToAi(state, snapshot);
            if (deferMotionToAi)
            {
                _smoothedPositions[state.NpcId] = transform.position;
                _smoothedRotations[state.NpcId] = transform.rotation;
            }
            else if (!snapshot && _smoothedPositions.TryGetValue(state.NpcId, out var currentPosition))
            {
                var alpha = 1f - Mathf.Exp(-18f * Time.deltaTime);
                if (Vector3.Distance(currentPosition, targetPosition) > 4f)
                {
                    currentPosition = targetPosition;
                }
                else
                {
                    currentPosition = Vector3.Lerp(currentPosition, targetPosition, alpha);
                }

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

            ApplyVisibility(transform, state.Active && state.Visible);
            ApplyAnimatorState(transform, state);
            ApplyScriptedFlags(record.Component, state.ScriptedFlags);

            if (!state.Active && transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(false);
            }

            _lastAppliedSeqById[state.NpcId] = state.NpcSeq;
            _lastAppliedMsById[state.NpcId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LogInfoThrottledApply(state);
            return true;
        }

        private static bool ShouldDeferMotionToAi(NpcBrainState state, bool snapshot)
        {
            if (snapshot || !state.Active || !state.IsMoving) return false;
            if (string.IsNullOrEmpty(state.NpcId)) return false;

            // Mike has a separate high-frequency AiTransform stream. Applying the
            // lower-rate brain heartbeat position here fights that smoother and
            // shows up as small snaps while he walks.
            return state.NpcId.StartsWith("Cabin/Mike/", StringComparison.Ordinal);
        }

        public void SuppressLocalBrain(CabinGameManager manager, string sceneName)
        {
            Refresh(manager, sceneName);
            foreach (var record in _records.Values)
            {
                SuppressLocalBrain(record);
            }
        }

        public string BuildOverlaySummary(bool hostSide)
        {
            var mike = "inactive";
            var hiker = "inactive";
            var seq = 0;
            var moving = false;
            long newestAge = -1;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            foreach (var record in _records.Values)
            {
                if (record == null || record.Component == null) continue;
                var active = record.Component.gameObject.activeInHierarchy;
                var name = record.Id;
                if (active && name.StartsWith("Cabin/Mike/", StringComparison.Ordinal))
                {
                    mike = name.Substring("Cabin/Mike/".Length);
                    moving = HasBoolField(record.Component, true, "moving", "go", "followingHost");
                }
                else if (active && name.StartsWith("Cabin/Hiker/", StringComparison.Ordinal))
                {
                    hiker = name.Substring("Cabin/Hiker/".Length);
                }

                if (_lastAppliedSeqById.TryGetValue(record.Id, out var appliedSeq))
                {
                    seq = Math.Max(seq, appliedSeq);
                    if (_lastAppliedMsById.TryGetValue(record.Id, out var appliedMs))
                    {
                        var age = Math.Max(0, nowMs - appliedMs);
                        newestAge = newestAge < 0 ? age : Math.Min(newestAge, age);
                    }
                }
            }

            if (hostSide)
            {
                return "NPC: Mike=" + mike + " moving=" + BoolText(moving) + " Hiker=" + hiker + " seq=" + seq;
            }

            return "NPC: Mike=" + mike + " age=" + (newestAge >= 0 ? newestAge + "ms" : "-") +
                   " Hiker=" + hiker + " missing=" + _lastMissingCount;
        }

        private Component ResolveComponent(CabinGameManager manager, Entry entry)
        {
            var field = typeof(CabinGameManager).GetField(entry.ManagerFieldName, FieldFlags);
            if (field != null)
            {
                var value = field.GetValue(manager);
                if (value is Component component)
                {
                    return component;
                }
            }

            return FindLoadedComponentByTypeName(entry.TypeName);
        }

        private static Component FindLoadedComponentByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            try
            {
                var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null || behaviour.gameObject == null) continue;
                    var scene = behaviour.gameObject.scene;
                    if (!scene.IsValid() || !scene.isLoaded) continue;
                    if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal))
                    {
                        return behaviour;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static bool IsCritical(string npcId, CabinGameManager manager, Component component, bool active)
        {
            if (manager == null) return active;

            var sequence = manager.CurrentSequence;
            if (npcId == "Cabin/Mike/Cook")
            {
                return active || sequence == SequenceType.Cooking ||
                       sequence == SequenceType.PickingBoardGame ||
                       sequence == SequenceType.PlayingJenga ||
                       sequence == SequenceType.GoingToPlayOuija ||
                       sequence == SequenceType.PlayingOuija ||
                       sequence == SequenceType.Eating;
            }

            if (npcId == "Cabin/Mike/Fishing")
            {
                return active || sequence == SequenceType.Fishing;
            }

            if (npcId == "Cabin/Mike/PostEating")
            {
                return active || manager.currentMike == CabinGameManager.CurrentMike.PostEating ||
                       sequence == SequenceType.HikerSequence ||
                       sequence == SequenceType.HostAtDoor ||
                       sequence == SequenceType.HostHittingDoor;
            }

            if (npcId == "Cabin/Mike/AfterHiding" ||
                npcId == "Cabin/Host/Hiding" ||
                npcId == "Cabin/Host/FixingSink")
            {
                return active || sequence == SequenceType.HostAtDoor || sequence == SequenceType.HostHittingDoor;
            }

            if (npcId == "Cabin/Hiker/Bridge" || npcId == "Cabin/Hiker/Window")
            {
                return active || sequence == SequenceType.HikerSequence ||
                       sequence == SequenceType.HostAtDoor ||
                       sequence == SequenceType.HostHittingDoor;
            }

            if (npcId == "Cabin/Mike/Rizzler")
            {
                return active || sequence == SequenceType.RizzSequence;
            }

            if (npcId == "Cabin/Mike/End" ||
                npcId == "Cabin/Host/EndGame" ||
                npcId == "Cabin/Nora/End" ||
                npcId == "Cabin/Cat/EndGame")
            {
                return active || manager.currentCabinSceneType == CabinGameManager.CabinSceneType.CabinSceneDark;
            }

            return active;
        }

        private static bool IsExpectedToResolve(string npcId, CabinGameManager manager)
        {
            if (manager == null) return false;

            var sequence = manager.CurrentSequence;
            if (npcId == "Cabin/Mike/Main" ||
                npcId == "Cabin/Mike/Fishing" ||
                npcId == "Cabin/Mike/Cook" ||
                npcId == "Cabin/Mike/PostEating" ||
                npcId == "Cabin/Mike/AfterHiding" ||
                npcId == "Cabin/Hiker/Bridge" ||
                npcId == "Cabin/Host/Hiding" ||
                npcId == "Cabin/Host/FixingSink")
            {
                return true;
            }

            if (npcId == "Cabin/Hiker/Window")
            {
                return sequence == SequenceType.HikerSequence ||
                       sequence == SequenceType.HostAtDoor ||
                       sequence == SequenceType.HostHittingDoor;
            }

            if (npcId == "Cabin/Mike/Rizzler")
            {
                return sequence == SequenceType.RizzSequence;
            }

            if (npcId == "Cabin/Mike/End" ||
                npcId == "Cabin/Host/EndGame" ||
                npcId == "Cabin/Nora/End" ||
                npcId == "Cabin/Cat/EndGame")
            {
                return manager.currentCabinSceneType == CabinGameManager.CabinSceneType.CabinSceneDark;
            }

            return false;
        }

        private void SuppressLocalBrain(CabinNpcRecord record)
        {
            if (record == null || record.Component == null) return;
            var transform = record.Component.transform;
            var navAgents = transform.GetComponentsInChildren<NavMeshAgent>(true);
            foreach (var navAgent in navAgents)
            {
                if (navAgent == null) continue;
                navAgent.enabled = false;
            }

            var pathAgents = transform.GetComponentsInChildren<NavmeshPathAgent>(true);
            foreach (var pathAgent in pathAgents)
            {
                if (pathAgent == null) continue;
                pathAgent.enabled = false;
            }

            var behaviours = transform.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                var typeName = behaviour.GetType().Name;
                if (!DecisionScriptNames.Contains(typeName)) continue;
                behaviour.enabled = false;
            }

            if (_suppressedLogged.Add(record.Id))
            {
                LogInfo("NPCBrain client suppress local AI npcId=" + record.Id);
            }
        }

        private static void ApplyVisibility(Transform transform, bool visible)
        {
            if (transform == null) return;
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.enabled = visible;
            }
        }

        private static void ApplyAnimatorState(Transform transform, NpcBrainState state)
        {
            if (transform == null || !state.HasAnimator) return;
            var animator = transform.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.layerCount <= 0) return;
            animator.enabled = true;
            animator.speed = state.AnimSpeed <= 0f ? 1f : state.AnimSpeed;
            if (state.AnimStateHash == 0) return;

            try
            {
                var current = animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash != state.AnimStateHash)
                {
                    animator.Play(state.AnimStateHash, 0, Mathf.Repeat(state.AnimNormalizedTime, 1f));
                }
            }
            catch (Exception)
            {
            }
        }

        private static void ApplyScriptedFlags(Component component, int flags)
        {
            if (component == null) return;
            WriteBoolField(component, "go", (flags & FlagGo) != 0);
            WriteBoolField(component, "moving", (flags & FlagMoving) != 0);
            WriteBoolField(component, "reachedPos", (flags & FlagReachedPos) != 0);
            WriteBoolField(component, "followingHost", (flags & FlagFollowingHost) != 0);
            WriteBoolField(component, "isInteractable", (flags & FlagInteractable) != 0);
            WriteBoolField(component, "playerCanSeeHiker", (flags & FlagCanSeePlayer) != 0);
        }

        private static int BuildScriptedFlags(Component component, bool critical, bool visible)
        {
            var flags = 0;
            if (critical) flags |= FlagCritical;
            if (visible) flags |= FlagRenderable;
            if (HasBoolField(component, true, "go")) flags |= FlagGo;
            if (HasBoolField(component, true, "moving")) flags |= FlagMoving;
            if (HasBoolField(component, true, "reachedPos")) flags |= FlagReachedPos;
            if (HasBoolField(component, true, "followingHost")) flags |= FlagFollowingHost;
            if (HasBoolField(component, true, "isInteractable")) flags |= FlagInteractable;
            if (HasBoolField(component, true, "hikerIsVisibleToPlayer", "playerHasSeenHiker")) flags |= FlagVisibleToPlayer;
            if (HasBoolField(component, true, "playerCanSeeHiker")) flags |= FlagCanSeePlayer;
            return flags;
        }

        private static Transform ResolveTarget(Component component)
        {
            if (component == null) return null;
            foreach (var fieldName in new[] { "target", "targetPosition", "currentTarget", "currentWaypoint", "playerLookAt", "lookAtPoint" })
            {
                var field = component.GetType().GetField(fieldName, FieldFlags);
                if (field == null) continue;
                var value = field.GetValue(component);
                if (value is Transform transform) return transform;
                if (value is Component childComponent) return childComponent.transform;
                if (value is GameObject gameObject) return gameObject.transform;
            }

            return null;
        }

        private static string ReadFirstFieldText(Component component, params string[] fieldNames)
        {
            if (component == null || fieldNames == null) return string.Empty;
            foreach (var fieldName in fieldNames)
            {
                var field = component.GetType().GetField(fieldName, FieldFlags);
                if (field == null) continue;
                try
                {
                    var value = field.GetValue(component);
                    if (value == null) continue;
                    return value.ToString();
                }
                catch (Exception)
                {
                }
            }

            return string.Empty;
        }

        private static bool HasBoolField(Component component, bool expected, params string[] fieldNames)
        {
            if (component == null || fieldNames == null) return false;
            foreach (var fieldName in fieldNames)
            {
                var field = component.GetType().GetField(fieldName, FieldFlags);
                if (field == null || field.FieldType != typeof(bool)) continue;
                try
                {
                    if ((bool)field.GetValue(component) == expected) return true;
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        private static void WriteBoolField(Component component, string fieldName, bool value)
        {
            if (component == null) return;
            var field = component.GetType().GetField(fieldName, FieldFlags);
            if (field == null || field.FieldType != typeof(bool)) return;
            try
            {
                field.SetValue(component, value);
            }
            catch (Exception)
            {
            }
        }

        private static bool HasVisibleRenderer(Transform transform)
        {
            if (transform == null) return false;
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private void MaybeLogMissing(string npcId, bool critical)
        {
            var now = Time.realtimeSinceStartup;
            if (_missingNextLogTimes.TryGetValue(npcId, out var nextLog) && now < nextLog)
            {
                return;
            }

            _missingNextLogTimes[npcId] = now + 8f;
            LogWarn("NPCBrain missing npcId=" + npcId + " critical=" + BoolText(critical));
        }

        private void LogInfoThrottledApply(NpcBrainState state)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var shouldLog = state.NpcSeq <= 1 || state.NpcSeq % 60 == 0;
            if (!shouldLog &&
                (!_lastApplyLogHashById.TryGetValue(state.NpcId, out var lastHash) ||
                 lastHash != state.StateHash))
            {
                shouldLog = true;
            }

            if (!shouldLog &&
                (!_lastApplyLogMsById.TryGetValue(state.NpcId, out var lastMs) ||
                 nowMs - lastMs >= 10000))
            {
                shouldLog = true;
            }

            if (!shouldLog) return;

            _lastApplyLogHashById[state.NpcId] = state.StateHash;
            _lastApplyLogMsById[state.NpcId] = nowMs;
            LogInfo("NPCBrain client apply npcId=" + state.NpcId +
                    " state=" + state.StateName +
                    " phase=" + state.Phase +
                    " sequence=" + state.Sequence +
                    " npcSeq=" + state.NpcSeq);
        }

        private void LogInfo(string message)
        {
            _logger?.LogInfo(message);
            _sessionLogWrite?.Invoke(message);
        }

        private void LogWarn(string message)
        {
            _logger?.LogWarning(message);
            _sessionLogWrite?.Invoke(message);
        }

        private static string BoolText(bool value)
        {
            return value ? "yes" : "no";
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = (int)2166136261;
                if (value == null) return hash;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                return hash;
            }
        }

        private sealed class Entry
        {
            public readonly string Id;
            public readonly string Kind;
            public readonly string ManagerFieldName;
            public readonly string TypeName;

            public Entry(string id, string kind, string managerFieldName, string typeName)
            {
                Id = id;
                Kind = kind;
                ManagerFieldName = managerFieldName;
                TypeName = typeName;
            }
        }
    }

    internal sealed class CabinNpcRecord
    {
        public readonly string Id;
        public readonly string Kind;
        public readonly string ManagerFieldName;
        public readonly string TypeName;
        public readonly Component Component;
        public readonly string Path;
        public readonly bool Critical;

        public CabinNpcRecord(string id, string kind, string managerFieldName, string typeName, Component component, string path, bool critical)
        {
            Id = id;
            Kind = kind;
            ManagerFieldName = managerFieldName;
            TypeName = typeName;
            Component = component;
            Path = path ?? string.Empty;
            Critical = critical;
        }
    }
}
