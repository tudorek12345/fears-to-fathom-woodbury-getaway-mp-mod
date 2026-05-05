using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class StandardUIContentTemplate : MonoBehaviour
{
	public virtual void Despawn()
	{
		Object.Destroy(base.gameObject);
	}
}
