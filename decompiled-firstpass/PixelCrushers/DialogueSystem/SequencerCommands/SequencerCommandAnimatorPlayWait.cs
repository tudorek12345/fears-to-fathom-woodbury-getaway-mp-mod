using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAnimatorPlayWait : SequencerCommand
{
	private const float maxDurationToWaitForStateStart = 1f;

	public void Start()
	{
		string parameter = GetParameter(0);
		Transform subject = GetSubject(1);
		float parameterAsFloat = GetParameterAsFloat(2);
		int parameterAsInt = GetParameterAsInt(3, -1);
		Animator animator = ((subject != null) ? subject.GetComponentInChildren<Animator>() : null);
		if (animator == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Sequencer: AnimatorPlayWait({1}, {2}, fade={3}, layer={4}): No Animator found on {2}", "Dialogue System", parameter, (subject != null) ? subject.name : GetParameter(1), parameterAsFloat, parameterAsInt));
			}
			Stop();
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorPlayWait({1}, {2}, {3})", "Dialogue System", parameter, subject, parameterAsFloat));
		}
		if (!animator.gameObject.activeSelf)
		{
			animator.gameObject.SetActive(value: true);
		}
		if (Tools.ApproximatelyZero(parameterAsFloat))
		{
			animator.Play(parameter, parameterAsInt, 0f);
		}
		else
		{
			animator.CrossFadeInFixedTime(parameter, parameterAsFloat, parameterAsInt);
		}
		StartCoroutine(MonitorState(animator, parameter));
	}

	private IEnumerator MonitorState(Animator animator, string stateName)
	{
		float maxStartTime = DialogueTime.time + 1f;
		AnimatorStateInfo animatorStateInfo;
		bool flag = CheckIsInState(animator, stateName, out animatorStateInfo);
		while (!flag && DialogueTime.time < maxStartTime)
		{
			yield return null;
			flag = CheckIsInState(animator, stateName, out animatorStateInfo);
		}
		if (flag)
		{
			yield return StartCoroutine(DialogueTime.WaitForSeconds(animatorStateInfo.length));
		}
		Stop();
	}

	private bool CheckIsInState(Animator animator, string stateName, out AnimatorStateInfo animatorStateInfo)
	{
		if (animator != null)
		{
			for (int i = 0; i < animator.layerCount; i++)
			{
				AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(i);
				if (currentAnimatorStateInfo.IsName(stateName))
				{
					animatorStateInfo = currentAnimatorStateInfo;
					return true;
				}
			}
		}
		animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		return false;
	}
}
