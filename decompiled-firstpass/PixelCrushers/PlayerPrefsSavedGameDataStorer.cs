using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class PlayerPrefsSavedGameDataStorer : SavedGameDataStorer
{
	[Tooltip("Save games under this PlayerPrefs key")]
	[SerializeField]
	private string m_playerPrefsKeyBase = "Save";

	[Tooltip("Encrypt saved game data.")]
	public bool encrypt;

	[Tooltip("If encrypting, use this password.")]
	public string encryptionPassword = "My Password";

	[Tooltip("Log debug info.")]
	[SerializeField]
	private bool m_debug;

	public string playerPrefsKeyBase
	{
		get
		{
			return m_playerPrefsKeyBase;
		}
		set
		{
			m_playerPrefsKeyBase = value;
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
	}

	public string GetPlayerPrefsKey(int slotNumber)
	{
		return m_playerPrefsKeyBase + slotNumber;
	}

	public override bool HasDataInSlot(int slotNumber)
	{
		return PlayerPrefs.HasKey(GetPlayerPrefsKey(slotNumber));
	}

	public override void StoreSavedGameData(int slotNumber, SavedGameData savedGameData)
	{
		string text = SaveSystem.Serialize(savedGameData);
		if (debug)
		{
			Debug.Log("Save System: Storing in PlayerPrefs key " + GetPlayerPrefsKey(slotNumber) + ": " + text);
		}
		PlayerPrefs.SetString(GetPlayerPrefsKey(slotNumber), encrypt ? EncryptionUtility.Encrypt(text, encryptionPassword) : text);
		PlayerPrefs.Save();
	}

	public override SavedGameData RetrieveSavedGameData(int slotNumber)
	{
		if (debug && HasDataInSlot(slotNumber))
		{
			Debug.Log("Save System: Retrieved from PlayerPrefs key " + GetPlayerPrefsKey(slotNumber) + ": " + PlayerPrefs.GetString(GetPlayerPrefsKey(slotNumber)));
		}
		string text = PlayerPrefs.GetString(GetPlayerPrefsKey(slotNumber));
		if (encrypt)
		{
			text = (EncryptionUtility.TryDecrypt(text, encryptionPassword, out var plainText) ? plainText : string.Empty);
		}
		if (!HasDataInSlot(slotNumber))
		{
			return new SavedGameData();
		}
		return SaveSystem.Deserialize<SavedGameData>(text);
	}

	public override void DeleteSavedGameData(int slotNumber)
	{
		PlayerPrefs.DeleteKey(GetPlayerPrefsKey(slotNumber));
		PlayerPrefs.Save();
	}
}
