using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopClientController
    {
        private readonly Camera _camera;
        private float _yaw;
        private float _pitch;

        private readonly float _moveSpeed = 4f;
        private readonly float _lookSpeed = 2f;

        public CoopClientController(Camera camera)
        {
            _camera = camera;
            var rot = _camera.transform.rotation.eulerAngles;
            _yaw = rot.y;
            _pitch = rot.x;
        }

        public void Update()
        {
            var mouseX = Input.GetAxis("Mouse X") * _lookSpeed;
            var mouseY = Input.GetAxis("Mouse Y") * _lookSpeed;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (Input.GetKey(KeyCode.Space)) move.y += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) move.y -= 1f;

            var speed = Input.GetKey(KeyCode.LeftShift) ? _moveSpeed * 1.5f : _moveSpeed;
            var delta = _camera.transform.TransformDirection(move.normalized) * speed * Time.unscaledDeltaTime;
            _camera.transform.position += delta;
        }
    }
}
