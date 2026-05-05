using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandLiveCamera : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private Transform subject;

	private Transform angleTransform;

	private Transform cameraTransform;

	private bool isLocalTransform;

	private Quaternion targetRotation;

	private Vector3 targetPosition;

	private float duration;

	private float startTime;

	private float endTime;

	private Quaternion originalRotation;

	private Vector3 originalPosition;

	private bool isOriginal;

	public void Start()
	{
		string text = GetParameter(0, "Closeup");
		subject = GetSubject(1, base.speaker);
		duration = GetParameterAsFloat(2);
		if (string.Equals(text, "default"))
		{
			text = SequencerTools.GetDefaultCameraAngle(subject);
		}
		isOriginal = string.Equals(text, "original");
		angleTransform = (isOriginal ? Camera.main.transform : ((base.sequencer.cameraAngles != null) ? base.sequencer.cameraAngles.transform.Find(text) : null));
		isLocalTransform = true;
		if (angleTransform == null)
		{
			isLocalTransform = false;
			GameObject gameObject = GameObject.Find(text);
			if (gameObject != null)
			{
				angleTransform = gameObject.transform;
			}
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: Camera({1}, {2}, {3}s)", "Dialogue System", text, Tools.GetGameObjectName(subject), duration));
		}
		if (angleTransform == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: Camera angle '{1}' wasn't found.", new object[2] { "Dialogue System", text }));
		}
		if (subject == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: Camera subject '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(1)
			}));
		}
		base.sequencer.TakeCameraControl();
		if (isOriginal || (angleTransform != null && subject != null))
		{
			cameraTransform = base.sequencer.sequencerCameraTransform;
			if (isOriginal)
			{
				targetRotation = base.sequencer.originalCameraRotation;
				targetPosition = base.sequencer.originalCameraPosition;
			}
			else if (isLocalTransform)
			{
				targetRotation = subject.rotation * angleTransform.localRotation;
				targetPosition = subject.position + subject.rotation * angleTransform.localPosition;
			}
			else
			{
				targetRotation = angleTransform.rotation;
				targetPosition = angleTransform.position;
			}
			if (duration > 0.05f)
			{
				startTime = DialogueTime.time;
				endTime = startTime + duration;
				originalRotation = cameraTransform.rotation;
				originalPosition = cameraTransform.position;
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
			if (isOriginal || (angleTransform != null && subject != null))
			{
				cameraTransform = base.sequencer.sequencerCameraTransform;
				if (isOriginal)
				{
					targetRotation = base.sequencer.originalCameraRotation;
					targetPosition = base.sequencer.originalCameraPosition;
				}
				else if (isLocalTransform)
				{
					targetRotation = subject.rotation * angleTransform.localRotation;
					targetPosition = subject.position + subject.rotation * angleTransform.localPosition;
				}
				else
				{
					targetRotation = angleTransform.rotation;
					targetPosition = angleTransform.position;
				}
			}
			float t = (DialogueTime.time - startTime) / duration;
			cameraTransform.rotation = Quaternion.Lerp(originalRotation, targetRotation, t);
			cameraTransform.position = Vector3.Lerp(originalPosition, targetPosition, t);
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if (angleTransform != null && subject != null && cameraTransform != null)
		{
			cameraTransform.rotation = targetRotation;
			cameraTransform.position = targetPosition;
		}
	}
}
