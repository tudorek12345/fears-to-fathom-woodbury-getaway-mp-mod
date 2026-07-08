using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class CabinChaserTargetAssist
    {
        private const long RemoteFreshMs = 2000;
        private const long DestinationRefreshMs = 120;
        private const long LogIntervalMs = 1500;
        private const float RemoteSwitchBiasMeters = 0.35f;
        private const float HostSwitchBiasMeters = 0.75f;
        private const float ChaseCatchDistance = 2.5f;
        private const float HauntCatchDistance = 1.4f;
        private const float TeleportSnapDistance = 10f;

        private readonly ManualLogSource _logger;
        private readonly Action<string> _sessionLogWrite;
        private readonly FieldInfo _navTargetField;
        private readonly FieldInfo _playerFirstPersonField;

        private bool _remoteSelected;
        private long _nextDestinationMs;
        private long _nextLogMs;
        private long _lastRemoteCatchMs;
        private Vector3 _lastDestination;
        private bool _hasDestination;

        public CabinChaserTargetAssist(ManualLogSource logger, Action<string> sessionLogWrite)
        {
            _logger = logger;
            _sessionLogWrite = sessionLogWrite;
            _navTargetField = typeof(HostEndGame).GetField("navTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerFirstPersonField = typeof(PlayerController).GetField("firstPersonController", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Reset()
        {
            _remoteSelected = false;
            _nextDestinationMs = 0;
            _nextLogMs = 0;
            _lastRemoteCatchMs = 0;
            _hasDestination = false;
        }

        public void Update(
            bool isLive,
            CabinGameManager cabinGameManager,
            Transform remotePlayerRoot,
            long lastRemoteTransformMs,
            int generation,
            int sessionId,
            long nowMs)
        {
            if (!isLive ||
                cabinGameManager == null ||
                remotePlayerRoot == null ||
                !IsCabinScene(SceneManager.GetActiveScene().name) ||
                lastRemoteTransformMs <= 0 ||
                nowMs - lastRemoteTransformMs > RemoteFreshMs)
            {
                _remoteSelected = false;
                return;
            }

            var death = DeathManager.instance ?? UnityEngine.Object.FindObjectOfType<DeathManager>();
            if (death == null || death.isDead || death.hostEndGame == null)
            {
                _remoteSelected = false;
                return;
            }

            var isActiveChase = death.hostIsChasing;
            var isActiveHaunt = death.hostIsHaunting && !death.waitForFarScare;
            if (!isActiveChase && !isActiveHaunt)
            {
                _remoteSelected = false;
                return;
            }

            var hostPlayer = TryResolveHostPlayer(cabinGameManager);
            if (hostPlayer == null)
            {
                _remoteSelected = false;
                return;
            }

            try
            {
                var chaser = death.hostEndGame;
                var chaserPosition = chaser.transform.position;
                var hostDistance = Vector3.Distance(chaserPosition, hostPlayer.position);
                var remoteDistance = Vector3.Distance(chaserPosition, remotePlayerRoot.position);
                var chooseRemote = _remoteSelected
                    ? remoteDistance <= hostDistance + HostSwitchBiasMeters
                    : remoteDistance + RemoteSwitchBiasMeters < hostDistance;

                if (!chooseRemote)
                {
                    if (_remoteSelected && nowMs >= _nextLogMs)
                    {
                        Log("Co-op chaser host: target=host reason=nearer hostDist=" +
                            FormatMeters(hostDistance) + " clientDist=" + FormatMeters(remoteDistance) +
                            " scene=" + SceneManager.GetActiveScene().name +
                            " gen=" + generation + " sid=" + sessionId);
                        _nextLogMs = nowMs + LogIntervalMs;
                    }

                    _remoteSelected = false;
                    return;
                }

                _remoteSelected = true;
                if (nowMs >= _nextDestinationMs)
                {
                    SteerChaser(chaser, remotePlayerRoot.position, isActiveChase);
                    _nextDestinationMs = nowMs + DestinationRefreshMs;
                }

                TryResolveRemoteCatch(death, chaser, remoteDistance, isActiveChase, isActiveHaunt, generation, sessionId, nowMs);

                if (nowMs >= _nextLogMs)
                {
                    Log("Co-op chaser host: target=client mode=" + (isActiveChase ? "chase" : "haunt") +
                        " hostDist=" + FormatMeters(hostDistance) +
                        " clientDist=" + FormatMeters(remoteDistance) +
                        " scene=" + SceneManager.GetActiveScene().name +
                        " gen=" + generation + " sid=" + sessionId);
                    _nextLogMs = nowMs + LogIntervalMs;
                }
            }
            catch (Exception ex)
            {
                if (nowMs >= _nextLogMs)
                {
                    _logger.LogWarning("Co-op chaser host: client target assist failed reason=" + ex.Message);
                    _nextLogMs = nowMs + LogIntervalMs;
                }

                _remoteSelected = false;
            }
        }

        private void SteerChaser(HostEndGame chaser, Vector3 remotePosition, bool chaseMode)
        {
            var destination = remotePosition;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(remotePosition, out hit, 2.0f, NavMesh.AllAreas))
            {
                destination = hit.position;
            }

            if (_hasDestination && Vector3.Distance(_lastDestination, destination) > TeleportSnapDistance)
            {
                _hasDestination = false;
            }

            var agent = chaser.navMeshAgent;
            if (agent != null)
            {
                if (!agent.enabled)
                {
                    agent.enabled = true;
                }

                agent.isStopped = false;
                agent.speed = chaseMode && !chaser.speedIncreaseWhenEnterRoom ? 2.5f : 4f;
                agent.SetDestination(destination);
            }

            if (chaser.animator != null)
            {
                chaser.animator.SetInteger("State", chaseMode ? 3 : 4);
            }

            _navTargetField?.SetValue(chaser, destination);
            FaceTarget(chaser.transform, remotePosition);
            _lastDestination = destination;
            _hasDestination = true;
        }

        private void TryResolveRemoteCatch(
            DeathManager death,
            HostEndGame chaser,
            float remoteDistance,
            bool chaseMode,
            bool hauntMode,
            int generation,
            int sessionId,
            long nowMs)
        {
            if (nowMs - _lastRemoteCatchMs < 2000)
            {
                return;
            }

            if (chaseMode && remoteDistance < ChaseCatchDistance)
            {
                death.hostIsChasing = false;
                death.HuntTriggeredFar();
                death.playerCaught = true;
                _lastRemoteCatchMs = nowMs;
                Log("Co-op chaser host: client caught mode=chase dist=" + FormatMeters(remoteDistance) +
                    " scene=" + SceneManager.GetActiveScene().name +
                    " gen=" + generation + " sid=" + sessionId);
                return;
            }

            if (!hauntMode || remoteDistance >= HauntCatchDistance)
            {
                return;
            }

            death.hostIsHaunting = false;
            if (chaser.navMeshAgent != null && chaser.navMeshAgent.enabled)
            {
                chaser.navMeshAgent.enabled = false;
            }

            death.playerCaught = true;
            death.StartCoroutine(death.PerformJumpscareAtRun(true));
            _lastRemoteCatchMs = nowMs;
            Log("Co-op chaser host: client caught mode=haunt dist=" + FormatMeters(remoteDistance) +
                " scene=" + SceneManager.GetActiveScene().name +
                " gen=" + generation + " sid=" + sessionId);
        }

        private Transform TryResolveHostPlayer(CabinGameManager cabinGameManager)
        {
            var player = cabinGameManager != null ? cabinGameManager.cabinPlayerController : null;
            if (player == null)
            {
                player = PlayerController.GetInstance() as CabinPlayerController;
            }

            if (player == null)
            {
                player = UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
            }

            if (player == null)
            {
                return null;
            }

            var firstPerson = _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(player) as FirstPersonController
                : null;
            return firstPerson != null ? firstPerson.transform : player.transform;
        }

        private static void FaceTarget(Transform chaser, Vector3 target)
        {
            var delta = target - chaser.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.001f)
            {
                return;
            }

            chaser.rotation = Quaternion.Slerp(
                chaser.rotation,
                Quaternion.LookRotation(delta.normalized, Vector3.up),
                0.35f);
        }

        private static bool IsCabinScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) &&
                   sceneName.IndexOf("CabinScene", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatMeters(float value)
        {
            return value.ToString("0.00") + "m";
        }

        private void Log(string line)
        {
            _logger.LogInfo(line);
            _sessionLogWrite?.Invoke(line);
        }
    }
}
