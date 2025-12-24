using UnityEngine;
using UnityEngine.Events;

public class DoorTrigger : MonoBehaviour
{
	private bool inUse;

	public string targetTag = "DoorNPC";

	public UnityEvent onEnterEvent;

	public UnityEvent onExitEvent;

	public bool InUse => inUse;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag(targetTag))
		{
			onEnterEvent.Invoke();
			inUse = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag(targetTag))
		{
			onExitEvent.Invoke();
			inUse = false;
		}
	}
}
