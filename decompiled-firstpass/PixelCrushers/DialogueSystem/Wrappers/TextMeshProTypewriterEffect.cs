using System.Text.RegularExpressions;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.Wrappers;

[AddComponentMenu("Pixel Crushers/Dialogue System/UI/TextMesh Pro/Effects/TextMesh Pro Typewriter Effect")]
[DisallowMultipleComponent]
public class TextMeshProTypewriterEffect : PixelCrushers.DialogueSystem.TextMeshProTypewriterEffect
{
	public new void OnEnable()
	{
		onBegin.AddListener(AddRichTags);
	}

	private void AddRichTags()
	{
		base.textComponent.text = "<font=dogicapixelbold SDF><mark=#00000090>" + RemoveRichTextTags(base.textComponent.text) + "</mark>";
	}

	private string RemoveRichTextTags(string text)
	{
		string pattern = "<\\/?.+?>";
		return Regex.Replace(text, pattern, string.Empty);
	}
}
