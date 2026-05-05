using UnityEngine;

namespace PixelCrushers;

public class StringAsset : ScriptableObject
{
	[TextArea(3, 20)]
	[SerializeField]
	private string m_text;

	public string text
	{
		get
		{
			return m_text;
		}
		set
		{
			m_text = value;
		}
	}

	public override string ToString()
	{
		return text;
	}
}
