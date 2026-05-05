using System.Collections.Generic;
using UnityEngine;

namespace EPOOutline;

public static class RendererFilteringUtility
{
	private static List<Outlinable> filteredOutlinables = new List<Outlinable>();

	public static void Filter(Camera camera, OutlineParameters parameters)
	{
		filteredOutlinables.Clear();
		int num = parameters.Mask.value & camera.cullingMask;
		foreach (Outlinable item in parameters.OutlinablesToRender)
		{
			long num2 = 1L << item.OutlineLayer;
			if ((parameters.OutlineLayerMask & num2) != 0L)
			{
				GameObject gameObject = item.gameObject;
				if (gameObject.activeInHierarchy && ((1 << gameObject.layer) & num) != 0)
				{
					filteredOutlinables.Add(item);
				}
			}
		}
		parameters.OutlinablesToRender.Clear();
		parameters.OutlinablesToRender.AddRange(filteredOutlinables);
	}
}
