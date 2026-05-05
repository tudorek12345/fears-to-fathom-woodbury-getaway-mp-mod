using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIResponseButton : MonoBehaviour
{
	public Button button;

	public Text label;

	[Tooltip("Set the button's text to this color by default")]
	public Color defaultColor = Color.white;

	[Tooltip("Apply emphasis tag colors to the button background")]
	public bool setButtonColor;

	[Tooltip("Apply emphasis tag colors to the button text")]
	public bool setLabelColor = true;

	public string Text
	{
		get
		{
			if (!((Object)(object)label != null))
			{
				return string.Empty;
			}
			return label.text;
		}
		set
		{
			if ((Object)(object)label != null)
			{
				label.text = UITools.StripRPGMakerCodes(value);
				UITools.SendTextChangeMessage(label);
			}
			else if (DialogueDebug.logErrors)
			{
				Debug.LogError(string.Format("{0}: No Text UI element is unassigned on {1}", new object[2] { "Dialogue System", base.name }));
			}
		}
	}

	public bool clickable
	{
		get
		{
			if ((Object)(object)button != null)
			{
				return ((Selectable)button).interactable;
			}
			return false;
		}
		set
		{
			if ((Object)(object)button != null)
			{
				((Selectable)button).interactable = value;
			}
		}
	}

	public bool visible { get; set; }

	public Response response { get; set; }

	public Transform target { get; set; }

	public void Reset()
	{
		Text = string.Empty;
		clickable = false;
		visible = false;
		response = null;
		SetColor(defaultColor);
	}

	public void Awake()
	{
		if ((Object)(object)button == null)
		{
			button = GetComponent<Button>();
		}
		if ((Object)(object)button == null)
		{
			Debug.LogWarning("Dialogue System: Response button '" + base.name + "' is missing a Unity UI Button component!", this);
		}
		Tools.DeprecationWarning(this);
	}

	public void SetFormattedText(FormattedText formattedText)
	{
		if (formattedText != null)
		{
			Text = UITools.GetUIFormattedText(formattedText);
			SetColor((formattedText.emphases.Length != 0) ? formattedText.emphases[0].color : defaultColor);
		}
	}

	public void SetUnformattedText(string unformattedText)
	{
		Text = unformattedText;
		SetColor(defaultColor);
	}

	protected virtual void SetColor(Color currentColor)
	{
		if (!((Object)(object)button != null) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: No Button is assigned to {1}", new object[2] { "Dialogue System", base.name }));
		}
		if ((Object)(object)label != null)
		{
			if (setLabelColor)
			{
				((Graphic)label).color = currentColor;
			}
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: No Text is assigned to {1}", new object[2] { "Dialogue System", base.name }));
		}
	}

	public void OnClick()
	{
		if (target != null)
		{
			target.SendMessage("OnClick", response, SendMessageOptions.RequireReceiver);
		}
	}
}
