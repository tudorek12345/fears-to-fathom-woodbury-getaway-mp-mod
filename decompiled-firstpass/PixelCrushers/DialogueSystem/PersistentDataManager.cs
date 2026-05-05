using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Language.Lua;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class PersistentDataManager
{
	public enum RecordPersistentDataOn
	{
		AllGameObjects,
		OnlyRegisteredGameObjects,
		NoGameObjects
	}

	public class AsyncSaveOperation
	{
		public bool isDone;

		public string content = string.Empty;
	}

	public class AsyncRawDataOperation
	{
		public bool isDone;

		public byte[] content;
	}

	public static bool includeActorData = true;

	public static bool includeAllItemData = false;

	public static bool includeLocationData = false;

	public static bool includeAllConversationFields = false;

	public static bool includeSimStatus = false;

	public static string saveConversationSimStatusWithField = string.Empty;

	public static string saveDialogueEntrySimStatusWithField = string.Empty;

	public static bool includeRelationshipAndStatusData = true;

	public static bool initializeNewVariables = true;

	public static bool initializeNewSimStatus = true;

	public static GetCustomSaveDataDelegate GetCustomSaveData = null;

	public static RecordPersistentDataOn recordPersistentDataOn = RecordPersistentDataOn.AllGameObjects;

	private static HashSet<GameObject> listeners = new HashSet<GameObject>();

	private static bool useConversationID = true;

	private static bool useEntryID = true;

	private static Dictionary<int, string> s_dialogueEntrySimStatusFieldLookupTable = new Dictionary<int, string>();

	public static int asyncGameObjectBatchSize = 1000;

	public static int asyncDialogueEntryBatchSize = 100;

	public static void RegisterPersistentData(GameObject go)
	{
		if (!(go == null) && Application.isPlaying)
		{
			listeners.Add(go);
		}
	}

	public static void UnregisterPersistentData(GameObject go)
	{
		if (Application.isPlaying)
		{
			listeners.Remove(go);
		}
	}

	public static void Reset(DatabaseResetOptions databaseResetOptions)
	{
		DialogueManager.ResetDatabase(databaseResetOptions);
	}

	public static void Reset()
	{
		Reset(DatabaseResetOptions.KeepAllLoaded);
	}

	public static void Record()
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Recording persistent data to Lua environment.", new object[1] { "Dialogue System" }));
		}
		SendPersistentDataMessage("OnRecordPersistentData");
	}

	public static void Apply()
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Applying persistent data from Lua environment.", new object[1] { "Dialogue System" }));
		}
		SendPersistentDataMessage("OnApplyPersistentData");
		DialogueManager.SendUpdateTracker();
	}

	private static void SendPersistentDataMessage(string message)
	{
		switch (recordPersistentDataOn)
		{
		case RecordPersistentDataOn.AllGameObjects:
			Tools.SendMessageToEveryone(message);
			break;
		case RecordPersistentDataOn.OnlyRegisteredGameObjects:
		{
			List<GameObject> list = new List<GameObject>(listeners);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				GameObject gameObject = list[num];
				if (gameObject != null)
				{
					gameObject.SendMessage(message, SendMessageOptions.DontRequireReceiver);
				}
			}
			break;
		}
		}
	}

	public static void LevelWillBeUnloaded()
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Broadcasting that level will be unloaded.", new object[1] { "Dialogue System" }));
		}
		SendPersistentDataMessage("OnLevelWillBeUnloaded");
	}

	public static void ApplySaveData(string saveData, DatabaseResetOptions databaseResetOptions = DatabaseResetOptions.KeepAllLoaded)
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Resetting Lua environment.", new object[1] { "Dialogue System" }));
		}
		DialogueManager.ResetDatabase(databaseResetOptions);
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Updating Lua environment with saved data.", new object[1] { "Dialogue System" }));
		}
		ApplyLuaInternal(saveData);
		Apply();
	}

	public static void ApplyLuaInternal(string saveData, bool allowExceptions = false)
	{
		if (!string.IsNullOrEmpty(saveData))
		{
			EnsureConversationTablesExistForAllSimX(saveData);
			EnsureQuestsExist(saveData);
			Lua.Run(saveData, DialogueDebug.LogInfo);
			ExpandCompressedSimStatusData();
			RefreshRelationshipAndStatusTablesFromLua();
			if (initializeNewVariables)
			{
				InitializeNewVariablesFromDatabase();
				InitializeNewActorFieldsFromDatabase();
				InitializeNewQuestEntriesFromDatabase();
				InitializeNewSimStatusFromDatabase();
			}
		}
	}

	private static void EnsureConversationTablesExistForAllSimX(string saveData)
	{
		if (!includeSimStatus || !DialogueManager.Instance.includeSimStatus || !(Lua.Environment.GetValue("Conversation") is LuaTable luaTable))
		{
			return;
		}
		int length = "Conversation[".Length;
		foreach (Match item in Regex.Matches(saveData, "Conversation\\[\\d+\\]"))
		{
			LuaNumber key = new LuaNumber(SafeConvert.ToInt(item.Value.Substring(length, item.Value.Length - (length + 1))));
			if (!luaTable.ContainsKey(key))
			{
				luaTable.SetKeyValue(key, new LuaTable());
			}
		}
	}

	private static void EnsureQuestsExist(string saveData)
	{
		if (includeAllItemData || DialogueManager.Instance.persistentDataSettings.includeAllItemData || !(Lua.Environment.GetValue("Item") is LuaTable luaTable))
		{
			return;
		}
		int length = "Item[".Length;
		int length2 = "].State".Length;
		foreach (Match item in Regex.Matches(saveData, "Item\\[[^\\]]+\\].State"))
		{
			string text = item.Value.Substring(length + 1, item.Value.Length - (length + length2 + 2));
			if (luaTable.GetKey(text) == LuaNil.Nil)
			{
				LuaString key = new LuaString(text);
				LuaTable luaTable2 = new LuaTable();
				luaTable2.RawSetValue("Name", new LuaString(text));
				luaTable2.RawSetValue("State", new LuaString("unassigned"));
				luaTable.SetKeyValue(key, luaTable2);
			}
		}
	}

	public static string GetSaveData()
	{
		Record();
		StringBuilder stringBuilder = new StringBuilder();
		AppendDialogueSystemData(stringBuilder);
		string text = stringBuilder.ToString();
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Saved data: {1}", new object[2] { "Dialogue System", text }));
		}
		return text;
	}

	public static void AppendDialogueSystemData(StringBuilder sb)
	{
		if (sb != null)
		{
			AppendVariableData(sb);
			AppendItemData(sb);
			AppendLocationData(sb);
			if (includeActorData)
			{
				AppendActorData(sb);
			}
			AppendConversationData(sb);
			if (includeRelationshipAndStatusData)
			{
				AppendRelationshipAndStatusTables(sb);
			}
			if (GetCustomSaveData != null)
			{
				sb.Append(GetCustomSaveData());
			}
		}
	}

	public static void AppendVariableData(StringBuilder sb)
	{
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Variable").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Variable[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			sb.Append("Variable={");
			bool flag = true;
			foreach (string key in asTable.Keys)
			{
				if (!string.IsNullOrEmpty(key))
				{
					if (!flag)
					{
						sb.Append(", ");
					}
					flag = false;
					object o = asTable[key.ToString()];
					sb.AppendFormat("{0}={1}", new object[2]
					{
						GetFieldKeyString(key),
						GetFieldValueString(o)
					});
				}
			}
			sb.Append("}; ");
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get variable data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void AppendItemData(StringBuilder sb)
	{
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Item").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Item[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			HashSet<string> hashSet = new HashSet<string>();
			if (!includeAllItemData)
			{
				DialogueDatabase masterDatabase = DialogueManager.masterDatabase;
				for (int i = 0; i < masterDatabase.items.Count; i++)
				{
					hashSet.Add(DialogueLua.StringToTableIndex(masterDatabase.items[i].Name));
				}
			}
			foreach (string key in asTable.Keys)
			{
				LuaTableWrapper luaTableWrapper = asTable[key.ToString()] as LuaTableWrapper;
				bool flag = !includeAllItemData && hashSet.Contains(key);
				if (luaTableWrapper == null)
				{
					continue;
				}
				if (flag)
				{
					foreach (string key2 in luaTableWrapper.Keys)
					{
						if (!string.IsNullOrEmpty(key2))
						{
							string text = key2.ToString();
							if (text.EndsWith("State"))
							{
								sb.AppendFormat("Item[\"{0}\"].{1}=\"{2}\"; ", new object[3]
								{
									DialogueLua.StringToTableIndex(key),
									text,
									luaTableWrapper[text]
								});
							}
							else if (string.Equals(text, "Track") || string.Equals(text, "Viewed"))
							{
								sb.AppendFormat("Item[\"{0}\"].{1}={2}; ", new object[3]
								{
									DialogueLua.StringToTableIndex(key),
									text,
									luaTableWrapper[text].ToString().ToLower()
								});
							}
						}
					}
				}
				else
				{
					sb.AppendFormat("Item[\"{0}\"]=", new object[1] { DialogueLua.StringToTableIndex(key) });
					AppendFields(sb, luaTableWrapper);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get item data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	private static void AppendFields(StringBuilder sb, LuaTableWrapper fields)
	{
		sb.Append("{");
		try
		{
			if (fields == null)
			{
				return;
			}
			foreach (string key in fields.Keys)
			{
				if (!string.IsNullOrEmpty(key))
				{
					string text = GetFieldValueString(fields[key]);
					if (string.Equals(key, "Pictures"))
					{
						text = text.Replace("\\", "/");
					}
					sb.AppendFormat("{0}={1}, ", new object[2]
					{
						GetFieldKeyString(key),
						text
					});
				}
			}
		}
		finally
		{
			sb.Append("}; ");
		}
	}

	private static string GetFieldKeyString(string key)
	{
		key = DialogueLua.StringToTableIndex(key);
		if (!IsValidVarName(key))
		{
			return "[\"" + key + "\"]";
		}
		return key;
	}

	private static bool IsValidVarName(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		char c = key[0];
		if (c != '_' && ('a' > c || c > 'z') && ('A' > c || c > 'Z'))
		{
			return false;
		}
		for (int i = 1; i < key.Length; i++)
		{
			char c2 = key[i];
			if (c2 != '_' && ('a' > c2 || c2 > 'z') && ('A' > c2 || c2 > 'Z') && ('0' > c2 || c2 > '9'))
			{
				return false;
			}
		}
		return true;
	}

	private static string GetFieldValueString(object o)
	{
		if (o == null)
		{
			return "nil";
		}
		Type type = o.GetType();
		if (type == typeof(string))
		{
			return string.Format("\"{0}\"", new object[1] { DialogueLua.DoubleQuotesToSingle(o.ToString().Replace("\n", "\\n").Replace("\\ ", "/ ")) });
		}
		if (type == typeof(bool))
		{
			return o.ToString().ToLower();
		}
		if (type == typeof(float) || type == typeof(double))
		{
			return ((float)o).ToString(CultureInfo.InvariantCulture);
		}
		if (type == typeof(LuaTableWrapper))
		{
			StringBuilder stringBuilder = new StringBuilder();
			AppendFields(stringBuilder, (LuaTableWrapper)o);
			return "{" + stringBuilder.ToString() + "}";
		}
		return o.ToString();
	}

	public static void AppendLocationData(StringBuilder sb)
	{
		if (!includeLocationData)
		{
			return;
		}
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Location").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Location[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			sb.Append("Location={");
			bool flag = true;
			foreach (string key in asTable.Keys)
			{
				if (!string.IsNullOrEmpty(key))
				{
					LuaTableWrapper fields = asTable[key] as LuaTableWrapper;
					if (!flag)
					{
						sb.Append(", ");
					}
					flag = false;
					sb.Append(GetFieldKeyString(key));
					sb.Append("={");
					try
					{
						AppendAssetFieldData(sb, fields);
					}
					finally
					{
						sb.Append("}");
					}
				}
			}
			sb.Append("}; ");
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get location data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void AppendActorData(StringBuilder sb)
	{
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Actor").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Actor[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			sb.Append("Actor={");
			bool flag = true;
			foreach (string key in asTable.Keys)
			{
				if (!string.IsNullOrEmpty(key))
				{
					LuaTableWrapper fields = asTable[key] as LuaTableWrapper;
					if (!flag)
					{
						sb.Append(", ");
					}
					flag = false;
					sb.Append(GetFieldKeyString(key));
					sb.Append("={");
					try
					{
						AppendAssetFieldData(sb, fields);
					}
					finally
					{
						sb.Append("}");
					}
				}
			}
			sb.Append("}; ");
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get actor data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	private static void AppendAssetFieldData(StringBuilder sb, LuaTableWrapper fields)
	{
		if (fields == null)
		{
			return;
		}
		bool flag = true;
		foreach (string key in fields.Keys)
		{
			if (!string.IsNullOrEmpty(key))
			{
				if (!flag)
				{
					sb.Append(", ");
				}
				flag = false;
				object o = fields[key];
				sb.AppendFormat("{0}={1}", new object[2]
				{
					GetFieldKeyString(key),
					GetFieldValueString(o)
				});
			}
		}
	}

	public static void AppendRelationshipAndStatusTables(StringBuilder sb)
	{
		try
		{
			sb.Append(DialogueLua.GetStatusTableAsLua());
			sb.Append(DialogueLua.GetRelationshipTableAsLua());
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get relationship and status data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void RefreshRelationshipAndStatusTablesFromLua()
	{
		DialogueLua.RefreshStatusTableFromLua();
		DialogueLua.RefreshRelationshipTableFromLua();
	}

	public static void AppendConversationData(StringBuilder sb)
	{
		if (includeAllConversationFields || DialogueManager.Instance.persistentDataSettings.includeAllConversationFields)
		{
			AppendAllConversationFields(sb);
		}
		if (includeSimStatus && DialogueManager.Instance.includeSimStatus)
		{
			AppendSimStatus(sb);
		}
	}

	private static void AppendAllConversationFields(StringBuilder sb)
	{
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Conversation").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Conversation[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			foreach (string key in asTable.Keys)
			{
				LuaTableWrapper asTable2 = Lua.Run("return Conversation[" + key + "]").AsTable;
				if (asTable2 == null)
				{
					continue;
				}
				sb.Append("Conversation[" + key + "]={");
				try
				{
					bool flag = true;
					foreach (string key2 in asTable2.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && !string.Equals(key2, "Dialog"))
						{
							if (!flag)
							{
								sb.Append(", ");
							}
							flag = false;
							object o = asTable2[key2.ToString()];
							sb.AppendFormat("{0}={1}", new object[2]
							{
								GetFieldKeyString(key2),
								GetFieldValueString(o)
							});
						}
					}
				}
				finally
				{
					sb.Append("}; ");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get conversation data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	private static void AppendSimStatus(StringBuilder sb)
	{
		try
		{
			useConversationID = string.IsNullOrEmpty(saveConversationSimStatusWithField);
			useEntryID = string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField);
			if (!(Lua.Environment.GetValue("Conversation") is LuaTable luaTable))
			{
				return;
			}
			for (int i = 0; i < luaTable.List.Count; i++)
			{
				int conversationID = i + 1;
				LuaTable fieldTable = luaTable.List[i] as LuaTable;
				AppendSimStatusForConversation(sb, luaTable, conversationID, fieldTable);
			}
			foreach (KeyValuePair<LuaValue, LuaValue> item in luaTable.Dict)
			{
				if (item.Key != null && item.Value != null && item.Value is LuaTable)
				{
					int conversationID2 = Tools.StringToInt(item.Key.ToString());
					LuaTable fieldTable2 = item.Value as LuaTable;
					AppendSimStatusForConversation(sb, luaTable, conversationID2, fieldTable2);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: GetSaveData() failed to get conversation data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	private static int AppendSimStatusForConversation(StringBuilder sb, LuaTable conversationTable, int conversationID, LuaTable fieldTable)
	{
		if (sb == null || conversationTable == null || fieldTable == null)
		{
			return 0;
		}
		if (!(fieldTable.GetValue("Dialog") is LuaTable luaTable))
		{
			return 0;
		}
		Conversation conversation = DialogueManager.MasterDatabase.GetConversation(conversationID);
		if (conversation == null)
		{
			return 0;
		}
		if (useConversationID)
		{
			sb.AppendFormat("Conversation[{0}].SimX=\"", conversationID);
		}
		else
		{
			sb.AppendFormat("Variable[\"Conversation_SimX_{0}\"]=\"", DialogueLua.StringToTableIndex(conversation.LookupValue(saveConversationSimStatusWithField)));
		}
		bool flag = true;
		for (int i = 0; i < luaTable.List.Count; i++)
		{
			int dialogueEntryID = i + 1;
			string text = dialogueEntryID.ToString();
			LuaTable obj = luaTable.List[i] as LuaTable;
			if (!flag)
			{
				sb.Append(";");
			}
			flag = false;
			if (useEntryID)
			{
				sb.Append(text);
			}
			else
			{
				DialogueEntry dialogueEntry = conversation.GetDialogueEntry(dialogueEntryID);
				string value = ((dialogueEntry != null) ? Field.LookupValue(dialogueEntry.fields, saveDialogueEntrySimStatusWithField) : text);
				sb.Append(value);
			}
			sb.Append(";");
			string simStatus = obj.GetValue("SimStatus").ToString();
			sb.Append(SimStatusToChar(simStatus));
		}
		if (!useEntryID)
		{
			s_dialogueEntrySimStatusFieldLookupTable.Clear();
			for (int j = 0; j < conversation.dialogueEntries.Count; j++)
			{
				DialogueEntry dialogueEntry2 = conversation.dialogueEntries[j];
				Field field = Field.Lookup(dialogueEntry2.fields, saveDialogueEntrySimStatusWithField);
				string value2 = ((field != null) ? field.value : dialogueEntry2.id.ToString());
				s_dialogueEntrySimStatusFieldLookupTable.Add(dialogueEntry2.id, value2);
			}
		}
		foreach (KeyValuePair<LuaValue, LuaValue> keyValuePair in luaTable.KeyValuePairs)
		{
			string text2 = keyValuePair.Key.ToString();
			LuaTable obj2 = keyValuePair.Value as LuaTable;
			if (!flag)
			{
				sb.Append(";");
			}
			flag = false;
			if (useEntryID)
			{
				sb.Append(text2);
			}
			else
			{
				int key = Tools.StringToInt(text2);
				sb.Append(s_dialogueEntrySimStatusFieldLookupTable[key]);
			}
			sb.Append(";");
			string simStatus2 = obj2.GetValue("SimStatus").ToString();
			sb.Append(SimStatusToChar(simStatus2));
		}
		sb.Append("\"; ");
		s_dialogueEntrySimStatusFieldLookupTable.Clear();
		return conversation.dialogueEntries.Count;
	}

	private static void ExpandCompressedSimStatusData()
	{
		if (!includeSimStatus || !DialogueManager.Instance.includeSimStatus)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<Conversation> conversations = DialogueManager.MasterDatabase.conversations;
		for (int i = 0; i < conversations.Count; i++)
		{
			hashSet.Add(conversations[i].id);
		}
		Dictionary<int, DialogueEntry> dialogueEntryCache = new Dictionary<int, DialogueEntry>();
		LuaString luaStringSimX = new LuaString("SimX");
		useConversationID = string.IsNullOrEmpty(saveConversationSimStatusWithField);
		useEntryID = string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField);
		if (!(Lua.Environment.GetValue("Conversation") is LuaTable luaTable))
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder(16384, int.MaxValue);
		for (int j = 0; j < luaTable.List.Count; j++)
		{
			int num = j + 1;
			LuaTable fieldTable = luaTable.List[j] as LuaTable;
			if (ExpandSimStatusForConversation(stringBuilder, num, num.ToString(), fieldTable, luaStringSimX, dialogueEntryCache))
			{
				hashSet.Remove(num);
			}
		}
		foreach (KeyValuePair<LuaValue, LuaValue> item in luaTable.Dict)
		{
			if (item.Key != null && item.Value != null && item.Value is LuaTable)
			{
				string text = item.Key.ToString();
				int num2 = Tools.StringToInt(text);
				LuaTable fieldTable2 = item.Value as LuaTable;
				if (ExpandSimStatusForConversation(stringBuilder, num2, text, fieldTable2, luaStringSimX, dialogueEntryCache))
				{
					hashSet.Remove(num2);
				}
			}
		}
		Lua.Run(stringBuilder.ToString());
		if (hashSet.Count <= 0)
		{
			return;
		}
		HashSet<int>.Enumerator enumerator2 = hashSet.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			int current2 = enumerator2.Current;
			Conversation conversation = DialogueManager.MasterDatabase.GetConversation(current2);
			if (conversation != null)
			{
				DialogueLua.AddToConversationTable(luaTable, conversation, addRaw: true);
			}
		}
	}

	private static bool ExpandSimStatusForConversation(StringBuilder sb, int conversationID, string conversationIDString, LuaTable fieldTable, LuaString luaStringSimX, Dictionary<int, DialogueEntry> dialogueEntryCache)
	{
		LuaTable luaTable = fieldTable.GetValue("Dialog") as LuaTable;
		if (luaTable == null)
		{
			luaTable = new LuaTable();
			fieldTable.AddRaw("Dialog", luaTable);
		}
		luaTable.List.Clear();
		luaTable.Dict.Clear();
		Conversation conversation = DialogueManager.MasterDatabase.GetConversation(conversationID);
		if (conversation == null)
		{
			return false;
		}
		string text;
		if (useConversationID)
		{
			LuaValue value = fieldTable.GetValue(luaStringSimX);
			if (value == null)
			{
				return false;
			}
			text = value.ToString();
			sb.AppendFormat("Conversation[{0}].SimX=nil;", conversationIDString);
		}
		else
		{
			string text2 = DialogueLua.StringToTableIndex(conversation.LookupValue(saveConversationSimStatusWithField));
			if (string.IsNullOrEmpty(text2))
			{
				text2 = conversation.id.ToString();
			}
			text = Lua.Run("return Variable[\"Conversation_SimX_" + text2 + "\"]").AsString;
			sb.Append("Variable[\"Conversation_SimX_" + text2 + "\"]=nil;");
		}
		if (string.IsNullOrEmpty(text) || string.Equals(text, "nil"))
		{
			return false;
		}
		string[] array = text.Split(';');
		int num = array.Length / 2;
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			DialogueEntry dialogueEntry = conversation.dialogueEntries[i];
			dialogueEntryCache[dialogueEntry.id] = dialogueEntry;
		}
		Dictionary<string, int> dictionary = null;
		if (!useEntryID)
		{
			dictionary = new Dictionary<string, int>();
			for (int j = 0; j < conversation.dialogueEntries.Count; j++)
			{
				DialogueEntry dialogueEntry = conversation.dialogueEntries[j];
				Field field = ((dialogueEntry != null) ? Field.Lookup(dialogueEntry.fields, saveDialogueEntrySimStatusWithField) : null);
				dictionary[(field != null) ? field.value : dialogueEntry.id.ToString()] = dialogueEntry.id;
			}
		}
		for (int k = 0; k < num; k++)
		{
			string text3 = array[2 * k];
			string text4 = CharToSimStatus(array[2 * k + 1][0]);
			LuaTable luaTable2 = new LuaTable();
			luaTable2.AddRaw("SimStatus", new LuaString(text4));
			if (useEntryID || dictionary.ContainsKey(text3))
			{
				int key = (useEntryID ? Tools.StringToInt(text3) : dictionary[text3]);
				dialogueEntryCache[key] = null;
				if (useEntryID)
				{
					luaTable.AddRaw(key, luaTable2);
				}
				else if (dictionary.ContainsKey(text3))
				{
					luaTable.AddRaw(dictionary[text3], luaTable2);
				}
			}
		}
		for (int l = 0; l < conversation.dialogueEntries.Count; l++)
		{
			DialogueEntry dialogueEntry = conversation.dialogueEntries[l];
			if (dialogueEntryCache[dialogueEntry.id] != null)
			{
				LuaTable luaTable3 = new LuaTable();
				luaTable3.AddRaw("SimStatus", new LuaString("Untouched"));
				luaTable.AddRaw(dialogueEntry.id, luaTable3);
			}
		}
		return true;
	}

	private static char SimStatusToChar(string simStatus)
	{
		return simStatus switch
		{
			"Untouched" => 'u', 
			"WasDisplayed" => 'd', 
			"WasOffered" => 'o', 
			_ => 'X', 
		};
	}

	private static string CharToSimStatus(char c)
	{
		return c switch
		{
			'u' => "Untouched", 
			'd' => "WasDisplayed", 
			'o' => "WasOffered", 
			_ => "ERROR", 
		};
	}

	public static void InitializeNewVariablesFromDatabase()
	{
		try
		{
			LuaTableWrapper asTable = Lua.Run("return Variable").AsTable;
			if (asTable == null)
			{
				if (DialogueDebug.LogErrors)
				{
					Debug.LogError(string.Format("{0}: Persistent Data Manager couldn't access Lua Variable[] table", new object[1] { "Dialogue System" }));
				}
				return;
			}
			DialogueDatabase masterDatabase = DialogueManager.MasterDatabase;
			if (masterDatabase == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(asTable.Keys);
			for (int i = 0; i < masterDatabase.variables.Count; i++)
			{
				Variable variable = masterDatabase.variables[i];
				string name = variable.Name;
				string item = DialogueLua.StringToTableIndex(name);
				if (!hashSet.Contains(item))
				{
					switch (variable.Type)
					{
					case FieldType.Boolean:
						DialogueLua.SetVariable(name, variable.InitialBoolValue);
						break;
					case FieldType.Number:
					case FieldType.Actor:
					case FieldType.Item:
					case FieldType.Location:
						DialogueLua.SetVariable(name, variable.InitialFloatValue);
						break;
					default:
						DialogueLua.SetVariable(name, variable.InitialValue);
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: InitializeNewVariablesFromDatabase() failed to get variable data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void InitializeNewActorFieldsFromDatabase()
	{
		try
		{
			DialogueDatabase masterDatabase = DialogueManager.MasterDatabase;
			if (masterDatabase == null)
			{
				return;
			}
			LuaTableWrapper asTable = Lua.Run("return Actor").AsTable;
			if (asTable == null || !asTable.IsValid)
			{
				throw new Exception("Internal error: Can't access Actor table");
			}
			for (int i = 0; i < masterDatabase.actors.Count; i++)
			{
				Actor actor = masterDatabase.actors[i];
				string text = DialogueLua.StringToTableIndex(actor.Name);
				if (!(asTable.luaTable.GetValue(text) is LuaTable luaTable))
				{
					LuaTable luaTable2 = new LuaTable();
					for (int j = 0; j < actor.fields.Count; j++)
					{
						Field field = actor.fields[j];
						string key = DialogueLua.StringToFieldName(field.title);
						luaTable2.AddRaw(key, DialogueLua.GetFieldLuaValue(field));
					}
					asTable.luaTable.AddRaw(text, luaTable2);
					continue;
				}
				HashSet<string> hashSet = new HashSet<string>();
				foreach (LuaValue key2 in luaTable.Keys)
				{
					hashSet.Add(key2.ToString());
				}
				for (int k = 0; k < actor.fields.Count; k++)
				{
					Field field2 = actor.fields[k];
					string text2 = DialogueLua.StringToFieldName(field2.title);
					if (!hashSet.Contains(text2))
					{
						LuaValue fieldLuaValue = DialogueLua.GetFieldLuaValue(field2);
						luaTable.AddRaw(text2, fieldLuaValue);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: InitializeNewActorFieldsFromDatabase() failed to get actor data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void InitializeNewQuestEntriesFromDatabase()
	{
		try
		{
			string text = string.Empty;
			DialogueDatabase masterDatabase = DialogueManager.MasterDatabase;
			if (masterDatabase == null)
			{
				return;
			}
			for (int i = 0; i < masterDatabase.items.Count; i++)
			{
				if (masterDatabase.items[i].IsItem)
				{
					continue;
				}
				Item item = masterDatabase.items[i];
				string name = item.Name;
				string text2 = DialogueLua.StringToTableIndex(name);
				if (!DialogueLua.DoesTableElementExist("Item", name))
				{
					string empty = string.Empty;
					empty = "Item[\"" + DialogueLua.StringToTableIndex(name) + "\"] = {{";
					for (int j = 0; j < item.fields.Count; j++)
					{
						Field field = item.fields[j];
						empty = empty + DialogueLua.StringToFieldName(field.title) + "=" + DialogueLua.ValueAsString(field.type, field.value) + ", ";
					}
					empty += "}}; ";
					text += empty;
				}
				int num = item.LookupInt("Entry Count");
				if (DialogueLua.GetQuestField(name, "Entry Count").AsInt < num)
				{
					text = text + "Item[\"" + text2 + "\"].Entry_Count=" + num + "; ";
					for (int k = 0; k < item.fields.Count; k++)
					{
						Field field2 = item.fields[k];
						if (field2.title.StartsWith("Entry ") && !field2.title.EndsWith(" Count"))
						{
							text = text + "Item[\"" + text2 + "\"]." + DialogueLua.StringToFieldName(field2.title) + " = " + DialogueLua.ValueAsString(field2.type, field2.value) + "; ";
						}
					}
				}
				Lua.Run(text);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("{0}: InitializeNewQuestEntriesFromDatabase() failed to get quest data: {1}", new object[2] { "Dialogue System", ex.Message }));
		}
	}

	public static void InitializeNewSimStatusFromDatabase()
	{
	}

	public static AsyncSaveOperation GetSaveDataAsync()
	{
		AsyncSaveOperation asyncSaveOperation = new AsyncSaveOperation();
		DialogueManager.Instance.StartCoroutine(GetSaveDataAsyncCoroutine(asyncSaveOperation));
		return asyncSaveOperation;
	}

	private static IEnumerator GetSaveDataAsyncCoroutine(AsyncSaveOperation asyncOp)
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Saving data asynchronously...", new object[2] { "Dialogue System", asyncGameObjectBatchSize }));
		}
		switch (recordPersistentDataOn)
		{
		case RecordPersistentDataOn.AllGameObjects:
			yield return DialogueManager.Instance.StartCoroutine(Tools.SendMessageToEveryoneAsync("OnRecordPersistentData", asyncGameObjectBatchSize));
			break;
		case RecordPersistentDataOn.OnlyRegisteredGameObjects:
		{
			int count = 0;
			foreach (GameObject listener in listeners)
			{
				if (listener != null)
				{
					listener.SendMessage("OnRecordPersistentData", SendMessageOptions.DontRequireReceiver);
					count++;
					if (count > asyncGameObjectBatchSize)
					{
						count = 0;
						yield return null;
					}
				}
			}
			break;
		}
		}
		StringBuilder sb = new StringBuilder();
		AppendVariableData(sb);
		yield return null;
		AppendItemData(sb);
		yield return null;
		AppendLocationData(sb);
		yield return null;
		if (includeActorData)
		{
			AppendActorData(sb);
		}
		yield return null;
		yield return DialogueManager.Instance.StartCoroutine(AppendConversationDataAsync(sb));
		yield return null;
		if (includeRelationshipAndStatusData)
		{
			AppendRelationshipAndStatusTables(sb);
		}
		if (GetCustomSaveData != null)
		{
			sb.Append(GetCustomSaveData());
		}
		string text = sb.ToString();
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Saved data asynchronously: {1}", new object[2] { "Dialogue System", text }));
		}
		asyncOp.content = text;
		asyncOp.isDone = true;
	}

	public static void RecordAsync()
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log(string.Format("{0}: Recording persistent data to Lua environment in batches of {1} GameObjects.", new object[2] { "Dialogue System", asyncGameObjectBatchSize }));
		}
		DialogueManager.Instance.StartCoroutine(Tools.SendMessageToEveryoneAsync("OnRecordPersistentData", asyncGameObjectBatchSize));
	}

	private static IEnumerator AppendConversationDataAsync(StringBuilder sb)
	{
		if (includeAllConversationFields || DialogueManager.Instance.persistentDataSettings.includeAllConversationFields)
		{
			AppendAllConversationFields(sb);
		}
		if (!includeSimStatus || !DialogueManager.Instance.includeSimStatus)
		{
			yield break;
		}
		int count = 0;
		useConversationID = string.IsNullOrEmpty(saveConversationSimStatusWithField);
		useEntryID = string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField);
		if (!(Lua.Environment.GetValue("Conversation") is LuaTable conversationTable))
		{
			yield break;
		}
		for (int i = 0; i < conversationTable.List.Count; i++)
		{
			int conversationID = i + 1;
			LuaTable fieldTable = conversationTable.List[i] as LuaTable;
			count += AppendSimStatusForConversation(sb, conversationTable, conversationID, fieldTable);
			if (count >= asyncDialogueEntryBatchSize)
			{
				count = 0;
				yield return null;
			}
		}
		foreach (KeyValuePair<LuaValue, LuaValue> item in conversationTable.Dict)
		{
			if (item.Key != null && item.Value != null && item.Value is LuaTable)
			{
				int conversationID2 = Tools.StringToInt(item.Key.ToString());
				LuaTable fieldTable2 = item.Value as LuaTable;
				count += AppendSimStatusForConversation(sb, conversationTable, conversationID2, fieldTable2);
				if (count >= asyncDialogueEntryBatchSize)
				{
					count = 0;
					yield return null;
				}
			}
		}
	}

	public static byte[] GetRawData()
	{
		Record();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		LuaTable luaTable = Lua.Run("return Conversation").AsTable.luaTable;
		PrepSimStatusForRawData(luaTable);
		WriteValue(binaryWriter, Lua.Run("return Actor").AsTable.luaTable);
		WriteValue(binaryWriter, Lua.Run("return Item").AsTable.luaTable);
		WriteValue(binaryWriter, Lua.Run("return Location").AsTable.luaTable);
		WriteValue(binaryWriter, Lua.Run("return Variable").AsTable.luaTable);
		WriteValue(binaryWriter, luaTable);
		WriteExtraData(binaryWriter);
		binaryWriter.Flush();
		return memoryStream.GetBuffer();
	}

	public static AsyncRawDataOperation GetRawDataAsync()
	{
		AsyncRawDataOperation asyncRawDataOperation = new AsyncRawDataOperation();
		DialogueManager.Instance.StartCoroutine(GetRawDataAsyncCoroutine(asyncRawDataOperation));
		return asyncRawDataOperation;
	}

	private static IEnumerator GetRawDataAsyncCoroutine(AsyncRawDataOperation asyncOp)
	{
		if (DialogueDebug.LogInfo)
		{
			Debug.Log("Dialogue System: Saving raw Lua data asynchronously...");
		}
		switch (recordPersistentDataOn)
		{
		case RecordPersistentDataOn.AllGameObjects:
			yield return DialogueManager.Instance.StartCoroutine(Tools.SendMessageToEveryoneAsync("OnRecordPersistentData", asyncGameObjectBatchSize));
			break;
		case RecordPersistentDataOn.OnlyRegisteredGameObjects:
		{
			int count = 0;
			foreach (GameObject listener in listeners)
			{
				if (listener != null)
				{
					listener.SendMessage("OnRecordPersistentData", SendMessageOptions.DontRequireReceiver);
					count++;
					if (count > asyncGameObjectBatchSize)
					{
						count = 0;
						yield return null;
					}
				}
			}
			break;
		}
		}
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(ms);
			LuaTable conversationTable = Lua.Run("return Conversation").AsTable.luaTable;
			yield return DialogueManager.Instance.StartCoroutine(PrepSimStatusForRawDataAsync(conversationTable));
			WriteValue(writer, Lua.Run("return Actor").AsTable.luaTable);
			yield return null;
			WriteValue(writer, Lua.Run("return Item").AsTable.luaTable);
			yield return null;
			WriteValue(writer, Lua.Run("return Location").AsTable.luaTable);
			yield return null;
			WriteValue(writer, Lua.Run("return Variable").AsTable.luaTable);
			yield return null;
			WriteValue(writer, conversationTable);
			yield return null;
			WriteExtraData(writer);
			writer.Flush();
			asyncOp.content = ms.GetBuffer();
		}
		asyncOp.isDone = true;
	}

	private static void WriteValue(BinaryWriter writer, LuaValue value)
	{
		if (value is LuaTable)
		{
			WriteTable(writer, value as LuaTable);
		}
		else if (value is LuaString)
		{
			writer.Write('S');
			writer.Write((value as LuaString).Text);
		}
		else if (value is LuaNumber)
		{
			writer.Write('N');
			writer.Write((value as LuaNumber).Number);
		}
		else if (value is LuaBoolean)
		{
			writer.Write('B');
			writer.Write((value as LuaBoolean).BoolValue);
		}
		else if (value is LuaNil)
		{
			writer.Write('X');
		}
		else
		{
			Debug.LogError("WriteValue unhandled " + value.GetType().Name + ": " + value.ToString());
		}
	}

	private static void WriteTable(BinaryWriter writer, LuaTable table)
	{
		writer.Write('T');
		if (table.List == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(table.List.Count);
			for (int i = 0; i < table.List.Count; i++)
			{
				WriteValue(writer, table.List[i]);
			}
		}
		if (table.Dict == null)
		{
			writer.Write(0);
			return;
		}
		writer.Write(table.Dict.Count);
		Dictionary<LuaValue, LuaValue>.Enumerator enumerator = table.Dict.GetEnumerator();
		while (enumerator.MoveNext())
		{
			WriteValue(writer, enumerator.Current.Key);
			WriteValue(writer, enumerator.Current.Value);
		}
	}

	public static void ApplyRawData(byte[] bytes)
	{
		using (MemoryStream input = new MemoryStream(bytes))
		{
			using BinaryReader reader = new BinaryReader(input);
			Lua.Run("Actor = {}; Item = {}; Location = {}; Variable = {}; Conversation = {}");
			ReadTable(reader, Lua.Run("return Actor").AsTable.luaTable);
			ReadTable(reader, Lua.Run("return Item").AsTable.luaTable);
			ReadTable(reader, Lua.Run("return Location").AsTable.luaTable);
			ReadTable(reader, Lua.Run("return Variable").AsTable.luaTable);
			ReadTable(reader, Lua.Run("return Conversation").AsTable.luaTable);
			ApplySimStatusFromRawData();
			ApplyExtraData(reader);
			RefreshRelationshipAndStatusTablesFromLua();
			if (initializeNewVariables)
			{
				InitializeNewVariablesFromDatabase();
				InitializeNewQuestEntriesFromDatabase();
			}
		}
		Apply();
	}

	private static LuaValue ReadValue(BinaryReader reader)
	{
		if ((ushort)reader.PeekChar() == 84)
		{
			LuaTable luaTable = new LuaTable();
			ReadTable(reader, luaTable);
			return luaTable;
		}
		char c = reader.ReadChar();
		switch (c)
		{
		case 'S':
			return new LuaString(reader.ReadString());
		case 'N':
			return new LuaNumber(reader.ReadDouble());
		case 'B':
			if (!reader.ReadBoolean())
			{
				return LuaBoolean.False;
			}
			return LuaBoolean.True;
		case 'X':
			return LuaNil.Nil;
		default:
			Debug.LogError("ReadValue unhandled type code " + c);
			return LuaNil.Nil;
		}
	}

	private static void ReadTable(BinaryReader reader, LuaTable table)
	{
		reader.Read();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			LuaValue item = ReadValue(reader);
			table.List.Add(item);
		}
		int num2 = reader.ReadInt32();
		for (int j = 0; j < num2; j++)
		{
			LuaValue key = ReadValue(reader);
			LuaValue value = ReadValue(reader);
			table.Dict.Add(key, value);
		}
	}

	private static void WriteExtraData(BinaryWriter writer)
	{
		if (includeRelationshipAndStatusData)
		{
			StringBuilder stringBuilder = new StringBuilder();
			AppendRelationshipAndStatusTables(stringBuilder);
			writer.Write(stringBuilder.ToString());
		}
		if (GetCustomSaveData != null)
		{
			writer.Write(GetCustomSaveData());
		}
	}

	private static void ApplyExtraData(BinaryReader reader)
	{
		if (includeRelationshipAndStatusData)
		{
			Lua.Run(reader.ReadString());
		}
		if (GetCustomSaveData != null)
		{
			Lua.Run(reader.ReadString(), DialogueDebug.LogInfo);
		}
	}

	private static void PrepSimStatusForRawData(LuaTable conversationTable)
	{
		if (!includeSimStatus || !DialogueManager.Instance.includeSimStatus || conversationTable == null)
		{
			return;
		}
		useConversationID = string.IsNullOrEmpty(saveConversationSimStatusWithField);
		useEntryID = string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField);
		if (useConversationID && useEntryID)
		{
			return;
		}
		Dictionary<int, DialogueEntry> dialogueEntryCache = new Dictionary<int, DialogueEntry>();
		StringBuilder sb = new StringBuilder(16384, int.MaxValue);
		for (int i = 0; i < conversationTable.List.Count; i++)
		{
			int conversationID = i + 1;
			LuaTable fieldTable = conversationTable.List[i] as LuaTable;
			PrepConversationSimStatusForRawData(conversationTable, conversationID, fieldTable, dialogueEntryCache, sb);
		}
		foreach (KeyValuePair<LuaValue, LuaValue> item in conversationTable.Dict)
		{
			if (item.Key != null && item.Value != null && item.Value is LuaTable)
			{
				int conversationID2 = Tools.StringToInt(item.Key.ToString());
				LuaTable fieldTable2 = item.Value as LuaTable;
				PrepConversationSimStatusForRawData(conversationTable, conversationID2, fieldTable2, dialogueEntryCache, sb);
			}
		}
	}

	private static IEnumerator PrepSimStatusForRawDataAsync(LuaTable conversationTable)
	{
		if (!includeSimStatus || !DialogueManager.Instance.includeSimStatus || conversationTable == null)
		{
			yield break;
		}
		useConversationID = string.IsNullOrEmpty(saveConversationSimStatusWithField);
		useEntryID = string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField);
		if (useConversationID && useEntryID)
		{
			yield break;
		}
		Dictionary<int, DialogueEntry> dialogueEntryCache = new Dictionary<int, DialogueEntry>();
		StringBuilder sb = new StringBuilder(16384, int.MaxValue);
		int numEntriesDone = 0;
		for (int i = 0; i < conversationTable.List.Count; i++)
		{
			int conversationID = i + 1;
			LuaTable fieldTable = conversationTable.List[i] as LuaTable;
			numEntriesDone += PrepConversationSimStatusForRawData(conversationTable, conversationID, fieldTable, dialogueEntryCache, sb);
			if (numEntriesDone >= asyncDialogueEntryBatchSize)
			{
				numEntriesDone = 0;
				yield return null;
			}
		}
		foreach (KeyValuePair<LuaValue, LuaValue> item in conversationTable.Dict)
		{
			if (item.Key != null && item.Value != null && item.Value is LuaTable)
			{
				int conversationID2 = Tools.StringToInt(item.Key.ToString());
				LuaTable fieldTable2 = item.Value as LuaTable;
				numEntriesDone += PrepConversationSimStatusForRawData(conversationTable, conversationID2, fieldTable2, dialogueEntryCache, sb);
				if (numEntriesDone >= asyncDialogueEntryBatchSize)
				{
					numEntriesDone = 0;
					yield return null;
				}
			}
		}
	}

	private static int PrepConversationSimStatusForRawData(LuaTable conversationTable, int conversationID, LuaTable fieldTable, Dictionary<int, DialogueEntry> dialogueEntryCache, StringBuilder sb)
	{
		if (conversationTable == null || fieldTable == null)
		{
			return 0;
		}
		if (!(fieldTable.GetValue("Dialog") is LuaTable luaTable))
		{
			return 0;
		}
		Conversation conversation = DialogueManager.MasterDatabase.GetConversation(conversationID);
		if (conversation == null)
		{
			return 0;
		}
		sb.Length = 0;
		DialogueEntry value;
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			value = conversation.dialogueEntries[i];
			dialogueEntryCache[value.id] = value;
		}
		bool flag = true;
		for (int j = 0; j < luaTable.List.Count; j++)
		{
			int num = j + 1;
			LuaTable obj = luaTable.List[j] as LuaTable;
			if (!flag)
			{
				sb.Append(";");
			}
			flag = false;
			if (!useEntryID && dialogueEntryCache.TryGetValue(num, out value))
			{
				sb.Append(Field.LookupValue(value.fields, saveDialogueEntrySimStatusWithField));
			}
			else
			{
				sb.Append(num);
			}
			sb.Append(";");
			Dictionary<LuaValue, LuaValue>.Enumerator enumerator = obj.Dict.GetEnumerator();
			enumerator.MoveNext();
			string simStatus = enumerator.Current.Value.ToString();
			sb.Append(SimStatusToChar(simStatus));
		}
		foreach (KeyValuePair<LuaValue, LuaValue> keyValuePair in luaTable.KeyValuePairs)
		{
			LuaTable obj2 = keyValuePair.Value as LuaTable;
			if (!flag)
			{
				sb.Append(";");
			}
			flag = false;
			if (!useEntryID)
			{
				int num2 = ((keyValuePair.Key is LuaNumber) ? ((int)(keyValuePair.Key as LuaNumber).Number) : Tools.StringToInt(keyValuePair.Key.ToString()));
				if (dialogueEntryCache.TryGetValue(num2, out value))
				{
					sb.Append(Field.LookupValue(value.fields, saveDialogueEntrySimStatusWithField));
				}
				else
				{
					sb.Append(num2);
				}
			}
			else
			{
				sb.Append(keyValuePair.Key.ToString());
			}
			sb.Append(";");
			Dictionary<LuaValue, LuaValue>.Enumerator enumerator3 = obj2.Dict.GetEnumerator();
			enumerator3.MoveNext();
			string simStatus2 = enumerator3.Current.Value.ToString();
			sb.Append(SimStatusToChar(simStatus2));
		}
		if (useConversationID)
		{
			Lua.Run("Conversation[" + conversationID + "].SimX=\"" + sb.ToString() + "\"");
		}
		else
		{
			string text = DialogueLua.StringToTableIndex(conversation.LookupValue(saveConversationSimStatusWithField));
			Lua.Run("Variable[\"Conversation_SimX_" + text + "\"]=\"" + sb.ToString() + "\"");
		}
		return conversation.dialogueEntries.Count;
	}

	private static void ApplySimStatusFromRawData()
	{
		if (includeSimStatus && DialogueManager.Instance.includeSimStatus && (!string.IsNullOrEmpty(saveConversationSimStatusWithField) || !string.IsNullOrEmpty(saveDialogueEntrySimStatusWithField)))
		{
			ExpandCompressedSimStatusData();
		}
	}
}
