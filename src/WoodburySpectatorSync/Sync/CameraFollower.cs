using System;
using UnityEngine;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Sync
{
    public sealed class CameraFollower
    {
        private readonly Settings _settings;
        private Camera _camera;
        private bool _createdCamera;
        private bool _hasTarget;
        private CameraState _target;

        public CameraFollower(Settings settings)
        {
            _settings = settings;
        }

        public void SetTarget(CameraState state)
        {
            _target = state;
            _hasTarget = true;
        }

        public void ResetTarget()
        {
            _hasTarget = false;
        }

        public void Update(bool connected)
        {
            if (!connected)
            {
                return;
            }

            EnsureCamera();
            ApplySpectatorLockdown();

            if (!_hasTarget || _camera == null)
            {
                return;
            }

            var transform = _camera.transform;
            var posAlpha = Mathf.Clamp01(_settings.SmoothingPosition.Value);
            var rotAlpha = Mathf.Clamp01(_settings.SmoothingRotation.Value);

            if (posAlpha <= 0f && rotAlpha <= 0f)
            {
                transform.position = _target.Position;
                transform.rotation = _target.Rotation;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _target.Position, posAlpha);
                transform.rotation = Quaternion.Slerp(transform.rotation, _target.Rotation, rotAlpha);
            }

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _target.Fov, posAlpha > 0f ? posAlpha : 1f);
        }

        private void EnsureCamera()
        {
            if (_camera != null) return;

            _camera = Camera.main;
            if (_camera == null)
            {
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    _camera = cameras[0];
                }
            }

            if (_camera == null)
            {
                var go = new GameObject("SpectatorCamera");
                _camera = go.AddComponent<Camera>();
                go.tag = "MainCamera";
                _createdCamera = true;
            }

            if (_createdCamera)
            {
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam != _camera && cam.enabled)
                    {
                        cam.enabled = false;
                    }
                }
            }
        }

        private void ApplySpectatorLockdown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
            if (listeners.Length > 1 && _camera != null)
            {
                var listener = _camera.GetComponent<AudioListener>();
                if (listener != null && listener.enabled)
                {
                    listener.enabled = false;
                }
            }
        }
    }
}
