using System.Collections;
using UnityEngine;

namespace EPOOutline.Demo;

public class Doughnut : MonoBehaviour, ICollectable
{
	[SerializeField]
	private float rotationSpeed = 30f;

	[SerializeField]
	private AudioClip eatSound;

	[SerializeField]
	private float moveAmplitude = 0.25f;

	[SerializeField]
	private float moveSpeed = 0.2f;

	private Outlinable outlinable;

	private Vector3 initialPosition;

	private float amplitudeShift;

	private bool isCollected;

	private void Start()
	{
		outlinable = GetComponent<Outlinable>();
		amplitudeShift = Random.Range(0f, 10f);
		initialPosition = base.transform.position;
	}

	private void Update()
	{
		if (!isCollected)
		{
			base.transform.position = initialPosition + Vector3.up * Mathf.Sin(Time.time * moveSpeed + amplitudeShift);
		}
		base.transform.Rotate(Vector3.up * rotationSpeed * Time.smoothDeltaTime, Space.World);
	}

	public void Collect(GameObject collector)
	{
		if (!isCollected)
		{
			isCollected = true;
			StartCoroutine(AnimateCollection(collector));
		}
	}

	private IEnumerator AnimateCollection(GameObject collector)
	{
		AudioSource.PlayClipAtPoint(eatSound, base.transform.position, 10f);
		float duration = 0.2f;
		float collectionRadius = 1.5f;
		float collectionAngle = Random.Range(0f, 360f);
		float timeLeft = duration;
		while (collector != null && timeLeft > 0f)
		{
			timeLeft -= Time.smoothDeltaTime;
			Vector3 vector = Quaternion.Euler(0f, collectionAngle, 0f) * Vector3.right;
			Vector3 b = collector.transform.position + vector + Vector3.up * 4.5f;
			base.transform.position = Vector3.Lerp(base.transform.position, b, Time.smoothDeltaTime * 5f);
			collectionAngle += Time.smoothDeltaTime * 360f;
			collectionRadius = Mathf.MoveTowards(collectionRadius, 0f, Time.smoothDeltaTime * 3.5f);
			yield return null;
		}
		timeLeft = duration;
		Vector3 initialScale = base.transform.localScale;
		while (timeLeft >= 0f)
		{
			timeLeft -= Time.smoothDeltaTime;
			base.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, 1f - timeLeft / duration);
			yield return null;
		}
		base.transform.localScale = Vector3.zero;
		Object.Destroy(base.gameObject);
	}
}
