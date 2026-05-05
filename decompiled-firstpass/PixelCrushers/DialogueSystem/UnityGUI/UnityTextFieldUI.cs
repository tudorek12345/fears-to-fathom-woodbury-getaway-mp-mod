using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class UnityTextFieldUI : MonoBehaviour, ITextFieldUI
{
	public GUIControl panel;

	public GUILabel label;

	public GUITextField textField;

	public KeyCode acceptKey = KeyCode.Return;

	public KeyCode cancelKey;

	private AcceptedTextDelegate acceptedText;

	private GUIControl control;

	private bool ignoreFirstAccept;

	private bool ignoreFirstCancel;

	public void Awake()
	{
		control = GetComponent<GUIControl>();
		if (control == null)
		{
			control = base.gameObject.AddComponent<GUIControl>();
		}
		control.visible = false;
	}

	public void StartTextInput(string labelText, string text, int maxLength, AcceptedTextDelegate acceptedText)
	{
		if (label != null)
		{
			label.text = labelText;
		}
		if (textField != null)
		{
			textField.text = text;
			textField.maxLength = maxLength;
			textField.TakeFocus();
			ignoreFirstAccept = acceptKey != KeyCode.None && Input.GetKeyDown(acceptKey);
			ignoreFirstCancel = cancelKey != KeyCode.None && Input.GetKeyDown(cancelKey);
		}
		this.acceptedText = acceptedText;
		Show();
	}

	private void OnGUI()
	{
		if (!control.visible)
		{
			return;
		}
		if (textField != null)
		{
			textField.TakeFocus();
		}
		if (IsAcceptKey())
		{
			if (ignoreFirstAccept)
			{
				ignoreFirstAccept = false;
				return;
			}
			Event.current.Use();
			AcceptTextInput();
		}
		else if (Event.current.isKey && cancelKey != KeyCode.None && Event.current.keyCode == cancelKey)
		{
			if (ignoreFirstCancel)
			{
				ignoreFirstCancel = false;
				return;
			}
			Event.current.Use();
			CancelTextInput();
		}
	}

	private bool IsAcceptKey()
	{
		if (IsKeyCodeReturn(acceptKey))
		{
			if (!Event.current.Equals(Event.KeyboardEvent("[enter]")) && !Event.current.Equals(Event.KeyboardEvent("return")) && (!Event.current.isKey || Event.current.keyCode != KeyCode.KeypadEnter) && (!Event.current.isKey || Event.current.keyCode != KeyCode.Return))
			{
				if (Event.current.type == EventType.KeyDown)
				{
					return Event.current.character == '\n';
				}
				return false;
			}
			return true;
		}
		if (acceptKey != KeyCode.None)
		{
			return Event.current.keyCode == acceptKey;
		}
		return false;
	}

	private bool IsKeyCodeReturn(KeyCode keyCode)
	{
		if (keyCode != KeyCode.KeypadEnter)
		{
			return keyCode == KeyCode.Return;
		}
		return true;
	}

	public void CancelTextInput()
	{
		Hide();
	}

	private void AcceptTextInput()
	{
		Hide();
		if (acceptedText != null)
		{
			if (IsKeyCodeReturn(acceptKey))
			{
				textField.text = textField.text.Replace("\n", "");
			}
			if (textField != null)
			{
				acceptedText(textField.text);
			}
			acceptedText = null;
		}
	}

	public void OnAccept(object data)
	{
		AcceptTextInput();
	}

	public void OnCancel(object data)
	{
		CancelTextInput();
	}

	private void Show()
	{
		SetControlsActive(value: true);
	}

	private void Hide()
	{
		SetControlsActive(value: false);
	}

	private void SetControlsActive(bool value)
	{
		control.visible = value;
		UnityDialogueUIControls.SetControlActive(label, value);
		UnityDialogueUIControls.SetControlActive(textField, value);
		UnityDialogueUIControls.SetControlActive(panel, value);
	}
}
