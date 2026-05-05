using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

public static class UITools
{
	public static Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();

	private static AbstractDialogueUI dialogueUI = null;

	public const string RPGMakerCodeQuarterPause = "\\,";

	public const string RPGMakerCodeFullPause = "\\.";

	public const string RPGMakerCodeSkipToEnd = "\\^";

	public const string RPGMakerCodeInstantOpen = "\\>";

	public const string RPGMakerCodeInstantClose = "\\<";

	public static void RequireEventSystem()
	{
		UIUtility.RequireEventSystem(DialogueDebug.logWarnings ? "Dialogue System: The scene is missing an EventSystem. Adding one." : null);
	}

	public static int GetAnimatorNameHash(AnimatorStateInfo animatorStateInfo)
	{
		return animatorStateInfo.fullPathHash;
	}

	public static void ClearSpriteCache()
	{
		spriteCache.Clear();
	}

	public static Sprite CreateSprite(Texture2D texture)
	{
		if (texture == null)
		{
			return null;
		}
		if (spriteCache.ContainsKey(texture) && spriteCache[texture] != null)
		{
			return spriteCache[texture];
		}
		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
		spriteCache[texture] = sprite;
		return sprite;
	}

	public static Sprite GetSprite(Texture2D texture, Sprite sprite)
	{
		if (!(sprite != null))
		{
			if (!(texture != null))
			{
				return null;
			}
			return CreateSprite(texture);
		}
		return sprite;
	}

	public static Texture2D GetTexture2D(Sprite sprite)
	{
		if (!(sprite != null))
		{
			return null;
		}
		return sprite.texture;
	}

	public static string GetUIFormattedText(FormattedText formattedText)
	{
		if (formattedText == null)
		{
			return string.Empty;
		}
		if (formattedText.italic)
		{
			return "<i>" + formattedText.text + "</i>";
		}
		return formattedText.text;
	}

	public static void SendTextChangeMessage(Text text)
	{
		if (Application.isPlaying && !((Object)(object)text == null))
		{
			if (dialogueUI == null)
			{
				dialogueUI = ((Component)(object)text).GetComponentInParent<AbstractDialogueUI>();
			}
			if (!(dialogueUI == null))
			{
				dialogueUI.SendMessage("OnTextChange", text, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	public static void SendTextChangeMessage(UITextField textField)
	{
		if (Application.isPlaying && !(textField.gameObject == null))
		{
			textField.gameObject.SendMessage("OnTextChange", textField, SendMessageOptions.DontRequireReceiver);
		}
	}

	public static void Select(Selectable selectable, bool allowStealFocus = true, EventSystem eventSystem = null)
	{
		UIUtility.Select(selectable, allowStealFocus, eventSystem);
	}

	public static string StripRPGMakerCodes(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		if (!s.Contains("\\"))
		{
			return s;
		}
		return s.Replace("\\,", string.Empty).Replace("\\.", string.Empty).Replace("\\^", string.Empty)
			.Replace("\\>", string.Empty)
			.Replace("\\<", string.Empty);
	}

	public static string StripEmTags(string s)
	{
		return Regex.Replace(s, "\\[em\\d+\\]|\\[/em\\d+\\]", string.Empty);
	}

	public static string WrapTextInColor(string text, Color color)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string text2 = "<color=" + Tools.ToWebColor(color) + ">";
		if (!text.Contains("<"))
		{
			return text2 + text + "</color>";
		}
		string text3 = string.Empty;
		int num = 0;
		foreach (Match item in Regex.Matches(text, "<i><color=^(?!.*</color>)</color></i>|<b><color=^(?!.*</color>)</color></b>|<i><b><color=^(?!.*</color>)</color></b></i>|<color=^(?!.*</color>)</color>"))
		{
			text3 = text3 + text2 + text.Substring(num, item.Index) + "</color>" + item.Value;
			num = item.Index + item.Value.Length;
		}
		if (num < text.Length)
		{
			text3 = text3 + text2 + text.Substring(num) + "</color>";
		}
		return text3;
	}

	public static void EnableInteractivity(GameObject go)
	{
		Canvas canvas = go.GetComponentInChildren<Canvas>() ?? go.GetComponentInParent<Canvas>();
		if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
		{
			canvas.worldCamera = Camera.main;
		}
		if (InputDeviceManager.instance == null || (InputDeviceManager.instance.controlGraphicRaycasters && InputDeviceManager.currentInputDevice == InputDevice.Mouse))
		{
			GraphicRaycaster val = go.GetComponentInChildren<GraphicRaycaster>() ?? go.GetComponentInParent<GraphicRaycaster>();
			if ((Object)(object)val != null)
			{
				((Behaviour)(object)val).enabled = true;
			}
		}
	}
}
