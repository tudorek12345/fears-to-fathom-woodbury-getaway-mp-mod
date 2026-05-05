using System.Collections.Generic;
using UnityEngine;
using ch.sycoforge.Decal;
using ch.sycoforge.Decal.Projectors.Geometry;

public class RuntimeDecalCombiner
{
	public static List<GameObject> Combine(IList<EasyDecal> decals)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		Dictionary<DecalTextureAtlas, List<EasyDecal>> dictionary = new Dictionary<DecalTextureAtlas, List<EasyDecal>>();
		foreach (EasyDecal decal in decals)
		{
			if ((int)decal.Source == 1 && ((DecalBase)decal).Projector != null)
			{
				if (!dictionary.ContainsKey(decal.Atlas))
				{
					dictionary.Add(decal.Atlas, new List<EasyDecal>());
				}
				dictionary[decal.Atlas].Add(decal);
			}
		}
		return Combine(dictionary);
	}

	private static List<GameObject> Combine(Dictionary<DecalTextureAtlas, List<EasyDecal>> mappings)
	{
		List<GameObject> list = new List<GameObject>();
		if (mappings.Count > 0)
		{
			foreach (DecalTextureAtlas key in mappings.Keys)
			{
				IList<EasyDecal> list2 = mappings[key];
				foreach (EasyDecal item in list2)
				{
					_ = item;
					GameObject gameObject = Combine(list2, key);
					if (gameObject != null)
					{
						list.Add(gameObject);
					}
				}
			}
		}
		return list;
	}

	private static GameObject Combine(IList<EasyDecal> decals, DecalTextureAtlas atlas)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		if (decals.Count > 0)
		{
			DynamicMesh val = new DynamicMesh((DecalBase)(object)DecalBase.DecalRoot, (RecreationMode)0);
			GameObject gameObject = new GameObject($"Combined Decals Root [{((Object)(object)atlas).name}]");
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			foreach (EasyDecal decal in decals)
			{
				if ((int)decal.Source == 1 && ((DecalBase)decal).Projector != null)
				{
					val.Add(((DecalBase)decal).Projector.Mesh, ((DecalBase)decal).LocalToWorldMatrix, gameObject.transform.worldToLocalMatrix);
					((Component)(object)decal).gameObject.SetActive(value: false);
				}
			}
			meshRenderer.material = atlas.Material;
			meshFilter.sharedMesh = val.ConvertToMesh((Mesh)null, false);
		}
		return null;
	}
}
