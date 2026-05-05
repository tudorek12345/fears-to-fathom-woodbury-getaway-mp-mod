using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UsableUnityUI : AbstractUsableUI
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	public Text nameText;

	public Text useMessageText;

	public Color inRangeColor = Color.yellow;

	public Color outOfRangeColor = Color.gray;

	public Graphic reticleInRange;

	public Graphic reticleOutOfRange;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	private Canvas canvas;

	private Animator animator;

	public void Awake()
	{
		canvas = GetComponent<Canvas>();
		animator = GetComponent<Animator>();
		Tools.DeprecationWarning(this);
	}

	public void Start()
	{
		Usable componentAnywhere = Tools.GetComponentAnywhere<Usable>(base.gameObject);
		if (componentAnywhere != null && (UnityEngine.Object)(object)nameText != null)
		{
			nameText.text = componentAnywhere.GetName();
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
		if ((UnityEngine.Object)(object)useMessageText != null)
		{
			useMessageText.text = useMessage;
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
		if ((UnityEngine.Object)(object)nameText != null)
		{
			((Graphic)nameText).color = color;
		}
		if ((UnityEngine.Object)(object)useMessageText != null)
		{
			((Graphic)useMessageText).color = color;
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
