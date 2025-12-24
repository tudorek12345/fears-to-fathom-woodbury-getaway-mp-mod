using System.Collections.Generic;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class RemotePlayerProxy
    {
        private readonly GameObject _root;
        private readonly CharacterController _characterController;
        private readonly Transform _cameraTransform;
        private readonly Animator _animator;
        private readonly HashSet<int> _animFloatParams = new HashSet<int>();
        private readonly HashSet<int> _animBoolParams = new HashSet<int>();

        public Transform Root => _root != null ? _root.transform : null;

        public RemotePlayerProxy(FirstPersonController source, Color tint)
        {
            _root = Object.Instantiate(source.gameObject, source.transform.position, source.transform.rotation);
            _root.name = "CoopRemotePlayer";

            var fpc = _root.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                fpc.enabled = false;
            }

            var cameras = _root.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.enabled = false;
            }

            var listeners = _root.GetComponentsInChildren<AudioListener>(true);
            foreach (var listener in listeners)
            {
                listener.enabled = false;
            }

            var audioSources = _root.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in audioSources)
            {
                audio.enabled = false;
            }

            _characterController = _root.GetComponent<CharacterController>();
            _cameraTransform = cameras.Length > 0 ? cameras[0].transform : null;

            _animator = _root.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                foreach (var param in _animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Float)
                    {
                        _animFloatParams.Add(param.nameHash);
                    }
                    else if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        _animBoolParams.Add(param.nameHash);
                    }
                }
            }

            EnsureVisibleBody(tint);
        }

        public void SetActive(bool value)
        {
            if (_root != null)
            {
                _root.SetActive(value);
            }
        }

        public void ApplyTransform(PlayerTransformState state)
        {
            if (_root == null) return;

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            _root.transform.SetPositionAndRotation(state.Position, state.Rotation);

            if (_cameraTransform != null)
            {
                _cameraTransform.rotation = state.CameraRotation;
                _cameraTransform.position = state.CameraPosition;
            }

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }
        }

        public void ApplyInput(PlayerInputState input)
        {
            if (_animator == null) return;

            SetFloatIfExists("MoveX", input.MoveX);
            SetFloatIfExists("MoveY", input.MoveY);
            SetFloatIfExists("Speed", new Vector2(input.MoveX, input.MoveY).magnitude);

            SetBoolIfExists("IsCrouching", input.Crouch);
            SetBoolIfExists("Crouch", input.Crouch);
            SetBoolIfExists("IsSprinting", input.Sprint);
            SetBoolIfExists("Sprint", input.Sprint);
            SetBoolIfExists("IsJumping", input.Jump);
            SetBoolIfExists("Jump", input.Jump);
        }

        private void SetFloatIfExists(string name, float value)
        {
            var hash = Animator.StringToHash(name);
            if (_animFloatParams.Contains(hash))
            {
                _animator.SetFloat(hash, value);
            }
        }

        private void SetBoolIfExists(string name, bool value)
        {
            var hash = Animator.StringToHash(name);
            if (_animBoolParams.Contains(hash))
            {
                _animator.SetBool(hash, value);
            }
        }

        private void EnsureVisibleBody(Color tint)
        {
            var renderers = _root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0) return;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(_root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = tint;
            }
        }
    }
}
