using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Language.Lua;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class DialogueLua
{
	public const string SimStatus = "SimStatus";

	public const string Untouched = "Untouched";

	public const string WasDisplayed = "WasDisplayed";

	public const string WasOffered = "WasOffered";

	public static bool includeSimStatus;

	public static bool replaceSlashWithUnderscore;

	private static Dictionary<string, string> statusTable;

	private static Dictionary<string, float> relationshipTable;

	private static bool isRegistering;

	private static bool hasCachedParticipants;

	private static string cachedActorName;

	private static string cachedConversantName;

	private static string cachedActorIndex;

	private static string cachedConversantIndex;

	static DialogueLua()
	{
		includeSimStatus = true;
		replaceSlashWithUnderscore = true;
		statusTable = new Dictionary<string, string>();
		relationshipTable = new Dictionary<string, float>();
		isRegistering = false;
		hasCachedParticipants = false;
		InitializeChatMapperVariables();
		RegisterLuaFunctions();
	}

	public static void RegisterLuaFunctions()
	{
		bool warnRegisteringExistingFunction = Lua.warnRegisteringExistingFunction;
		Lua.warnRegisteringExistingFunction = false;
		RegisterChatMapperFunctions();
		RegisterDialogueSystemFunctions();
		Lua.warnRegisteringExistingFunction = warnRegisteringExistingFunction;
	}

	private static IEnumerator RegisterLuaFunctionsAfterFrame()
	{
		isRegistering = true;
		yield return CoroutineUtility.endOfFrame;
		RegisterLuaFunctions();
		isRegistering = false;
		if (hasCachedParticipants)
		{
			hasCachedParticipants = false;
			SetParticipants(cachedActorName, cachedConversantName, cachedActorIndex, cachedConversantIndex);
		}
	}

	public static void InitializeChatMapperVariables()
	{
		Lua.Run("Actor = {}; Item = {}; Quest = Item; Location = {}; Conversation = {}; Variable = {}; Variable[\"Alert\"] = \"\"", DialogueDebug.LogInfo);
		Lua.Run("unassigned='unassigned'; active='active'; success='success'; failure='failure'; abandoned='abandoned'", DialogueDebug.LogInfo);
		statusTable.Clear();
		relationshipTable.Clear();
	}

	public static void AddChatMapperVariables(DialogueDatabase database, List<DialogueDatabase> loadedDatabases)
	{
		if (!(database == null))
		{
			bool addRaw = loadedDatabases == null || loadedDatabases.Count == 0;
			AddToTable("Actor", database.actors, loadedDatabases, addRaw);
			AddToTable("Item", database.items, loadedDatabases, addRaw);
			AddToTable("Location", database.locations, loadedDatabases, addRaw);
			AddToVariableTable(database.variables, loadedDatabases, addRaw);
			AddToConversationTable(database.conversations, loadedDatabases, addRaw);
			if (!string.IsNullOrEmpty(database.globalUserScript))
			{
				Lua.Run(database.globalUserScript, DialogueDebug.LogInfo);
			}
		}
	}

	public static void RemoveChatMapperVariables(DialogueDatabase database, List<DialogueDatabase> loadedDatabases)
	{
		if (database != null)
		{
			RemoveFromTable("Actor", database.actors, loadedDatabases);
			RemoveFromTable("Item", database.items, loadedDatabases);
			RemoveFromTable("Location", database.locations, loadedDatabases);
			RemoveFromTable("Variable", database.variables, loadedDatabases);
			RemoveFromTable("Conversation", database.conversations, loadedDatabases);
		}
	}

	public static void SetParticipants(string actorName, string conversantName, string actorIndex = null, string conversantIndex = null)
	{
		SetVariable("Actor", actorName);
		SetVariable("Conversant", conversantName);
		SetVariable("ActorIndex", StringToTableIndex(string.IsNullOrEmpty(actorIndex) ? actorName : actorIndex));
		SetVariable("ConversantIndex", StringToTableIndex(string.IsNullOrEmpty(conversantIndex) ? actorName : conversantIndex));
		if (isRegistering)
		{
			hasCachedParticipants = true;
			cachedActorName = actorName;
			cachedConversantName = conversantName;
			cachedActorIndex = actorIndex;
			cachedConversantIndex = conversantIndex;
		}
	}

	public static string GetSimStatus(DialogueEntry dialogueEntry)
	{
		if (dialogueEntry == null)
		{
			return string.Empty;
		}
		return GetSimStatus(dialogueEntry.conversationID, dialogueEntry.id);
	}

	public static string GetSimStatus(int conversationID, int entryID)
	{
		if (includeSimStatus && GetSimStatusTable(conversationID, entryID).GetValue("SimStatus") is LuaString luaString)
		{
			return luaString.Text;
		}
		return "Untouched";
	}

	private static LuaTable GetSimStatusTable(int conversationID, int entryID)
	{
		LuaTable luaTable = Lua.Environment.GetValue("Conversation") as LuaTable;
		LuaTable luaTable2 = luaTable.GetValue(conversationID) as LuaTable;
		if (luaTable2 == null)
		{
			AddToConversationTable(luaTable, DialogueManager.masterDatabase.GetConversation(conversationID), addRaw: true);
			luaTable2 = luaTable.GetValue(conversationID) as LuaTable;
		}
		LuaTable luaTable3 = ((luaTable2 != null) ? (luaTable2.GetValue("Dialog") as LuaTable) : AddToConversationTable(luaTable, DialogueManager.MasterDatabase.GetConversation(conversationID), addRaw: true));
		if (luaTable3 == null)
		{
			luaTable3 = AddNewDialogTableToConversationFields(luaTable2, DialogueManager.masterDatabase.GetConversation(conversationID));
		}
		LuaTable luaTable4 = luaTable3.GetValue(entryID) as LuaTable;
		if (luaTable4 == null)
		{
			luaTable4 = new LuaTable();
			luaTable4.AddRaw("SimStatus", new LuaString("Untouched"));
			luaTable3.AddRaw(entryID, luaTable4);
		}
		return luaTable4;
	}

	public static void MarkDialogueEntryUntouched(DialogueEntry dialogueEntry)
	{
		MarkDialogueEntry(dialogueEntry, "Untouched");
	}

	public static void MarkDialogueEntryDisplayed(DialogueEntry dialogueEntry)
	{
		MarkDialogueEntry(dialogueEntry, "WasDisplayed");
	}

	public static void MarkDialogueEntryOffered(DialogueEntry dialogueEntry)
	{
		if (includeSimStatus && dialogueEntry != null && !string.Equals(GetSimStatus(dialogueEntry), "WasDisplayed"))
		{
			MarkDialogueEntry(dialogueEntry, "WasOffered");
		}
	}

	public static void MarkDialogueEntry(DialogueEntry dialogueEntry, string status)
	{
		if (includeSimStatus && dialogueEntry != null)
		{
			GetSimStatusTable(dialogueEntry.conversationID, dialogueEntry.id).SetNameValue("SimStatus", new LuaString(status));
		}
	}

	private static HashSet<string> GetExistingAssetNames(string arrayName, List<DialogueDatabase> loadedDatabases)
	{
		if (loadedDatabases == null || loadedDatabases.Count == 0)
		{
			return null;
		}
		HashSet<string> hashSet = new HashSet<string>();
		switch (arrayName)
		{
		case "Actor":
			foreach (DialogueDatabase loadedDatabase in loadedDatabases)
			{
				foreach (Actor actor in loadedDatabase.actors)
				{
					hashSet.Add(actor.Name);
				}
			}
			break;
		case "Item":
			foreach (DialogueDatabase loadedDatabase2 in loadedDatabases)
			{
				foreach (Item item in loadedDatabase2.items)
				{
					hashSet.Add(item.Name);
				}
			}
			break;
		case "Location":
			foreach (DialogueDatabase loadedDatabase3 in loadedDatabases)
			{
				foreach (Location location in loadedDatabase3.locations)
				{
					hashSet.Add(location.Name);
				}
			}
			break;
		case "Variable":
			foreach (DialogueDatabase loadedDatabase4 in loadedDatabases)
			{
				foreach (Variable variable in loadedDatabase4.variables)
				{
					hashSet.Add(variable.Name);
				}
			}
			break;
		}
		return hashSet;
	}

	private static void AddToTable<T>(string arrayName, List<T> assets, List<DialogueDatabase> loadedDatabases, bool addRaw) where T : Asset
	{
		Lua.WasInvoked = true;
		if (!(Lua.Environment.GetValue(arrayName) is LuaTable luaTable))
		{
			return;
		}
		HashSet<string> existingAssetNames = GetExistingAssetNames(arrayName, loadedDatabases);
		for (int i = 0; i < assets.Count; i++)
		{
			T val = assets[i];
			string name = val.Name;
			if (existingAssetNames == null || !existingAssetNames.Contains(name))
			{
				string text = StringToTableIndex(name);
				LuaTable luaTable2 = new LuaTable();
				for (int j = 0; j < val.fields.Count; j++)
				{
					Field field = val.fields[j];
					string key = StringToFieldName(field.title);
					luaTable2.AddRaw(key, GetFieldLuaValue(field));
				}
				if (addRaw)
				{
					luaTable.AddRaw(text, luaTable2);
				}
				else
				{
					luaTable.SetKeyValue(new LuaString(text), luaTable2);
				}
			}
		}
	}

	private static void AddToVariableTable(List<Variable> variables, List<DialogueDatabase> loadedDatabases, bool addRaw)
	{
		Lua.WasInvoked = true;
		if (!(Lua.Environment.GetValue("Variable") is LuaTable luaTable))
		{
			return;
		}
		HashSet<string> existingAssetNames = GetExistingAssetNames("Variable", loadedDatabases);
		for (int i = 0; i < variables.Count; i++)
		{
			Variable variable = variables[i];
			string name = variable.Name;
			if (existingAssetNames == null || !existingAssetNames.Contains(name))
			{
				string text = StringToTableIndex(name);
				if (addRaw)
				{
					luaTable.AddRaw(text, GetVariableLuaValue(variable));
				}
				else
				{
					luaTable.SetNameValue(text, GetVariableLuaValue(variable));
				}
			}
		}
	}

	public static void AddToConversationTable(List<Conversation> conversations, List<DialogueDatabase> loadedDatabases, bool addRaw = false)
	{
		Lua.WasInvoked = true;
		if (Lua.Environment.GetValue("Conversation") is LuaTable conversationTable)
		{
			for (int i = 0; i < conversations.Count; i++)
			{
				Conversation conversation = conversations[i];
				AddToConversationTable(conversationTable, conversation, addRaw);
			}
		}
	}

	public static LuaTable AddToConversationTable(LuaTable conversationTable, Conversation conversation, bool addRaw = false)
	{
		LuaTable result = null;
		LuaTable luaTable = new LuaTable();
		for (int i = 0; i < conversation.fields.Count; i++)
		{
			Field field = conversation.fields[i];
			string key = StringToFieldName(field.title);
			luaTable.AddRaw(key, GetFieldLuaValue(field));
		}
		if (includeSimStatus)
		{
			result = AddNewDialogTableToConversationFields(luaTable, conversation);
		}
		if (addRaw)
		{
			conversationTable.AddRaw(conversation.id, luaTable);
		}
		else
		{
			conversationTable.SetKeyValue(new LuaNumber(conversation.id), luaTable);
		}
		return result;
	}

	private static LuaTable AddNewDialogTableToConversationFields(LuaTable fieldTable, Conversation conversation)
	{
		LuaTable luaTable = new LuaTable();
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			DialogueEntry dialogueEntry = conversation.dialogueEntries[i];
			LuaTable luaTable2 = new LuaTable();
			luaTable2.AddRaw("SimStatus", new LuaString("Untouched"));
			luaTable.AddRaw(dialogueEntry.id, luaTable2);
		}
		fieldTable.AddRaw("Dialog", luaTable);
		return luaTable;
	}

	public static LuaValue GetFieldLuaValue(Field field)
	{
		if (field == null)
		{
			return LuaNil.Nil;
		}
		switch (field.type)
		{
		case FieldType.Number:
		case FieldType.Actor:
		case FieldType.Item:
		case FieldType.Location:
			return new LuaNumber(Tools.StringToFloat(field.value));
		case FieldType.Boolean:
			if (!Tools.StringToBool(field.value))
			{
				return LuaBoolean.False;
			}
			return LuaBoolean.True;
		default:
			return new LuaString(field.value);
		}
	}

	private static LuaValue GetVariableLuaValue(Variable field)
	{
		if (field == null)
		{
			return LuaNil.Nil;
		}
		switch (field.Type)
		{
		case FieldType.Number:
		case FieldType.Actor:
		case FieldType.Item:
		case FieldType.Location:
			return new LuaNumber(Tools.StringToFloat(field.InitialValue));
		case FieldType.Boolean:
			if (!Tools.StringToBool(field.InitialValue))
			{
				return LuaBoolean.False;
			}
			return LuaBoolean.True;
		default:
			return new LuaString(field.InitialValue);
		}
	}

	private static void SetFields(string record, List<Field> fields, string extraField = null)
	{
		StringBuilder stringBuilder = new StringBuilder($"{record} = {{ Status = \"\", ", 1024);
		for (int i = 0; i < fields.Count; i++)
		{
			Field field = fields[i];
			if (!string.IsNullOrEmpty(field.title))
			{
				stringBuilder.AppendFormat("{0} = {1}, ", new object[2]
				{
					StringToFieldName(field.title),
					FieldValueAsString(field)
				});
			}
		}
		if (!string.IsNullOrEmpty(extraField))
		{
			stringBuilder.Append(extraField);
		}
		stringBuilder.Append('}');
		Lua.Run(stringBuilder.ToString(), DialogueDebug.LogInfo);
	}

	public static string FieldValueAsString(Field field)
	{
		return ValueAsString(field.type, field.value);
	}

	public static string ValueAsString(FieldType fieldType, string fieldValue)
	{
		switch (fieldType)
		{
		case FieldType.Number:
		case FieldType.Actor:
		case FieldType.Item:
		case FieldType.Location:
			if (!string.IsNullOrEmpty(fieldValue))
			{
				return fieldValue;
			}
			return "0";
		case FieldType.Boolean:
			if (!string.IsNullOrEmpty(fieldValue))
			{
				return fieldValue.ToLower();
			}
			return "false";
		default:
			return $"\"{DoubleQuotesToSingle(fieldValue)}\"";
		}
	}

	private static void RemoveFromTable<T>(string arrayName, List<T> assets, List<DialogueDatabase> loadedDatabases) where T : Asset
	{
		for (int i = 0; i < assets.Count; i++)
		{
			T val = assets[i];
			if (!DialogueDatabase.Contains(loadedDatabases, val))
			{
				if (val is Conversation)
				{
					Lua.Run(string.Format("{0}[{1}] = nil", new object[2] { arrayName, val.id }), DialogueDebug.LogInfo);
				}
				else
				{
					Lua.Run(string.Format("{0}[\"{1}\"] = nil", new object[2]
					{
						arrayName,
						StringToTableIndex(val.Name)
					}), DialogueDebug.LogInfo);
				}
			}
		}
	}

	private static void RegisterDialogueSystemFunctions()
	{
		Lua.RegisterFunction("RandomElement", null, typeof(DialogueLua).GetMethod("RandomElement"));
		Lua.RegisterFunction("GetLocalizedText", null, typeof(DialogueLua).GetMethod("GetLocalizedText"));
	}

	public static string RandomElement(string list)
	{
		if (string.IsNullOrEmpty(list))
		{
			return string.Empty;
		}
		string[] array = list.Split(new char[1] { '|' }, StringSplitOptions.None);
		return array[UnityEngine.Random.Range(0, array.Length)];
	}

	public static string GetLocalizedText(string tableName, string elementName, string fieldName)
	{
		return GetLocalizedTableField(tableName, elementName, fieldName).AsString;
	}

	private static void RegisterChatMapperFunctions()
	{
		Lua.RegisterFunction("GetStatus", null, typeof(DialogueLua).GetMethod("GetStatus"));
		Lua.RegisterFunction("SetStatus", null, typeof(DialogueLua).GetMethod("SetStatus"));
		Lua.RegisterFunction("GetRelationship", null, typeof(DialogueLua).GetMethod("GetRelationship"));
		Lua.RegisterFunction("SetRelationship", null, typeof(DialogueLua).GetMethod("SetRelationship"));
		Lua.RegisterFunction("IncRelationship", null, typeof(DialogueLua).GetMethod("IncRelationship"));
		Lua.RegisterFunction("DecRelationship", null, typeof(DialogueLua).GetMethod("DecRelationship"));
	}

	private static string GetStatusKey(LuaTable asset1, LuaTable asset2)
	{
		if (asset1 == null || asset2 == null)
		{
			if (DialogueDebug.LogWarnings)
			{
				Debug.LogWarning("Dialogue System: Syntax error in status function");
			}
			return "INVALID";
		}
		string text = StringToTableIndex(asset1.GetValue("Name").ToString());
		string text2 = StringToTableIndex(asset2.GetValue("Name").ToString());
		return text + "," + text2;
	}

	private static string GetRelationshipKey(LuaTable actor1, LuaTable actor2, string relationshipType)
	{
		if (actor1 == null || actor2 == null || relationshipType == null)
		{
			if (DialogueDebug.LogWarnings)
			{
				Debug.LogWarning("Dialogue System: Syntax error in relationship function");
			}
			return "INVALID";
		}
		string text = StringToTableIndex(actor1.GetValue("Name").ToString());
		string text2 = StringToTableIndex(actor2.GetValue("Name").ToString());
		string text3 = SanitizeForStatusTable(relationshipType.ToString());
		return string.Format("{0},{1},'{2}'", new object[3] { text, text2, text3 });
	}

	private static string SanitizeForStatusTable(string s)
	{
		return string.Join("_", s.Split(',', ';', '"', '\''));
	}

	private static float GetLuaFloat(LuaNumber luaNumber)
	{
		if (luaNumber == null)
		{
			return 0f;
		}
		return (float)luaNumber.Number;
	}

	public static string GetStatus(LuaTable asset1, LuaTable asset2)
	{
		string statusKey = GetStatusKey(asset1, asset2);
		if (!statusTable.ContainsKey(statusKey))
		{
			return string.Empty;
		}
		return statusTable[statusKey];
	}

	public static void SetStatus(LuaTable asset1, LuaTable asset2, string statusValue)
	{
		string statusKey = GetStatusKey(asset1, asset2);
		statusTable[statusKey] = statusValue ?? string.Empty;
	}

	public static float GetRelationship(LuaTable actor1, LuaTable actor2, string relationshipType)
	{
		string relationshipKey = GetRelationshipKey(actor1, actor2, relationshipType);
		if (!relationshipTable.ContainsKey(relationshipKey))
		{
			return 0f;
		}
		return relationshipTable[relationshipKey];
	}

	public static void SetRelationship(LuaTable actor1, LuaTable actor2, string relationshipType, float value)
	{
		relationshipTable[GetRelationshipKey(actor1, actor2, relationshipType)] = value;
	}

	public static void IncRelationship(LuaTable actor1, LuaTable actor2, string relationshipType, float incrementAmount)
	{
		string relationshipKey = GetRelationshipKey(actor1, actor2, relationshipType);
		if (relationshipTable.ContainsKey(relationshipKey))
		{
			relationshipTable[relationshipKey] += incrementAmount;
		}
		else
		{
			relationshipTable.Add(relationshipKey, incrementAmount);
		}
	}

	public static void DecRelationship(LuaTable actor1, LuaTable actor2, string relationshipType, float decrementAmount)
	{
		string relationshipKey = GetRelationshipKey(actor1, actor2, relationshipType);
		if (relationshipTable.ContainsKey(relationshipKey))
		{
			relationshipTable[relationshipKey] -= decrementAmount;
		}
		else
		{
			relationshipTable.Add(relationshipKey, 0f - decrementAmount);
		}
	}

	public static string GetStatusTableAsLua()
	{
		StringBuilder stringBuilder = new StringBuilder(1024, 65536);
		stringBuilder.Append("StatusTable = \"");
		foreach (KeyValuePair<string, string> item in statusTable)
		{
			stringBuilder.AppendFormat("{0},'{1}';", new object[2]
			{
				item.Key,
				SanitizeForStatusTable(item.Value)
			});
		}
		stringBuilder.Append("\"; ");
		return stringBuilder.ToString();
	}

	public static string GetRelationshipTableAsLua()
	{
		StringBuilder stringBuilder = new StringBuilder(1024, 65536);
		stringBuilder.Append("RelationshipTable = \"");
		foreach (KeyValuePair<string, float> item in relationshipTable)
		{
			stringBuilder.AppendFormat("{0},{1};", new object[2] { item.Key, item.Value });
		}
		stringBuilder.Append("\"; ");
		return stringBuilder.ToString();
	}

	public static void RefreshStatusTableFromLua()
	{
		statusTable.Clear();
		string asString = Lua.Run("return StatusTable").AsString;
		char[] separator = new char[1] { ';' };
		char[] separator2 = new char[1] { ',' };
		string[] array = asString.Split(separator);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(separator2);
			if (array2.Length > 2)
			{
				string key = array2[0] + "," + array2[1];
				string value = array2[2].Substring(1, array2[2].Length - 2);
				statusTable[key] = value;
			}
		}
	}

	public static void RefreshRelationshipTableFromLua()
	{
		relationshipTable.Clear();
		string asString = Lua.Run("return RelationshipTable").AsString;
		char[] separator = new char[1] { ';' };
		char[] separator2 = new char[1] { ',' };
		string[] array = asString.Split(separator);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(separator2);
			if (array2.Length > 3)
			{
				object[] args = new string[3]
				{
					array2[0],
					array2[1],
					array2[2]
				};
				string key = string.Format("{0},{1},{2}", args);
				float value = Tools.StringToFloat(array2[3]);
				relationshipTable[key] = value;
			}
		}
	}

	public static string DoubleQuotesToSingle(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", string.Empty);
		}
		return string.Empty;
	}

	public static string SpacesToUnderscores(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Replace(' ', '_');
		}
		return string.Empty;
	}

	public static string StringToTableIndex(string s)
	{
		if (replaceSlashWithUnderscore)
		{
			if (!string.IsNullOrEmpty(s))
			{
				return SpacesToUnderscores(DoubleQuotesToSingle(s.Replace('"', '_'))).Replace('-', '_').Replace('(', '_').Replace(')', '_')
					.Replace("/", "_");
			}
			return string.Empty;
		}
		if (!string.IsNullOrEmpty(s))
		{
			return SpacesToUnderscores(DoubleQuotesToSingle(s.Replace('"', '_'))).Replace('-', '_').Replace('(', '_').Replace(')', '_');
		}
		return string.Empty;
	}

	public static string StringToLocalizedTableIndex(string s)
	{
		if (Localization.IsDefaultLanguage || string.IsNullOrEmpty(s))
		{
			return StringToTableIndex(s);
		}
		return StringToTableIndex(s + "_" + Localization.Language);
	}

	public static string StringToFieldName(string s)
	{
		return StringToTableIndex(s).Replace('.', '_');
	}

	public static string StringToLocalizedFieldName(string s)
	{
		if (Localization.IsDefaultLanguage || string.IsNullOrEmpty(s))
		{
			return StringToFieldName(s);
		}
		return StringToFieldName(s + "_" + Localization.Language);
	}

	public static bool DoesTableElementExist(string table, string element)
	{
		if (string.IsNullOrEmpty(element))
		{
			return false;
		}
		if (!(Lua.Environment.GetValue(table) is LuaTable luaTable))
		{
			return false;
		}
		return luaTable.GetKey(StringToTableIndex(element)) != LuaNil.Nil;
	}

	public static Lua.Result GetTableField(string table, string element, string field)
	{
		if (!(Lua.Environment.GetValue(table) is LuaTable luaTable))
		{
			return Lua.NoResult;
		}
		if (!(luaTable.GetValue(StringToTableIndex(element)) is LuaTable luaTable2))
		{
			return Lua.NoResult;
		}
		LuaValue value = luaTable2.GetValue(StringToTableIndex(field));
		if (value == null || value == LuaNil.Nil)
		{
			return Lua.NoResult;
		}
		return new Lua.Result(value);
	}

	public static void SetTableField(string table, string element, string field, object value)
	{
		Lua.WasInvoked = true;
		if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(element) || string.IsNullOrEmpty(field))
		{
			return;
		}
		if (!(Lua.Environment.GetValue(table) is LuaTable luaTable))
		{
			throw new NullReferenceException("Table not found in Lua environment: " + table);
		}
		LuaTable luaTable2 = luaTable.GetValue(StringToTableIndex(element)) as LuaTable;
		if (luaTable2 == null)
		{
			Lua.Run($"{table}[\"{StringToTableIndex(element)}\"] = {{}}");
			luaTable2 = luaTable.GetValue(StringToTableIndex(element)) as LuaTable;
			if (luaTable2 == null)
			{
				throw new NullReferenceException("Unable to find or add element: " + element);
			}
		}
		luaTable2.SetNameValue(StringToTableIndex(field), LuaInterpreterExtensions.ObjectToLuaValue(value));
	}

	public static Lua.Result GetActorField(string actor, string field)
	{
		return GetTableField("Actor", actor, field);
	}

	public static void SetActorField(string actor, string field, object value)
	{
		SetTableField("Actor", actor, field, value);
	}

	public static Lua.Result GetItemField(string item, string field)
	{
		return GetTableField("Item", item, field);
	}

	public static void SetItemField(string item, string field, object value)
	{
		SetTableField("Item", item, field, value);
	}

	public static Lua.Result GetQuestField(string quest, string field)
	{
		return GetTableField("Item", quest, field);
	}

	public static void SetQuestField(string quest, string field, object value)
	{
		SetTableField("Item", quest, field, value);
	}

	public static Lua.Result GetLocationField(string location, string field)
	{
		return GetTableField("Location", location, field);
	}

	public static void SetLocationField(string location, string field, object value)
	{
		SetTableField("Location", location, field, value);
	}

	public static string[] GetAllVariables()
	{
		List<string> list = new List<string>();
		LuaTableWrapper asTable = Lua.Run("return Variable").asTable;
		if (asTable != null)
		{
			list.AddRange(asTable.keys);
		}
		return list.ToArray();
	}

	public static bool DoesVariableExist(string variable)
	{
		return DoesTableElementExist("Variable", variable);
	}

	public static Lua.Result GetVariable(string variable)
	{
		if (!(Lua.Environment.GetValue("Variable") is LuaTable luaTable))
		{
			return Lua.NoResult;
		}
		return new Lua.Result(luaTable.GetValue(StringToTableIndex(variable)));
	}

	public static bool GetVariable(string variable, bool defaultValue)
	{
		Lua.Result variable2 = GetVariable(variable);
		if (!variable2.isBool)
		{
			return defaultValue;
		}
		return variable2.asBool;
	}

	public static string GetVariable(string variable, string defaultValue)
	{
		Lua.Result variable2 = GetVariable(variable);
		if (!variable2.isString)
		{
			return defaultValue;
		}
		return variable2.asString;
	}

	public static int GetVariable(string variable, int defaultValue)
	{
		Lua.Result variable2 = GetVariable(variable);
		if (!variable2.isNumber)
		{
			return defaultValue;
		}
		return variable2.asInt;
	}

	public static float GetVariable(string variable, float defaultValue)
	{
		Lua.Result variable2 = GetVariable(variable);
		if (!variable2.isNumber)
		{
			return defaultValue;
		}
		return variable2.asFloat;
	}

	public static void SetVariable(string variable, object value)
	{
		Lua.WasInvoked = true;
		if (Lua.Environment.GetValue("Variable") is LuaTable luaTable)
		{
			luaTable.SetNameValue(StringToTableIndex(variable), LuaInterpreterExtensions.ObjectToLuaValue(value));
		}
	}

	public static Lua.Result GetLocalizedTableField(string table, string element, string field)
	{
		Lua.Result tableField = GetTableField(table, element, StringToLocalizedTableIndex(field));
		if (Localization.UseDefaultIfUndefined && (tableField.luaValue == null || tableField.luaValue is LuaNil || (tableField.luaValue is LuaString && string.IsNullOrEmpty((tableField.luaValue as LuaString).Text))))
		{
			return GetTableField(table, element, field);
		}
		return tableField;
	}

	public static void SetLocalizedTableField(string table, string element, string field, object value)
	{
		SetTableField(table, element, StringToLocalizedTableIndex(field), value);
	}

	public static Lua.Result GetLocalizedActorField(string actor, string field)
	{
		return GetLocalizedTableField("Actor", actor, field);
	}

	public static void SetLocalizedActorField(string actor, string field, object value)
	{
		SetActorField(actor, StringToLocalizedTableIndex(field), value);
	}

	public static Lua.Result GetLocalizedItemField(string item, string field)
	{
		return GetLocalizedTableField("Item", item, field);
	}

	public static void SetLocalizedItemField(string item, string field, object value)
	{
		SetItemField(item, StringToLocalizedTableIndex(field), value);
	}

	public static Lua.Result GetLocalizedQuestField(string quest, string field)
	{
		return GetLocalizedItemField(quest, field);
	}

	public static void SetLocalizedQuestField(string quest, string field, object value)
	{
		SetLocalizedItemField(quest, field, value);
	}

	public static Lua.Result GetLocalizedLocationField(string location, string field)
	{
		return GetLocalizedTableField("Location", location, field);
	}

	public static void SetLocalizedLocationField(string location, string field, object value)
	{
		SetLocationField(location, StringToLocalizedTableIndex(field), value);
	}

	public static Lua.Result GetConversationField(int conversationID, string field)
	{
		return Lua.Run(string.Format("return Conversation[{0}].{1}", new object[2]
		{
			conversationID,
			StringToTableIndex(field)
		}), debug: false, allowExceptions: true);
	}

	public static void SetConversationField(int conversationID, string field, object value)
	{
		string text = ((value == null) ? "nil" : ((value.GetType() == typeof(string)) ? ("\"" + DoubleQuotesToSingle(value.ToString()) + "\"") : value.ToString()));
		Lua.Run(string.Format("Conversation[{0}].{1} = {2}", new object[3]
		{
			conversationID,
			StringToTableIndex(field),
			text
		}), debug: false, allowExceptions: true);
	}

	public static Lua.Result GetLocalizedConversationField(int conversationID, string field)
	{
		return Lua.Run(string.Format("return Conversation[{0}].{1}", new object[2]
		{
			conversationID,
			StringToLocalizedTableIndex(field)
		}), debug: false, allowExceptions: true);
	}
}
