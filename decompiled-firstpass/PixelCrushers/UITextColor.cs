using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
public class UITextColor : MonoBehaviour
{
	public Color color;

	public Text text;

	private Color m_originalColor;

	private void Awake()
	{
		if ((Object)(object)text == null)
		{
			text = GetComponentInChildren<Text>();
		}
		if ((Object)(object)text != null)
		{
			m_originalColor = ((Graphic)text).color;
		}
	}

	public void ApplyColor()
	{
		if (!((Object)(object)text == null))
		{
			m_originalColor = ((Graphic)text).color;
			((Graphic)text).color = color;
		}
	}

	public void UndoColor()
	{
		if (!((Object)(object)text == null))
		{
			((Graphic)text).color = m_originalColor;
		}
	}
}
