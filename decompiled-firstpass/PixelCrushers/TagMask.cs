using System;
using UnityEngine;

namespace PixelCrushers;

[Serializable]
public class TagMask
{
	[SerializeField]
	public string[] m_tags = new string[1] { "Player" };

	public string[] tags
	{
		get
		{
			return m_tags;
		}
		set
		{
			m_tags = value;
		}
	}

	public bool IsInTagMask(string tag)
	{
		if (tags == null || tags.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < tags.Length; i++)
		{
			if (string.Equals(tag, tags[i]))
			{
				return true;
			}
		}
		return false;
	}
}
