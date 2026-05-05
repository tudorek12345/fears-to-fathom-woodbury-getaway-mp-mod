using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
[RequireComponent(typeof(Selectable))]
public class DeselectPreviousOnPointerEnter : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IDeselectHandler, IEventSystemUser
{
	private EventSystem m_eventSystem;

	public GameObject panel;

	public EventSystem eventSystem
	{
		get
		{
			if ((Object)(object)m_eventSystem != null)
			{
				return m_eventSystem;
			}
			return EventSystem.current;
		}
		set
		{
			m_eventSystem = value;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!eventSystem.alreadySelecting)
		{
			eventSystem.SetSelectedGameObject(base.gameObject);
		}
	}

	public void OnDeselect(BaseEventData eventData)
	{
		GetComponent<Selectable>().OnPointerExit((PointerEventData)null);
	}
}
