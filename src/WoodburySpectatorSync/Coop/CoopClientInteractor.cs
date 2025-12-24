using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopClientInteractor
    {
        private readonly CoopClient _client;
        private readonly float _range;

        public CoopClientInteractor(CoopClient client, float range = 3f)
        {
            _client = client;
            _range = range;
        }

        public void Update(Camera camera)
        {
            if (_client == null || !_client.IsConnected || camera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, _range))
                {
                    var interactable = hit.collider.GetComponent<Iinteractable>();
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
