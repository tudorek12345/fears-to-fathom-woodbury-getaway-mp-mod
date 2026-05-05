using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PixelCrushers;

[AddComponentMenu("")]
public class StandardSceneTransitionManager : SceneTransitionManager
{
	[Serializable]
	public class TransitionInfo
	{
		[Tooltip("Animator for this transition.")]
		public Animator animator;

		[Tooltip("Trigger parameter to set.")]
		public string trigger;

		[Tooltip("Duration to wait for the animation.")]
		public float animationDuration;

		[Tooltip("Total duration to wait for the transition.")]
		public float minTransitionDuration;

		public UnityEvent onTransitionStart = new UnityEvent();

		public UnityEvent onTransitionEnd = new UnityEvent();

		public void TriggerAnimation()
		{
			if (!(animator == null) && !string.IsNullOrEmpty(trigger))
			{
				animator.SetTrigger(trigger);
			}
		}
	}

	[Tooltip("Pause time during the transition.")]
	public bool pauseDuringTransition = true;

	[Tooltip("Transition to play before leaving the current scene.")]
	public TransitionInfo leaveSceneTransition = new TransitionInfo();

	[Tooltip("If set, show this loading scene while loading the real destination scene asynchronously.")]
	public string loadingSceneName;

	[Tooltip("Transition to play after entering the new scene.")]
	public TransitionInfo enterSceneTransition = new TransitionInfo();

	public override IEnumerator LeaveScene()
	{
		leaveSceneTransition.onTransitionStart.Invoke();
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		float minAnimationTime = realtimeSinceStartup + leaveSceneTransition.animationDuration;
		float minEndTime = realtimeSinceStartup + Mathf.Max(leaveSceneTransition.minTransitionDuration, leaveSceneTransition.animationDuration);
		if (pauseDuringTransition)
		{
			Time.timeScale = 0f;
		}
		leaveSceneTransition.TriggerAnimation();
		while (Time.realtimeSinceStartup < minAnimationTime)
		{
			yield return null;
		}
		if (!string.IsNullOrEmpty(loadingSceneName))
		{
			yield return SceneManager.LoadSceneAsync(loadingSceneName);
		}
		while (Time.realtimeSinceStartup < minEndTime)
		{
			yield return null;
		}
		leaveSceneTransition.onTransitionEnd.Invoke();
	}

	public override IEnumerator EnterScene()
	{
		enterSceneTransition.onTransitionStart.Invoke();
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		float minAnimationTime = realtimeSinceStartup + enterSceneTransition.animationDuration;
		float minEndTime = realtimeSinceStartup + Mathf.Max(enterSceneTransition.minTransitionDuration, enterSceneTransition.animationDuration);
		enterSceneTransition.TriggerAnimation();
		while (Time.realtimeSinceStartup < minAnimationTime)
		{
			yield return null;
		}
		while (Time.realtimeSinceStartup < minEndTime)
		{
			yield return null;
		}
		if (pauseDuringTransition)
		{
			Time.timeScale = 1f;
		}
		enterSceneTransition.onTransitionEnd.Invoke();
	}
}
