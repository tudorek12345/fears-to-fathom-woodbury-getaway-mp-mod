using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUsableUI : AbstractUsableUI
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	public UITextField nameText;

	public UITextField useMessageText;

	public Color inRangeColor = Color.yellow;

	public Color outOfRangeColor = Color.gray;

	public Graphic reticleInRange;

	public Graphic reticleOutOfRange;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	[Tooltip("You can leave this unassigned if the Canvas is on this GameObject.")]
	public Canvas canvas;

	protected Animator animator;

	protected Usable usable;

	public virtual void Awake()
	{
		if (canvas == null)
		{
			canvas = GetComponent<Canvas>();
		}
		animator = GetComponent<Animator>();
		if (animator == null && canvas != null)
		{
			animator = canvas.GetComponent<Animator>();
		}
	}

	public virtual void Start()
	{
		usable = Tools.GetComponentAnywhere<Usable>(base.gameObject);
		if (usable != null && nameText != null)
		{
			nameText.text = usable.GetName();
		}
		if (canvas != null)
		{
			canvas.enabled = false;
		}
	}

	public override void Show(string useMessage)
	{
		if (canvas != null)
		{
			canvas.enabled = true;
		}
		if (usable != null && nameText != null)
		{
			nameText.text = usable.GetName();
		}
		if (useMessageText != null)
		{
			useMessageText.text = DialogueManager.GetLocalizedText(useMessage);
		}
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.showTrigger))
		{
			animator.SetTrigger(animationTransitions.showTrigger);
		}
	}

	public override void Hide()
	{
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.hideTrigger))
		{
			animator.SetTrigger(animationTransitions.hideTrigger);
		}
		else if (canvas != null)
		{
			canvas.enabled = false;
		}
	}

	public void OnBarkStart(Transform actor)
	{
		Hide();
	}

	public void OnConversationStart(Transform actor)
	{
		Hide();
	}

	public override void UpdateDisplay(bool inRange)
	{
		Color color = (inRange ? inRangeColor : outOfRangeColor);
		if (nameText != null)
		{
			nameText.color = color;
		}
		if (useMessageText != null)
		{
			useMessageText.color = color;
		}
		Tools.SetGameObjectActive((Component)(object)reticleInRange, inRange);
		Tools.SetGameObjectActive((Component)(object)reticleOutOfRange, !inRange);
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
