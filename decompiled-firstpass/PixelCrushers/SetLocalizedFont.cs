using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SetLocalizedFont : MonoBehaviour
{
	[SerializeField]
	private bool m_setOnEnable = true;

	[Tooltip("Overrides UILocalizationManager's Localized Fonts if set.")]
	[SerializeField]
	private LocalizedFonts m_localizedFonts;

	private bool m_started;

	private float m_initialFontSize = -1f;

	private Text text;

	private TextMeshProUGUI textMeshPro;

	private void Awake()
	{
		text = GetComponent<Text>();
		textMeshPro = GetComponent<TextMeshProUGUI>();
	}

	private void Start()
	{
		m_started = true;
		if (m_setOnEnable)
		{
			SetCurrentLocalizedFont();
		}
	}

	private void OnEnable()
	{
		if (m_started)
		{
			SetCurrentLocalizedFont();
		}
	}

	public void SetCurrentLocalizedFont()
	{
		if (m_initialFontSize == -1f)
		{
			if ((Object)(object)text != null)
			{
				m_initialFontSize = text.fontSize;
			}
			if ((Object)(object)textMeshPro != null)
			{
				m_initialFontSize = ((TMP_Text)textMeshPro).fontSize;
			}
		}
		LocalizedFonts localizedFonts = ((m_localizedFonts != null) ? m_localizedFonts : UILocalizationManager.instance.localizedFonts);
		if (localizedFonts == null)
		{
			return;
		}
		if ((Object)(object)text != null)
		{
			Font font = localizedFonts.GetFont(UILocalizationManager.instance.currentLanguage);
			if (font != null)
			{
				text.font = font;
				text.fontSize = Mathf.RoundToInt(localizedFonts.GetFontScale(UILocalizationManager.instance.currentLanguage) * m_initialFontSize);
			}
		}
		if ((Object)(object)textMeshPro != null)
		{
			TMP_FontAsset textMeshProFont = localizedFonts.GetTextMeshProFont(UILocalizationManager.instance.currentLanguage);
			if ((Object)(object)textMeshProFont != null)
			{
				((TMP_Text)textMeshPro).font = textMeshProFont;
				((TMP_Text)textMeshPro).fontSize = localizedFonts.GetFontScale(UILocalizationManager.instance.currentLanguage) * m_initialFontSize;
			}
		}
	}
}
