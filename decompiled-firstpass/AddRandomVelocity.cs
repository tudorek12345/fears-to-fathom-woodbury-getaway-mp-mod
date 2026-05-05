using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiActor))]
public class AddRandomVelocity : MonoBehaviour
{
	public float intensity = 5f;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			GetComponent<ObiActor>().AddForce(Random.onUnitSphere * intensity, ForceMode.VelocityChange);
		}
	}
}
