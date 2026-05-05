using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PersistentDestructible : MonoBehaviour
{
	public enum RecordOn
	{
		Destroy,
		Disable
	}

	[Tooltip("Record destroyed on Destroy or Disable.")]
	public RecordOn recordOn;

	[Tooltip("Unique Dialogue System variable (Boolean) to record whether the GameObject has been destroyed/disabled.")]
	public string variableName = string.Empty;

	[Tooltip("Spawn an instance of this when destroyed.")]
	public GameObject spawnWhenDestroyed;

	protected bool listenForOnDestroy;

	protected string ActualVariableName
	{
		get
		{
			if (!string.IsNullOrEmpty(variableName))
			{
				return variableName;
			}
			return DialogueActor.GetPersistentDataName(base.transform);
		}
	}

	protected virtual void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
		listenForOnDestroy = true;
	}

	public void OnApplyPersistentData()
	{
		if (DialogueLua.GetVariable(ActualVariableName).asBool)
		{
			base.gameObject.BroadcastMessage("OnLevelWillBeUnloaded", SendMessageOptions.DontRequireReceiver);
			switch (recordOn)
			{
			case RecordOn.Destroy:
				Object.Destroy(base.gameObject);
				break;
			case RecordOn.Disable:
				base.gameObject.SetActive(value: false);
				break;
			}
			SpawnCorpse();
		}
	}

	public void OnLevelWillBeUnloaded()
	{
		listenForOnDestroy = false;
	}

	public void OnApplicationQuit()
	{
		listenForOnDestroy = false;
	}

	public void OnDestroy()
	{
		if (listenForOnDestroy && recordOn == RecordOn.Destroy)
		{
			MarkDestroyed();
		}
	}

	private void MarkDestroyed()
	{
		if (Application.isPlaying && !(DialogueManager.instance == null) && DialogueManager.databaseManager != null && !(DialogueManager.masterDatabase == null))
		{
			DialogueLua.SetVariable(ActualVariableName, true);
			SpawnCorpse();
		}
	}

	public void OnDisable()
	{
		if (listenForOnDestroy && recordOn == RecordOn.Disable)
		{
			MarkDestroyed();
			PersistentDataManager.UnregisterPersistentData(base.gameObject);
		}
	}

	private void SpawnCorpse()
	{
		if (!(spawnWhenDestroyed == null))
		{
			Object.Instantiate(spawnWhenDestroyed, base.transform.position, base.transform.rotation);
		}
	}
}
