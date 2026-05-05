using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class DiskSavedGameDataStorer : SavedGameDataStorer
{
	public enum BasePath
	{
		PersistentDataPath,
		DataPath,
		Custom
	}

	protected class SavedGameInfo
	{
		public string sceneName;

		public SavedGameInfo(string sceneName)
		{
			this.sceneName = sceneName;
		}
	}

	[Tooltip("Persistent Data Path: Usual location where Unity stores data to be kept between runs.\nData Path: Game data folder on target device.\nCustom: Set below.")]
	public BasePath storeSaveFilesIn;

	public string customPath;

	[Tooltip("Encrypt saved game files.")]
	public bool encrypt = true;

	[Tooltip("If encrypting, use this password.")]
	public string encryptionPassword = "My Password";

	[Tooltip("Log debug info.")]
	[SerializeField]
	private bool m_debug;

	protected List<SavedGameInfo> m_savedGameInfo;

	protected List<SavedGameInfo> savedGameInfo
	{
		get
		{
			if (m_savedGameInfo == null)
			{
				LoadSavedGameInfoFromFile();
			}
			return m_savedGameInfo;
		}
	}

	public bool debug
	{
		get
		{
			if (m_debug)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
		set
		{
			m_debug = value;
		}
	}

	public virtual void Start()
	{
		LoadSavedGameInfoFromFile();
	}

	protected virtual string GetBasePath()
	{
		return storeSaveFilesIn switch
		{
			BasePath.DataPath => Application.dataPath, 
			BasePath.Custom => customPath, 
			_ => Application.persistentDataPath, 
		};
	}

	public virtual string GetSaveGameFilename(int slotNumber)
	{
		return GetBasePath() + "/save_" + slotNumber + ".dat";
	}

	public virtual string GetSavedGameInfoFilename()
	{
		return GetBasePath() + "/saveinfo.dat";
	}

	public virtual void LoadSavedGameInfoFromFile()
	{
		m_savedGameInfo = new List<SavedGameInfo>();
		string savedGameInfoFilename = GetSavedGameInfoFilename();
		if (!VerifySavedGameInfoFile(savedGameInfoFilename))
		{
			return;
		}
		if (debug)
		{
			Debug.Log("Save System: DiskSavedGameDataStorer loading " + savedGameInfoFilename);
		}
		try
		{
			using StreamReader streamReader = new StreamReader(savedGameInfoFilename);
			int num = 0;
			while (!streamReader.EndOfStream && num < 999)
			{
				string sceneName = streamReader.ReadLine().Replace("<cr>", "\n");
				m_savedGameInfo.Add(new SavedGameInfo(sceneName));
				num++;
			}
		}
		catch (Exception)
		{
			Debug.Log("Save System: DiskSavedGameDataStorer - Error reading file: " + savedGameInfoFilename);
		}
	}

	protected virtual bool VerifySavedGameInfoFile(string saveInfoFilename)
	{
		if (string.IsNullOrEmpty(saveInfoFilename) || !File.Exists(saveInfoFilename))
		{
			if (!Directory.Exists(Path.GetDirectoryName(saveInfoFilename)))
			{
				return false;
			}
			int num = 0;
			for (int i = 0; i <= 100; i++)
			{
				if (File.Exists(GetSaveGameFilename(i)))
				{
					num = i;
				}
			}
			savedGameInfo.Clear();
			for (int j = 0; j <= num; j++)
			{
				savedGameInfo.Add(new SavedGameInfo(string.Empty));
			}
			WriteSavedGameInfoToDisk();
		}
		return true;
	}

	public virtual void UpdateSavedGameInfoToFile(int slotNumber, SavedGameData savedGameData)
	{
		for (int i = savedGameInfo.Count; i <= slotNumber; i++)
		{
			savedGameInfo.Add(new SavedGameInfo(string.Empty));
		}
		if (0 <= slotNumber && slotNumber < savedGameInfo.Count)
		{
			savedGameInfo[slotNumber].sceneName = ((savedGameData != null) ? savedGameData.sceneName : string.Empty);
		}
		WriteSavedGameInfoToDisk();
	}

	protected virtual void WriteSavedGameInfoToDisk()
	{
		string savedGameInfoFilename = GetSavedGameInfoFilename();
		if (debug)
		{
			Debug.Log("Save System: DiskSavedGameDataStorer updating " + savedGameInfoFilename);
		}
		try
		{
			using StreamWriter streamWriter = new StreamWriter(savedGameInfoFilename);
			for (int i = 0; i < savedGameInfo.Count; i++)
			{
				streamWriter.WriteLine(savedGameInfo[i].sceneName.Replace("\n", "<cr>"));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Save System: DiskSavedGameDataStorer - Can't create file: " + savedGameInfoFilename);
			throw ex;
		}
	}

	public override bool HasDataInSlot(int slotNumber)
	{
		if (0 <= slotNumber && slotNumber < savedGameInfo.Count && !string.IsNullOrEmpty(savedGameInfo[slotNumber].sceneName))
		{
			return File.Exists(GetSaveGameFilename(slotNumber));
		}
		return false;
	}

	public override void StoreSavedGameData(int slotNumber, SavedGameData savedGameData)
	{
		string text = SaveSystem.Serialize(savedGameData);
		if (debug)
		{
			Debug.Log("Save System: DiskSavedGameDataStorer - Saving " + GetSaveGameFilename(slotNumber) + ": " + text);
		}
		WriteStringToFile(GetSaveGameFilename(slotNumber), encrypt ? EncryptionUtility.Encrypt(text, encryptionPassword) : text);
		UpdateSavedGameInfoToFile(slotNumber, savedGameData);
	}

	public override SavedGameData RetrieveSavedGameData(int slotNumber)
	{
		string text = ReadStringFromFile(GetSaveGameFilename(slotNumber));
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (encrypt)
		{
			text = (EncryptionUtility.TryDecrypt(text, encryptionPassword, out var plainText) ? plainText : string.Empty);
		}
		if (debug)
		{
			Debug.Log("Save System: DiskSavedGameDataStorer - Loading " + GetSaveGameFilename(slotNumber) + ": " + text);
		}
		return SaveSystem.Deserialize<SavedGameData>(text);
	}

	public override void DeleteSavedGameData(int slotNumber)
	{
		try
		{
			string saveGameFilename = GetSaveGameFilename(slotNumber);
			if (File.Exists(saveGameFilename))
			{
				File.Delete(saveGameFilename);
			}
		}
		catch (Exception)
		{
		}
		UpdateSavedGameInfoToFile(slotNumber, null);
	}

	public static void WriteStringToFile(string filename, string data)
	{
		try
		{
			string text = filename + ".tmp";
			using (StreamWriter streamWriter = new StreamWriter(text))
			{
				streamWriter.WriteLine(data);
			}
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			File.Move(text, filename);
		}
		catch (Exception ex)
		{
			Debug.LogError("Save System: Can't create saved game file: " + filename);
			throw ex;
		}
	}

	public static string ReadStringFromFile(string filename)
	{
		if (!File.Exists(filename))
		{
			return string.Empty;
		}
		try
		{
			using StreamReader streamReader = new StreamReader(filename);
			return streamReader.ReadToEnd();
		}
		catch (Exception)
		{
			Debug.Log("Save System: Error reading file: " + filename);
			return string.Empty;
		}
	}
}
