using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

public class QueuedSequencerCommand
{
	public string command;

	public string[] parameters;

	public float startTime;

	public string messageToWaitFor;

	public string endMessage;

	public bool required;

	public Transform speaker;

	public Transform listener;

	public QueuedSequencerCommand(string command, string[] parameters, float startTime, string messageToWaitFor, string endMessage, bool required, Transform speaker = null, Transform listener = null)
	{
		this.command = command;
		this.parameters = parameters;
		this.startTime = startTime;
		this.messageToWaitFor = messageToWaitFor;
		this.endMessage = endMessage;
		this.required = required;
		this.speaker = speaker;
		this.listener = listener;
	}
}
