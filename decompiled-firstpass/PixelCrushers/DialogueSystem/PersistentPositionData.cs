using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PersistentPositionData : MonoBehaviour
{
	public string overrideActorName;

	public bool recordCurrentLevel = true;

	[HideInInspector]
	public bool restoreCurrentLevelPosition = true;

	private string actorName
	{
		get
		{
			if (!string.IsNullOrEmpty(overrideActorName))
			{
				return overrideActorName;
			}
			return DialogueActor.GetPersistentDataName(base.gameObject.transform);
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

	public void Start()
	{
		if (string.IsNullOrEmpty(overrideActorName))
		{
			overrideActorName = DialogueActor.GetPersistentDataName(base.transform);
		}
	}

	public void OnRecordPersistentData()
	{
		string positionString = GetPositionString();
		string text = (recordCurrentLevel ? ("Position_" + SanitizeLevelName(Tools.loadedLevelName)) : "Position");
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Persistent Position Data Actor[" + actorName + "]." + text + "='" + positionString + "'", this);
		}
		DialogueLua.SetActorField(actorName, text, positionString);
	}

	public void OnApplyPersistentData()
	{
		string asString = DialogueLua.GetActorField(actorName, "Spawnpoint").asString;
		if (!string.IsNullOrEmpty(asString))
		{
			GameObject gameObject = Tools.GameObjectHardFind(asString);
			if (gameObject == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: Persistent Position Data found Actor[" + actorName + "].Spawnpoint value '" + asString + "' but can't find a GameObject with this name in the scene. Moving actor to saved position instead.", this);
				}
			}
			else
			{
				base.transform.position = gameObject.transform.position;
				base.transform.rotation = gameObject.transform.rotation;
				if (DialogueDebug.logInfo)
				{
					Debug.Log("Dialogue System: Persistent Position Data spawning " + actorName + " at spawnpoint " + gameObject, this);
				}
			}
			DialogueLua.SetActorField(actorName, "Spawnpoint", string.Empty);
			if (gameObject != null)
			{
				return;
			}
		}
		string text = (recordCurrentLevel ? ("Position_" + SanitizeLevelName(Tools.loadedLevelName)) : "Position");
		string asString2 = DialogueLua.GetActorField(actorName, text).asString;
		if (!string.IsNullOrEmpty(asString2))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Persistent Position Data restoring " + actorName + " to position " + asString2, this);
			}
			ApplyPositionString(asString2);
		}
		else if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Persistent Position Data Actor[" + actorName + "]." + text + " is blank. Not moving " + actorName, this);
		}
	}

	private string GetPositionString()
	{
		string text = (recordCurrentLevel ? DialogueLua.DoubleQuotesToSingle("," + Tools.loadedLevelName) : string.Empty);
		return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}{7}", base.transform.position.x, base.transform.position.y, base.transform.position.z, base.transform.rotation.x, base.transform.rotation.y, base.transform.rotation.z, base.transform.rotation.w, text);
	}

	private void ApplyPositionString(string s)
	{
		if (string.IsNullOrEmpty(s) || s.Equals("nil"))
		{
			return;
		}
		string[] array = s.Split(',');
		if (7 <= array.Length && array.Length <= 8 && (!recordCurrentLevel || array.Length != 8 || string.Equals(array[7], Tools.loadedLevelName)))
		{
			float[] array2 = new float[7];
			for (int i = 0; i < 7; i++)
			{
				array2[i] = 0f;
				float.TryParse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture, out array2[i]);
			}
			base.transform.position = new Vector3(array2[0], array2[1], array2[2]);
			base.transform.rotation = new Quaternion(array2[3], array2[4], array2[5], array2[6]);
		}
	}

	public static string SanitizeLevelName(string levelName)
	{
		return DialogueLua.StringToTableIndex(levelName).Replace(".", "_");
	}
}
