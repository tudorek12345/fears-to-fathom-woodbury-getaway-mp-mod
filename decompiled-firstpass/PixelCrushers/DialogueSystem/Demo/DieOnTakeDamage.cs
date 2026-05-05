using UnityEngine;

namespace PixelCrushers.DialogueSystem.Demo;

[AddComponentMenu("")]
public class DieOnTakeDamage : MonoBehaviour
{
	public GameObject deadPrefab;

	private void TakeDamage(float damage)
	{
		if (deadPrefab != null)
		{
			GameObject gameObject = Object.Instantiate(deadPrefab, base.transform.position, base.transform.rotation);
			if (gameObject != null)
			{
				gameObject.transform.parent = base.transform.parent;
			}
		}
		Object.Destroy(base.gameObject);
	}

	private void Damage(float damage)
	{
		TakeDamage(damage);
	}
}
