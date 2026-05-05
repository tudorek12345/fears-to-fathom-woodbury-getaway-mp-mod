using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SetEventSystem : MonoBehaviour
{
	public EventSystem eventSystem;

	private void Start()
	{
		AssignEventSystemToHierarchy(eventSystem);
	}

	public void AssignEventSystemToHierarchy(EventSystem eventSystem)
	{
		this.eventSystem = eventSystem;
		UIUtility.SetEventSystemInChildren(base.transform, eventSystem);
	}
}
