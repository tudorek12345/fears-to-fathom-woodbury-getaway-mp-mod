using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    public sealed class RemoteAvatar
    {
        private readonly GameObject _root;
        private readonly Transform _cameraAnchor;

        public Transform Root => _root.transform;
        public Transform CameraAnchor => _cameraAnchor;

        public RemoteAvatar(string name, Color color)
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _root.name = name;
            _root.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);

            var renderer = _root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Object.Destroy(_root.GetComponent<Collider>());

            var head = new GameObject(name + "_Head");
            head.transform.SetParent(_root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            _cameraAnchor = head.transform;
        }

        public void SetActive(bool value)
        {
            _root.SetActive(value);
        }
    }
}
