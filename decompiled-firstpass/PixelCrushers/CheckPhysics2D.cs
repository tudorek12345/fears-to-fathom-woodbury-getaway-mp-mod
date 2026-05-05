using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers;

[AddComponentMenu("")]
public class CheckPhysics2D : MonoBehaviour
{
	public UnityEvent usePhysics2DDefined = new UnityEvent();

	public UnityEvent usePhysics2DUndefined = new UnityEvent();

	private void Start()
	{
		usePhysics2DUndefined.Invoke();
	}
}
