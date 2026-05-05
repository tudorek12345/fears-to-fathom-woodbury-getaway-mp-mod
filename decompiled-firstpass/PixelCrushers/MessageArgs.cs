using System;
using UnityEngine;

namespace PixelCrushers;

[Serializable]
public struct MessageArgs
{
	public object sender;

	public object target;

	public string message;

	public string parameter;

	public object[] values;

	public bool hasTarget
	{
		get
		{
			if (target != null)
			{
				return !string.IsNullOrEmpty(targetString);
			}
			return false;
		}
	}

	public bool isTargetString
	{
		get
		{
			Type type = ((target != null) ? target.GetType() : null);
			if (target != null)
			{
				if (!(type == typeof(string)))
				{
					return type == typeof(StringField);
				}
				return true;
			}
			return false;
		}
	}

	public string targetString
	{
		get
		{
			if (target == null)
			{
				return string.Empty;
			}
			Type type = target.GetType();
			if (!(type == typeof(string)))
			{
				if (!(type == typeof(StringField)))
				{
					return string.Empty;
				}
				return StringField.GetStringValue((StringField)target);
			}
			return (string)target;
		}
	}

	public object firstValue
	{
		get
		{
			if (values == null || values.Length == 0)
			{
				return null;
			}
			return values[0];
		}
	}

	public int intValue
	{
		get
		{
			try
			{
				return (int)firstValue;
			}
			catch (Exception)
			{
				return 0;
			}
		}
	}

	public MessageArgs(object sender, object target, string message, string parameter, object[] values = null)
	{
		this.sender = sender;
		this.target = target;
		this.message = message;
		this.parameter = parameter;
		this.values = values;
	}

	public MessageArgs(object sender, string message, string parameter, object[] values = null)
	{
		this.sender = sender;
		target = null;
		this.message = message;
		this.parameter = parameter;
		this.values = values;
	}

	public bool Matches(string message, string parameter)
	{
		if (string.Equals(message, this.message))
		{
			if (!string.IsNullOrEmpty(parameter))
			{
				return string.Equals(parameter, this.parameter);
			}
			return true;
		}
		return false;
	}

	public bool Matches(StringField message, StringField parameter)
	{
		if (string.Equals(message.value, this.message))
		{
			if (!StringField.IsNullOrEmpty(parameter))
			{
				return string.Equals(parameter.value, this.parameter);
			}
			return true;
		}
		return false;
	}

	public bool Matches(StringField message, string parameter)
	{
		if (string.Equals(message.value, this.message))
		{
			if (!string.IsNullOrEmpty(parameter))
			{
				return string.Equals(parameter, this.parameter);
			}
			return true;
		}
		return false;
	}

	public bool Matches(string message, StringField parameter)
	{
		if (string.Equals(message, this.message))
		{
			if (!StringField.IsNullOrEmpty(parameter))
			{
				return string.Equals(parameter.value, this.parameter);
			}
			return true;
		}
		return false;
	}

	public bool IsRequiredSender(string requiredSender)
	{
		if (!string.IsNullOrEmpty(requiredSender))
		{
			return string.Equals(requiredSender, GetSenderString());
		}
		return true;
	}

	public bool IsRequiredTarget(string requiredTarget)
	{
		if (!string.IsNullOrEmpty(requiredTarget))
		{
			return string.Equals(requiredTarget, GetTargetString());
		}
		return true;
	}

	public string GetSenderString()
	{
		return GetObjectString(sender);
	}

	public string GetTargetString()
	{
		return GetObjectString(target);
	}

	private string GetObjectString(object obj)
	{
		if (obj == null)
		{
			return string.Empty;
		}
		Type type = obj.GetType();
		if (type == typeof(string))
		{
			return (string)obj;
		}
		if (type == typeof(StringField))
		{
			return StringField.GetStringValue((StringField)obj);
		}
		if (type == typeof(GameObject))
		{
			return (obj as GameObject).name;
		}
		if (type == typeof(Component))
		{
			return (obj as Component).name;
		}
		return obj.ToString();
	}
}
