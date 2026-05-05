using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAudioWWW : SequencerCommand
{
	public void Start()
	{
		Debug.Log("Dialogue System: Sequencer: AudioWWW() is deprecated in Unity 2018+.");
		Stop();
	}
}
