using System.Collections;
using UnityEngine;

public class DoubleSidedTriggerDoor : MonoBehaviour
{
	[SerializeField]
	private float openAngle = 90f;

	[SerializeField]
	private float rotationSeconds = 0.8f;

	[SerializeField]
	private float closeDelay = 0.5f;

	[SerializeField]
	private Transform doorTransform;

	[SerializeField]
	private DoorTrigger leftSideTrigger;

	[SerializeField]
	private DoorTrigger rightSideTrigger;

	[SerializeField]
	private bool isOpen;

	private bool rotating;

	private bool disableLeftSide;

	private bool disableRightSide;

	private Vector3 closedDoorRotation;

	private Vector3 openedDoorRightRotation;

	private Vector3 openedDoorLeftRotation;

	private void Start()
	{
		closedDoorRotation = doorTransform.eulerAngles;
		openedDoorRightRotation = doorTransform.eulerAngles + Vector3.up * openAngle;
		openedDoorLeftRotation = doorTransform.eulerAngles + Vector3.up * (0f - openAngle);
		if (isOpen)
		{
			doorTransform.eulerAngles = openedDoorRightRotation;
		}
	}

	private void Update()
	{
		if (!leftSideTrigger.InUse && !rightSideTrigger.InUse)
		{
			disableRightSide = false;
			disableLeftSide = false;
		}
	}

	public void OpenViaLeftSide()
	{
		if (!disableLeftSide && !rotating)
		{
			StartCoroutine(LerpEulerAngles(openedDoorRightRotation));
			disableRightSide = true;
		}
	}

	public void OpenViaRightSide()
	{
		if (!disableRightSide && !rotating)
		{
			StartCoroutine(LerpEulerAngles(openedDoorLeftRotation));
			disableLeftSide = true;
		}
	}

	public void CloseViaLeftSide()
	{
		StartCoroutine(CloseWithDelay(closedDoorRotation));
	}

	public void CloseViaRightSide()
	{
		StartCoroutine(CloseWithDelay(closedDoorRotation));
	}

	private IEnumerator CloseWithDelay(Vector3 targetRotation)
	{
		while (disableLeftSide || disableRightSide)
		{
			yield return null;
		}
		yield return new WaitForSeconds(closeDelay);
		StartCoroutine(LerpEulerAngles(targetRotation));
	}

	public IEnumerator LerpEulerAngles(Vector3 targetRotation)
	{
		if (!rotating)
		{
			rotating = true;
			Vector3 initialRotation = doorTransform.eulerAngles;
			float timer = 0f;
			while (timer <= rotationSeconds)
			{
				float y = Mathf.LerpAngle(initialRotation.y, targetRotation.y, timer / rotationSeconds);
				doorTransform.eulerAngles = new Vector3(initialRotation.x, y, initialRotation.z);
				timer += Time.deltaTime;
				yield return null;
			}
			doorTransform.eulerAngles = targetRotation;
			rotating = false;
		}
	}
}
