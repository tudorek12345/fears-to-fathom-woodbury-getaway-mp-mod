using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class TypewriterUtility
{
	public static AbstractTypewriterEffect GetTypewriter(UITextField textField)
	{
		AbstractTypewriterEffect abstractTypewriterEffect = null;
		if ((Object)(object)textField.textMeshProUGUI != null)
		{
			abstractTypewriterEffect = ((Component)(object)textField.textMeshProUGUI).GetComponent<AbstractTypewriterEffect>();
		}
		if (abstractTypewriterEffect == null && (Object)(object)textField.uiText != null)
		{
			abstractTypewriterEffect = ((Component)(object)textField.uiText).GetComponent<AbstractTypewriterEffect>();
		}
		return abstractTypewriterEffect;
	}

	public static bool HasTypewriter(UITextField textField)
	{
		return GetTypewriter(textField) != null;
	}

	public static float GetTypewriterSpeed(AbstractTypewriterEffect typewriter)
	{
		if (!(typewriter != null))
		{
			return -1f;
		}
		return typewriter.GetSpeed();
	}

	public static float GetTypewriterSpeed(UITextField textField)
	{
		return GetTypewriterSpeed(GetTypewriter(textField));
	}

	public static void SetTypewriterSpeed(UITextField textField, float charactersPerSecond)
	{
		AbstractTypewriterEffect typewriter = GetTypewriter(textField);
		if (typewriter != null)
		{
			typewriter.SetSpeed(charactersPerSecond);
		}
	}

	public static void StartTyping(UITextField textField, string text, int fromIndex = 0)
	{
		AbstractTypewriterEffect typewriter = GetTypewriter(textField);
		if (typewriter != null && typewriter.enabled)
		{
			typewriter.StartTyping(text, fromIndex);
		}
	}

	public static void StopTyping(UITextField textField)
	{
		AbstractTypewriterEffect typewriter = GetTypewriter(textField);
		if (typewriter != null && typewriter.enabled)
		{
			typewriter.StopTyping();
		}
	}
}
