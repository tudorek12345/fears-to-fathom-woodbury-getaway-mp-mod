using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class DialogueDatabase : ScriptableObject
{
	[Serializable]
	public class SyncInfo
	{
		public bool syncActors;

		public bool syncItems;

		public bool syncLocations;

		public bool syncVariables;

		public DialogueDatabase syncActorsDatabase;

		public DialogueDatabase syncItemsDatabase;

		public DialogueDatabase syncLocationsDatabase;

		public DialogueDatabase syncVariablesDatabase;
	}

	public delegate string GetCustomEntrytagDelegate(Conversation conversation, DialogueEntry entry);

	public string version;

	public string author;

	public string description;

	public string globalUserScript;

	public const int NumEmphasisSettings = 4;

	public EmphasisSetting[] emphasisSettings = new EmphasisSetting[4];

	public int baseID = 1;

	public List<Actor> actors = new List<Actor>();

	public List<Item> items = new List<Item>();

	public List<Location> locations = new List<Location>();

	public List<Variable> variables = new List<Variable>();

	public List<Conversation> conversations = new List<Conversation>();

	public SyncInfo syncInfo = new SyncInfo();

	public string templateJson = string.Empty;

	private Dictionary<string, Actor> actorNameCache;

	private Dictionary<string, Item> itemNameCache;

	private Dictionary<string, Location> locationNameCache;

	private Dictionary<string, Variable> variableNameCache;

	private Dictionary<string, Conversation> conversationTitleCache;

	public static GetCustomEntrytagDelegate getCustomEntrytag = null;

	public const string InvalidEntrytag = "invalid_entrytag";

	public const string VoiceOverFileFieldName = "VoiceOverFile";

	private static Regex entrytagRegex = new Regex(string.Format("[{0}]", Regex.Escape(" " + new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))));

	private static Regex actorNameLineNumberEntrytagRegex = new Regex("[,\"\t\r\n\\/<>?]");

	public int playerID => actors.Find((Actor a) => a.IsPlayer)?.id ?? 0;

	public void ResetEmphasisSettings()
	{
		emphasisSettings[0] = new EmphasisSetting(Color.white, bold: false, italic: false, underline: false);
		emphasisSettings[1] = new EmphasisSetting(Color.red, bold: false, italic: false, underline: false);
		emphasisSettings[2] = new EmphasisSetting(Color.green, bold: false, italic: false, underline: false);
		emphasisSettings[3] = new EmphasisSetting(Color.blue, bold: false, italic: false, underline: false);
	}

	public bool IsPlayerID(int actorID)
	{
		return actors.Find((Actor a) => a.id == actorID)?.IsPlayer ?? false;
	}

	public bool IsPlayer(string actorName)
	{
		return GetActor(actorName)?.IsPlayer ?? false;
	}

	public CharacterType GetCharacterType(int actorID)
	{
		if (!IsPlayerID(actorID))
		{
			return CharacterType.NPC;
		}
		return CharacterType.PC;
	}

	private void SetupCaches()
	{
		if (actorNameCache == null)
		{
			actorNameCache = CreateCache(actors);
		}
		if (itemNameCache == null)
		{
			itemNameCache = CreateCache(items);
		}
		if (locationNameCache == null)
		{
			locationNameCache = CreateCache(locations);
		}
		if (variableNameCache == null)
		{
			variableNameCache = CreateCache(variables);
		}
		if (conversationTitleCache == null)
		{
			conversationTitleCache = CreateCache(conversations);
		}
	}

	private Dictionary<string, T> CreateCache<T>(List<T> assets) where T : Asset
	{
		bool flag = typeof(T) == typeof(Conversation);
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		if (Application.isPlaying)
		{
			for (int i = 0; i < assets.Count; i++)
			{
				T val = assets[i];
				string key = (flag ? (val as Conversation).Title : val.Name);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, val);
				}
			}
		}
		return dictionary;
	}

	public void ResetCache()
	{
		actorNameCache = null;
		itemNameCache = null;
		locationNameCache = null;
		variableNameCache = null;
		conversationTitleCache = null;
	}

	public Actor GetActor(string actorName)
	{
		if (string.IsNullOrEmpty(actorName))
		{
			return null;
		}
		SetupCaches();
		if (!actorNameCache.ContainsKey(actorName))
		{
			return actors.Find((Actor a) => string.Equals(a.Name, actorName));
		}
		return actorNameCache[actorName];
	}

	public Actor GetActor(int id)
	{
		return actors.Find((Actor a) => a.id == id);
	}

	public Item GetItem(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return null;
		}
		SetupCaches();
		if (!itemNameCache.ContainsKey(itemName))
		{
			return items.Find((Item i) => string.Equals(i.Name, itemName));
		}
		return itemNameCache[itemName];
	}

	public Item GetItem(int id)
	{
		return items.Find((Item i) => i.id == id);
	}

	public Location GetLocation(string locationName)
	{
		if (string.IsNullOrEmpty(locationName))
		{
			return null;
		}
		SetupCaches();
		if (!locationNameCache.ContainsKey(locationName))
		{
			return locations.Find((Location l) => string.Equals(l.Name, locationName));
		}
		return locationNameCache[locationName];
	}

	public Location GetLocation(int id)
	{
		return locations.Find((Location l) => l.id == id);
	}

	public Variable GetVariable(string variableName)
	{
		if (string.IsNullOrEmpty(variableName))
		{
			return null;
		}
		SetupCaches();
		if (!variableNameCache.ContainsKey(variableName))
		{
			return variables.Find((Variable v) => string.Equals(v.Name, variableName));
		}
		return variableNameCache[variableName];
	}

	public Variable GetVariable(int id)
	{
		return variables.Find((Variable v) => v.id == id);
	}

	public void AddConversation(Conversation conversation)
	{
		SetupCaches();
		string title = conversation.Title;
		if (!conversationTitleCache.ContainsKey(title))
		{
			conversationTitleCache.Add(title, conversation);
		}
		conversations.Add(conversation);
		LinkUtility.SortOutgoingLinks(this, conversation);
	}

	public Conversation GetConversation(string conversationTitle)
	{
		if (string.IsNullOrEmpty(conversationTitle))
		{
			return null;
		}
		SetupCaches();
		if (!conversationTitleCache.ContainsKey(conversationTitle))
		{
			return conversations.Find((Conversation c) => string.Equals(c.Title, conversationTitle));
		}
		return conversationTitleCache[conversationTitle];
	}

	public Conversation GetConversation(int conversationID)
	{
		return conversations.Find((Conversation c) => c.id == conversationID);
	}

	public DialogueEntry GetDialogueEntry(int conversationID, int dialogueEntryID)
	{
		return GetConversation(conversationID)?.GetDialogueEntry(dialogueEntryID);
	}

	public DialogueEntry GetDialogueEntry(Link link)
	{
		if (link != null)
		{
			Conversation conversation = GetConversation(link.destinationConversationID);
			if (conversation != null && conversation.dialogueEntries != null)
			{
				return conversation.dialogueEntries.Find((DialogueEntry e) => e.id == link.destinationDialogueID);
			}
		}
		return null;
	}

	public string GetLinkText(Link link)
	{
		DialogueEntry dialogueEntry = GetDialogueEntry(link);
		if (dialogueEntry != null)
		{
			return dialogueEntry.responseButtonText;
		}
		return string.Empty;
	}

	public void Add(DialogueDatabase database)
	{
		if (database != null)
		{
			AddEmphasisSettings(database.emphasisSettings);
			AddGlobalUserScript(database);
			SetupCaches();
			AddAssets(actors, database.actors, actorNameCache);
			AddAssets(items, database.items, itemNameCache);
			AddAssets(locations, database.locations, locationNameCache);
			AddAssets(variables, database.variables, variableNameCache);
			AddAssets(conversations, database.conversations, conversationTitleCache);
		}
	}

	private void AddAssets<T>(List<T> myAssets, List<T> assetsToAdd, Dictionary<string, T> cache) where T : Asset
	{
		bool flag = typeof(T) == typeof(Conversation);
		for (int i = 0; i < assetsToAdd.Count; i++)
		{
			T val = assetsToAdd[i];
			if (val == null)
			{
				continue;
			}
			string text = (flag ? (val as Conversation).Title : val.Name);
			if (text == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: A " + typeof(T).Name + " has an invalid name.");
				}
			}
			else if (!cache.ContainsKey(text))
			{
				cache.Add(text, val);
				myAssets.Add(val);
			}
		}
	}

	private void AddEmphasisSettings(EmphasisSetting[] newEmphasisSettings)
	{
		if (emphasisSettings == null || emphasisSettings.Length < 4)
		{
			emphasisSettings = newEmphasisSettings;
		}
		else
		{
			if (newEmphasisSettings == null)
			{
				return;
			}
			emphasisSettings = new EmphasisSetting[newEmphasisSettings.Length];
			for (int i = 0; i < newEmphasisSettings.Length; i++)
			{
				if (emphasisSettings[i] == null || emphasisSettings[i].IsEmpty)
				{
					emphasisSettings[i] = newEmphasisSettings[i];
				}
			}
		}
	}

	private void AddGlobalUserScript(DialogueDatabase database)
	{
		if (!string.IsNullOrEmpty(globalUserScript))
		{
			if (!string.IsNullOrEmpty(database.globalUserScript))
			{
				globalUserScript = string.Format("{0}; {1}", new object[2] { globalUserScript, database.globalUserScript });
			}
		}
		else if (!string.IsNullOrEmpty(database.globalUserScript))
		{
			globalUserScript = database.globalUserScript;
		}
	}

	public void Remove(DialogueDatabase database)
	{
		if (database != null)
		{
			SetupCaches();
			RemoveAssets(actors, database.actors, actorNameCache);
			RemoveAssets(items, database.items, itemNameCache);
			RemoveAssets(locations, database.locations, locationNameCache);
			RemoveAssets(variables, database.variables, variableNameCache);
			RemoveAssets(conversations, database.conversations, conversationTitleCache);
		}
	}

	public void Remove(DialogueDatabase database, List<DialogueDatabase> keep)
	{
		if (database != null)
		{
			SetupCaches();
			RemoveAssets(actors, database.actors, actorNameCache, keep);
			RemoveAssets(items, database.items, itemNameCache, keep);
			RemoveAssets(locations, database.locations, locationNameCache, keep);
			RemoveAssets(variables, database.variables, variableNameCache, keep);
			RemoveAssets(conversations, database.conversations, conversationTitleCache, keep);
		}
	}

	private void RemoveAssets<T>(List<T> myAssets, List<T> assetsToRemove, Dictionary<string, T> cache) where T : Asset
	{
		bool flag = typeof(T) == typeof(Conversation);
		for (int i = 0; i < assetsToRemove.Count; i++)
		{
			T val = assetsToRemove[i];
			string key = (flag ? (val as Conversation).Title : val.Name);
			if (cache.ContainsKey(key))
			{
				myAssets.Remove(cache[key]);
				cache.Remove(key);
			}
		}
	}

	private void RemoveAssets<T>(List<T> myAssets, List<T> assetsToRemove, Dictionary<string, T> cache, List<DialogueDatabase> keep) where T : Asset
	{
		bool flag = typeof(T) == typeof(Conversation);
		for (int i = 0; i < assetsToRemove.Count; i++)
		{
			T val = assetsToRemove[i];
			if (!Contains(keep, val))
			{
				string key = (flag ? (val as Conversation).Title : val.Name);
				if (cache.ContainsKey(key))
				{
					myAssets.Remove(cache[key]);
					cache.Remove(key);
				}
			}
		}
	}

	public void Clear()
	{
		actors.Clear();
		items.Clear();
		locations.Clear();
		variables.Clear();
		conversations.Clear();
		ResetCache();
	}

	public void SyncAll()
	{
		SyncActors();
		SyncItems();
		SyncLocations();
		SyncVariables();
	}

	public void SyncActors()
	{
		ResetCache();
		if (!syncInfo.syncActors || syncInfo.syncActorsDatabase == null || syncInfo.syncActorsDatabase == this)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (Actor actor2 in syncInfo.syncActorsDatabase.actors)
		{
			Actor actor = GetActor(actor2.id);
			if (actor != null && actor.Name != actor2.Name)
			{
				dictionary.Add(DialogueLua.StringToTableIndex(actor.Name), DialogueLua.StringToTableIndex(actor2.Name));
			}
		}
		actors.RemoveAll((Actor x) => syncInfo.syncActorsDatabase.GetActor(x.id) != null);
		for (int num = 0; num < syncInfo.syncActorsDatabase.actors.Count; num++)
		{
			actors.Insert(num, new Actor(syncInfo.syncActorsDatabase.actors[num]));
		}
		foreach (Conversation conversation in conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string key = item.Key;
					string value = item.Value;
					if (!string.IsNullOrEmpty(dialogueEntry.conditionsString) && dialogueEntry.conditionsString.Contains(key))
					{
						dialogueEntry.conditionsString = ReplaceLuaIndex(dialogueEntry.conditionsString, "Actor", key, value);
					}
					if (!string.IsNullOrEmpty(dialogueEntry.userScript) && dialogueEntry.userScript.Contains(key))
					{
						dialogueEntry.userScript = ReplaceLuaIndex(dialogueEntry.userScript, "Actor", key, value);
					}
				}
			}
		}
	}

	public void SyncItems()
	{
		ResetCache();
		if (!syncInfo.syncItems || syncInfo.syncItemsDatabase == null || syncInfo.syncItemsDatabase == this)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (Item item2 in syncInfo.syncItemsDatabase.items)
		{
			Item item = GetItem(item2.id);
			if (item != null && item.Name != item2.Name)
			{
				dictionary.Add(DialogueLua.StringToTableIndex(item.Name), DialogueLua.StringToTableIndex(item2.Name));
			}
		}
		items.RemoveAll((Item x) => syncInfo.syncItemsDatabase.GetItem(x.id) != null);
		for (int num = 0; num < syncInfo.syncItemsDatabase.items.Count; num++)
		{
			items.Insert(num, new Item(syncInfo.syncItemsDatabase.items[num]));
		}
		foreach (Conversation conversation in conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				foreach (KeyValuePair<string, string> item3 in dictionary)
				{
					string key = item3.Key;
					string value = item3.Value;
					if (!string.IsNullOrEmpty(dialogueEntry.conditionsString) && dialogueEntry.conditionsString.Contains(key))
					{
						dialogueEntry.conditionsString = ReplaceLuaIndex(dialogueEntry.conditionsString, "Item", key, value);
					}
					if (!string.IsNullOrEmpty(dialogueEntry.userScript) && dialogueEntry.userScript.Contains(key))
					{
						dialogueEntry.userScript = ReplaceLuaIndex(dialogueEntry.userScript, "Item", key, value);
					}
				}
			}
		}
	}

	public void SyncLocations()
	{
		ResetCache();
		if (!syncInfo.syncLocations || syncInfo.syncLocationsDatabase == null || syncInfo.syncLocationsDatabase == this)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (Location location2 in syncInfo.syncLocationsDatabase.locations)
		{
			Location location = GetLocation(location2.id);
			if (location != null && location.Name != location2.Name)
			{
				dictionary.Add(DialogueLua.StringToTableIndex(location.Name), DialogueLua.StringToTableIndex(location2.Name));
			}
		}
		locations.RemoveAll((Location x) => syncInfo.syncLocationsDatabase.GetLocation(x.id) != null);
		for (int num = 0; num < syncInfo.syncLocationsDatabase.locations.Count; num++)
		{
			locations.Insert(num, new Location(syncInfo.syncLocationsDatabase.locations[num]));
		}
		foreach (Conversation conversation in conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string key = item.Key;
					string value = item.Value;
					if (!string.IsNullOrEmpty(dialogueEntry.conditionsString) && dialogueEntry.conditionsString.Contains(key))
					{
						dialogueEntry.conditionsString = ReplaceLuaIndex(dialogueEntry.conditionsString, "Location", key, value);
					}
					if (!string.IsNullOrEmpty(dialogueEntry.userScript) && dialogueEntry.userScript.Contains(key))
					{
						dialogueEntry.userScript = ReplaceLuaIndex(dialogueEntry.userScript, "Location", key, value);
					}
				}
			}
		}
	}

	public void SyncVariables()
	{
		ResetCache();
		if (!syncInfo.syncVariables || syncInfo.syncVariablesDatabase == null || syncInfo.syncVariablesDatabase == this)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (Variable variable2 in syncInfo.syncVariablesDatabase.variables)
		{
			Variable variable = GetVariable(variable2.id);
			if (variable != null && variable.Name != variable2.Name)
			{
				dictionary.Add(DialogueLua.StringToTableIndex(variable.Name), DialogueLua.StringToTableIndex(variable2.Name));
			}
		}
		variables.RemoveAll((Variable x) => syncInfo.syncVariablesDatabase.GetVariable(x.id) != null);
		for (int num = 0; num < syncInfo.syncVariablesDatabase.variables.Count; num++)
		{
			variables.Insert(num, new Variable(syncInfo.syncVariablesDatabase.variables[num]));
		}
		foreach (Conversation conversation in conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string key = item.Key;
					string value = item.Value;
					if (!string.IsNullOrEmpty(dialogueEntry.conditionsString) && dialogueEntry.conditionsString.Contains(key))
					{
						dialogueEntry.conditionsString = ReplaceLuaIndex(dialogueEntry.conditionsString, "Variable", key, value);
					}
					if (!string.IsNullOrEmpty(dialogueEntry.userScript) && dialogueEntry.userScript.Contains(key))
					{
						dialogueEntry.userScript = ReplaceLuaIndex(dialogueEntry.userScript, "Variable", key, value);
					}
				}
			}
		}
	}

	private string ReplaceLuaIndex(string luaCode, string tableName, string oldLuaIndex, string newLuaIndex)
	{
		return luaCode.Replace(tableName + "[\"" + oldLuaIndex + "\"]", tableName + "[\"" + newLuaIndex + "\"]").Replace(tableName + "['" + oldLuaIndex + "']", tableName + "['" + newLuaIndex + "']");
	}

	public static bool ContainsName<T>(List<T> assetList, string assetName) where T : Asset
	{
		if (assetList != null)
		{
			return assetList.Find((T x) => string.Equals(x.Name, assetName)) != null;
		}
		return false;
	}

	public static bool ContainsID<T>(List<T> assetList, int assetID) where T : Asset
	{
		if (assetList != null)
		{
			return assetList.Find((T x) => x.id == assetID) != null;
		}
		return false;
	}

	public static bool ContainsTitle(List<Conversation> conversations, string title)
	{
		if (conversations != null)
		{
			return conversations.Find((Conversation x) => string.Equals(x.Title, title)) != null;
		}
		return false;
	}

	public static bool Contains(DialogueDatabase database, Asset asset)
	{
		if (asset == null)
		{
			return false;
		}
		database.SetupCaches();
		if (asset is Actor)
		{
			return database.actorNameCache.ContainsKey(asset.Name);
		}
		if (asset is Item)
		{
			return database.itemNameCache.ContainsKey(asset.Name);
		}
		if (asset is Location)
		{
			return database.locationNameCache.ContainsKey(asset.Name);
		}
		if (asset is Variable)
		{
			return database.variableNameCache.ContainsKey(asset.Name);
		}
		if (asset is Conversation)
		{
			return database.conversationTitleCache.ContainsKey((asset as Conversation).Title);
		}
		Debug.LogError(string.Format("{0}: Unexpected asset type {1}", new object[2]
		{
			"Dialogue System",
			asset.GetType().Name
		}));
		return false;
	}

	public static bool Contains(List<DialogueDatabase> databaseList, Asset asset)
	{
		foreach (DialogueDatabase database in databaseList)
		{
			if (Contains(database, asset))
			{
				return true;
			}
		}
		return false;
	}

	public string GetEntrytag(Conversation conversation, DialogueEntry entry, EntrytagFormat entrytagFormat)
	{
		if (conversation == null || entry == null)
		{
			return "invalid_entrytag";
		}
		Actor actor = null;
		switch (entrytagFormat)
		{
		case EntrytagFormat.ActorName_ConversationID_EntryID:
			actor = GetActor(entry.ActorID);
			if (actor == null)
			{
				return "invalid_entrytag";
			}
			return string.Format("{0}_{1}_{2}", entrytagRegex.Replace(actor.Name, "_"), conversation.id, entry.id);
		case EntrytagFormat.ConversationTitle_EntryID:
			return string.Format("{0}_{1}", entrytagRegex.Replace(conversation.Title, "_"), entry.id);
		case EntrytagFormat.ActorNameLineNumber:
		{
			actor = GetActor(entry.ActorID);
			if (actor == null)
			{
				return "invalid_entrytag";
			}
			int num = conversation.id * 500 + entry.id;
			return string.Format("{0}{1}", actorNameLineNumberEntrytagRegex.Replace(actor.Name, "_"), num);
		}
		case EntrytagFormat.ConversationID_ActorName_EntryID:
			actor = GetActor(entry.ActorID);
			if (actor == null)
			{
				return "invalid_entrytag";
			}
			return string.Format("{0}_{1}_{2}", conversation.id, entrytagRegex.Replace(actor.Name, "_"), entry.id);
		case EntrytagFormat.ActorName_ConversationTitle_EntryDescriptor:
		{
			actor = GetActor(entry.ActorID);
			if (actor == null)
			{
				return "invalid_entrytag";
			}
			string input = ((!string.IsNullOrEmpty(entry.Title)) ? entry.Title : ((!string.IsNullOrEmpty(entry.currentMenuText)) ? entry.currentMenuText : entry.id.ToString()));
			return string.Format("{0}_{1}_{2}", entrytagRegex.Replace(actor.Name, "_"), entrytagRegex.Replace(conversation.Title, "_"), entrytagRegex.Replace(input, "_"));
		}
		case EntrytagFormat.VoiceOverFile:
		{
			if (entry == null)
			{
				return "invalid_entrytag";
			}
			string text = Field.LookupValue(entry.fields, "VoiceOverFile");
			if (text != null)
			{
				return entrytagRegex.Replace(text, "_");
			}
			return "invalid_entrytag";
		}
		case EntrytagFormat.Title:
		{
			if (entry == null)
			{
				return "invalid_entrytag";
			}
			string text2 = Field.LookupValue(entry.fields, "Title");
			if (text2 != null)
			{
				return entrytagRegex.Replace(text2, "_");
			}
			return "invalid_entrytag";
		}
		case EntrytagFormat.Custom:
			if (getCustomEntrytag == null)
			{
				return "invalid_entrytag";
			}
			return getCustomEntrytag(conversation, entry);
		default:
			return "invalid_entrytag";
		}
	}

	public string GetEntrytag(int conversationID, int dialogueEntryID, EntrytagFormat entrytagFormat)
	{
		Conversation conversation = GetConversation(conversationID);
		DialogueEntry entry = conversation?.GetDialogueEntry(dialogueEntryID);
		return GetEntrytag(conversation, entry, entrytagFormat);
	}

	public string GetEntrytaglocal(Conversation conversation, DialogueEntry entry, EntrytagFormat entrytagFormat)
	{
		return GetEntrytag(conversation, entry, entrytagFormat) + "_" + Localization.language;
	}

	public string GetEntrytaglocal(int conversationID, int dialogueEntryID, EntrytagFormat entrytagFormat)
	{
		Conversation conversation = GetConversation(conversationID);
		DialogueEntry entry = conversation?.GetDialogueEntry(dialogueEntryID);
		return GetEntrytaglocal(conversation, entry, entrytagFormat);
	}
}
