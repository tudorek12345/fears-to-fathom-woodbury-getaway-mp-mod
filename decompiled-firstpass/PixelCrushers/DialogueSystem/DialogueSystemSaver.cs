using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class DialogueSystemSaver : Saver
{
	[Serializable]
	public class RawData
	{
		public byte[] bytes;
	}

	[Tooltip("If data was restored immediately after loading a scene, don't apply it again after save system waits specified number of frames for other scripts to initialize.")]
	public bool skipApplyDataAfterFramesIfApplyImmediate = true;

	[Tooltip("Save using raw data dump. If your database is extremely large, this method is faster but generates larger saved game data. If you use this option, use BinaryDataSerializer instead of JsonDataSerializer or data will be ridiculously large.")]
	public bool saveRawData;

	private bool m_appliedImmediate;

	public override void Reset()
	{
		base.Reset();
		saveAcrossSceneChanges = true;
	}

	public override void Start()
	{
		base.Start();
		SaveSystem.loadStarted += OnLoadGameStarted;
	}

	public override void OnDestroy()
	{
		SaveSystem.loadStarted -= OnLoadGameStarted;
		base.OnDestroy();
	}

	private void OnLoadGameStarted()
	{
		DialogueManager.StopAllConversations();
	}

	public override string RecordData()
	{
		if (saveRawData)
		{
			return SaveSystem.Serialize(new RawData
			{
				bytes = PersistentDataManager.GetRawData()
			});
		}
		return PersistentDataManager.GetSaveData();
	}

	public override void ApplyDataImmediate()
	{
		string data = SaveSystem.currentSavedGameData.GetData(key);
		if (string.IsNullOrEmpty(data))
		{
			return;
		}
		if (saveRawData)
		{
			RawData rawData = SaveSystem.Deserialize<RawData>(data);
			if (rawData != null && rawData.bytes != null)
			{
				PersistentDataManager.ApplyRawData(rawData.bytes);
			}
		}
		else
		{
			PersistentDataManager.ApplyLuaInternal(data);
		}
		m_appliedImmediate = true;
	}

	public override void ApplyData(string data)
	{
		if (m_appliedImmediate)
		{
			m_appliedImmediate = false;
			if (skipApplyDataAfterFramesIfApplyImmediate)
			{
				PersistentDataManager.Apply();
				return;
			}
		}
		if (saveRawData)
		{
			RawData rawData = SaveSystem.Deserialize<RawData>(data);
			if (rawData != null && rawData.bytes != null)
			{
				PersistentDataManager.ApplyRawData(rawData.bytes);
			}
		}
		else
		{
			PersistentDataManager.ApplySaveData(data);
		}
	}

	public override void OnBeforeSceneChange()
	{
		PersistentDataManager.LevelWillBeUnloaded();
	}

	public override void OnRestartGame()
	{
		DialogueManager.StopAllConversations();
		DialogueManager.ResetDatabase();
		DialogueManager.SendUpdateTracker();
	}
}
