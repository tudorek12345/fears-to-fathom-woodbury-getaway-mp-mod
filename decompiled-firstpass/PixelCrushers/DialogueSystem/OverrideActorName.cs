using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class OverrideActorName : DialogueActor
{
	public string overrideName
	{
		get
		{
			return actor;
		}
		set
		{
			actor = value;
		}
	}

	public string internalName
	{
		get
		{
			return persistentDataName;
		}
		set
		{
			persistentDataName = value;
		}
	}

	public static string GetInternalName(Transform t)
	{
		return DialogueActor.GetPersistentDataName(t);
	}
}
