using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PersistentActiveDataMultiple : MonoBehaviour
{
	[Serializable]
	public class TargetConditionPair
	{
		public GameObject target;

		public Condition condition;
	}

	public List<TargetConditionPair> targetsAndConditions = new List<TargetConditionPair>();

	public bool checkOnStart = true;

	protected virtual void Start()
	{
		if (checkOnStart)
		{
			CheckAllTargets();
		}
	}

	protected virtual void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	protected virtual void OnDisable()
	{
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
	}

	public void OnApplyPersistentData()
	{
		CheckAllTargets();
	}

	public virtual void CheckAllTargets()
	{
		if (!base.enabled)
		{
			return;
		}
		foreach (TargetConditionPair targetsAndCondition in targetsAndConditions)
		{
			if (targetsAndCondition.target == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: No target is assigned to Persistent Active Data Multiple component on " + base.name + ".", this);
				}
			}
			else
			{
				targetsAndCondition.target.SetActive(targetsAndCondition.condition.IsTrue(null));
			}
		}
	}
}
