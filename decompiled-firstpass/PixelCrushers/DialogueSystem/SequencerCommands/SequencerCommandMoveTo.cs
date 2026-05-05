using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandMoveTo : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private Transform target;

	private Transform subject;

	private Rigidbody subjectRigidbody;

	private float duration;

	private float startTime;

	private float endTime;

	private Vector3 originalPosition;

	private Quaternion originalRotation;

	public void Start()
	{
		target = GetSubject(0);
		subject = GetSubject(1);
		duration = GetParameterAsFloat(2);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: MoveTo({1}, {2}, {3})", "Dialogue System", target, subject, duration));
		}
		if (target == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: MoveTo() target '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(0)
			}));
		}
		if (subject == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: MoveTo() subject '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(1)
			}));
		}
		if (subject != null && target != null && subject != target)
		{
			subjectRigidbody = subject.GetComponent<Rigidbody>();
			if (duration > 0.05f)
			{
				startTime = DialogueTime.time;
				endTime = startTime + duration;
				originalPosition = subject.position;
				originalRotation = subject.rotation;
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

	private void SetPosition(Vector3 newPosition, Quaternion newRotation)
	{
		if (subjectRigidbody != null && !subjectRigidbody.isKinematic)
		{
			subjectRigidbody.MoveRotation(newRotation);
			subjectRigidbody.MovePosition(newPosition);
		}
		else
		{
			subject.rotation = newRotation;
			subject.position = newPosition;
		}
	}

	public void Update()
	{
		if (DialogueTime.time < endTime)
		{
			float t = (DialogueTime.time - startTime) / duration;
			SetPosition(Vector3.Lerp(originalPosition, target.position, t), Quaternion.Lerp(originalRotation, target.rotation, t));
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if (subject != null && target != null && subject != target)
		{
			SetPosition(target.position, target.rotation);
		}
	}
}
