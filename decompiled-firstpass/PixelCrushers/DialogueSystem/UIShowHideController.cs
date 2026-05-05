using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class UIShowHideController
{
	public enum TransitionMode
	{
		State,
		Trigger
	}

	public enum State
	{
		Undefined,
		Showing,
		Shown,
		Hiding,
		Hidden
	}

	public static float maxWaitDuration = 5f;

	private TransitionMode m_transitionMode;

	private Animator m_animator;

	private bool m_lookedForAnimator;

	private Coroutine m_animCoroutine;

	public Component panel { get; set; }

	public State state { get; set; }

	public bool debug { get; set; }

	public UIShowHideController(GameObject gameObjectToControl, Component panelToControl, TransitionMode animationMode = TransitionMode.Trigger, bool debug = false)
	{
		panel = panelToControl;
		m_animator = ((gameObjectToControl != null) ? gameObjectToControl.GetComponent<Animator>() : null);
		if (m_animator == null && panelToControl != null)
		{
			m_animator = panelToControl.GetComponent<Animator>();
		}
		state = State.Undefined;
		m_transitionMode = animationMode;
		m_animCoroutine = null;
		this.debug = debug;
	}

	public void Show(string showState, bool pauseAfterAnimation, Action callback, bool wait = true)
	{
		CancelCurrentAnim();
		switch (m_transitionMode)
		{
		case TransitionMode.State:
			m_animCoroutine = DialogueManager.instance.StartCoroutine(WaitForAnimationState(State.Showing, State.Shown, showState, pauseAfterAnimation, panelActive: true, wait, callback));
			break;
		case TransitionMode.Trigger:
			m_animCoroutine = DialogueManager.instance.StartCoroutine(WaitForAnimationTrigger(State.Showing, State.Shown, showState, pauseAfterAnimation, panelActive: true, wait, callback));
			break;
		}
	}

	public void Hide(string hideState, Action callback)
	{
		CancelCurrentAnim();
		switch (m_transitionMode)
		{
		case TransitionMode.State:
			m_animCoroutine = DialogueManager.instance.StartCoroutine(WaitForAnimationState(State.Hiding, State.Hidden, hideState, pauseAfterAnimation: false, panelActive: false, wait: true, callback));
			break;
		case TransitionMode.Trigger:
			m_animCoroutine = DialogueManager.instance.StartCoroutine(WaitForAnimationTrigger(State.Hiding, State.Hidden, hideState, pauseAfterAnimation: false, panelActive: false, wait: true, callback));
			break;
		}
	}

	private IEnumerator WaitForAnimationState(State stateWhenBegin, State stateWhenEnd, string stateName, bool pauseAfterAnimation, bool panelActive, bool wait, Action callback)
	{
		if (state != stateWhenEnd)
		{
			if (state == State.Hiding || state == State.Showing)
			{
				yield return null;
			}
			state = stateWhenBegin;
			if (panel != null && !panel.gameObject.activeSelf)
			{
				panel.gameObject.SetActive(value: true);
				yield return null;
			}
			if (CanTriggerAnimation(stateName))
			{
				if (debug)
				{
					Debug.Log("<color=green>" + panel.name + ".Animator.Play(" + stateName + ") time=" + Time.time + "</color>");
				}
				CheckAnimatorModeAndTimescale(stateName);
				m_animator.Play(stateName);
				if (wait)
				{
					yield return null;
					float num = ((m_animator != null) ? m_animator.GetCurrentAnimatorStateInfo(0).length : 0f);
					if (Mathf.Approximately(0f, Time.timeScale))
					{
						float timeout = Time.realtimeSinceStartup + num;
						while (Time.realtimeSinceStartup < timeout)
						{
							yield return null;
						}
					}
					else
					{
						yield return new WaitForSeconds(num);
					}
					if (debug)
					{
						Debug.Log("<color=red>... finished " + panel.name + ".Animator.Play(" + stateName + ") time=" + Time.time + "</color>");
					}
				}
			}
		}
		if (!panelActive)
		{
			Tools.SetGameObjectActive(panel, value: false);
		}
		if (pauseAfterAnimation)
		{
			Time.timeScale = 0f;
		}
		m_animCoroutine = null;
		state = stateWhenEnd;
		callback?.Invoke();
	}

	private IEnumerator WaitForAnimationTrigger(State stateWhenBegin, State stateWhenEnd, string triggerName, bool pauseAfterAnimation, bool panelActive, bool wait, Action callback)
	{
		if (state != stateWhenEnd)
		{
			state = stateWhenBegin;
			if (panelActive && panel != null && !panel.gameObject.activeSelf)
			{
				panel.gameObject.SetActive(value: true);
				yield return null;
			}
			if (CanTriggerAnimation(triggerName) && m_animator.gameObject.activeSelf)
			{
				if (debug)
				{
					Debug.Log("<color=green>" + panel.name + ".Animator.SetTrigger(" + triggerName + ") time=" + Time.time + "</color>");
				}
				CheckAnimatorModeAndTimescale(triggerName);
				float timeout = Time.realtimeSinceStartup + maxWaitDuration;
				int goalHashID = Animator.StringToHash(m_animator.GetLayerName(0) + "." + triggerName);
				int oldHashId = UITools.GetAnimatorNameHash(m_animator.GetCurrentAnimatorStateInfo(0));
				int currentHashID = oldHashId;
				m_animator.SetTrigger(triggerName);
				if (wait)
				{
					while (currentHashID != goalHashID && currentHashID == oldHashId && Time.realtimeSinceStartup < timeout)
					{
						yield return null;
						currentHashID = ((m_animator != null && m_animator.isActiveAndEnabled && m_animator.runtimeAnimatorController != null && m_animator.layerCount > 0) ? UITools.GetAnimatorNameHash(m_animator.GetCurrentAnimatorStateInfo(0)) : currentHashID);
					}
					if (m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && currentHashID == goalHashID && Time.realtimeSinceStartup < timeout)
					{
						float length = m_animator.GetCurrentAnimatorStateInfo(0).length;
						if (Mathf.Approximately(0f, Time.timeScale))
						{
							timeout = Time.realtimeSinceStartup + length;
							while (Time.realtimeSinceStartup < timeout)
							{
								yield return null;
							}
						}
						else
						{
							yield return new WaitForSeconds(length);
						}
					}
					if (debug)
					{
						Debug.Log("<color=red>... finished " + panel.name + ".Animator.SetTrigger(" + triggerName + ") time=" + Time.time + "</color>");
					}
				}
			}
		}
		if (!panelActive)
		{
			Tools.SetGameObjectActive(panel, value: false);
		}
		if (pauseAfterAnimation)
		{
			Time.timeScale = 0f;
		}
		m_animCoroutine = null;
		state = stateWhenEnd;
		callback?.Invoke();
	}

	private void CheckAnimatorModeAndTimescale(string triggerName)
	{
		if (Mathf.Approximately(0f, Time.timeScale) && m_animator.updateMode != AnimatorUpdateMode.UnscaledTime && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Time is paused but animator mode isn't set to Unscaled Time; the animation triggered by " + triggerName + " won't play.", m_animator);
		}
	}

	private void CancelCurrentAnim()
	{
		if (m_animCoroutine != null)
		{
			DialogueManager.instance.StopCoroutine(m_animCoroutine);
			m_animCoroutine = null;
		}
	}

	public void ClearTrigger(string triggerName)
	{
		if (HasAnimator() && !string.IsNullOrEmpty(triggerName) && m_animator.isActiveAndEnabled)
		{
			m_animator.ResetTrigger(triggerName);
		}
	}

	private bool CanTriggerAnimation(string stateName)
	{
		if (HasAnimator())
		{
			return !string.IsNullOrEmpty(stateName);
		}
		return false;
	}

	private bool HasAnimator()
	{
		if (m_animator == null && !m_lookedForAnimator)
		{
			m_lookedForAnimator = true;
			if (panel != null)
			{
				m_animator = panel.GetComponent<Animator>();
				if (m_animator == null)
				{
					m_animator = panel.GetComponentInChildren<Animator>();
				}
				state = ((m_animator != null && m_animator.gameObject.activeInHierarchy) ? State.Shown : State.Hidden);
			}
		}
		if (m_animator != null && m_animator.isInitialized)
		{
			return m_animator.gameObject.activeSelf;
		}
		return false;
	}
}
