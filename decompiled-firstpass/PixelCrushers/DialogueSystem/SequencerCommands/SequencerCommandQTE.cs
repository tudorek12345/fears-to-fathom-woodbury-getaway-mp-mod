using System;
using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandQTE : SequencerCommand
{
	private int index;

	private float stopTime;

	private string button;

	private string variableName;

	private string variableQTEValue;

	private FieldType variableType;

	public void Start()
	{
		index = GetParameterAsInt(0);
		DialogueManager.dialogueUI.ShowQTEIndicator(index);
		button = ((index < DialogueManager.displaySettings.inputSettings.qteButtons.Length) ? DialogueManager.displaySettings.inputSettings.qteButtons[index] : null);
		float parameterAsFloat = GetParameterAsFloat(1);
		stopTime = DialogueTime.time + parameterAsFloat;
		variableName = GetParameter(2);
		variableQTEValue = GetParameter(3);
		variableType = GetVariableType();
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: QTE(index={1}, {2}sec, {3}, {4})", "Dialogue System", index, parameterAsFloat, variableName, variableQTEValue));
		}
		Lua.Run($"Variable[\"{variableName}\"] = \"\"", DialogueDebug.logInfo);
	}

	private FieldType GetVariableType()
	{
		if (string.Equals(variableQTEValue, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(variableQTEValue, "false", StringComparison.OrdinalIgnoreCase))
		{
			return FieldType.Boolean;
		}
		if (float.TryParse(variableQTEValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var _))
		{
			return FieldType.Number;
		}
		return FieldType.Text;
	}

	public void Update()
	{
		if (!string.IsNullOrEmpty(button) && DialogueManager.getInputButtonDown(button))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Sequencer: QTE(" + GetParameters() + ") triggered. Setting " + variableName + " to " + variableQTEValue);
			}
			if (variableType == FieldType.Boolean)
			{
				DialogueLua.SetVariable(variableName, Tools.StringToBool(variableQTEValue));
			}
			else
			{
				DialogueLua.SetVariable(variableName, ValueAsString(variableType, variableQTEValue));
			}
			DialogueManager.instance.SendMessage("OnConversationContinueAll", SendMessageOptions.DontRequireReceiver);
			Stop();
		}
		else if (DialogueTime.time >= stopTime)
		{
			Stop();
		}
	}

	private string ValueAsString(FieldType fieldType, string fieldValue)
	{
		switch (fieldType)
		{
		case FieldType.Number:
		case FieldType.Actor:
		case FieldType.Item:
		case FieldType.Location:
			if (!string.IsNullOrEmpty(fieldValue))
			{
				return fieldValue;
			}
			return "0";
		case FieldType.Boolean:
			if (!string.IsNullOrEmpty(fieldValue))
			{
				return fieldValue.ToLower();
			}
			return "false";
		default:
			return fieldValue;
		}
	}

	public void OnDestroy()
	{
		DialogueManager.dialogueUI.HideQTEIndicator(index);
	}
}
