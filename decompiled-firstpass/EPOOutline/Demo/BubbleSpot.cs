using System.Collections;
using UnityEngine;

namespace EPOOutline.Demo;

public class BubbleSpot : MonoBehaviour
{
	[SerializeField]
	private Transform trackPosition;

	[SerializeField]
	private Vector3 trackShift;

	[SerializeField]
	private Camera targetCamera;

	[SerializeField]
	private Transform bubble;

	[SerializeField]
	private bool visibleFromBegining;

	[SerializeField]
	private float showDelay;

	[SerializeField]
	private float showDuration = 5f;

	[SerializeField]
	private bool once;

	private bool wasShown;

	private int playersInside;

	private IEnumerator Start()
	{
		Hide(0f);
		if (visibleFromBegining)
		{
			yield return new WaitForSeconds(showDelay);
			Show();
			yield return new WaitForSeconds(showDuration);
			Hide();
		}
	}

	private void Reset()
	{
		targetCamera = Object.FindObjectOfType<Camera>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<Character>() && playersInside++ == 0)
		{
			Show();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<Character>() && --playersInside == 0)
		{
			Hide();
		}
	}

	private void Show()
	{
		if (!wasShown || !once)
		{
			wasShown = true;
			Show(0.5f);
		}
	}

	private void Hide()
	{
		Hide(0.15f);
	}

	private void Hide(float duration)
	{
		bubble.gameObject.SetActive(value: false);
	}

	private void Show(float duration)
	{
		bubble.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if ((bool)trackPosition)
		{
			base.transform.position = trackPosition.position + trackShift;
		}
		bubble.transform.position = targetCamera.WorldToScreenPoint(base.transform.position);
	}
}
