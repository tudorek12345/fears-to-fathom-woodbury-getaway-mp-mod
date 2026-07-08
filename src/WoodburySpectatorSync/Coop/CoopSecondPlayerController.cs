using System;
using BepInEx.Logging;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class CoopSecondPlayerController : MonoBehaviour
    {
        private const string PresenceName = "CoopSecondPlayerPresence";
        private const string CameraAnchorName = "CoopSecondPlayerCameraAnchor";

        private static Texture2D _panelTexture;
        private static Texture2D _accentTexture;
        private static GUIStyle _seatTitleStyle;
        private static GUIStyle _seatDetailStyle;

        private Transform _visualRoot;
        private Transform _cameraAnchor;
        private CapsuleCollider _presenceCollider;
        private string _displayName = string.Empty;
        private string _role = string.Empty;
        private Vector3 _lastPosition;
        private float _lastSampleTime;
        private bool _hasSample;
        private bool _logged;
        private bool _vehicleSeatEnabled;
        private bool _vehicleSeatPrompt;
        private bool _predictionEnabled;
        private float _predictionSeconds;
        private float _maxPredictionDistance;
        private bool _seatbeltLocked = true;
        private CoopVehiclePassengerSeat.SeatSide _requestedSeatSide = CoopVehiclePassengerSeat.SeatSide.Auto;
        private CoopVehiclePassengerSeat.SeatPose _lastSeatPose;
        private bool _hasLastSeatPose;
        private PlayerTransformState _lastNetworkState;
        private bool _hasNetworkState;
        private long _nextSeatLogMs;

        public Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;
        public Transform CameraAnchor => _cameraAnchor;
        public Vector3 Velocity { get; private set; }
        public bool PresenceColliderEnabled => _presenceCollider != null && _presenceCollider.enabled;
        public bool IsVehicleSeated => _hasLastSeatPose;

        public static CoopSecondPlayerController Attach(
            GameObject root,
            Transform visualRoot,
            Transform cameraAnchor,
            bool enablePresenceCollider,
            bool enableVehicleSeat,
            bool showVehicleSeatPrompt,
            bool predictionEnabled,
            float predictionSeconds,
            float maxPredictionDistance,
            ManualLogSource logger,
            Action<string> sessionLogWrite)
        {
            if (root == null) return null;

            var controller = root.GetComponent<CoopSecondPlayerController>();
            if (controller == null)
            {
                controller = root.AddComponent<CoopSecondPlayerController>();
            }

            controller.Initialize(
                visualRoot,
                cameraAnchor,
                enablePresenceCollider,
                enableVehicleSeat,
                showVehicleSeatPrompt,
                predictionEnabled,
                predictionSeconds,
                maxPredictionDistance,
                logger,
                sessionLogWrite);
            return controller;
        }

        public void ConfigureName(string displayName, string role)
        {
            _displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            _role = string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToUpperInvariant();
        }

        public void ApplyNetworkState(PlayerTransformState state, ref Vector3 bodyPosition, ref Quaternion bodyRotation)
        {
            _lastNetworkState = state;
            _hasNetworkState = true;

            var appliedPosition = bodyPosition;
            var appliedRotation = bodyRotation;
            var seated = TryResolveVehicleSeat(out var seatPose);
            if (seated)
            {
                appliedPosition = PredictSeatPosition(seatPose);
                appliedRotation = seatPose.Rotation;
                _lastSeatPose = seatPose;
                _hasLastSeatPose = true;
            }
            else
            {
                _hasLastSeatPose = false;
            }

            bodyPosition = appliedPosition;
            bodyRotation = appliedRotation;

            if (_cameraAnchor != null)
            {
                _cameraAnchor.SetPositionAndRotation(state.CameraPosition, state.CameraRotation);
            }

            ApplyPose(appliedPosition, appliedRotation);
        }

        private void Initialize(
            Transform visualRoot,
            Transform cameraAnchor,
            bool enablePresenceCollider,
            bool enableVehicleSeat,
            bool showVehicleSeatPrompt,
            bool predictionEnabled,
            float predictionSeconds,
            float maxPredictionDistance,
            ManualLogSource logger,
            Action<string> sessionLogWrite)
        {
            _visualRoot = visualRoot != null ? visualRoot : transform;
            _cameraAnchor = cameraAnchor != null ? cameraAnchor : EnsureCameraAnchor();
            _presenceCollider = EnsurePresenceCollider();
            _presenceCollider.enabled = enablePresenceCollider;
            _vehicleSeatEnabled = enableVehicleSeat;
            _vehicleSeatPrompt = showVehicleSeatPrompt;
            _predictionEnabled = predictionEnabled;
            _predictionSeconds = Mathf.Clamp(predictionSeconds, 0f, 0.25f);
            _maxPredictionDistance = Mathf.Clamp(maxPredictionDistance, 0f, 2f);

            // Keep this off the game's story trigger layers. It is a co-op presence primitive,
            // not a second local gameplay actor.
            gameObject.layer = 2; // Ignore Raycast.
            if (_presenceCollider != null)
            {
                _presenceCollider.gameObject.layer = 2;
            }

            if (!_logged)
            {
                _logged = true;
                var line = "Co-op second player controller ready root=" + NetPath.GetPath(transform) +
                           " visual=" + (_visualRoot != null ? NetPath.GetPath(_visualRoot) : "-") +
                           " cameraAnchor=" + (_cameraAnchor != null ? NetPath.GetPath(_cameraAnchor) : "-") +
                           " presenceCollider=" + (enablePresenceCollider ? "enabled" : "disabled") +
                           " vehicleSeat=" + (enableVehicleSeat ? "enabled" : "disabled") +
                           " prediction=" + (_predictionEnabled ? _predictionSeconds.ToString("0.###") + "s" : "off");
                if (logger != null) logger.LogInfo(line);
                if (sessionLogWrite != null) sessionLogWrite(line);
            }
        }

        private void Update()
        {
            if (!_vehicleSeatEnabled)
            {
                return;
            }

            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift && Input.GetKeyDown(KeyCode.F12))
            {
                CycleSeatSide();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F12))
            {
                _seatbeltLocked = !_seatbeltLocked;
            }
        }

        private void LateUpdate()
        {
            if (!_vehicleSeatEnabled || !_seatbeltLocked || !_hasNetworkState)
            {
                return;
            }

            CoopVehiclePassengerSeat.SeatPose seatPose;
            if (!TryResolveVehicleSeat(out seatPose))
            {
                _hasLastSeatPose = false;
                return;
            }

            _lastSeatPose = seatPose;
            _hasLastSeatPose = true;

            var appliedPosition = PredictSeatPosition(seatPose);
            var appliedRotation = seatPose.Rotation;
            ApplyPose(appliedPosition, appliedRotation);

            if (_cameraAnchor != null)
            {
                _cameraAnchor.SetPositionAndRotation(_lastNetworkState.CameraPosition, _lastNetworkState.CameraRotation);
            }
        }

        private void OnGUI()
        {
            if (!_vehicleSeatEnabled || !_vehicleSeatPrompt)
            {
                return;
            }

            if (!_hasLastSeatPose)
            {
                CoopVehiclePassengerSeat.SeatPose pose;
                if (!TryResolveVehicleSeat(out pose))
                {
                    return;
                }

                _lastSeatPose = pose;
            }

            DrawSeatOverlay(_lastSeatPose);
        }

        private bool TryResolveVehicleSeat(out CoopVehiclePassengerSeat.SeatPose seatPose)
        {
            seatPose = default;
            if (!_vehicleSeatEnabled || !_seatbeltLocked)
            {
                return false;
            }

            if (!CoopVehiclePassengerSeat.TryResolve(_role, _requestedSeatSide, out seatPose))
            {
                return false;
            }

            MaybeLogSeat(seatPose);
            return true;
        }

        private Vector3 PredictSeatPosition(CoopVehiclePassengerSeat.SeatPose pose)
        {
            if (!_predictionEnabled || _predictionSeconds <= 0f || _maxPredictionDistance <= 0f ||
                pose.Velocity.sqrMagnitude < 0.0001f)
            {
                return pose.Position;
            }

            var predictedOffset = pose.Velocity * _predictionSeconds;
            predictedOffset.y = Mathf.Clamp(predictedOffset.y, -0.08f, 0.08f);
            if (predictedOffset.sqrMagnitude > _maxPredictionDistance * _maxPredictionDistance)
            {
                predictedOffset = Vector3.ClampMagnitude(predictedOffset, _maxPredictionDistance);
            }

            return pose.Position + predictedOffset;
        }

        private void ApplyPose(Vector3 position, Quaternion rotation)
        {
            var now = Time.realtimeSinceStartup;
            if (_hasSample)
            {
                var deltaTime = Mathf.Max(0.001f, now - _lastSampleTime);
                Velocity = (position - _lastPosition) / deltaTime;
            }
            else
            {
                Velocity = Vector3.zero;
                _hasSample = true;
            }

            _lastPosition = position;
            _lastSampleTime = now;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void CycleSeatSide()
        {
            if (_requestedSeatSide == CoopVehiclePassengerSeat.SeatSide.Auto)
            {
                _requestedSeatSide = CoopVehiclePassengerSeat.ResolveSide(_role, CoopVehiclePassengerSeat.SeatSide.Auto) == CoopVehiclePassengerSeat.SeatSide.BackLeft
                    ? CoopVehiclePassengerSeat.SeatSide.BackRight
                    : CoopVehiclePassengerSeat.SeatSide.BackLeft;
                return;
            }

            _requestedSeatSide = _requestedSeatSide == CoopVehiclePassengerSeat.SeatSide.BackLeft
                ? CoopVehiclePassengerSeat.SeatSide.BackRight
                : CoopVehiclePassengerSeat.SeatSide.BackLeft;
        }

        private void MaybeLogSeat(CoopVehiclePassengerSeat.SeatPose pose)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs < _nextSeatLogMs)
            {
                return;
            }

            _nextSeatLogMs = nowMs + 10000;
            Debug.Log("Co-op vehicle passenger seat active role=" + (_role ?? string.Empty) +
                      " name=" + (_displayName ?? string.Empty) +
                      " vehicle=" + pose.VehicleName +
                      " seat=" + pose.SeatName +
                      " fallback=" + pose.FallbackPose);
        }

        private void DrawSeatOverlay(CoopVehiclePassengerSeat.SeatPose pose)
        {
            EnsureSeatStyles();

            var width = 276f;
            var height = 68f;
            var x = Mathf.Max(16f, Screen.width - width - 22f);
            var y = Mathf.Max(92f, Screen.height - height - 86f);
            var rect = new Rect(x, y, width, height);

            var oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.color = new Color(1f, 0.38f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), _accentTexture);

            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 18f), "Passenger seat", _seatTitleStyle);
            var detail = (_seatbeltLocked ? "Seatbelt locked" : "Seatbelt unlocked") +
                         "  |  " + pose.SeatName +
                         "\nF12 toggle, Shift+F12 switch side";
            GUI.Label(new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, 34f), detail, _seatDetailStyle);

            GUI.color = oldColor;
        }

        private static void EnsureSeatStyles()
        {
            if (_panelTexture != null)
            {
                return;
            }

            _panelTexture = CreateTexture(new Color(0.02f, 0.018f, 0.015f, 0.72f));
            _accentTexture = CreateTexture(Color.white);
            _seatTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.83f, 0.58f, 0.98f) },
                clipping = TextClipping.Clip
            };
            _seatDetailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.9f, 0.9f, 0.88f, 0.94f) },
                wordWrap = true,
                clipping = TextClipping.Clip
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Transform EnsureCameraAnchor()
        {
            var existing = transform.Find(CameraAnchorName);
            if (existing != null) return existing;

            var anchor = new GameObject(CameraAnchorName);
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            anchor.transform.localRotation = Quaternion.identity;
            return anchor.transform;
        }

        private CapsuleCollider EnsurePresenceCollider()
        {
            var existing = transform.Find(PresenceName);
            GameObject presenceObject;
            if (existing != null)
            {
                presenceObject = existing.gameObject;
            }
            else
            {
                presenceObject = new GameObject(PresenceName);
                presenceObject.transform.SetParent(transform, false);
                presenceObject.transform.localPosition = Vector3.zero;
                presenceObject.transform.localRotation = Quaternion.identity;
            }

            var collider = presenceObject.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = presenceObject.AddComponent<CapsuleCollider>();
            }

            collider.center = new Vector3(0f, 0.88f, 0f);
            collider.height = 1.76f;
            collider.radius = 0.28f;
            collider.direction = 1;
            collider.isTrigger = true;
            collider.enabled = false;
            return collider;
        }
    }
}
