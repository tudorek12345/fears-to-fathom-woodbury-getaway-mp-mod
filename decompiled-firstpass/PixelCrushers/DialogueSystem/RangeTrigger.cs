using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class RangeTrigger : MonoBehaviour
{
	[Tooltip("These conditions must be true for the Range Trigger to affect GameObjects and components and invoke events")]
	public Condition condition;

	[Tooltip("Activate these GameObjects on trigger enter, deactivate them on trigger exit")]
	public GameObject[] gameObjects;

	[Tooltip("Enable these components on trigger enter, disable them on trigger exit")]
	public Component[] components;

	public UnityEvent onEnter = new UnityEvent();

	public UnityEvent onExit = new UnityEvent();

	public void OnTriggerEnter(Collider other)
	{
		if (condition.IsTrue(other.transform))
		{
			SetTargets(value: true);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (condition.IsTrue(other.transform))
		{
			SetTargets(value: false);
		}
	}

	private void SetTargets(bool value)
	{
		GameObject[] array = gameObjects;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(value);
			}
		}
		Component[] array2 = components;
		for (int i = 0; i < array2.Length; i++)
		{
			Tools.SetComponentEnabled(array2[i], (!value) ? Toggle.False : Toggle.True);
		}
		if (value)
		{
			onEnter.Invoke();
		}
		else
		{
			onExit.Invoke();
		}
	}
}
