using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkTrigger : BarkStarter
{
	[Tooltip("The target that the bark is directed to. If assigned, the target will get an OnBarkEnd event.")]
	public Transform target;

	[Tooltip("Event that starts the conversation.")]
	[DialogueTriggerEvent]
	public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

	private bool listenForOnDestroy;

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnBarkEnd)
		{
			TryBark(Tools.Select(target, actor));
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnConversationEnd)
		{
			TryBark(Tools.Select(target, actor));
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnSequenceEnd)
		{
			TryBark(Tools.Select(target, actor));
		}
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryBark(Tools.Select(target, actor));
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryBark(Tools.Select(target, null));
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryBark(Tools.Select(target, null));
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerEnter)
		{
			TryBark(Tools.Select(target, other.transform), other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryBark(Tools.Select(target, other.transform), other.transform);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnCollisionEnter)
		{
			TryBark(Tools.Select(target, collision.collider.transform), collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryBark(Tools.Select(target, collision.collider.transform), collision.collider.transform);
		}
	}

	protected override void Start()
	{
		base.Start();
		if (trigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(BarkAfterOneFrame());
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		listenForOnDestroy = true;
		if (trigger == DialogueTriggerEvent.OnEnable)
		{
			StartCoroutine(BarkAfterOneFrame());
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDisable)
		{
			TryBark(target);
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
			TryBark(target);
		}
	}

	private IEnumerator BarkAfterOneFrame()
	{
		yield return null;
		TryBark(target);
	}
}
