using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopClientInteractor
    {
        private readonly CoopClient _client;
        private readonly float _range;
        private readonly System.Func<bool> _suppressWorldClick;

        public CoopClientInteractor(CoopClient client, System.Func<bool> suppressWorldClick = null, float range = 3f)
        {
            _client = client;
            _suppressWorldClick = suppressWorldClick;
            _range = range;
        }

        public void Update(Camera camera)
        {
            if (_client == null || !_client.IsConnected || camera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (_suppressWorldClick != null && _suppressWorldClick())
                {
                    return;
                }

                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, _range))
                {
                    var interactable = hit.collider.GetComponent<Iinteractable>();
                    if (interactable == null)
                    {
                        interactable = hit.collider.GetComponentInParent<Iinteractable>();
                    }
                    if (interactable == null)
                    {
                        interactable = hit.collider.GetComponentInChildren<Iinteractable>();
                    }
                    if (interactable != null)
                    {
                        var path = NetPath.GetPath(interactable.gameObject.transform);
                        _client.Enqueue(new InteractRequestMessage(1, path, 0));
                    }
                }
            }
        }
    }
}
