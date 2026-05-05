using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelCrushers;

public static class DictionaryExtensions
{
	public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Predicate<TKey> match)
	{
		if (dictionary == null || match == null)
		{
			return 0;
		}
		List<TKey> list = dictionary.Keys.Where((TKey k) => match(k)).ToList();
		if (list.Count > 0)
		{
			foreach (TKey item in list)
			{
				dictionary.Remove(item);
			}
		}
		return list.Count;
	}
}
