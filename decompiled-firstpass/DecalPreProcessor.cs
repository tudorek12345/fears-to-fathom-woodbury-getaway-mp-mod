using UnityEngine;
using ch.sycoforge.Decal;

[RequireComponent(typeof(EasyDecal))]
public class DecalPreProcessor : MonoBehaviour
{
	private void Awake()
	{
		((DecalBase)GetComponent<EasyDecal>()).Distance = Random.Range(0.0001f, 0.001f);
	}
}
