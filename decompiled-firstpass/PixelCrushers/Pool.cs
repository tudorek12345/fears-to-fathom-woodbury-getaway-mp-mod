using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

public class Pool<T> where T : new()
{
	private List<T> m_free = new List<T>();

	private List<T> m_used = new List<T>();

	public T Get()
	{
		lock (m_free)
		{
			if (m_free.Count > 0)
			{
				T val = m_free[0];
				m_used.Add(val);
				m_free.RemoveAt(0);
				return val;
			}
			T val2 = new T();
			m_used.Add(val2);
			return val2;
		}
	}

	public void Release(T item)
	{
		lock (m_free)
		{
			m_free.Add(item);
			m_used.Remove(item);
		}
	}

	public void Allocate(int initialSize)
	{
		while (m_free.Count < initialSize)
		{
			m_free.Add(new T());
		}
	}

	public void Trim(int max)
	{
		m_free.RemoveRange(0, Mathf.Min(m_free.Count, max));
	}
}
