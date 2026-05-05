using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers;

public class UIAnimatorMonitor
{
	public static float MaxWaitDuration = 10f;

	private MonoBehaviour m_target;

	private bool m_lookedForAnimator;

	private Animator m_animator;

	private Animation m_animation;

	private Coroutine m_coroutine;

	public string currentTrigger { get; private set; }

	public UIAnimatorMonitor(GameObject target)
	{
		m_target = ((target != null) ? target.GetComponent<MonoBehaviour>() : null);
		currentTrigger = string.Empty;
	}

	public UIAnimatorMonitor(MonoBehaviour target)
	{
		m_target = target;
		currentTrigger = string.Empty;
	}

	public void SetTrigger(string triggerName, Action callback, bool wait = true)
	{
		if (!(m_target == null))
		{
			m_target.gameObject.SetActive(value: true);
			CancelCurrentAnimation();
			if (m_target.gameObject.activeInHierarchy)
			{
				m_coroutine = m_target.StartCoroutine(WaitForAnimation(triggerName, callback, wait));
			}
		}
	}

	private IEnumerator WaitForAnimation(string triggerName, Action callback, bool wait)
	{
		if (HasAnimator() && !string.IsNullOrEmpty(triggerName))
		{
			if (IsAnimatorValid())
			{
				CheckAnimatorModeAndTimescale(triggerName);
				m_animator.SetTrigger(triggerName);
				currentTrigger = triggerName;
				float timeout = Time.realtimeSinceStartup + MaxWaitDuration;
				int goalHashID = Animator.StringToHash(m_animator.GetLayerName(0) + "." + triggerName);
				int oldHashId = UIUtility.GetAnimatorNameHash(m_animator.GetCurrentAnimatorStateInfo(0));
				int num = oldHashId;
				if (wait)
				{
					while (num != goalHashID && num == oldHashId && Time.realtimeSinceStartup < timeout)
					{
						yield return null;
						num = (IsAnimatorValid() ? UIUtility.GetAnimatorNameHash(m_animator.GetCurrentAnimatorStateInfo(0)) : 0);
					}
					if (m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && Time.realtimeSinceStartup < timeout && IsAnimatorValid())
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
				}
			}
			else if (m_animation != null && m_animation.enabled)
			{
				m_animation.Play(triggerName);
				if (wait)
				{
					AnimationClip clip = m_animation.GetClip(triggerName);
					if (clip != null)
					{
						yield return new WaitForSeconds(clip.length);
					}
				}
			}
		}
		currentTrigger = string.Empty;
		m_coroutine = null;
		callback?.Invoke();
	}

	private bool HasAnimator()
	{
		if (m_animator == null && m_animation == null && !m_lookedForAnimator)
		{
			m_lookedForAnimator = true;
			if (m_target != null)
			{
				m_animator = m_target.GetComponent<Animator>();
				if (m_animator == null)
				{
					m_animation = m_target.GetComponent<Animation>();
					if (m_animation == null)
					{
						m_animator = m_target.GetComponentInChildren<Animator>();
						if (m_animator == null)
						{
							m_animation = m_target.GetComponentInChildren<Animation>();
						}
					}
				}
			}
		}
		if (!(m_animator != null))
		{
			return m_animation != null;
		}
		return true;
	}

	private bool IsAnimatorValid()
	{
		if (m_animator != null && m_animator.enabled)
		{
			return m_animator.runtimeAnimatorController != null;
		}
		return false;
	}

	private void CheckAnimatorModeAndTimescale(string triggerName)
	{
		if (!(m_animator == null) && Mathf.Approximately(0f, Time.timeScale) && m_animator.updateMode != AnimatorUpdateMode.UnscaledTime)
		{
			m_animator.updateMode = AnimatorUpdateMode.UnscaledTime;
		}
	}

	public void CancelCurrentAnimation()
	{
		if (m_coroutine != null && !(m_target == null))
		{
			currentTrigger = string.Empty;
			m_target.StopCoroutine(m_coroutine);
			m_coroutine = null;
		}
	}
}
