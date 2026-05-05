using UnityEngine;
using UnityEngine.EventSystems;

namespace EPOOutline.Demo;

public class InteractableObject : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	[SerializeField]
	private AudioClip interactionSound;

	[SerializeField]
	private bool affectOutlinable = true;

	private Outlinable outlinable;

	private void Start()
	{
		if (affectOutlinable)
		{
			outlinable = GetComponent<Outlinable>();
			outlinable.enabled = false;
			outlinable.FrontParameters.FillPass.SetFloat("_PublicAngle", 35f);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (affectOutlinable)
		{
			outlinable.enabled = true;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (affectOutlinable)
		{
			outlinable.enabled = false;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		AudioSource.PlayClipAtPoint(interactionSound, base.transform.position, 1f);
	}
}
