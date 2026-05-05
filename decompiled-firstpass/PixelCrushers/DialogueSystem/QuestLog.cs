using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class QuestLog
{
	public delegate void QuestChangedDelegate(string questName, QuestState newState);

	public class QuestWatchItem
	{
		private string questName;

		private int entryNumber;

		private LuaWatchFrequency frequency;

		private string luaExpression;

		private QuestChangedDelegate questChangedHandler;

		public QuestWatchItem(string questName, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
		{
			this.questName = questName;
			entryNumber = 0;
			this.frequency = frequency;
			luaExpression = $"return Item[\"{DialogueLua.StringToTableIndex(questName)}\"].State";
			this.questChangedHandler = questChangedHandler;
			DialogueManager.AddLuaObserver(luaExpression, frequency, OnLuaChanged);
		}

		public QuestWatchItem(string questName, int entryNumber, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
		{
			this.questName = questName;
			this.entryNumber = entryNumber;
			this.frequency = frequency;
			luaExpression = string.Format("return Item[\"{0}\"].Entry_{1}_State", new object[2]
			{
				DialogueLua.StringToTableIndex(questName),
				entryNumber
			});
			this.questChangedHandler = questChangedHandler;
			DialogueManager.AddLuaObserver(luaExpression, frequency, OnLuaChanged);
		}

		public bool Matches(string questName, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
		{
			if (string.Equals(questName, this.questName) && frequency == this.frequency)
			{
				return questChangedHandler == this.questChangedHandler;
			}
			return false;
		}

		public bool Matches(string questName, int entryNumber, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
		{
			if (string.Equals(questName, this.questName) && entryNumber == this.entryNumber && frequency == this.frequency)
			{
				return questChangedHandler == this.questChangedHandler;
			}
			return false;
		}

		public void StopObserving()
		{
			DialogueManager.RemoveLuaObserver(luaExpression, frequency, OnLuaChanged);
		}

		private void OnLuaChanged(LuaWatchItem luaWatchItem, Lua.Result newResult)
		{
			if (string.Equals(luaWatchItem.luaExpression, luaExpression) && questChangedHandler != null)
			{
				questChangedHandler(questName, StringToState(newResult.asString));
			}
		}
	}

	public const string UnassignedStateString = "unassigned";

	public const string ActiveStateString = "active";

	public const string SuccessStateString = "success";

	public const string FailureStateString = "failure";

	public const string AbandonedStateString = "abandoned";

	public const string GrantableStateString = "grantable";

	public const string ReturnToNPCStateString = "returnToNPC";

	public const string DoneStateString = "done";

	public static StringToQuestStateDelegate StringToState = DefaultStringToState;

	public static QuestStateToStringDelegate StateToString = DefaultStateToString;

	public static CurrentQuestStateDelegate CurrentQuestStateOverride = null;

	public static SetQuestStateDelegate SetQuestStateOverride = null;

	public static CurrentQuestEntryStateDelegate CurrentQuestEntryStateOverride = null;

	public static SetQuestEntryStateDelegate SetQuestEntryStateOverride = null;

	public static bool trackOneQuestAtATime = false;

	private static readonly List<QuestWatchItem> questWatchList = new List<QuestWatchItem>();

	public static void RegisterQuestLogFunctions()
	{
		Lua.RegisterFunction("CurrentQuestState", null, typeof(QuestLog).GetMethod("CurrentQuestState"));
		Lua.RegisterFunction("CurrentQuestEntryState", null, typeof(QuestLog).GetMethod("CurrentQuestEntryState"));
		Lua.RegisterFunction("SetQuestState", null, typeof(QuestLog).GetMethod("SetQuestState", new Type[2]
		{
			typeof(string),
			typeof(string)
		}));
		Lua.RegisterFunction("SetQuestEntryState", null, typeof(QuestLog).GetMethod("SetQuestEntryState", new Type[3]
		{
			typeof(string),
			typeof(double),
			typeof(string)
		}));
		Lua.RegisterFunction("UpdateQuestIndicators", null, typeof(QuestLog).GetMethod("UpdateQuestIndicators", new Type[1] { typeof(string) }));
	}

	public static void AddQuest(string questName, string description, string successDescription, string failureDescription, QuestState state)
	{
		if (!string.IsNullOrEmpty(questName))
		{
			Lua.Run($"Item[\"{DialogueLua.StringToTableIndex(questName)}\"] = {{ Name = \"{DialogueLua.DoubleQuotesToSingle(questName)}\", Is_Item = false, Description = \"{DialogueLua.DoubleQuotesToSingle(description)}\", Success_Description = \"{DialogueLua.DoubleQuotesToSingle(successDescription)}\", Failure_Description = \"{DialogueLua.DoubleQuotesToSingle(failureDescription)}\", State = \"{StateToString(state)}\" }}", DialogueDebug.logInfo);
		}
	}

	public static void AddQuest(string questName, string description, QuestState state)
	{
		if (!string.IsNullOrEmpty(questName))
		{
			Lua.Run($"Item[\"{DialogueLua.StringToTableIndex(questName)}\"] = {{ Name = \"{DialogueLua.DoubleQuotesToSingle(questName)}\", Is_Item = false, Description = \"{DialogueLua.DoubleQuotesToSingle(description)}\", State = \"{StateToString(state)}\" }}", DialogueDebug.logInfo);
		}
	}

	public static void AddQuest(string questName, string description)
	{
		AddQuest(questName, description, QuestState.Unassigned);
	}

	public static void DeleteQuest(string questName)
	{
		if (!string.IsNullOrEmpty(questName))
		{
			Lua.Run($"Item[\"{DialogueLua.StringToTableIndex(questName)}\"] = nil", DialogueDebug.logInfo);
		}
	}

	public static QuestState GetQuestState(string questName)
	{
		return StringToState(CurrentQuestState(questName));
	}

	public static string CurrentQuestState(string questName)
	{
		if (CurrentQuestStateOverride != null)
		{
			return CurrentQuestStateOverride(questName);
		}
		return DefaultCurrentQuestState(questName);
	}

	public static string DefaultCurrentQuestState(string questName)
	{
		return DialogueLua.GetQuestField(questName, "State").asString;
	}

	public static void SetQuestState(string questName, QuestState state)
	{
		SetQuestState(questName, StateToString(state));
	}

	public static void SetQuestState(string questName, string state)
	{
		if (SetQuestStateOverride != null)
		{
			SetQuestStateOverride(questName, state);
		}
		else
		{
			DefaultSetQuestState(questName, state);
		}
	}

	public static void DefaultSetQuestState(string questName, string state)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, "State", state);
			SendUpdateTracker();
			InformQuestStateChange(questName);
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Quest '" + questName + "' doesn't exist. Can't set state to " + state);
		}
	}

	private static void SendUpdateTracker()
	{
		DialogueManager.SendUpdateTracker();
	}

	public static void InformQuestStateChange(string questName)
	{
		DialogueManager.instance.BroadcastMessage("OnQuestStateChange", questName, SendMessageOptions.DontRequireReceiver);
	}

	public static void InformQuestEntryStateChange(string questName, int entryNumber)
	{
		DialogueManager.instance.BroadcastMessage("OnQuestEntryStateChange", new QuestEntryArgs(questName, entryNumber), SendMessageOptions.DontRequireReceiver);
	}

	public static bool IsQuestUnassigned(string questName)
	{
		return GetQuestState(questName) == QuestState.Unassigned;
	}

	public static bool IsQuestActive(string questName)
	{
		return GetQuestState(questName) == QuestState.Active;
	}

	public static bool IsQuestSuccessful(string questName)
	{
		return GetQuestState(questName) == QuestState.Success;
	}

	public static bool IsQuestFailed(string questName)
	{
		return GetQuestState(questName) == QuestState.Failure;
	}

	public static bool IsQuestAbandoned(string questName)
	{
		return GetQuestState(questName) == QuestState.Abandoned;
	}

	public static bool IsQuestDone(string questName)
	{
		QuestState questState = GetQuestState(questName);
		if (questState != QuestState.Success)
		{
			return questState == QuestState.Failure;
		}
		return true;
	}

	public static bool IsQuestInStateMask(string questName, QuestState stateMask)
	{
		QuestState questState = GetQuestState(questName);
		return (stateMask & questState) == questState;
	}

	public static bool IsQuestEntryInStateMask(string questName, int entryNumber, QuestState stateMask)
	{
		QuestState questEntryState = GetQuestEntryState(questName, entryNumber);
		return (stateMask & questEntryState) == questEntryState;
	}

	public static void StartQuest(string questName)
	{
		SetQuestState(questName, QuestState.Active);
	}

	public static void CompleteQuest(string questName)
	{
		SetQuestState(questName, QuestState.Success);
	}

	public static void FailQuest(string questName)
	{
		SetQuestState(questName, QuestState.Failure);
	}

	public static void AbandonQuest(string questName)
	{
		SetQuestState(questName, QuestState.Abandoned);
	}

	public static QuestState DefaultStringToState(string s)
	{
		if (string.Equals(s, "active"))
		{
			return QuestState.Active;
		}
		if (string.Equals(s, "success") || string.Equals(s, "done"))
		{
			return QuestState.Success;
		}
		if (string.Equals(s, "failure"))
		{
			return QuestState.Failure;
		}
		if (string.Equals(s, "abandoned"))
		{
			return QuestState.Abandoned;
		}
		if (string.Equals(s, "grantable"))
		{
			return QuestState.Grantable;
		}
		if (string.Equals(s, "returnToNPC"))
		{
			return QuestState.ReturnToNPC;
		}
		return QuestState.Unassigned;
	}

	public static string DefaultStateToString(QuestState state)
	{
		return state switch
		{
			QuestState.Active => "active", 
			QuestState.Success => "success", 
			QuestState.Failure => "failure", 
			QuestState.Abandoned => "abandoned", 
			QuestState.Grantable => "grantable", 
			QuestState.ReturnToNPC => "returnToNPC", 
			_ => "unassigned", 
		};
	}

	public static string GetQuestTitle(string questName)
	{
		string asString = DialogueLua.GetLocalizedQuestField(questName, "Display Name").asString;
		if (string.IsNullOrEmpty(asString))
		{
			asString = DialogueLua.GetLocalizedQuestField(questName, "Name").asString;
		}
		return asString;
	}

	public static string GetQuestDescription(string questName)
	{
		return GetQuestState(questName) switch
		{
			QuestState.Success => GetQuestDescription(questName, QuestState.Success) ?? GetQuestDescription(questName, QuestState.Active), 
			QuestState.Failure => GetQuestDescription(questName, QuestState.Failure) ?? GetQuestDescription(questName, QuestState.Active), 
			_ => GetQuestDescription(questName, QuestState.Active), 
		};
	}

	public static string GetQuestDescription(string questName, QuestState state)
	{
		string defaultDescriptionFieldForState = GetDefaultDescriptionFieldForState(state);
		string asString = DialogueLua.GetLocalizedQuestField(questName, defaultDescriptionFieldForState).asString;
		if (!string.Equals(asString, "nil") && !string.IsNullOrEmpty(asString))
		{
			return asString;
		}
		return null;
	}

	private static string GetDefaultDescriptionFieldForState(QuestState state)
	{
		return state switch
		{
			QuestState.Success => "Success_Description", 
			QuestState.Failure => "Failure_Description", 
			_ => "Description", 
		};
	}

	public static void SetQuestDescription(string questName, QuestState state, string description)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, GetDefaultDescriptionFieldForState(state), description);
		}
	}

	public static string GetQuestAbandonSequence(string questName)
	{
		return DialogueLua.GetLocalizedQuestField(questName, "Abandon Sequence").asString;
	}

	public static void SetQuestAbandonSequence(string questName, string sequence)
	{
		DialogueLua.SetLocalizedQuestField(questName, "Abandon Sequence", sequence);
	}

	public static int GetQuestEntryCount(string questName)
	{
		return DialogueLua.GetQuestField(questName, "Entry_Count").asInt;
	}

	public static void AddQuestEntry(string questName, string description)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			int questEntryCount = GetQuestEntryCount(questName);
			questEntryCount++;
			DialogueLua.SetQuestField(questName, "Entry_Count", questEntryCount);
			string entryFieldName = GetEntryFieldName(questEntryCount);
			DialogueLua.SetQuestField(questName, entryFieldName, DialogueLua.DoubleQuotesToSingle(description));
			string entryStateFieldName = GetEntryStateFieldName(questEntryCount);
			DialogueLua.SetQuestField(questName, entryStateFieldName, "unassigned");
		}
	}

	public static string GetQuestEntry(string questName, int entryNumber)
	{
		QuestState questEntryState = GetQuestEntryState(questName, entryNumber);
		string text = GetEntryFieldName(entryNumber);
		if (questEntryState == QuestState.Success && DialogueLua.DoesTableElementExist("Quest", text + " Success"))
		{
			text += " Success";
		}
		else if (questEntryState == QuestState.Failure && DialogueLua.DoesTableElementExist("Quest", text + " Failure"))
		{
			text += " Failure";
		}
		return DialogueLua.GetLocalizedQuestField(questName, text).asString;
	}

	public static void SetQuestEntry(string questName, int entryNumber, string description)
	{
		string entryFieldName = GetEntryFieldName(entryNumber);
		DialogueLua.SetLocalizedQuestField(questName, entryFieldName, DialogueLua.DoubleQuotesToSingle(description));
	}

	public static QuestState GetQuestEntryState(string questName, int entryNumber)
	{
		return StringToState(CurrentQuestEntryState(questName, entryNumber));
	}

	public static string CurrentQuestEntryState(string questName, double entryNumber)
	{
		if (CurrentQuestEntryStateOverride != null)
		{
			return CurrentQuestEntryStateOverride(questName, (int)entryNumber);
		}
		return DefaultCurrentQuestEntryState(questName, (int)entryNumber);
	}

	public static string DefaultCurrentQuestEntryState(string questName, int entryNumber)
	{
		return DialogueLua.GetQuestField(questName, GetEntryStateFieldName(entryNumber)).asString;
	}

	public static void SetQuestEntryState(string questName, int entryNumber, QuestState state)
	{
		SetQuestEntryState(questName, entryNumber, StateToString(state));
	}

	public static void SetQuestEntryState(string questName, double entryNumber, string state)
	{
		if (SetQuestEntryStateOverride != null)
		{
			SetQuestEntryStateOverride(questName, (int)entryNumber, state);
		}
		else
		{
			DefaultSetQuestEntryState(questName, (int)entryNumber, state);
		}
	}

	public static void DefaultSetQuestEntryState(string questName, int entryNumber, string state)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, GetEntryStateFieldName(entryNumber), state);
			InformQuestStateChange(questName);
			InformQuestEntryStateChange(questName, entryNumber);
			SendUpdateTracker();
		}
		else if (DialogueDebug.logWarnings)
		{
			string[] obj = new string[6] { "Dialogue System: Quest '", questName, "' doesn't exist. Can't set entry ", null, null, null };
			int num = entryNumber;
			obj[3] = num.ToString();
			obj[4] = " state to ";
			obj[5] = state;
			Debug.LogWarning(string.Concat(obj));
		}
	}

	public static string GetEntryFieldName(int entryNumber)
	{
		return $"Entry_{entryNumber}";
	}

	public static string GetEntryStateFieldName(int entryNumber)
	{
		return $"Entry_{entryNumber}_State";
	}

	public static bool IsQuestTrackingAvailable(string questName)
	{
		return DialogueLua.GetQuestField(questName, "Trackable").asBool;
	}

	public static void SetQuestTrackingAvailable(string questName, bool value)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, "Trackable", value);
			SendUpdateTracker();
		}
	}

	public static bool IsQuestTrackingEnabled(string questName)
	{
		if (!IsQuestTrackingAvailable(questName))
		{
			return false;
		}
		return DialogueLua.GetQuestField(questName, "Track").asBool;
	}

	public static void SetQuestTracking(string questName, bool value)
	{
		if (!DialogueLua.DoesTableElementExist("Quest", questName))
		{
			return;
		}
		if (value)
		{
			if (trackOneQuestAtATime)
			{
				string[] allQuests = GetAllQuests();
				foreach (string text in allQuests)
				{
					if (!string.Equals(text, questName) && IsQuestTrackingEnabled(text))
					{
						DialogueLua.SetQuestField(text, "Track", false);
						DialogueManager.instance.BroadcastMessage("OnQuestTrackingDisabled", text, SendMessageOptions.DontRequireReceiver);
					}
				}
			}
			if (!IsQuestTrackingAvailable(questName))
			{
				SetQuestTrackingAvailable(questName, value: true);
			}
		}
		DialogueLua.SetQuestField(questName, "Track", value);
		SendUpdateTracker();
		DialogueManager.instance.BroadcastMessage(value ? "OnQuestTrackingEnabled" : "OnQuestTrackingDisabled", questName, SendMessageOptions.DontRequireReceiver);
	}

	public static bool IsQuestAbandonable(string questName)
	{
		return DialogueLua.GetQuestField(questName, "Abandonable").asBool;
	}

	public static bool IsQuestVisible(string questName)
	{
		string asString = Lua.Run("return Quest[" + DialogueLua.StringToTableIndex(questName) + "].Visible").asString;
		if (string.IsNullOrEmpty(asString) || string.Equals(asString, "nil"))
		{
			return true;
		}
		return string.Compare(asString, "false", ignoreCase: true) == 0;
	}

	public static void SetQuestVisibility(string questName)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, "Visible", true);
		}
	}

	public static bool WasQuestViewed(string questName)
	{
		return DialogueLua.GetQuestField(questName, "Viewed").asBool;
	}

	public static void MarkQuestViewed(string questName)
	{
		if (DialogueLua.DoesTableElementExist("Quest", questName))
		{
			DialogueLua.SetQuestField(questName, "Viewed", true);
		}
	}

	public static string GetQuestGroup(string questName)
	{
		return DialogueLua.GetLocalizedQuestField(questName, "Group").asString;
	}

	public static string GetQuestGroupDisplayName(string questName)
	{
		string text = DialogueLua.GetLocalizedQuestField(questName, "Group Display Name").asString;
		if (string.IsNullOrEmpty(text) || text == "nil")
		{
			text = GetQuestGroup(questName);
		}
		return text;
	}

	public static string[] GetAllGroups()
	{
		return GetAllGroups(QuestState.Active, sortByGroupName: true);
	}

	public static string[] GetAllGroups(QuestState flags)
	{
		return GetAllGroups(flags, sortByGroupName: true);
	}

	public static string[] GetAllGroups(QuestState flags, bool sortByGroupName)
	{
		List<string> list = new List<string>();
		LuaTableWrapper asTable = Lua.Run("return Item").asTable;
		if (!asTable.isValid)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Quest Log couldn't access Lua Item[] table. Has the Dialogue Manager loaded a database yet?", new object[1] { "Dialogue System" }));
			}
			return list.ToArray();
		}
		foreach (object value in asTable.values)
		{
			if (!(value is LuaTableWrapper luaTableWrapper))
			{
				continue;
			}
			string questName = null;
			string item = null;
			bool flag = false;
			try
			{
				object obj = luaTableWrapper["Name"];
				questName = ((obj != null) ? obj.ToString() : string.Empty);
				object obj2 = luaTableWrapper["Group"];
				item = ((obj2 != null) ? obj2.ToString() : string.Empty);
				flag = false;
				object obj3 = luaTableWrapper["Is_Item"];
				if (obj3 != null)
				{
					flag = ((!(obj3.GetType() == typeof(bool))) ? Tools.StringToBool(obj3.ToString()) : ((bool)obj3));
				}
			}
			catch
			{
			}
			if (!flag && !list.Contains(item) && IsQuestInStateMask(questName, flags))
			{
				list.Add(item);
			}
		}
		if (sortByGroupName)
		{
			list.Sort();
		}
		return list.ToArray();
	}

	public static string[] GetAllQuests()
	{
		return GetAllQuests(QuestState.Active, sortByName: true, null);
	}

	public static string[] GetAllQuests(QuestState flags)
	{
		return GetAllQuests(flags, sortByName: true, null);
	}

	public static string[] GetAllQuests(QuestState flags, bool sortByName)
	{
		return GetAllQuests(flags, sortByName, null);
	}

	public static string[] GetAllQuests(QuestState flags, bool sortByName, string group)
	{
		List<string> list = new List<string>();
		LuaTableWrapper asTable = Lua.Run("return Item").asTable;
		if (!asTable.isValid)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Quest Log couldn't access Lua Item[] table. Has the Dialogue Manager loaded a database yet?", new object[1] { "Dialogue System" }));
			}
			return list.ToArray();
		}
		bool flag = group != null;
		foreach (object value in asTable.values)
		{
			if (!(value is LuaTableWrapper luaTableWrapper))
			{
				continue;
			}
			string text = null;
			string b = null;
			bool flag2 = false;
			try
			{
				object obj = luaTableWrapper["Name"];
				text = ((obj != null) ? obj.ToString() : string.Empty);
				if (flag)
				{
					object obj2 = luaTableWrapper["Group"];
					b = ((obj2 != null) ? obj2.ToString() : string.Empty);
				}
				flag2 = false;
				object obj3 = luaTableWrapper["Is_Item"];
				if (obj3 != null)
				{
					flag2 = ((!(obj3.GetType() == typeof(bool))) ? Tools.StringToBool(obj3.ToString()) : ((bool)obj3));
				}
			}
			catch
			{
			}
			if (flag2)
			{
				continue;
			}
			if (string.IsNullOrEmpty(text))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: A quest name (item name in Item[] table) is null or empty", new object[1] { "Dialogue System" }));
				}
			}
			else if ((!flag || string.Equals(group, b)) && IsQuestInStateMask(text, flags))
			{
				list.Add(text);
			}
		}
		if (sortByName)
		{
			list.Sort();
		}
		return list.ToArray();
	}

	public static QuestGroupRecord[] GetAllGroupsAndQuests(QuestState flags, bool sort = true)
	{
		List<QuestGroupRecord> list = new List<QuestGroupRecord>();
		LuaTableWrapper asTable = Lua.Run("return Item").asTable;
		if (!asTable.isValid)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Quest Log couldn't access Lua Item[] table. Has the Dialogue Manager loaded a database yet?", new object[1] { "Dialogue System" }));
			}
			return list.ToArray();
		}
		foreach (object value in asTable.values)
		{
			if (!(value is LuaTableWrapper luaTableWrapper))
			{
				continue;
			}
			string text = null;
			string groupName = null;
			bool flag = false;
			try
			{
				object obj = luaTableWrapper["Name"];
				text = ((obj != null) ? obj.ToString() : string.Empty);
				object obj2 = luaTableWrapper["Group"];
				groupName = ((obj2 != null) ? obj2.ToString() : string.Empty);
				flag = false;
				object obj3 = luaTableWrapper["Is_Item"];
				if (obj3 != null)
				{
					flag = ((!(obj3.GetType() == typeof(bool))) ? Tools.StringToBool(obj3.ToString()) : ((bool)obj3));
				}
			}
			catch
			{
			}
			if (flag)
			{
				continue;
			}
			if (string.IsNullOrEmpty(text))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: A quest name (item name in Item[] table) is null or empty", new object[1] { "Dialogue System" }));
				}
			}
			else if (IsQuestInStateMask(text, flags))
			{
				list.Add(new QuestGroupRecord(groupName, text));
			}
		}
		if (sort)
		{
			list.Sort();
		}
		return list.ToArray();
	}

	public static void AddQuestStateObserver(string questName, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
	{
		questWatchList.Add(new QuestWatchItem(questName, frequency, questChangedHandler));
	}

	public static void AddQuestStateObserver(string questName, int entryNumber, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
	{
		questWatchList.Add(new QuestWatchItem(questName, entryNumber, frequency, questChangedHandler));
	}

	public static void RemoveQuestStateObserver(string questName, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
	{
		foreach (QuestWatchItem questWatch in questWatchList)
		{
			if (questWatch.Matches(questName, frequency, questChangedHandler))
			{
				questWatch.StopObserving();
			}
		}
		questWatchList.RemoveAll((QuestWatchItem questWatchItem) => questWatchItem.Matches(questName, frequency, questChangedHandler));
	}

	public static void RemoveQuestStateObserver(string questName, int entryNumber, LuaWatchFrequency frequency, QuestChangedDelegate questChangedHandler)
	{
		foreach (QuestWatchItem questWatch in questWatchList)
		{
			if (questWatch.Matches(questName, entryNumber, frequency, questChangedHandler))
			{
				questWatch.StopObserving();
			}
		}
		questWatchList.RemoveAll((QuestWatchItem questWatchItem) => questWatchItem.Matches(questName, entryNumber, frequency, questChangedHandler));
	}

	public static void RemoveAllQuestStateObservers()
	{
		foreach (QuestWatchItem questWatch in questWatchList)
		{
			questWatch.StopObserving();
		}
		questWatchList.Clear();
	}

	public static void UpdateQuestIndicators(string questName)
	{
		QuestStateDispatcher questStateDispatcher = GameObjectUtility.FindFirstObjectByType<QuestStateDispatcher>();
		if (questStateDispatcher != null)
		{
			questStateDispatcher.OnQuestStateChange(questName);
		}
	}
}
