using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class TriggerEvent : TagMaskEvent
{
	[SerializeField]
	private GameObjectUnityEvent m_onTriggerEnter = new GameObjectUnityEvent();

	[SerializeField]
	private GameObjectUnityEvent m_onTriggerExit = new GameObjectUnityEvent();

	public GameObjectUnityEvent onTriggerEnter
	{
		get
		{
			return m_onTriggerEnter;
		}
		set
		{
			m_onTriggerEnter = value;
		}
	}

	public GameObjectUnityEvent onTriggerExit
	{
		get
		{
			return m_onTriggerExit;
		}
		set
		{
			m_onTriggerExit = value;
		}
	}

	protected virtual void OnTriggerEnter(Collider other)
	{
		if (IsInTagMask(other.tag))
		{
			onTriggerEnter.Invoke(other.gameObject);
		}
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (IsInTagMask(other.tag))
		{
			onTriggerExit.Invoke(other.gameObject);
		}
	}
}
