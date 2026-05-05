using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class EnableOnStart : MonoBehaviour
{
	[Tooltip("Enable this component when on start.")]
	[SerializeField]
	private Component m_component;

	public Component component
	{
		get
		{
			return m_component;
		}
		set
		{
			m_component = value;
		}
	}

	private void Start()
	{
		ComponentUtility.SetComponentEnabled(m_component, value: true);
	}
}
