using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConversationTrigger : ConversationStarter
{
	[Tooltip("The primary actor (e.g., player). If unassigned, the GameObject that triggered the conversation.")]
	public Transform actor;

	[Tooltip("Try to start the conversation when this event occurs.")]
	[DialogueTriggerEvent]
	public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

	[Tooltip("Stop the triggered conversation if this GameObject receives OnTriggerExit.")]
	public bool stopConversationOnTriggerExit;

	private float earliestTimeToAllowTriggerExit;

	private const float MarginToAllowTriggerExit = 0.2f;

	private bool listenForOnDestroy;

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnBarkEnd)
		{
			TryStartConversation(Tools.Select(this.actor, actor));
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnConversationEnd)
		{
			TryStartConversation(Tools.Select(this.actor, actor));
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnSequenceEnd)
		{
			TryStartConversation(Tools.Select(this.actor, actor));
		}
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartConversation(Tools.Select(this.actor, actor));
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartConversation(actor);
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStartConversation(actor);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerEnter)
		{
			TryStartConversationOnTriggerEnter(other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		CheckOnTriggerExit(other.transform);
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnCollisionEnter)
		{
			TryStartConversation(collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStartConversation(collision.collider.transform);
		}
	}

	private void TryStartConversationOnTriggerEnter(Transform otherTransform)
	{
		if (!(otherTransform != actor) || condition.IsTrue(otherTransform))
		{
			TryStartConversation(Tools.Select(actor, otherTransform), otherTransform);
			earliestTimeToAllowTriggerExit = Time.time + 0.2f;
		}
	}

	private void CheckOnTriggerExit(Transform otherTransform)
	{
		if (!base.enabled)
		{
			return;
		}
		if (stopConversationOnTriggerExit && DialogueManager.isConversationActive && Time.time > earliestTimeToAllowTriggerExit && (DialogueManager.currentActor == otherTransform || DialogueManager.currentConversant == otherTransform))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Stopping conversation because {1} exited trigger area.", new object[2] { "Dialogue System", otherTransform.name }));
			}
			DialogueManager.StopConversation();
		}
		else if (trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStartConversationOnTriggerEnter(otherTransform);
		}
	}

	public void Start()
	{
		if (trigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(StartConversationAfterOneFrame());
		}
	}

	public void OnEnable()
	{
		listenForOnDestroy = true;
		if (trigger == DialogueTriggerEvent.OnEnable)
		{
			StartCoroutine(StartConversationAfterOneFrame());
		}
	}

	public void OnDisable()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDisable)
		{
			TryStartConversation(actor);
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
			TryStartConversation(actor);
		}
	}

	private IEnumerator StartConversationAfterOneFrame()
	{
		yield return null;
		TryStartConversation(actor);
	}
}
