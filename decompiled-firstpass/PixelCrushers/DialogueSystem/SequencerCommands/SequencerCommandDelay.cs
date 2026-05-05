using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandDelay : SequencerCommand
{
	private float stopTime;

	public void Start()
	{
		float parameterAsFloat = GetParameterAsFloat(0);
		stopTime = DialogueTime.time + parameterAsFloat;
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: Delay({1})", new object[2] { "Dialogue System", parameterAsFloat }));
		}
	}

	public void Update()
	{
		if (DialogueTime.time >= stopTime)
		{
			Stop();
		}
	}
}
