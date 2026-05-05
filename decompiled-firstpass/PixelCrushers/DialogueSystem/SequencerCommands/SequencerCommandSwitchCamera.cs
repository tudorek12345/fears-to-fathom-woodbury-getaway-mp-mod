using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandSwitchCamera : SequencerCommand
{
	public void Start()
	{
		Transform subject = GetSubject(0);
		Camera camera = ((subject != null) ? subject.GetComponent<Camera>() : null);
		if (camera != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SwitchCamera({1})", new object[2] { "Dialogue System", camera.name }));
			}
			base.sequencer.SwitchCamera(camera);
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: SwitchCamera({1}): Camera not found.", new object[2]
			{
				"Dialogue System",
				GetParameter(0)
			}));
		}
		Stop();
	}
}
