using UnityEngine.EventSystems;

namespace PixelCrushers;

public interface IEventSystemUser
{
	EventSystem eventSystem { get; set; }
}
