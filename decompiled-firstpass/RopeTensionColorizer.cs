using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiRope))]
[RequireComponent(typeof(MeshRenderer))]
public class RopeTensionColorizer : MonoBehaviour
{
	public float minTension;

	public float maxTension = 0.2f;

	public Color normalColor = Color.green;

	public Color tensionColor = Color.red;

	public RopeTenser tenser;

	public float tenserThreshold = -5f;

	public float tenserMax = 0.1f;

	private ObiRope rope;

	private Material localMaterial;

	private void Awake()
	{
		rope = GetComponent<ObiRope>();
		localMaterial = GetComponent<MeshRenderer>().material;
	}

	private void OnDestroy()
	{
		Object.Destroy(localMaterial);
	}

	private void Update()
	{
		if (!(tenser == null))
		{
			float num = Mathf.Min((tenser.transform.position.y - tenserThreshold) / tenserMax, 1f);
			if (num > 0f)
			{
				float num2 = (((ObiRopeBase)rope).CalculateLength() / ((ObiRopeBase)rope).restLength - 1f - minTension) / (maxTension - minTension);
				localMaterial.color = Color.Lerp(normalColor, tensionColor, num2 * num);
			}
			else
			{
				localMaterial.color = normalColor;
			}
		}
	}
}
