using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAnimatorLayer : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private int layerIndex = 1;

	private float weight;

	private Transform subject;

	private float duration;

	private Animator animator;

	private float startTime;

	private float endTime;

	private float originalWeight;

	public void Start()
	{
		layerIndex = GetParameterAsInt(0, 1);
		weight = GetParameterAsFloat(1, 1f);
		subject = GetSubject(2);
		duration = GetParameterAsFloat(3);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorLayer({1}, {2}, {3}, {4})", "Dialogue System", layerIndex, weight, subject, duration));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorLayer(): subject '{1}' wasn't found.", new object[2]
				{
					"Dialogue System",
					GetParameter(2)
				}));
			}
			Stop();
			return;
		}
		animator = subject.GetComponentInChildren<Animator>();
		if (animator == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorLayer(): no Animator found on '{1}'.", new object[2] { "Dialogue System", subject.name }));
			}
			Stop();
		}
		else if (layerIndex < 1 || layerIndex >= animator.layerCount)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorLayer(): layer index {1} is invalid.", new object[2] { "Dialogue System", layerIndex }));
			}
			Stop();
		}
		else if (duration < 0.05f)
		{
			Stop();
		}
		else
		{
			startTime = DialogueTime.time;
			endTime = startTime + duration;
			originalWeight = animator.GetLayerWeight(layerIndex);
		}
	}

	public void Update()
	{
		if (DialogueTime.time < endTime)
		{
			float num = (DialogueTime.time - startTime) / duration;
			float num2 = Mathf.Lerp(originalWeight, weight, num / duration);
			if (animator != null)
			{
				animator.SetLayerWeight(layerIndex, num2);
			}
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if (animator != null && 0 < layerIndex && layerIndex < animator.layerCount)
		{
			animator.SetLayerWeight(layerIndex, weight);
		}
	}
}
