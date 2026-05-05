using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUISubtitleControls : AbstractUISubtitleControls
{
	[Tooltip("Optional panel for the subtitle elements")]
	public Graphic panel;

	[Tooltip("Subtitle text")]
	public Text line;

	[Tooltip("Optional image for speaker's portrait")]
	public Image portraitImage;

	[Tooltip("Optional label for speaker's name")]
	public Text portraitName;

	[Tooltip("Optional continue button; configure OnClick to invoke dialogue UI's OnContinue method")]
	public Button continueButton;

	[Tooltip("Ignore RPGMaker-style pause codes")]
	public bool ignorePauseCodes;

	[Tooltip("Optional animation transitions; panel should have an Animator")]
	public UIAnimationTransitions animationTransitions = new UIAnimationTransitions();

	[Tooltip("When the subtitle UI elements should be visible.")]
	public UIVisibility uiVisibility;

	private UIShowHideController m_showHideController;

	private bool m_haveSavedOriginalColor;

	private Color m_originalColor = Color.white;

	public bool isVisible
	{
		get
		{
			if (!((UnityEngine.Object)(object)panel != null))
			{
				if ((UnityEngine.Object)(object)line != null)
				{
					return ((Component)(object)line).gameObject.activeInHierarchy;
				}
				return false;
			}
			return ((Component)(object)panel).gameObject.activeInHierarchy;
		}
	}

	public override bool hasText
	{
		get
		{
			if ((UnityEngine.Object)(object)line != null)
			{
				return !string.IsNullOrEmpty(line.text);
			}
			return false;
		}
	}

	private UIShowHideController showHideController
	{
		get
		{
			if (m_showHideController == null)
			{
				m_showHideController = new UIShowHideController(null, (Component)(object)panel, animationTransitions.transitionMode, animationTransitions.debug);
			}
			return m_showHideController;
		}
	}

	public void CheckSubtitlePortrait(CharacterType characterType)
	{
		if (uiVisibility == UIVisibility.AlwaysFromStart)
		{
			DialogueManager.instance.StartCoroutine(SetSubtitlePortrait(characterType));
		}
	}

	private IEnumerator SetSubtitlePortrait(CharacterType characterType)
	{
		if ((UnityEngine.Object)(object)portraitName != null)
		{
			portraitName.text = string.Empty;
		}
		if ((UnityEngine.Object)(object)portraitImage != null)
		{
			portraitImage.sprite = null;
		}
		if ((UnityEngine.Object)(object)line != null)
		{
			line.text = string.Empty;
		}
		yield return CoroutineUtility.endOfFrame;
		CharacterInfo characterInfo = ((characterType == CharacterType.NPC) ? DialogueManager.conversationModel.conversantInfo : DialogueManager.conversationModel.actorInfo);
		if (characterInfo != null)
		{
			if ((UnityEngine.Object)(object)portraitName != null && string.IsNullOrEmpty(portraitName.text))
			{
				portraitName.text = characterInfo.Name;
			}
			if ((UnityEngine.Object)(object)portraitImage != null && portraitImage.sprite == null)
			{
				portraitImage.sprite = characterInfo.portrait;
			}
		}
	}

	public override void SetActive(bool value)
	{
		if (value || uiVisibility == UIVisibility.AlwaysFromStart || ((uiVisibility == UIVisibility.AlwaysOnceShown || uiVisibility == UIVisibility.UntilSuperceded) && isVisible))
		{
			ShowPanel();
		}
		else
		{
			HidePanel();
		}
	}

	public void ForceHide()
	{
		HidePanel();
	}

	public void ForceShow()
	{
		showHideController.state = UIShowHideController.State.Hidden;
		ActivateUIElements();
	}

	private void ShowPanel()
	{
		ActivateUIElements();
		animationTransitions.ClearTriggers(showHideController);
		showHideController.Show(animationTransitions.showTrigger, pauseAfterAnimation: false, null);
	}

	private void HidePanel()
	{
		animationTransitions.ClearTriggers(showHideController);
		showHideController.Hide(animationTransitions.hideTrigger, DeactivateUIElements);
	}

	public void ActivateUIElements()
	{
		SetUIElementsActive(value: true);
	}

	public void DeactivateUIElements()
	{
		SetUIElementsActive(value: false);
	}

	private void SetUIElementsActive(bool value)
	{
		Tools.SetGameObjectActive((Component)(object)panel, value);
		Tools.SetGameObjectActive((Component)(object)line, value);
		Tools.SetGameObjectActive((Component)(object)portraitImage, value);
		Tools.SetGameObjectActive((Component)(object)portraitName, value);
		Tools.SetGameObjectActive((Component)(object)continueButton, value: false);
	}

	public override void ShowContinueButton()
	{
		Tools.SetGameObjectActive((Component)(object)continueButton, value: true);
	}

	public override void HideContinueButton()
	{
		Tools.SetGameObjectActive((Component)(object)continueButton, value: false);
	}

	public override void SetSubtitle(Subtitle subtitle)
	{
		if (subtitle != null && !string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			if ((UnityEngine.Object)(object)portraitImage != null)
			{
				portraitImage.sprite = subtitle.GetSpeakerPortrait();
			}
			if ((UnityEngine.Object)(object)portraitName != null)
			{
				portraitName.text = subtitle.speakerInfo.Name;
				UITools.SendTextChangeMessage(portraitName);
			}
			if ((UnityEngine.Object)(object)line != null)
			{
				UnityUITypewriterEffect component = ((Component)(object)line).GetComponent<UnityUITypewriterEffect>();
				if (component != null && component.enabled)
				{
					component.Stop();
					component.playOnEnable = false;
				}
				SetFormattedText(line, subtitle.formattedText);
				if (component != null && component.enabled)
				{
					component.PlayText(subtitle.formattedText.text);
				}
			}
		}
		else if ((UnityEngine.Object)(object)line != null && subtitle != null)
		{
			SetFormattedText(line, subtitle.formattedText);
		}
	}

	public override void ClearSubtitle()
	{
		SetFormattedText(line, null);
	}

	private void SetFormattedText(Text label, FormattedText formattedText)
	{
		if (!((UnityEngine.Object)(object)label != null))
		{
			return;
		}
		if (formattedText != null)
		{
			string text = UITools.GetUIFormattedText(formattedText);
			if (ignorePauseCodes)
			{
				text = UITools.StripRPGMakerCodes(text);
			}
			label.text = text;
			UITools.SendTextChangeMessage(label);
			if (!m_haveSavedOriginalColor)
			{
				m_originalColor = ((Graphic)label).color;
				m_haveSavedOriginalColor = true;
			}
			((Graphic)label).color = ((formattedText.emphases.Length != 0) ? formattedText.emphases[0].color : m_originalColor);
		}
		else
		{
			label.text = string.Empty;
		}
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (currentSubtitle != null && string.Equals(currentSubtitle.speakerInfo.nameInDatabase, actorName) && (UnityEngine.Object)(object)portraitImage != null)
		{
			portraitImage.sprite = AbstractDialogueUI.GetValidPortraitSprite(actorName, portraitSprite);
		}
	}

	public void AutoFocus(bool allowStealFocus = true)
	{
		UITools.Select((Selectable)(object)continueButton, allowStealFocus);
	}
}
