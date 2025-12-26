using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    public sealed class RemoteAvatar
    {
        private GameObject _root;
        private Transform _cameraAnchor;
        private readonly string _name;
        private readonly Color _color;

        public Transform Root => _root.transform;
        public Transform CameraAnchor => _cameraAnchor;

        public RemoteAvatar(string name, Color color)
        {
            _name = name;
            _color = color;
            CreateRoot();
        }

        public void SetActive(bool value)
        {
            EnsureAlive();
            if (_root == null) return;
            try
            {
                _root.SetActive(value);
            }
            catch
            {
                _root = null;
                _cameraAnchor = null;
            }
        }

        public void EnsureAlive()
        {
            if (_root != null) return;
            CreateRoot();
        }

        private void CreateRoot()
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _root.name = _name;
            _root.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);

            var renderer = _root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = _color;
            }

            Object.Destroy(_root.GetComponent<Collider>());

            var head = new GameObject(_name + "_Head");
            head.transform.SetParent(_root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            _cameraAnchor = head.transform;
        }
    }
}
