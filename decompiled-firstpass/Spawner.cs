using UnityEngine;

public class Spawner : MonoBehaviour
{
	public Transform prefab;

	public float randomRange = 0.5f;

	public float timeToSpawn = 4f;

	private float timer;

	public void Update()
	{
		timer += Time.deltaTime;
		if (timer >= timeToSpawn)
		{
			timer = 0f;
			Object.Instantiate(prefab, base.transform.position + Random.insideUnitSphere * randomRange, base.transform.rotation).gameObject.SetActive(value: true);
		}
	}

	public void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, randomRange);
	}
}
