using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ExtraDatabases : MonoBehaviour
{
	public DialogueTriggerEvent addTrigger = DialogueTriggerEvent.OnStart;

	public DialogueTriggerEvent removeTrigger = DialogueTriggerEvent.None;

	public DialogueDatabase[] databases = new DialogueDatabase[0];

	public Condition condition = new Condition();

	[Tooltip("As soon as one event (add or remove) has occurred, destroy this component.")]
	public bool once;

	[Tooltip("Add/remove one database per frame instead of adding them all at the same time. Useful to avoid stutter when adding several databases.")]
	public bool onePerFrame;

	protected bool m_trying;

	protected Coroutine m_destroyCoroutine;

	protected int m_numActiveCoroutines;

	public static event Action addedDatabases;

	public static event Action removedDatabases;

	protected virtual void TryAddDatabases(Transform interactor, bool onePerFrame)
	{
		if (m_trying)
		{
			return;
		}
		m_trying = true;
		try
		{
			if (condition == null || condition.IsTrue(interactor))
			{
				AddDatabases(onePerFrame);
			}
		}
		finally
		{
			m_trying = false;
		}
	}

	public virtual void AddDatabases(bool onePerFrame)
	{
		if (onePerFrame)
		{
			StartCoroutine(AddDatabasesCoroutine());
		}
		else if (base.gameObject.activeInHierarchy && base.enabled)
		{
			AddDatabasesImmediate();
		}
	}

	protected virtual void AddDatabasesImmediate()
	{
		DialogueDatabase[] array = databases;
		foreach (DialogueDatabase database in array)
		{
			AddDatabase(database);
		}
		ExtraDatabases.addedDatabases();
		if (once)
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	protected virtual IEnumerator AddDatabasesCoroutine()
	{
		m_numActiveCoroutines++;
		if (once && m_destroyCoroutine == null)
		{
			m_destroyCoroutine = StartCoroutine(DestroyCoroutine());
		}
		DialogueDatabase[] array = databases;
		foreach (DialogueDatabase database in array)
		{
			AddDatabase(database);
			yield return null;
		}
		ExtraDatabases.addedDatabases();
		m_numActiveCoroutines--;
	}

	protected virtual void AddDatabase(DialogueDatabase database)
	{
		if (database != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Adding database " + database.name, this);
			}
			DialogueManager.AddDatabase(database);
		}
	}

	protected virtual void TryRemoveDatabases(Transform interactor, bool onePerFrame)
	{
		if (m_trying)
		{
			return;
		}
		m_trying = true;
		try
		{
			if (condition == null || condition.IsTrue(interactor))
			{
				RemoveDatabases(onePerFrame);
			}
		}
		finally
		{
			m_trying = false;
		}
	}

	public virtual void RemoveDatabases(bool onePerFrame)
	{
		if (onePerFrame)
		{
			StartCoroutine(RemoveDatabasesCoroutine());
		}
		else if (base.gameObject.activeInHierarchy && base.enabled)
		{
			RemoveDatabasesImmediate();
		}
	}

	protected virtual void RemoveDatabasesImmediate()
	{
		DialogueDatabase[] array = databases;
		foreach (DialogueDatabase database in array)
		{
			RemoveDatabase(database);
		}
		ExtraDatabases.removedDatabases();
		if (once)
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	protected virtual IEnumerator RemoveDatabasesCoroutine()
	{
		m_numActiveCoroutines++;
		if (once && m_destroyCoroutine == null)
		{
			m_destroyCoroutine = StartCoroutine(DestroyCoroutine());
		}
		DialogueDatabase[] array = databases;
		foreach (DialogueDatabase database in array)
		{
			RemoveDatabase(database);
			yield return null;
		}
		ExtraDatabases.removedDatabases();
		m_numActiveCoroutines--;
	}

	protected virtual void RemoveDatabase(DialogueDatabase database)
	{
		if (database != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Removing database " + database.name, this);
			}
			DialogueManager.RemoveDatabase(database);
		}
	}

	protected virtual IEnumerator DestroyCoroutine()
	{
		while (m_numActiveCoroutines > 0)
		{
			yield return null;
		}
		m_destroyCoroutine = null;
		UnityEngine.Object.Destroy(this);
	}

	public virtual void Start()
	{
		if (addTrigger == DialogueTriggerEvent.OnStart || removeTrigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(StartEndOfFrame());
		}
	}

	protected virtual IEnumerator StartEndOfFrame()
	{
		yield return CoroutineUtility.endOfFrame;
		if (addTrigger == DialogueTriggerEvent.OnStart)
		{
			TryAddDatabases(null, onePerFrame);
		}
		if (removeTrigger == DialogueTriggerEvent.OnStart)
		{
			TryRemoveDatabases(null, onePerFrame);
		}
	}

	public virtual void OnEnable()
	{
		if (addTrigger == DialogueTriggerEvent.OnEnable)
		{
			TryAddDatabases(null, onePerFrame);
		}
		if (removeTrigger == DialogueTriggerEvent.OnEnable)
		{
			TryRemoveDatabases(null, onePerFrame);
		}
	}

	public virtual void OnDisable()
	{
		if (addTrigger == DialogueTriggerEvent.OnDisable)
		{
			TryAddDatabases(null, onePerFrame: false);
		}
		if (removeTrigger == DialogueTriggerEvent.OnDisable)
		{
			TryRemoveDatabases(null, onePerFrame: false);
		}
	}

	public virtual void OnDestroy()
	{
		if (addTrigger == DialogueTriggerEvent.OnDestroy)
		{
			TryAddDatabases(null, onePerFrame: false);
		}
		if (removeTrigger == DialogueTriggerEvent.OnDestroy)
		{
			TryRemoveDatabases(null, onePerFrame: false);
		}
	}

	public virtual void OnUse(Transform actor)
	{
		if (base.enabled)
		{
			if (addTrigger == DialogueTriggerEvent.OnUse)
			{
				TryAddDatabases(actor, onePerFrame);
			}
			if (removeTrigger == DialogueTriggerEvent.OnUse)
			{
				TryRemoveDatabases(actor, onePerFrame);
			}
		}
	}

	public virtual void OnUse(string message)
	{
		if (base.enabled)
		{
			if (addTrigger == DialogueTriggerEvent.OnUse)
			{
				TryAddDatabases(null, onePerFrame);
			}
			if (removeTrigger == DialogueTriggerEvent.OnUse)
			{
				TryRemoveDatabases(null, onePerFrame);
			}
		}
	}

	public virtual void OnUse()
	{
		if (base.enabled)
		{
			if (addTrigger == DialogueTriggerEvent.OnUse)
			{
				TryAddDatabases(null, onePerFrame);
			}
			if (removeTrigger == DialogueTriggerEvent.OnUse)
			{
				TryRemoveDatabases(null, onePerFrame);
			}
		}
	}

	public virtual void OnTriggerEnter(Collider other)
	{
		if (base.enabled)
		{
			if (addTrigger == DialogueTriggerEvent.OnTriggerEnter)
			{
				TryAddDatabases(other.transform, onePerFrame);
			}
			if (removeTrigger == DialogueTriggerEvent.OnTriggerEnter)
			{
				TryRemoveDatabases(other.transform, onePerFrame);
			}
		}
	}

	public virtual void OnTriggerExit(Collider other)
	{
		if (base.enabled)
		{
			if (addTrigger == DialogueTriggerEvent.OnTriggerExit)
			{
				TryAddDatabases(other.transform, onePerFrame);
			}
			if (removeTrigger == DialogueTriggerEvent.OnTriggerExit)
			{
				TryRemoveDatabases(other.transform, onePerFrame);
			}
		}
	}

	static ExtraDatabases()
	{
		ExtraDatabases.addedDatabases = delegate
		{
		};
		ExtraDatabases.removedDatabases = delegate
		{
		};
	}
}
