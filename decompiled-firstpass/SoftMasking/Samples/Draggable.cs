using UnityEngine;
using UnityEngine.EventSystems;

namespace SoftMasking.Samples;

[RequireComponent(typeof(RectTransform))]
public class Draggable : UIBehaviour, IDragHandler, IEventSystemHandler
{
	private RectTransform _rectTransform;

	protected override void Awake()
	{
		((UIBehaviour)this).Awake();
		_rectTransform = ((Component)this).GetComponent<RectTransform>();
	}

	public void OnDrag(PointerEventData eventData)
	{
		_rectTransform.anchoredPosition += eventData.delta;
	}
}
