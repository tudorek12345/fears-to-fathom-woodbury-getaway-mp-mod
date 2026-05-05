using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardBarkUI : AbstractBarkUI
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	[Tooltip("Optional canvas group, for example to play fade animations.")]
	public CanvasGroup canvasGroup;

	[Tooltip("UI text control for bark text.")]
	public UITextField barkText;

	[Tooltip("Optional UI text control for barker's name if Include Name is ticked. If unassigned and Include Name is ticked, name is prepended to Bark Text.")]
	public UITextField nameText;

	[Tooltip("If Name Text is unassigned, prepend barker's name to Bark Text.")]
	public bool includeName;

	[Tooltip("Optional to show barker's portrait image.")]
	public Image portraitImage;

	[Tooltip("Show barker's portrait image.")]
	public bool showPortraitImage;

	[HideInInspector]
	public float doneTime;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	[Tooltip("The duration in seconds to show the bark text before fading it out. If zero, use the Dialogue Manager's Bark Settings.")]
	public float duration = 4f;

	[Tooltip("Keep bark canvas anchor point always in camera view.")]
	public bool keepInView;

	[Tooltip("Keep the bark text onscreen until the sequence ends.")]
	public bool waitUntilSequenceEnds;

	[Tooltip("If bark is visible and waiting for sequence to end, but new bark wants to show, cancel wait for previous sequence.")]
	public bool cancelWaitUntilSequenceEndsIfReplacingBark;

	[Tooltip("Wait for an OnContinue message.")]
	public bool waitForContinueButton;

	public BarkSubtitleSetting textDisplaySetting;

	protected int numSequencesActive;

	protected bool hasEverBarked;

	protected Canvas canvas { get; set; }

	protected Animator animator { get; set; }

	protected AbstractTypewriterEffect typewriter { get; set; }

	protected Vector3 originalCanvasLocalPosition { get; set; }

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

	protected virtual void Awake()
	{
		canvas = GetComponentInChildren<Canvas>();
		animator = GetComponentInChildren<Animator>();
		typewriter = TypewriterUtility.GetTypewriter(barkText);
		if (animator == null && canvasGroup != null)
		{
			animator = canvasGroup.GetComponentInChildren<Animator>();
		}
	}

	protected virtual void Start()
	{
		if (canvas != null)
		{
			if (waitForContinueButton && canvas.worldCamera == null)
			{
				canvas.worldCamera = Camera.main;
			}
			canvas.enabled = false;
			originalCanvasLocalPosition = canvas.GetComponent<RectTransform>().localPosition;
		}
		if (nameText != null)
		{
			nameText.SetActive(includeName);
		}
		Tools.SetGameObjectActive((Component)(object)portraitImage, value: false);
	}

	protected virtual void Update()
	{
		if (!hasEverBarked)
		{
			return;
		}
		if (!waitUntilSequenceEnds && doneTime > 0f && DialogueTime.time >= doneTime)
		{
			Hide();
		}
		else if (keepInView && isPlaying)
		{
			Camera main = Camera.main;
			if (!(main == null))
			{
				Vector3 position = main.WorldToViewportPoint(canvas.transform.position);
				position.x = Mathf.Clamp01(position.x);
				position.y = Mathf.Clamp01(position.y);
				canvas.transform.position = main.ViewportToWorldPoint(position);
			}
		}
	}

	public virtual bool ShouldShowText(Subtitle subtitle)
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
		hasEverBarked = true;
		SetUIElementsActive(value: false);
		string text = subtitle.formattedText.text;
		if (includeName && !string.IsNullOrEmpty(Tools.StripTextMeshProTags(subtitle.speakerInfo.Name)))
		{
			if (nameText != null)
			{
				nameText.text = subtitle.speakerInfo.Name;
			}
			else
			{
				text = $"{subtitle.speakerInfo.Name}: {subtitle.formattedText.text}";
			}
		}
		else if (nameText != null && nameText.gameObject != null)
		{
			nameText.gameObject.SetActive(value: false);
		}
		if (showPortraitImage && subtitle.speakerInfo.portrait != null)
		{
			Tools.SetGameObjectActive((Component)(object)portraitImage, value: true);
			portraitImage.sprite = subtitle.speakerInfo.portrait;
		}
		else
		{
			Tools.SetGameObjectActive((Component)(object)portraitImage, value: false);
		}
		if (barkText != null)
		{
			barkText.text = text;
		}
		SetUIElementsActive(value: true);
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.showTrigger))
		{
			if (!string.IsNullOrEmpty(animationTransitions.hideTrigger))
			{
				animator.ResetTrigger(animationTransitions.hideTrigger);
			}
			animator.SetTrigger(animationTransitions.showTrigger);
		}
		if (typewriter != null)
		{
			typewriter.StartTyping(text);
		}
		float num = (Mathf.Approximately(0f, duration) ? DialogueManager.GetBarkDuration(text) : duration);
		if (waitUntilSequenceEnds)
		{
			numSequencesActive++;
		}
		doneTime = (waitForContinueButton ? float.PositiveInfinity : (DialogueTime.time + num));
	}

	protected virtual void SetUIElementsActive(bool value)
	{
		if (nameText.gameObject != base.gameObject && includeName)
		{
			nameText.SetActive(value);
		}
		if (barkText.gameObject != base.gameObject)
		{
			barkText.SetActive(value);
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

	public virtual void OnBarkEnd(Transform actor)
	{
		if (waitUntilSequenceEnds && !waitForContinueButton && IsActorMe(actor))
		{
			numSequencesActive--;
			if (numSequencesActive <= 0)
			{
				Hide();
			}
		}
	}

	protected virtual bool IsActorMe(Transform actor)
	{
		Transform parent = base.transform;
		while (parent != null)
		{
			if (parent == actor)
			{
				return true;
			}
			parent = parent.parent;
		}
		return false;
	}

	public virtual void OnContinue()
	{
		Hide();
	}

	public override void Hide()
	{
		if (!hasEverBarked)
		{
			if (canvas != null)
			{
				canvas.enabled = false;
			}
			return;
		}
		numSequencesActive = 0;
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.hideTrigger))
		{
			if (!string.IsNullOrEmpty(animationTransitions.showTrigger))
			{
				animator.ResetTrigger(animationTransitions.showTrigger);
			}
			animator.SetTrigger(animationTransitions.hideTrigger);
		}
		else if (canvas != null)
		{
			canvas.enabled = false;
		}
		if (canvas != null)
		{
			canvas.GetComponent<RectTransform>().localPosition = originalCanvasLocalPosition;
		}
		doneTime = 0f;
	}

	protected virtual bool CanTriggerAnimations()
	{
		if (animator != null)
		{
			return animationTransitions != null;
		}
		return false;
	}
}
