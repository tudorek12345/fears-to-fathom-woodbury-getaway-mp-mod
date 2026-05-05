using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIBarkUI : AbstractBarkUI
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	[Tooltip("Optional canvas group, for example to play fade animations.")]
	public CanvasGroup canvasGroup;

	[Tooltip("UI text control for the bark text.")]
	public Text barkText;

	[Tooltip("Optional UI text control for the actor's name if Include Name is ticked. If unassigned and Include Name is ticked, the name is prepended to the Bark Text.")]
	public Text nameText;

	[Tooltip("Show the barker's name.")]
	public bool includeName;

	[HideInInspector]
	public float doneTime;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	[Tooltip("The duration in seconds to show the bark text before fading it out. If zero, use the Dialogue Manager's Bark Settings.")]
	public float duration = 4f;

	[Tooltip("Keep the bark text onscreen until the sequence ends.")]
	public bool waitUntilSequenceEnds;

	public bool waitForContinueButton;

	public BarkSubtitleSetting textDisplaySetting;

	private Canvas canvas;

	private Animator animator;

	public override bool isPlaying
	{
		get
		{
			if (canvas != null && canvas.enabled)
			{
				return DialogueTime.time < doneTime;
			}
			return false;
		}
	}

	public void Awake()
	{
		canvas = GetComponentInChildren<Canvas>();
		animator = GetComponentInChildren<Animator>();
		if (animator == null && canvasGroup != null)
		{
			animator = canvasGroup.GetComponentInChildren<Animator>();
		}
		Tools.DeprecationWarning(this, "Use StandardBarkUI instead.");
	}

	public void Start()
	{
		if (canvas != null)
		{
			if (waitForContinueButton && canvas.worldCamera == null)
			{
				canvas.worldCamera = Camera.main;
			}
			canvas.enabled = false;
		}
		if ((UnityEngine.Object)(object)nameText != null)
		{
			((Component)(object)nameText).gameObject.SetActive(includeName);
		}
	}

	public bool ShouldShowText(Subtitle subtitle)
	{
		bool num = textDisplaySetting == BarkSubtitleSetting.Show || (textDisplaySetting == BarkSubtitleSetting.SameAsDialogueManager && DialogueManager.displaySettings.subtitleSettings.showNPCSubtitlesDuringLine);
		bool flag = subtitle != null && !string.IsNullOrEmpty(subtitle.formattedText.text);
		return num && flag;
	}

	public override void Bark(Subtitle subtitle)
	{
		if (!ShouldShowText(subtitle))
		{
			return;
		}
		SetUIElementsActive(value: false);
		string text = subtitle.formattedText.text;
		if (includeName)
		{
			if ((UnityEngine.Object)(object)nameText != null)
			{
				nameText.text = subtitle.speakerInfo.Name;
			}
			else
			{
				text = $"{text}: {subtitle.formattedText.text}";
			}
		}
		if ((UnityEngine.Object)(object)barkText != null)
		{
			barkText.text = text;
		}
		SetUIElementsActive(value: true);
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.showTrigger))
		{
			animator.SetTrigger(animationTransitions.showTrigger);
		}
		CancelInvoke("Hide");
		float num = (Mathf.Approximately(0f, duration) ? DialogueManager.GetBarkDuration(text) : duration);
		if (!waitUntilSequenceEnds && !waitForContinueButton)
		{
			Invoke("Hide", num);
		}
		doneTime = DialogueTime.time + num;
	}

	private void SetUIElementsActive(bool value)
	{
		if ((UnityEngine.Object)(object)nameText != null && ((Component)(object)nameText).gameObject != base.gameObject)
		{
			((Component)(object)nameText).gameObject.SetActive(value);
		}
		if ((UnityEngine.Object)(object)barkText != null && ((Component)(object)barkText).gameObject != base.gameObject)
		{
			((Component)(object)barkText).gameObject.SetActive(value);
		}
		if (canvas != null && canvas.gameObject != base.gameObject)
		{
			canvas.gameObject.SetActive(value);
		}
		if (value && canvas != null)
		{
			canvas.enabled = true;
		}
	}

	public void OnBarkEnd(Transform actor)
	{
		if (waitUntilSequenceEnds && !waitForContinueButton)
		{
			Hide();
		}
	}

	public void OnContinue()
	{
		Hide();
	}

	public override void Hide()
	{
		if (canvas.enabled && CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.hideTrigger))
		{
			if (!string.IsNullOrEmpty(animationTransitions.hideTrigger))
			{
				animator.ResetTrigger(animationTransitions.showTrigger);
			}
			animator.SetTrigger(animationTransitions.hideTrigger);
		}
		else if (canvas != null)
		{
			canvas.enabled = false;
		}
		doneTime = 0f;
	}

	private bool CanTriggerAnimations()
	{
		if (animator != null)
		{
			return animationTransitions != null;
		}
		return false;
	}
}
