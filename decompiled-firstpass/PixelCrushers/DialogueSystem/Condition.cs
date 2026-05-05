using System;
using System.Linq;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Condition
{
	public enum LastEvaluationValue
	{
		None,
		True,
		False
	}

	public string[] luaConditions = new string[0];

	public QuestCondition[] questConditions = new QuestCondition[0];

	public string[] acceptedTags = new string[0];

	public GameObject[] acceptedGameObjects = new GameObject[0];

	[HideInInspector]
	public int luaWizardIndex = -1;

	[HideInInspector]
	public LastEvaluationValue lastEvaluationValue;

	public bool IsTrue(Transform interactor)
	{
		bool flag = LuaConditionsAreTrue() && QuestConditionsAreTrue() && IsAcceptedTag(interactor) && IsAcceptedGameObject(interactor);
		lastEvaluationValue = (flag ? LastEvaluationValue.True : LastEvaluationValue.False);
		return flag;
	}

	private bool LuaConditionsAreTrue()
	{
		if (luaConditions != null)
		{
			for (int i = 0; i < luaConditions.Length; i++)
			{
				if (!Lua.IsTrue(luaConditions[i], DialogueDebug.logInfo))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool QuestConditionsAreTrue()
	{
		if (questConditions != null)
		{
			for (int i = 0; i < questConditions.Length; i++)
			{
				QuestCondition questCondition = questConditions[i];
				if (questCondition != null && !questCondition.IsTrue)
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IsAcceptedTag(Transform interactor)
	{
		if (interactor == null || acceptedTags == null || acceptedTags.Length == 0)
		{
			return true;
		}
		return Enumerable.Contains(acceptedTags, interactor.tag);
	}

	private bool IsAcceptedGameObject(Transform interactor)
	{
		if (interactor == null || acceptedGameObjects == null || acceptedGameObjects.Length == 0)
		{
			return true;
		}
		return Enumerable.Contains(acceptedGameObjects, interactor.gameObject);
	}
}
