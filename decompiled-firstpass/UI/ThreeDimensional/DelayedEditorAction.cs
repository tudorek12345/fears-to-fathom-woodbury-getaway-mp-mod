using System;
using UnityEngine;

namespace UI.ThreeDimensional;

internal class DelayedEditorAction
{
	internal double TimeToExecute;

	internal Action Action;

	internal MonoBehaviour ActionTarget;

	internal bool ForceEvenIfTargetIsGone;

	public DelayedEditorAction(double timeToExecute, Action action, MonoBehaviour actionTarget, bool forceEvenIfTargetIsGone = false)
	{
		TimeToExecute = timeToExecute;
		Action = action;
		ActionTarget = actionTarget;
		ForceEvenIfTargetIsGone = forceEvenIfTargetIsGone;
	}
}
