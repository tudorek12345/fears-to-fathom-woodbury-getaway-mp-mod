using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelCrushers;

public static class UIUtility
{
	public static void RequireEventSystem(string message = null)
	{
		if ((Object)(object)GameObjectUtility.FindFirstObjectByType<EventSystem>() == null)
		{
			if (message != null)
			{
				Debug.LogWarning(message);
			}
			((Component)(object)new GameObject("EventSystem").AddComponent<EventSystem>()).gameObject.AddComponent<StandaloneInputModule>();
		}
	}

	public static void SetEventSystemInChildren(Transform t, EventSystem eventSystem)
	{
		if (t == null)
		{
			return;
		}
		IEventSystemUser component = t.GetComponent<IEventSystemUser>();
		if (component != null)
		{
			component.eventSystem = eventSystem;
		}
		foreach (Transform item in t)
		{
			SetEventSystemInChildren(item, eventSystem);
		}
	}

	public static int GetAnimatorNameHash(AnimatorStateInfo animatorStateInfo)
	{
		return animatorStateInfo.fullPathHash;
	}

	public static void Select(Selectable selectable, bool allowStealFocus = true, EventSystem eventSystem = null)
	{
		EventSystem val = (((Object)(object)eventSystem != null) ? eventSystem : EventSystem.current);
		if (!((Object)(object)val == null) && !((Object)(object)selectable == null) && !val.alreadySelecting && (val.currentSelectedGameObject == null || allowStealFocus))
		{
			EventSystem.current = val;
			val.SetSelectedGameObject(((Component)(object)selectable).gameObject);
			selectable.Select();
			selectable.OnSelect((BaseEventData)null);
		}
	}

	public static Font GetDefaultFont()
	{
		return Resources.GetBuiltinResource<Font>((SafeConvert.ToInt(Application.unityVersion.Split('.')[0]) >= 2022) ? "LegacyRuntime.ttf" : "Arial.ttf");
	}
}
