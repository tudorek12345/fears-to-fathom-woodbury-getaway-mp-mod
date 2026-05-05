using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class InstantiatePrefabs : MonoBehaviour
{
	public enum Position
	{
		ScreenSpaceUI,
		OriginalPosition,
		ParentPosition
	}

	[Tooltip("Make instances children of this parent. If unassigned, use this GameObject.")]
	[SerializeField]
	private Transform m_parent;

	[Tooltip("Prefabs to instantiate.")]
	[SerializeField]
	private GameObject[] m_prefabs = new GameObject[0];

	[Tooltip("Untick for screen-space GameObjects such as UI elements; tick for world-space GameObjects.")]
	[SerializeField]
	private Position m_position;

	private void OnEnable()
	{
		if (m_parent == null)
		{
			m_parent = base.transform;
		}
		for (int i = 0; i < m_prefabs.Length; i++)
		{
			GameObject gameObject = m_prefabs[i];
			if (gameObject != null)
			{
				GameObject gameObject2 = ((m_position == Position.ParentPosition) ? Object.Instantiate(gameObject, m_parent.position, m_parent.rotation) : Object.Instantiate(gameObject));
				if (gameObject2 == null)
				{
					Debug.LogWarning("Instantiate Prefabs was unable to instantiate " + gameObject, this);
					continue;
				}
				gameObject2.transform.SetParent(m_parent, m_position != Position.ScreenSpaceUI);
				gameObject2.name = gameObject.name;
			}
		}
		Object.Destroy(this);
	}
}
