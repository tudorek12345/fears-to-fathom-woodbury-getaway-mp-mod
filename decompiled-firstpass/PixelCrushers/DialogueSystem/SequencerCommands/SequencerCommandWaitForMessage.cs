using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandWaitForMessage : SequencerCommand
{
	private List<string> requiredMessages = new List<string>();

	public void Awake()
	{
		requiredMessages.AddRange(base.parameters);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: WaitForMessage({1})", new object[2]
			{
				"Dialogue System",
				GetParameters()
			}));
		}
		requiredMessages.RemoveAll((string x) => string.IsNullOrEmpty(x));
		if (requiredMessages.Count == 0)
		{
			Stop();
		}
	}

	public void OnSequencerMessage(string message)
	{
		if (requiredMessages.Contains(message))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: WaitForMessage({1}) received message", new object[2] { "Dialogue System", message }));
			}
			requiredMessages.RemoveAll((string x) => x == message);
			if (requiredMessages.Count == 0)
			{
				Stop();
			}
		}
	}
}
