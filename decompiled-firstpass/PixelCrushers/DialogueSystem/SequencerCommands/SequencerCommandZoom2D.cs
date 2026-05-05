using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandZoom2D : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private bool original;

	private Transform subject;

	private Vector3 targetPosition;

	private Vector3 originalPosition;

	private float targetSize;

	private float originalSize;

	private float duration;

	private float startTime;

	private float endTime;

	public void Start()
	{
		original = string.Equals(GetParameter(0), "original");
		subject = (original ? null : GetSubject(0, base.speaker));
		targetSize = GetParameterAsFloat(1, 16f);
		duration = GetParameterAsFloat(2);
		if (DialogueDebug.logInfo)
		{
			if (original)
			{
				Debug.Log(string.Format("{0}: Sequencer: Zoom2D(original, -, {1}s)", new object[2] { "Dialogue System", duration }));
			}
			else
			{
				Debug.Log(string.Format("{0}: Sequencer: Zoom2D({1}, {2}, {3}s)", "Dialogue System", Tools.GetGameObjectName(subject), targetSize, duration));
			}
		}
		if (subject == null && !original && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: Camera subject '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(0)
			}));
		}
		base.sequencer.TakeCameraControl();
		if (subject != null || original)
		{
			if (original)
			{
				targetPosition = base.sequencer.originalCameraPosition;
				targetSize = base.sequencer.originalOrthographicSize;
			}
			else
			{
				targetPosition = new Vector3(subject.position.x, subject.position.y, base.sequencer.sequencerCamera.transform.position.z);
			}
			originalPosition = base.sequencer.sequencerCamera.transform.position;
			originalSize = base.sequencer.sequencerCamera.orthographicSize;
			if (duration > 0.05f)
			{
				startTime = DialogueTime.time;
				endTime = startTime + duration;
			}
			else
			{
				Stop();
			}
		}
		else
		{
			Stop();
		}
	}

	public void Update()
	{
		if (DialogueTime.time < endTime)
		{
			float t = (DialogueTime.time - startTime) / duration;
			if (base.sequencer != null && base.sequencer.sequencerCamera != null)
			{
				base.sequencer.sequencerCamera.transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
				base.sequencer.sequencerCamera.orthographicSize = Mathf.Lerp(originalSize, targetSize, t);
			}
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if ((subject != null || original) && base.sequencer != null && base.sequencer.sequencerCamera != null)
		{
			base.sequencer.sequencerCamera.transform.position = targetPosition;
			base.sequencer.sequencerCamera.orthographicSize = targetSize;
		}
	}
}
