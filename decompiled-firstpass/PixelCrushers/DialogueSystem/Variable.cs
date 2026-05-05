using System;
using PixelCrushers.DialogueSystem.ChatMapper;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Variable : Asset
{
	public string InitialValue
	{
		get
		{
			return LookupValue("Initial Value");
		}
		set
		{
			Field.SetValue(fields, "Initial Value", value);
		}
	}

	public float InitialFloatValue
	{
		get
		{
			return LookupFloat("Initial Value");
		}
		set
		{
			Field.SetValue(fields, "Initial Value", value);
		}
	}

	public bool InitialBoolValue
	{
		get
		{
			return LookupBool("Initial Value");
		}
		set
		{
			Field.SetValue(fields, "Initial Value", value);
		}
	}

	public FieldType Type
	{
		get
		{
			return LookupInitialValueType();
		}
		set
		{
			SetInitialValueType(value);
		}
	}

	public Variable()
	{
	}

	public Variable(Variable sourceVariable)
		: base(sourceVariable)
	{
	}

	public Variable(UserVariable chatMapperUserVariable)
	{
		Assign(chatMapperUserVariable);
	}

	public void Assign(UserVariable chatMapperUserVariable)
	{
		if (chatMapperUserVariable != null)
		{
			Assign(0, chatMapperUserVariable.Fields);
			Field field = Field.Lookup(fields, "Initial Value");
			if (field != null && field.type == FieldType.Number && (string.Equals(field.value, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(field.value, "False", StringComparison.OrdinalIgnoreCase)))
			{
				field.type = FieldType.Boolean;
			}
		}
	}

	private FieldType LookupInitialValueType()
	{
		return Field.Lookup(fields, "Initial Value")?.type ?? FieldType.Text;
	}

	private void SetInitialValueType(FieldType type)
	{
		Field field = Field.Lookup(fields, "Initial Value");
		if (field != null)
		{
			field.type = type;
			field.typeString = Field.GetTypeString(type);
		}
	}
}
