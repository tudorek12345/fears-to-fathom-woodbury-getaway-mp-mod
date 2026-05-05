using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LuaTrigger : DialogueEventStarter
{
	[DialogueTriggerEvent]
	public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

	public Condition condition;

	[LuaScriptWizard(false)]
	public string luaCode;

	private bool tryingToStart;

	private bool listenForOnDestroy;

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnBarkEnd)
		{
			TryStart(actor);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnConversationEnd)
		{
			TryStart(actor);
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnSequenceEnd)
		{
			TryStart(actor);
		}
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(actor);
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerEnter)
		{
			TryStart(other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStart(other.transform);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnCollisionEnter)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void Start()
	{
		if (trigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(StartAfterOneFrame());
		}
	}

	public void OnEnable()
	{
		listenForOnDestroy = true;
		if (trigger == DialogueTriggerEvent.OnEnable)
		{
			StartCoroutine(StartAfterOneFrame());
		}
	}

	public void OnDisable()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDisable)
		{
			TryStart(null);
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
			TryStart(null);
		}
	}

	private IEnumerator StartAfterOneFrame()
	{
		yield return null;
		TryStart(null);
	}

	public void TryStart(Transform actor)
	{
		if (tryingToStart)
		{
			return;
		}
		tryingToStart = true;
		try
		{
			if ((condition == null || condition.IsTrue(actor)) && !string.IsNullOrEmpty(luaCode))
			{
				Lua.Run(luaCode, DialogueDebug.logInfo);
				DialogueManager.CheckAlerts();
				DialogueManager.SendUpdateTracker();
				DestroyIfOnce();
			}
		}
		finally
		{
			tryingToStart = false;
		}
	}
}
