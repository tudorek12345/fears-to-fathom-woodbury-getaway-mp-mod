using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class CollisionEvent : TagMaskEvent
{
	[SerializeField]
	private GameObjectUnityEvent m_onCollisionEnter = new GameObjectUnityEvent();

	[SerializeField]
	private GameObjectUnityEvent m_onCollisionExit = new GameObjectUnityEvent();

	public GameObjectUnityEvent onCollisionEnter
	{
		get
		{
			return m_onCollisionEnter;
		}
		set
		{
			m_onCollisionEnter = value;
		}
	}

	public GameObjectUnityEvent onCollisionExit
	{
		get
		{
			return m_onCollisionExit;
		}
		set
		{
			m_onCollisionExit = value;
		}
	}

	protected virtual void OnCollisionEnter(Collision collision)
	{
		if (IsInTagMask(collision.gameObject.tag))
		{
			onCollisionEnter.Invoke(collision.gameObject);
		}
	}

	protected virtual void OnCollisionExit(Collision collision)
	{
		if (IsInTagMask(collision.gameObject.tag))
		{
			onCollisionExit.Invoke(collision.gameObject);
		}
	}
}
