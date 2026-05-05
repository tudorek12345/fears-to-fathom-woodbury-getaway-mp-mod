using System;
using UnityEngine;

namespace UI.ThreeDimensional;

public class DelayedAction
{
	public float timeToExecute;

	public Action action;

	public MonoBehaviour target;

	public bool forceEvenIfTargetIsInactive;
}
