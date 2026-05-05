using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkOnIdle : BarkStarter
{
	[Tooltip("Bark as soon as this component starts the first time.")]
	public bool barkOnStart;

	[Tooltip("Bark when the component is enabled. If disabled and reenabled, barks again.")]
	public bool barkOnEnable;

	[Tooltip("Minimum seconds between barks.")]
	public float minSeconds = 5f;

	[Tooltip("Maximum seconds between barks.")]
	public float maxSeconds = 10f;

	[Tooltip("Target to whom bark is addressed. Leave unassigned to just bark into the air.")]
	public Transform target;

	private bool started;

	protected override bool useOnce => false;

	protected override void Start()
	{
		base.Start();
		started = true;
		StartBarkLoop();
		if (barkOnStart && !barkOnEnable)
		{
			TryIdleBark();
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		StartBarkLoop();
		if (barkOnEnable)
		{
			TryIdleBark();
		}
	}

	public virtual void StartBarkLoop()
	{
		if (started)
		{
			StopAllCoroutines();
			StartCoroutine(BarkLoop());
		}
	}

	protected virtual IEnumerator BarkLoop()
	{
		while (true)
		{
			yield return new WaitForSeconds(Random.Range(minSeconds, maxSeconds));
			TryIdleBark();
		}
	}

	protected virtual void TryIdleBark()
	{
		if (base.enabled && (!DialogueManager.isConversationActive || allowDuringConversations) && !DialogueTime.isPaused)
		{
			TryBark(target);
		}
	}
}
