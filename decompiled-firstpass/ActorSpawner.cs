using Obi;
using UnityEngine;

public class ActorSpawner : MonoBehaviour
{
	public ObiActor template;

	public int basePhase = 2;

	public int maxInstances = 32;

	public float spawnDelay = 0.3f;

	private int phase;

	private int instances;

	private float timeFromLastSpawn;

	private void Update()
	{
		timeFromLastSpawn += Time.deltaTime;
		if (Input.GetMouseButtonDown(0) && instances < maxInstances && timeFromLastSpawn > spawnDelay)
		{
			GameObject obj = Object.Instantiate(((Component)(object)template).gameObject, base.transform.position, Quaternion.identity);
			obj.transform.SetParent(base.transform.parent);
			obj.GetComponent<ObiActor>().SetFilterCategory(basePhase + phase);
			phase++;
			instances++;
			timeFromLastSpawn = 0f;
		}
	}
}
