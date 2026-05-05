using UnityEngine;

public class Wrappable : MonoBehaviour
{
	private bool wrapped;

	public Color normalColor = new Color(0.2f, 0.2f, 0.8f);

	public Color wrappedColor = new Color(0.9f, 0.9f, 0.2f);

	private Material localMaterial;

	public void Awake()
	{
		localMaterial = GetComponent<MeshRenderer>().material;
	}

	public void OnDestroy()
	{
		Object.Destroy(localMaterial);
	}

	public void Reset()
	{
		wrapped = false;
		localMaterial.color = normalColor;
	}

	public void SetWrapped()
	{
		wrapped = true;
		localMaterial.color = wrappedColor;
	}

	public bool IsWrapped()
	{
		return wrapped;
	}
}
