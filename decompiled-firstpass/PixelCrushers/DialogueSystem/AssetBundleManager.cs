using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class AssetBundleManager
{
	private HashSet<AssetBundle> m_bundles = new HashSet<AssetBundle>();

	public void RegisterAssetBundle(AssetBundle bundle)
	{
		if (!(bundle == null))
		{
			m_bundles.Add(bundle);
		}
	}

	public void UnregisterAssetBundle(AssetBundle bundle)
	{
		if (!(bundle == null))
		{
			m_bundles.Remove(bundle);
		}
	}

	public UnityEngine.Object Load(string name)
	{
		foreach (AssetBundle bundle in m_bundles)
		{
			if (bundle.Contains(name))
			{
				return LoadFromBundle(bundle, name);
			}
		}
		return Resources.Load(name);
	}

	public UnityEngine.Object Load(string name, Type type)
	{
		foreach (AssetBundle bundle in m_bundles)
		{
			if (bundle.Contains(name))
			{
				return LoadFromBundle(bundle, name, type);
			}
		}
		return Resources.Load(name, type);
	}

	private UnityEngine.Object LoadFromBundle(AssetBundle bundle, string name)
	{
		return bundle.LoadAsset(name);
	}

	private UnityEngine.Object LoadFromBundle(AssetBundle bundle, string name, Type type)
	{
		return bundle.LoadAsset(name, type);
	}
}
