using UnityEngine;
using UnityEngine.AI;

namespace EPOOutline.Demo;

public class Character : MonoBehaviour
{
	[SerializeField]
	private AudioSource walkSource;

	[SerializeField]
	private NavMeshAgent agent;

	[SerializeField]
	private Animator characterAnimator;

	private float initialWalkVolume;

	private Camera mainCamera;

	private void Start()
	{
		initialWalkVolume = walkSource.volume;
		mainCamera = Camera.main;
		agent.updateRotation = false;
	}

	private void Update()
	{
		Vector3 forward = mainCamera.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 right = mainCamera.transform.right;
		right.y = 0f;
		right.Normalize();
		Vector3 forward2 = forward * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal");
		if (forward2.magnitude > 0.1f)
		{
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(forward2), Time.deltaTime * agent.angularSpeed);
		}
		agent.velocity = forward2.normalized * agent.speed;
		walkSource.volume = initialWalkVolume * (agent.velocity.magnitude / agent.speed);
		characterAnimator.SetBool("IsRunning", forward2.magnitude > 0.1f);
	}

	private void OnTriggerEnter(Collider other)
	{
		other.GetComponent<ICollectable>()?.Collect(base.gameObject);
	}
}
