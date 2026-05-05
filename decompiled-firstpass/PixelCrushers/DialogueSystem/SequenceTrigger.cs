using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SequenceTrigger : SequenceStarter
{
	[Tooltip("Trigger that starts the sequence.")]
	[DialogueTriggerEvent]
	public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

	[Tooltip("Tick to wait one frame to allow other components to finish their OnStart/OnEnable.")]
	public bool waitOneFrameOnStartOrEnable = true;

	private bool listenForOnDestroy;

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnBarkEnd)
		{
			TryStartSequence(actor);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnConversationEnd)
		{
			TryStartSequence(actor);
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnSequenceEnd)
		{
			TryStartSequence(actor);
		}
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartSequence(actor);
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartSequence(null);
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartSequence(null);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerEnter)
		{
			TryStartSequence(other.transform, other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStartSequence(other.transform, other.transform);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnCollisionEnter)
		{
			TryStartSequence(collision.collider.transform, collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStartSequence(collision.collider.transform, collision.collider.transform);
		}
	}

	public void Start()
	{
		if (trigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(StartSequenceAfterOneFrame());
		}
	}

	public void OnEnable()
	{
		listenForOnDestroy = true;
		if (trigger == DialogueTriggerEvent.OnEnable)
		{
			StartCoroutine(StartSequenceAfterOneFrame());
		}
	}

	public void OnDisable()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDisable)
		{
			TryStartSequence(null);
		}
	}

	public void OnLevelWillBeUnloaded()
	{
		listenForOnDestroy = false;
	}

	public void OnApplicationQuit()
	{
		listenForOnDestroy = false;
	}

	public void OnDestroy()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDestroy)
		{
			TryStartSequence(null);
		}
	}

	private IEnumerator StartSequenceAfterOneFrame()
	{
		if (waitOneFrameOnStartOrEnable)
		{
			yield return null;
		}
		TryStartSequence(null);
	}
}
