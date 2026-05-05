using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem;

public class BarkHistory
{
	public BarkOrder order;

	public int index;

	public List<int> entries;

	public BarkHistory(BarkOrder order)
	{
		this.order = order;
		index = 0;
		entries = null;
	}

	public int GetNextIndex(int numEntries)
	{
		if (order == BarkOrder.Random)
		{
			if (numEntries == 0)
			{
				return 0;
			}
			if (entries == null)
			{
				entries = new List<int>();
			}
			if (entries.Count != numEntries || index >= entries.Count)
			{
				int num = ((entries.Count > 0) ? entries[entries.Count - 1] : 0);
				entries.Clear();
				for (int i = 0; i < numEntries; i++)
				{
					entries.Add(i);
				}
				entries.Shuffle();
				if (entries[0] == num)
				{
					entries.RemoveAt(0);
					entries.Add(num);
				}
				index = 0;
			}
			if (0 > index || index >= entries.Count)
			{
				return 0;
			}
			return entries[index++];
		}
		int result = index % numEntries;
		index = (index + 1) % numEntries;
		return result;
	}

	public void Reset()
	{
		index = 0;
	}
}
