using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace EPOOutline.Demo;

public class Chicken : MonoBehaviour
{
	[SerializeField]
	private bool alwaysActive;

	[SerializeField]
	private bool updateChicken = true;

	[SerializeField]
	private float searchRadius = 5f;

	private Outlinable outlinable;

	private NavMeshAgent agent;

	private Animator animator;

	private int enteredCount;

	private static int priority;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		outlinable = GetComponent<Outlinable>();
		animator = GetComponent<Animator>();
		if (!alwaysActive)
		{
			outlinable.enabled = false;
		}
		agent.avoidancePriority = priority++;
		if (updateChicken)
		{
			StartCoroutine(UpdateChicken());
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!alwaysActive && (bool)other.GetComponent<Character>())
		{
			outlinable.enabled = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!alwaysActive && (bool)other.GetComponent<Character>() && --enteredCount == 0)
		{
			outlinable.enabled = false;
		}
	}

	private IEnumerator UpdateChicken()
	{
		NavMeshPath path = new NavMeshPath();
		while (true)
		{
			animator.CrossFade("Walk In Place", 0.1f);
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			Vector3 vector = new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * searchRadius;
			if (!NavMesh.SamplePosition(base.transform.position + vector, out var hit, searchRadius, -1))
			{
				yield return null;
				continue;
			}
			Debug.DrawLine(base.transform.position, hit.position, Color.yellow, 3f);
			if (!NavMesh.CalculatePath(base.transform.position, hit.position, -1, path))
			{
				yield return null;
				continue;
			}
			agent.destination = hit.position;
			while (agent.pathStatus != NavMeshPathStatus.PathComplete)
			{
				yield return null;
			}
			float timeToWait = agent.remainingDistance / agent.speed * 1.5f;
			while (agent.remainingDistance > agent.stoppingDistance && timeToWait > 0f)
			{
				timeToWait -= Time.deltaTime;
				yield return null;
			}
			animator.CrossFade("Eat", 0.1f);
			yield return new WaitForSeconds(Random.Range(1f, 5f));
			yield return null;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
		Gizmos.DrawSphere(base.transform.position, searchRadius);
	}
}
