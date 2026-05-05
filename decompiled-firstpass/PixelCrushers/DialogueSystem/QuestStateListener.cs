using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class QuestStateListener : MonoBehaviour
{
	[Serializable]
	public class QuestStateIndicatorLevel
	{
		[Tooltip("Quest state to listen for.")]
		public QuestState questState;

		[Tooltip("Conditions that must also be true.")]
		public Condition condition;

		[Tooltip("Indicator level to use when this quest state is reached.")]
		public int indicatorLevel;

		public UnityEvent onEnterState = new UnityEvent();
	}

	[Serializable]
	public class QuestEntryStateIndicatorLevel
	{
		[Tooltip("Quest entry number.")]
		public int entryNumber;

		[Tooltip("Quest entry state to listen for.")]
		public QuestState questState;

		[Tooltip("Conditions that must also be true.")]
		public Condition condition;

		[Tooltip("Indicator level to use when this quest state is reached.")]
		public int indicatorLevel;

		public UnityEvent onEnterState = new UnityEvent();
	}

	[QuestPopup(true)]
	public string questName;

	public QuestStateIndicatorLevel[] questStateIndicatorLevels = new QuestStateIndicatorLevel[0];

	public QuestEntryStateIndicatorLevel[] questEntryStateIndicatorLevels = new QuestEntryStateIndicatorLevel[0];

	[Tooltip("When starting component, do not invoke any OnEnterState() events.")]
	public bool suppressOnEnterStateEventsOnStart;

	protected QuestStateDispatcher m_questStateDispatcher;

	protected QuestStateIndicator m_questStateIndicator;

	private bool m_started;

	protected bool m_suppressOnEnterStateEvent;

	protected QuestStateDispatcher questStateDispatcher
	{
		get
		{
			if (m_questStateDispatcher == null)
			{
				if (DialogueManager.instance != null)
				{
					m_questStateDispatcher = DialogueManager.instance.GetComponent<QuestStateDispatcher>();
					if (m_questStateDispatcher == null)
					{
						m_questStateDispatcher = GameObjectUtility.FindFirstObjectByType<QuestStateDispatcher>();
						if (m_questStateDispatcher == null)
						{
							m_questStateDispatcher = DialogueManager.instance.gameObject.AddComponent<QuestStateDispatcher>();
						}
					}
				}
				else
				{
					m_questStateDispatcher = GameObjectUtility.FindFirstObjectByType<QuestStateDispatcher>();
					if (m_questStateDispatcher == null)
					{
						GameObject gameObject = new GameObject("QuestStateDispatcher");
						UnityEngine.Object.DontDestroyOnLoad(gameObject);
						m_questStateDispatcher = gameObject.AddComponent<QuestStateDispatcher>();
					}
				}
			}
			return m_questStateDispatcher;
		}
	}

	protected QuestStateIndicator questStateIndicator
	{
		get
		{
			if (m_questStateIndicator == null)
			{
				m_questStateIndicator = GetComponent<QuestStateIndicator>();
			}
			return m_questStateIndicator;
		}
	}

	protected bool started
	{
		get
		{
			return m_started;
		}
		set
		{
			m_started = value;
		}
	}

	protected virtual void OnApplicationQuit()
	{
		base.enabled = false;
	}

	protected virtual IEnumerator Start()
	{
		if (!base.enabled)
		{
			yield break;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: " + base.name + ": Listening for state changes to quest '" + questName + "'.", this);
		}
		started = true;
		if (questStateDispatcher == null)
		{
			if (DialogueDebug.logErrors)
			{
				Debug.LogWarning("Dialogue System: Unexpected error. Quest State Listener on " + base.name + " can't find or create a Quest State Dispatcher.", this);
			}
		}
		else
		{
			questStateDispatcher.AddListener(this);
		}
		yield return null;
		m_suppressOnEnterStateEvent = suppressOnEnterStateEventsOnStart;
		UpdateIndicator();
		m_suppressOnEnterStateEvent = false;
	}

	protected virtual void OnEnable()
	{
		if (started)
		{
			questStateDispatcher.AddListener(this);
		}
	}

	protected virtual void OnDisable()
	{
		if (m_questStateDispatcher != null)
		{
			m_questStateDispatcher.RemoveListener(this);
		}
	}

	public virtual void OnChange()
	{
		UpdateIndicator();
	}

	public virtual void UpdateIndicator()
	{
		QuestState questState = QuestLog.GetQuestState(questName);
		for (int i = 0; i < questStateIndicatorLevels.Length; i++)
		{
			QuestStateIndicatorLevel questStateIndicatorLevel = questStateIndicatorLevels[i];
			if ((questState & questStateIndicatorLevel.questState) != 0 && questStateIndicatorLevel.condition.IsTrue(null))
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log("Dialogue System: " + base.name + ": Quest '" + questName + "' changed to state " + questState.ToString() + ".", this);
				}
				if (questStateIndicator != null)
				{
					questStateIndicator.SetIndicatorLevel(this, questStateIndicatorLevel.indicatorLevel);
				}
				if (!m_suppressOnEnterStateEvent)
				{
					questStateIndicatorLevel.onEnterState.Invoke();
				}
			}
		}
		for (int j = 0; j < questEntryStateIndicatorLevels.Length; j++)
		{
			QuestEntryStateIndicatorLevel questEntryStateIndicatorLevel = questEntryStateIndicatorLevels[j];
			QuestState questEntryState = QuestLog.GetQuestEntryState(questName, questEntryStateIndicatorLevel.entryNumber);
			if ((questEntryState & questEntryStateIndicatorLevel.questState) != 0 && questEntryStateIndicatorLevel.condition.IsTrue(null))
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log("Dialogue System: " + base.name + ": Quest '" + questName + "' entry " + questEntryStateIndicatorLevel.entryNumber + " changed to state " + questEntryState.ToString() + ".", this);
				}
				if (questStateIndicator != null)
				{
					questStateIndicator.SetIndicatorLevel(this, questEntryStateIndicatorLevel.indicatorLevel);
				}
				if (!m_suppressOnEnterStateEvent)
				{
					questEntryStateIndicatorLevel.onEnterState.Invoke();
				}
			}
		}
	}
}
