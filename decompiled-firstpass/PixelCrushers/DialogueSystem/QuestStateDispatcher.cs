using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class QuestStateDispatcher : MonoBehaviour
{
	private List<QuestStateListener> m_listeners = new List<QuestStateListener>();

	public List<QuestStateListener> listeners => m_listeners;

	protected virtual void OnEnable()
	{
		SaveSystem.saveDataApplied += UpdateListeners;
	}

	protected virtual void OnDisable()
	{
		SaveSystem.saveDataApplied -= UpdateListeners;
	}

	public virtual void AddListener(QuestStateListener listener)
	{
		if (!(listener == null))
		{
			m_listeners.Add(listener);
		}
	}

	public virtual void RemoveListener(QuestStateListener listener)
	{
		m_listeners.Remove(listener);
	}

	private void UpdateListeners()
	{
		for (int i = 0; i < m_listeners.Count; i++)
		{
			QuestStateListener questStateListener = m_listeners[i];
			if (!(questStateListener == null))
			{
				questStateListener.UpdateIndicator();
			}
		}
	}

	public virtual void OnQuestStateChange(string questName)
	{
		for (int i = 0; i < m_listeners.Count; i++)
		{
			QuestStateListener questStateListener = m_listeners[i];
			if (!(questStateListener == null) && string.Equals(questName, questStateListener.questName))
			{
				questStateListener.OnChange();
			}
		}
	}
}
