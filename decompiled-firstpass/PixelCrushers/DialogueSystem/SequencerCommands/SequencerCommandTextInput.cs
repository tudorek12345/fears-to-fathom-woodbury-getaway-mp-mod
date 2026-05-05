using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandTextInput : SequencerCommand
{
	private ITextFieldUI textFieldUI;

	private string variableName = string.Empty;

	private bool acceptedText;

	public void Start()
	{
		Transform transform = FindTextFieldUIObject();
		if (transform != null)
		{
			bool activeSelf = transform.gameObject.activeSelf;
			if (!activeSelf)
			{
				transform.gameObject.SetActive(value: true);
			}
			textFieldUI = transform.GetComponent(typeof(ITextFieldUI)) as ITextFieldUI;
			if (!activeSelf)
			{
				transform.gameObject.SetActive(value: false);
			}
		}
		string text = GetParameter(1);
		variableName = GetParameter(2);
		int parameterAsInt = GetParameterAsInt(3);
		bool flag = string.Equals(GetParameter(4), "clear");
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: TextInput({1}, {2}, {3}, {4})", "Dialogue System", Tools.GetObjectName(transform), text, variableName, parameterAsInt));
		}
		if (string.IsNullOrEmpty(variableName))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Sequencer: TextInput({1}): The third parameter must be the name of a Dialogue System variable.", new object[2]
				{
					"Dialogue System",
					GetParameters()
				}));
			}
			Stop();
		}
		else if (textFieldUI == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Sequencer: TextInput(): Text Field UI not found on a GameObject '{1}'. Did you specify the correct GameObject name?", new object[2]
				{
					"Dialogue System",
					GetParameter(0)
				}));
			}
			Stop();
		}
		else
		{
			if (text.StartsWith("var="))
			{
				text = DialogueLua.GetVariable(text.Substring(4)).asString;
			}
			string text2 = (flag ? string.Empty : DialogueLua.GetVariable(variableName).asString);
			textFieldUI.StartTextInput(text, text2, parameterAsInt, OnAcceptedText);
		}
	}

	private Transform FindTextFieldUIObject()
	{
		MonoBehaviour monoBehaviour = DialogueManager.dialogueUI as MonoBehaviour;
		if (monoBehaviour != null)
		{
			Transform transform = FindChildRecursive(monoBehaviour.transform, GetParameter(0));
			if (transform != null)
			{
				return transform;
			}
		}
		return GetSubject(0);
	}

	private Transform FindChildRecursive(Transform t, string childName)
	{
		if (t != null && t.gameObject.activeInHierarchy)
		{
			if (string.Equals(t.name, childName))
			{
				return t;
			}
			foreach (Transform item in t)
			{
				Transform transform = FindChildRecursive(item, childName);
				if (transform != null)
				{
					return transform;
				}
			}
		}
		return null;
	}

	public void OnAcceptedText(string text)
	{
		if (!acceptedText)
		{
			Variable variable = DialogueManager.masterDatabase.GetVariable(variableName);
			if (variable != null && variable.Type == FieldType.Number)
			{
				float num = Tools.StringToFloat(text);
				DialogueLua.SetVariable(variableName, num);
			}
			else
			{
				DialogueLua.SetVariable(variableName, text);
			}
		}
		acceptedText = true;
		Stop();
	}

	public void OnDestroy()
	{
		if (!acceptedText && textFieldUI != null)
		{
			textFieldUI.CancelTextInput();
		}
	}
}
