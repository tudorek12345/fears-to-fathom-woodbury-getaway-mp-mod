using System;
using System.Collections.Generic;
using UnityEngine;

namespace EPOOutline;

[Serializable]
public class SerializedPass : ISerializationCallbackReceiver
{
	public enum PropertyType
	{
		Color,
		Vector,
		Float,
		Range,
		TexEnv
	}

	[Serializable]
	private class SerializedPropertyKeyValuePair
	{
		[SerializeField]
		public string PropertyName;

		[SerializeField]
		public SerializedPassProperty Property;
	}

	[Serializable]
	private class SerializedPassProperty
	{
		[SerializeField]
		public Color ColorValue;

		[SerializeField]
		public float FloatValue;

		[SerializeField]
		public Vector4 VectorValue;

		[SerializeField]
		public PropertyType PropertyType;
	}

	[SerializeField]
	private Shader shader;

	[SerializeField]
	private List<SerializedPropertyKeyValuePair> serializedProperties = new List<SerializedPropertyKeyValuePair>();

	private Dictionary<int, SerializedPassProperty> propertiesById = new Dictionary<int, SerializedPassProperty>();

	private Dictionary<string, SerializedPassProperty> propertiesByName = new Dictionary<string, SerializedPassProperty>();

	private Material material;

	private bool propertiesIsDirty;

	public Shader Shader
	{
		get
		{
			return shader;
		}
		set
		{
			propertiesIsDirty = true;
			shader = value;
		}
	}

	public Material Material
	{
		get
		{
			if (shader == null)
			{
				return null;
			}
			if (material == null || material.shader != shader)
			{
				if (material != null)
				{
					UnityEngine.Object.DestroyImmediate(material);
				}
				material = new Material(shader);
			}
			if (!propertiesIsDirty)
			{
				return material;
			}
			foreach (KeyValuePair<int, SerializedPassProperty> item in propertiesById)
			{
				switch (item.Value.PropertyType)
				{
				case PropertyType.Color:
					material.SetColor(item.Key, item.Value.ColorValue);
					break;
				case PropertyType.Vector:
					material.SetVector(item.Key, item.Value.VectorValue);
					break;
				case PropertyType.Float:
					material.SetFloat(item.Key, item.Value.FloatValue);
					break;
				case PropertyType.Range:
					material.SetFloat(item.Key, item.Value.FloatValue);
					break;
				}
			}
			propertiesIsDirty = false;
			return material;
		}
	}

	public bool HasProperty(string name)
	{
		return propertiesByName.ContainsKey(name);
	}

	public bool HasProperty(int hash)
	{
		return propertiesById.ContainsKey(hash);
	}

	public Vector4 GetVector(string name)
	{
		SerializedPassProperty value = null;
		if (!propertiesByName.TryGetValue(name, out value))
		{
			Debug.LogError("The property " + name + " doesn't exist");
			return Vector4.zero;
		}
		if (value.PropertyType == PropertyType.Vector)
		{
			return value.VectorValue;
		}
		Debug.LogError("The property " + name + " is not a vector property");
		return Vector4.zero;
	}

	public Vector4 GetVector(int hash)
	{
		SerializedPassProperty value = null;
		if (!propertiesById.TryGetValue(hash, out value))
		{
			Debug.LogError("The property " + hash + " doesn't exist");
			return Vector4.zero;
		}
		if (value.PropertyType == PropertyType.Vector)
		{
			return value.VectorValue;
		}
		Debug.LogError("The property " + hash + " is not a vector property");
		return Vector4.zero;
	}

	public void SetVector(string name, Vector4 value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesByName.TryGetValue(name, out value2))
		{
			value2 = new SerializedPassProperty();
			value2.PropertyType = PropertyType.Vector;
			propertiesByName.Add(name, value2);
			propertiesById.Add(Shader.PropertyToID(name), value2);
		}
		if (value2.PropertyType != PropertyType.Vector)
		{
			Debug.LogError("The property " + name + " is not a vector property");
		}
		else
		{
			value2.VectorValue = value;
		}
	}

	public void SetVector(int hash, Vector4 value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesById.TryGetValue(hash, out value2))
		{
			Debug.LogWarning("The property " + hash + " doesn't exist. Use string overload to create one.");
		}
		else if (value2.PropertyType != PropertyType.Vector)
		{
			Debug.LogError("The property " + hash + " is not a vector property");
		}
		else
		{
			value2.VectorValue = value;
		}
	}

	public float GetFloat(string name)
	{
		SerializedPassProperty value = null;
		if (!propertiesByName.TryGetValue(name, out value))
		{
			Debug.LogError("The property " + name + " doesn't exist");
			return 0f;
		}
		if (value.PropertyType == PropertyType.Float || value.PropertyType == PropertyType.Range)
		{
			return value.FloatValue;
		}
		Debug.LogError("The property " + name + " is not a float property");
		return 0f;
	}

	public float GetFloat(int hash)
	{
		SerializedPassProperty value = null;
		if (!propertiesById.TryGetValue(hash, out value))
		{
			Debug.LogError("The property " + hash + " is doesn't exist");
			return 0f;
		}
		if (value.PropertyType == PropertyType.Float || value.PropertyType == PropertyType.Range)
		{
			return value.FloatValue;
		}
		Debug.LogError("The property " + hash + " is not a float property");
		return 0f;
	}

	public void SetFloat(string name, float value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesByName.TryGetValue(name, out value2))
		{
			value2 = new SerializedPassProperty();
			value2.PropertyType = PropertyType.Float;
			propertiesByName.Add(name, value2);
			propertiesById.Add(Shader.PropertyToID(name), value2);
		}
		if (value2.PropertyType != PropertyType.Float && value2.PropertyType != PropertyType.Range)
		{
			Debug.LogError("The property " + name + " is not a float property");
		}
		else
		{
			value2.FloatValue = value;
		}
	}

	public void SetFloat(int hash, float value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesById.TryGetValue(hash, out value2))
		{
			Debug.LogError("The property " + hash + " doesn't exist. Use string overload to create one.");
		}
		else if (value2.PropertyType != PropertyType.Float)
		{
			Debug.LogError("The property " + hash + " is not a float property");
		}
		else
		{
			value2.FloatValue = value;
		}
	}

	public Color GetColor(string name)
	{
		SerializedPassProperty value = null;
		if (!propertiesByName.TryGetValue(name, out value))
		{
			Debug.LogError("The property " + name + " doesn't exist");
			return Color.black;
		}
		if (value.PropertyType == PropertyType.Color)
		{
			return value.ColorValue;
		}
		Debug.LogError("The property " + name + " is not a color property");
		return Color.black;
	}

	public Color GetColor(int hash)
	{
		SerializedPassProperty value = null;
		if (!propertiesById.TryGetValue(hash, out value))
		{
			Debug.LogError("The property " + hash + " doesn't exist");
			return Color.black;
		}
		if (value.PropertyType == PropertyType.Color)
		{
			return value.ColorValue;
		}
		return Color.black;
	}

	public void SetColor(string name, Color value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesByName.TryGetValue(name, out value2))
		{
			value2 = new SerializedPassProperty();
			value2.PropertyType = PropertyType.Color;
			propertiesByName.Add(name, value2);
			propertiesById.Add(Shader.PropertyToID(name), value2);
		}
		if (value2.PropertyType != PropertyType.Color)
		{
			Debug.LogError("The property " + name + " is not a color property.");
		}
		else
		{
			value2.ColorValue = value;
		}
	}

	public void SetColor(int hash, Color value)
	{
		propertiesIsDirty = true;
		SerializedPassProperty value2 = null;
		if (!propertiesById.TryGetValue(hash, out value2))
		{
			Debug.LogError("The property " + hash + " doesn't exist. Use string overload to create one.");
		}
		else if (value2.PropertyType != PropertyType.Color)
		{
			Debug.LogError("The property " + hash + " is not a color property");
		}
		else
		{
			value2.ColorValue = value;
		}
	}

	public void OnBeforeSerialize()
	{
		serializedProperties.Clear();
		foreach (KeyValuePair<string, SerializedPassProperty> item in propertiesByName)
		{
			SerializedPropertyKeyValuePair serializedPropertyKeyValuePair = new SerializedPropertyKeyValuePair();
			serializedPropertyKeyValuePair.Property = item.Value;
			serializedPropertyKeyValuePair.PropertyName = item.Key;
			serializedProperties.Add(serializedPropertyKeyValuePair);
		}
	}

	public void OnAfterDeserialize()
	{
		propertiesIsDirty = true;
		propertiesById.Clear();
		propertiesByName.Clear();
		foreach (SerializedPropertyKeyValuePair serializedProperty in serializedProperties)
		{
			if (!propertiesByName.ContainsKey(serializedProperty.PropertyName))
			{
				propertiesById.Add(Shader.PropertyToID(serializedProperty.PropertyName), serializedProperty.Property);
				propertiesByName.Add(serializedProperty.PropertyName, serializedProperty.Property);
			}
		}
	}
}
