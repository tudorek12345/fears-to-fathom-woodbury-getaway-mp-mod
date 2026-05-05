using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public abstract class SequenceStarter : DialogueEventStarter
{
	[Tooltip("The sequence to play.")]
	[TextArea(1, 20)]
	public string sequence;

	[Tooltip("Speaker to use for the sequence (leave unassigned if no speaker is needed). Sequencer commands can reference 'speaker' and 'listener', so you may need to define them here.")]
	public Transform speaker;

	[Tooltip("Listener to use for the sequence (leave unassigned if no listener is needed). Sequencer commands can reference 'speaker' and 'listener', so you may need to define them here.")]
	public Transform listener;

	public Condition condition;

	private bool tryingToStart;

	public void TryStartSequence(Transform actor)
	{
		TryStartSequence(actor, actor);
	}

	public void TryStartSequence(Transform actor, Transform interactor)
	{
		if (tryingToStart)
		{
			return;
		}
		tryingToStart = true;
		try
		{
			if ((condition == null || condition.IsTrue(interactor)) && !string.IsNullOrEmpty(sequence))
			{
				DialogueManager.PlaySequence(sequence, Tools.Select(speaker, base.transform), Tools.Select(listener, actor));
				DestroyIfOnce();
			}
		}
		finally
		{
			tryingToStart = false;
		}
	}
}
