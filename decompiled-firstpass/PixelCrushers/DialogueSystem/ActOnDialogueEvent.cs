using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public abstract class ActOnDialogueEvent : MonoBehaviour
{
	[Serializable]
	public class Action
	{
		public Condition condition = new Condition();
	}

	[Tooltip("Trigger when this dialogue event occurs.")]
	public DialogueEvent trigger;

	[Tooltip("Destroy this component after triggering. If you need to remember across scene changes and saved games, use a Condition instead.")]
	public bool once;

	[HideInInspector]
	public DialogueDatabase selectedDatabase;

	public void OnBarkStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnBark)
		{
			TryStartActions(actor);
		}
	}

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnBark)
		{
			TryEndActions(actor);
			DestroyIfOnce();
		}
	}

	public void OnConversationStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnConversation)
		{
			TryStartActions(actor);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnConversation)
		{
			TryEndActions(actor);
			DestroyIfOnce();
		}
	}

	public void OnSequenceStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnSequence)
		{
			TryStartActions(actor);
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueEvent.OnSequence)
		{
			TryEndActions(actor);
			DestroyIfOnce();
		}
	}

	public abstract void TryStartActions(Transform actor);

	public abstract void TryEndActions(Transform actor);

	private void DestroyIfOnce()
	{
		if (once)
		{
			UnityEngine.Object.Destroy(this);
		}
	}
}
