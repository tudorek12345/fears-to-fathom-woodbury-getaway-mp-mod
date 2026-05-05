using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class AlwaysFaceCamera : MonoBehaviour
{
	[Tooltip("Leave Y rotation untouched.")]
	[SerializeField]
	private bool m_yAxisOnly;

	[Tooltip("Flip 180 degrees.")]
	[SerializeField]
	private bool m_rotate180;

	private Camera m_mainCamera;

	public bool yAxisOnly
	{
		get
		{
			return m_yAxisOnly;
		}
		set
		{
			m_yAxisOnly = value;
		}
	}

	public bool rotate180
	{
		get
		{
			return m_rotate180;
		}
		set
		{
			m_rotate180 = value;
		}
	}

	private void Update()
	{
		if (m_mainCamera == null || !m_mainCamera.enabled || !m_mainCamera.gameObject.activeInHierarchy)
		{
			m_mainCamera = Camera.main;
		}
		if (m_mainCamera == null || !m_mainCamera.enabled || !m_mainCamera.gameObject.activeInHierarchy)
		{
			return;
		}
		if (rotate180)
		{
			if (yAxisOnly)
			{
				base.transform.rotation = Quaternion.Euler(base.transform.rotation.eulerAngles.x, (m_mainCamera.transform.rotation.eulerAngles + 180f * Vector3.up).y, base.transform.rotation.eulerAngles.z);
			}
			else
			{
				base.transform.rotation = Quaternion.LookRotation(-m_mainCamera.transform.forward, m_mainCamera.transform.up);
			}
		}
		else if (yAxisOnly)
		{
			base.transform.rotation = Quaternion.Euler(base.transform.rotation.eulerAngles.x, m_mainCamera.transform.rotation.eulerAngles.y, base.transform.rotation.eulerAngles.z);
		}
		else
		{
			base.transform.rotation = m_mainCamera.transform.rotation;
		}
	}
}
