using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

public class ArcweaveImporter
{
	protected enum CodeState
	{
		None,
		InIf,
		InElseIf,
		InElse
	}

	protected enum ContentPieceType
	{
		Text,
		Code
	}

	protected class ContentPiece
	{
		public ContentPieceType type;

		public string text;

		public ContentPiece(ContentPieceType type, string text)
		{
			this.type = type;
			this.text = text;
		}
	}

	public bool merge;

	public string arcweaveProjectPath;

	public string contentJson;

	public List<string> questBoardGuids = new List<string>();

	public List<ArcweaveConversationInfo> conversationInfo = new List<ArcweaveConversationInfo>();

	public List<string> playerComponentGuids = new List<string>();

	public List<string> npcComponentGuids = new List<string>();

	public List<string> itemComponentGuids = new List<string>();

	public List<string> locationComponentGuids = new List<string>();

	public bool boardsFoldout = true;

	public bool componentsFoldout = true;

	public int numPlayers = 1;

	public List<string> globalVariables = new List<string>();

	public bool importPortraits = true;

	public bool importGuids;

	public Template template = Template.FromDefault();

	public DialogueDatabase database;

	public Dictionary<string, string> elementNames = new Dictionary<string, string>();

	public string[] componentNames = new string[0];

	public ArcweaveBoardNode rootBoardNode;

	protected Dictionary<string, ArcweaveType> arcweaveLookup = new Dictionary<string, ArcweaveType>();

	protected Dictionary<string, DialogueEntry> dialogueEntryLookup = new Dictionary<string, DialogueEntry>();

	protected Dictionary<string, Actor> actorLookup = new Dictionary<string, Actor>();

	protected int currentPlayerID;

	protected int currentNpcID;

	protected const string NoneSequence = "None()";

	protected const string ContinueSequence = "Continue()";

	private const string DeleteTag = "$$Delete$$";

	protected static Regex SequenceRegex = new Regex("\\[SEQUENCE:[^\\]]*\\]");

	protected static Regex BlockRegex = new Regex("<p[^>]*>|</p>|<blockquote[^>]*>|</blockquote>|<span[^>]*>|</span>");

	protected static Regex CodeStartRegex = new Regex("<pre[^>]*><code>");

	protected static Regex CodeEndRegex = new Regex("</code></pre>");

	protected static Regex IdentifierRegex = new Regex("(?<![^\\s+,(!*/-])\\w+(?![^\\s)+,*/-])");

	protected static Regex IncrementorRegex = new Regex("\\+=|\\-=");

	protected static Regex VisitsRegex = new Regex("visits\\(<[^\\)]+\\)");

	protected static List<string> ReservedKeywords = new List<string>("if|elseif|else|endif|is|not|and|or|true|false|abs|sqr|sqrt|random|reset|resetAll|roll|show|visits".Split('|'));

	protected static string[] CodeFieldPrefixes = new string[3] { "_IF", "_ELSEIF", "_ELSE" };

	public ArcweaveProject arcweaveProject { get; protected set; }

	public Dictionary<string, string[]> conversationElements { get; protected set; } = new Dictionary<string, string[]>();

	public Dictionary<string, Board> leafBoards { get; protected set; } = new Dictionary<string, Board>();

	public virtual void Setup(string arcweaveProjectPath, string contentJson, List<string> questBoardGuids, List<ArcweaveConversationInfo> conversationInfo, List<string> playerComponentGuids, List<string> npcComponentGuids, List<string> itemComponentGuids, List<string> locationComponentGuids, bool boardsFoldout, bool componentsFoldout, bool importPortraits, bool importGuids, int numPlayers, string globalVariables, bool merge, Template template)
	{
		this.arcweaveProjectPath = arcweaveProjectPath;
		this.contentJson = contentJson;
		this.questBoardGuids = questBoardGuids;
		this.conversationInfo = conversationInfo;
		this.playerComponentGuids = playerComponentGuids;
		this.npcComponentGuids = npcComponentGuids;
		this.itemComponentGuids = itemComponentGuids;
		this.locationComponentGuids = locationComponentGuids;
		this.boardsFoldout = boardsFoldout;
		this.componentsFoldout = componentsFoldout;
		this.importPortraits = importPortraits;
		this.importGuids = importGuids;
		this.numPlayers = numPlayers;
		this.globalVariables = ParseGlobalVariables(globalVariables);
		this.merge = merge;
		this.template = ((template != null) ? template : Template.FromDefault());
	}

	public virtual void Setup(string jsonPrefs, DialogueDatabase database, Template template)
	{
		ArcweaveImportPrefs arcweaveImportPrefs = JsonUtility.FromJson<ArcweaveImportPrefs>(jsonPrefs);
		if (arcweaveImportPrefs != null)
		{
			Setup(arcweaveImportPrefs.arcweaveProjectPath, arcweaveImportPrefs.contentJson, arcweaveImportPrefs.questBoardGuids, arcweaveImportPrefs.conversationInfo, arcweaveImportPrefs.playerComponentGuids, arcweaveImportPrefs.npcComponentGuids, arcweaveImportPrefs.itemComponentGuids, arcweaveImportPrefs.locationComponentGuids, arcweaveImportPrefs.boardsFoldout, arcweaveImportPrefs.componentsFoldout, arcweaveImportPrefs.importPortraits, arcweaveImportPrefs.importGuids, arcweaveImportPrefs.numPlayers, arcweaveImportPrefs.globalVariables, arcweaveImportPrefs.merge, template);
			this.database = database;
		}
	}

	public virtual void Clear()
	{
		arcweaveProject = null;
	}

	protected virtual List<string> ParseGlobalVariables(string s)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(s))
		{
			string[] array = s.Split(',');
			foreach (string text in array)
			{
				list.Add(text.Trim());
			}
		}
		return list;
	}

	public virtual void Import(string jsonPrefs, DialogueDatabase database, Template template)
	{
		Setup(jsonPrefs, database, template);
		LoadAndConvert();
	}

	public virtual void LoadAndConvert()
	{
		LoadJson();
		Convert();
	}

	public virtual bool IsJsonLoaded()
	{
		if (arcweaveProject != null && arcweaveProject.boards != null)
		{
			return arcweaveProject.boards.Count > 0;
		}
		return false;
	}

	public virtual bool LoadJson()
	{
		try
		{
			arcweaveProject = null;
			string text = ((!string.IsNullOrEmpty(arcweaveProjectPath) && arcweaveProjectPath.StartsWith("Assets")) ? arcweaveProjectPath.Substring("Assets".Length) : string.Empty);
			string text2 = Application.dataPath + text + "/project_settings.json";
			string text3 = "";
			if (!string.IsNullOrEmpty(contentJson))
			{
				text3 = contentJson;
			}
			else
			{
				if (!File.Exists(text2))
				{
					Debug.LogError("Dialogue System: Arcweave JSON file '" + text2 + "' doesn't exist.");
					return false;
				}
				text3 = File.ReadAllText(text2);
			}
			if (string.IsNullOrEmpty(text3))
			{
				Debug.LogError("Dialogue System: Unable to read '" + text2 + "'.");
				return false;
			}
			arcweaveProject = JsonConvert.DeserializeObject<ArcweaveProject>(text3);
			if (!IsJsonLoaded())
			{
				Debug.LogError("Dialogue System: Arcweave project '" + text2 + "' is empty or could not be loaded.");
				return false;
			}
			Debug.Log($"Dialogue System: Successfully loaded {text2} containing {arcweaveProject.boards.Count} boards.");
		}
		catch (Exception ex)
		{
			Debug.LogError("Dialogue System: Arcweave Project could not be deserialized: " + ex.Message);
		}
		try
		{
			CatalogAllArcweaveTypes();
			RecordElementNames();
			RecordComponentNames();
			RecordBoardHierarchy();
			SortBoardElements();
			conversationElements.Clear();
			return true;
		}
		catch (Exception ex2)
		{
			Debug.LogError("Dialogue System: Unable to cache element names in deserialized Arcweave Project: " + ex2.Message);
			arcweaveProject = null;
			return false;
		}
	}

	protected virtual void SortBoardElements()
	{
		foreach (Board value in leafBoards.Values)
		{
			value.elements.Sort((string a, string b) => elementNames[a].CompareTo(elementNames[b]));
		}
	}

	protected virtual void RecordBoardHierarchy()
	{
		rootBoardNode = null;
		foreach (KeyValuePair<string, Board> board in arcweaveProject.boards)
		{
			string key = board.Key;
			Board value = board.Value;
			if (value.root)
			{
				rootBoardNode = new ArcweaveBoardNode(key, value, null);
				break;
			}
		}
		if (rootBoardNode == null)
		{
			Debug.LogError("Dialogue System: Can't find root board in Arcweave Project.");
			arcweaveProject = null;
		}
		else
		{
			leafBoards.Clear();
			RecordBoardChildren(rootBoardNode);
		}
	}

	protected virtual void RecordBoardChildren(ArcweaveBoardNode boardNode)
	{
		if (boardNode == null || boardNode.board == null)
		{
			return;
		}
		if (boardNode.board.children == null)
		{
			if (!leafBoards.ContainsKey(boardNode.guid))
			{
				leafBoards.Add(boardNode.guid, boardNode.board);
			}
			return;
		}
		foreach (string child in boardNode.board.children)
		{
			Board board = arcweaveLookup[child] as Board;
			ArcweaveBoardNode arcweaveBoardNode = new ArcweaveBoardNode(child, board, boardNode);
			boardNode.children.Add(arcweaveBoardNode);
			RecordBoardChildren(arcweaveBoardNode);
		}
	}

	protected virtual void RecordElementNames()
	{
		List<KeyValuePair<string, Element>> list = arcweaveProject.elements.ToList();
		list.Sort((KeyValuePair<string, Element> kvp1, KeyValuePair<string, Element> kvp2) => GetElementName(kvp1.Value).CompareTo(GetElementName(kvp2.Value)));
		arcweaveProject.elements.Clear();
		list.ForEach(delegate(KeyValuePair<string, Element> x)
		{
			arcweaveProject.elements.Add(x.Key, x.Value);
		});
		elementNames.Clear();
		foreach (KeyValuePair<string, Element> element in arcweaveProject.elements)
		{
			string elementName = GetElementName(element.Value);
			elementNames[element.Key] = elementName;
		}
	}

	protected string GetElementName(Element element)
	{
		if (element == null)
		{
			return string.Empty;
		}
		string text = TouchUpRichText(element.title);
		if (string.IsNullOrEmpty(text))
		{
			text = TouchUpRichText(element.content);
		}
		return text.Replace("/", "∕");
	}

	protected virtual void RecordComponentNames()
	{
		List<string> list = new List<string>();
		list.Add("Player");
		List<KeyValuePair<string, ArcweaveComponent>> list2 = arcweaveProject.components.ToList();
		list2.Sort((KeyValuePair<string, ArcweaveComponent> kvp1, KeyValuePair<string, ArcweaveComponent> kvp2) => TouchUpRichText(kvp1.Value.name).CompareTo(TouchUpRichText(kvp2.Value.name)));
		arcweaveProject.components.Clear();
		list2.ForEach(delegate(KeyValuePair<string, ArcweaveComponent> x)
		{
			arcweaveProject.components.Add(x.Key, x.Value);
		});
		foreach (KeyValuePair<string, ArcweaveComponent> component in arcweaveProject.components)
		{
			list.Add(TouchUpRichText(component.Value.name));
		}
		componentNames = list.ToArray();
	}

	public virtual void Convert()
	{
		if (IsJsonLoaded())
		{
			CopySourceToDialogueDatabase(database);
			TouchUpDialogueDatabase(database);
		}
	}

	public virtual void CopySourceToDialogueDatabase(DialogueDatabase database)
	{
		this.database = database;
		database.description = arcweaveProject.name;
		AddVariables();
		AddLocations();
		AddActors(playerComponentGuids, "Player", isPlayer: true);
		AddActors(npcComponentGuids, "NPC", isPlayer: false);
		AddConversations();
		AddQuests();
	}

	protected virtual void CatalogAllArcweaveTypes()
	{
		arcweaveLookup.Clear();
		CatalogDictionary(arcweaveProject.boards);
		CatalogDictionary(arcweaveProject.notes);
		CatalogDictionary(arcweaveProject.elements);
		CatalogDictionary(arcweaveProject.jumpers);
		CatalogDictionary(arcweaveProject.connections);
		CatalogDictionary(arcweaveProject.branches);
		CatalogDictionary(arcweaveProject.components);
		CatalogDictionary(arcweaveProject.attributes);
		CatalogDictionary(arcweaveProject.assets);
		CatalogDictionary(arcweaveProject.variables);
		CatalogDictionary(arcweaveProject.conditions);
	}

	protected virtual void CatalogDictionary<T>(Dictionary<string, T> dict) where T : ArcweaveType
	{
		foreach (KeyValuePair<string, T> item in dict)
		{
			arcweaveLookup[item.Key] = item.Value;
		}
	}

	protected T LookupArcweave<T>(string guid) where T : ArcweaveType
	{
		if (!arcweaveLookup.TryGetValue(guid, out var value))
		{
			return null;
		}
		return value as T;
	}

	protected virtual void AddVariables()
	{
		AddVariables("");
		if (numPlayers > 1)
		{
			for (int i = 0; i < numPlayers; i++)
			{
				AddVariables("Player" + i + "_");
			}
		}
	}

	protected virtual void AddVariables(string prefix)
	{
		foreach (KeyValuePair<string, ArcweaveVariable> variable2 in arcweaveProject.variables)
		{
			ArcweaveVariable arcweaveVariable = variable2.Value;
			if (string.IsNullOrEmpty(arcweaveVariable.name))
			{
				continue;
			}
			if (merge)
			{
				database.variables.RemoveAll((Variable x) => x.Name == arcweaveVariable.name);
			}
			if (globalVariables.Contains(arcweaveVariable.name) && !string.IsNullOrEmpty(prefix))
			{
				continue;
			}
			Variable variable = template.CreateVariable(template.GetNextVariableID(database), prefix + arcweaveVariable.name, string.Empty);
			database.variables.Add(variable);
			switch (arcweaveVariable.type)
			{
			case "boolean":
			{
				variable.Type = FieldType.Boolean;
				JToken value8 = arcweaveVariable.value;
				variable.InitialBoolValue = (bool)((JValue)((value8 is JValue) ? value8 : null)).Value;
				break;
			}
			case "float":
			{
				variable.Type = FieldType.Number;
				JToken value7 = arcweaveVariable.value;
				variable.InitialFloatValue = (float)((JValue)((value7 is JValue) ? value7 : null)).Value;
				break;
			}
			case "integer":
			{
				variable.Type = FieldType.Number;
				JToken value2 = arcweaveVariable.value;
				if (((JValue)((value2 is JValue) ? value2 : null)).Value.GetType() == typeof(long))
				{
					JToken value3 = arcweaveVariable.value;
					variable.InitialFloatValue = (long)((JValue)((value3 is JValue) ? value3 : null)).Value;
					break;
				}
				JToken value4 = arcweaveVariable.value;
				if (((JValue)((value4 is JValue) ? value4 : null)).Value.GetType() == typeof(int))
				{
					JToken value5 = arcweaveVariable.value;
					variable.InitialFloatValue = (int)((JValue)((value5 is JValue) ? value5 : null)).Value;
					break;
				}
				string[] obj = new string[6] { "Variable ", variable2.Key, " '", arcweaveVariable.name, "' is not an int, type is: ", null };
				JToken value6 = arcweaveVariable.value;
				obj[5] = ((JValue)((value6 is JValue) ? value6 : null)).Value.GetType().Name;
				Debug.Log(string.Concat(obj));
				break;
			}
			case "string":
			{
				variable.Type = FieldType.Text;
				JToken value = arcweaveVariable.value;
				variable.InitialValue = (string)((JValue)((value is JValue) ? value : null)).Value;
				break;
			}
			default:
				Debug.LogWarning("Dialogue System: Can't import variable " + variable.Name + " type '" + arcweaveVariable.type + "'.");
				break;
			}
		}
		database.variables.Sort((Variable a, Variable b) => a.Name.CompareTo(b.Name));
	}

	protected virtual void AddLocations()
	{
		foreach (string locationComponentGuid in locationComponentGuids)
		{
			if (!arcweaveProject.components.TryGetValue(locationComponentGuid, out var component))
			{
				continue;
			}
			if (merge)
			{
				database.locations.RemoveAll((Location x) => x.Name == component.name);
			}
			Location location = template.CreateLocation(template.GetNextLocationID(database), component.name);
			database.locations.Add(location);
			AddAttributes(location.fields, component.attributes);
		}
	}

	protected virtual void AddActors(List<string> guids, string defaultActorName, bool isPlayer)
	{
		Actor actor = null;
		foreach (string guid in guids)
		{
			if (!arcweaveProject.components.TryGetValue(guid, out var component))
			{
				continue;
			}
			if (merge)
			{
				database.actors.RemoveAll((Actor x) => x.Name == component.name);
			}
			actor = template.CreateActor(template.GetNextActorID(database), component.name, isPlayer);
			actorLookup[guid] = actor;
			database.actors.Add(actor);
			AddAttributes(actor.fields, component.attributes);
		}
		if (actor != null)
		{
			return;
		}
		if (merge)
		{
			database.actors.RemoveAll((Actor x) => x.Name == defaultActorName);
		}
		actor = template.CreateActor(template.GetNextActorID(database), defaultActorName, isPlayer);
		database.actors.Add(actor);
	}

	protected virtual void AddAttributes(List<Field> fields, List<string> attributes)
	{
		foreach (string attribute in attributes)
		{
			if (arcweaveProject.attributes.TryGetValue(attribute, out var value) && value.value.type == "string")
			{
				JToken data = value.value.data;
				string value2 = TouchUpRichText((string)((JValue)((data is JValue) ? data : null)).Value);
				Field.SetValue(fields, value.name, value2);
			}
		}
	}

	protected virtual Actor FindActorByComponentIndex(int index)
	{
		if (0 > index || index >= componentNames.Length)
		{
			return null;
		}
		return database.actors.Find((Actor x) => x.Name == componentNames[index]);
	}

	protected virtual void AddConversations()
	{
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		dialogueEntryLookup.Clear();
		foreach (ArcweaveConversationInfo item in conversationInfo)
		{
			string boardGuid = item.boardGuid;
			if (!arcweaveProject.boards.TryGetValue(boardGuid, out var value))
			{
				continue;
			}
			Actor actor = FindActorByComponentIndex(item.actorIndex) ?? database.actors.Find((Actor x) => x.IsPlayer);
			Actor actor2 = FindActorByComponentIndex(item.conversantIndex) ?? database.actors.Find((Actor x) => !x.IsPlayer);
			currentPlayerID = actor?.id ?? 0;
			currentNpcID = actor2?.id ?? 1;
			string conversationTitle = GetConversationTitle(value);
			if (merge)
			{
				database.conversations.RemoveAll((Conversation x) => x.Title == conversationTitle);
			}
			Conversation conversation = template.CreateConversation(template.GetNextConversationID(database), conversationTitle);
			database.conversations.Add(conversation);
			conversation.ActorID = currentPlayerID;
			conversation.ConversantID = currentNpcID;
			DialogueEntry dialogueEntry = template.CreateDialogueEntry(0, conversation.id, "START");
			dialogueEntryLookup[boardGuid] = dialogueEntry;
			conversation.dialogueEntries.Add(dialogueEntry);
			dialogueEntry.ActorID = currentPlayerID;
			dialogueEntry.ConversantID = currentNpcID;
			foreach (string element2 in value.elements)
			{
				Element element = LookupArcweave<Element>(element2);
				if (element != null)
				{
					DialogueEntry orCreateDialogueEntry = GetOrCreateDialogueEntry(conversation, element2);
					orCreateDialogueEntry.Title = StripHtmlCodes(element.title);
					orCreateDialogueEntry.ActorID = GetActorIDFromTitle(element, currentNpcID);
					orCreateDialogueEntry.ConversantID = currentPlayerID;
					SetActorIDsFromComponents(orCreateDialogueEntry, element);
					ProcessContent(orCreateDialogueEntry, element.content);
				}
			}
			foreach (string connection4 in value.connections)
			{
				Connection connection = LookupArcweave<Connection>(connection4);
				if (connection != null)
				{
					string code;
					string text = ExtractCode(connection.label, out code);
					bool flag = string.IsNullOrEmpty(text);
					DialogueEntry orCreateDialogueEntry2 = GetOrCreateDialogueEntry(conversation, connection4);
					orCreateDialogueEntry2.ActorID = (flag ? currentNpcID : currentPlayerID);
					orCreateDialogueEntry2.ConversantID = currentPlayerID;
					orCreateDialogueEntry2.isGroup = flag;
					orCreateDialogueEntry2.DialogueText = TouchUpRichText(text);
					if (flag && string.IsNullOrEmpty(code))
					{
						orCreateDialogueEntry2.Title = "$$Delete$$";
					}
				}
			}
			foreach (string branch3 in value.branches)
			{
				if (LookupArcweave<Branch>(branch3) != null)
				{
					DialogueEntry orCreateDialogueEntry3 = GetOrCreateDialogueEntry(conversation, branch3);
					orCreateDialogueEntry3.ActorID = currentNpcID;
					orCreateDialogueEntry3.ConversantID = currentPlayerID;
					orCreateDialogueEntry3.isGroup = true;
				}
			}
			foreach (string jumper2 in value.jumpers)
			{
				Jumper jumper = LookupArcweave<Jumper>(jumper2);
				if (jumper != null)
				{
					DialogueEntry orCreateDialogueEntry4 = GetOrCreateDialogueEntry(conversation, jumper2);
					orCreateDialogueEntry4.ActorID = currentNpcID;
					orCreateDialogueEntry4.ConversantID = currentPlayerID;
					orCreateDialogueEntry4.Title = "Jumper." + jumper.elementId;
					orCreateDialogueEntry4.isGroup = true;
				}
			}
			foreach (string branch4 in value.branches)
			{
				Branch branch = LookupArcweave<Branch>(branch4);
				if (branch == null)
				{
					continue;
				}
				string currentCumulativeCondition = string.Empty;
				DialogueEntry dialogueEntry2 = CreateConditionEntry(conversation, branch.conditions.ifCondition, ref currentCumulativeCondition);
				if (dialogueEntry2 != null)
				{
					dialogueEntry2.Title = "if " + (arcweaveLookup[branch.conditions.ifCondition] as Condition).script;
				}
				if (branch.conditions.elseIfConditions != null)
				{
					foreach (JToken item2 in (JArray)branch.conditions.elseIfConditions)
					{
						string text2 = (string)((JValue)((item2 is JValue) ? item2 : null)).Value;
						DialogueEntry dialogueEntry3 = CreateConditionEntry(conversation, text2, ref currentCumulativeCondition);
						if (dialogueEntry3 != null)
						{
							dialogueEntry3.Title = "elseif " + (arcweaveLookup[text2] as Condition).script;
						}
					}
				}
				DialogueEntry dialogueEntry4 = CreateConditionEntry(conversation, branch.conditions.elseCondition, ref currentCumulativeCondition);
				if (dialogueEntry4 != null)
				{
					dialogueEntry4.Title = "else";
				}
			}
			int startIndex = item.startIndex;
			if (0 > startIndex || startIndex >= value.elements.Count)
			{
				continue;
			}
			if (dialogueEntryLookup.TryGetValue(value.elements[startIndex], out var value2))
			{
				dialogueEntry.outgoingLinks.Add(new Link(conversation.id, dialogueEntry.id, conversation.id, value2.id));
			}
			foreach (string connection5 in value.connections)
			{
				Connection connection2 = LookupArcweave<Connection>(connection5);
				if (connection2 != null)
				{
					DialogueEntry orCreateDialogueEntry5 = GetOrCreateDialogueEntry(conversation, connection5);
					orCreateDialogueEntry5.ActorID = currentPlayerID;
					orCreateDialogueEntry5.ConversantID = currentNpcID;
					ProcessContent(orCreateDialogueEntry5, connection2.label);
					if (string.IsNullOrEmpty(connection2.label))
					{
						orCreateDialogueEntry5.ActorID = currentNpcID;
						orCreateDialogueEntry5.isGroup = true;
					}
				}
			}
			foreach (string branch5 in value.branches)
			{
				Branch branch2 = LookupArcweave<Branch>(branch5);
				if (branch2 == null)
				{
					continue;
				}
				if (!string.IsNullOrEmpty(branch2.conditions.ifCondition))
				{
					AddLink(branch5, branch2.conditions.ifCondition);
				}
				if (branch2.conditions.elseIfConditions != null)
				{
					foreach (JToken item3 in (JArray)branch2.conditions.elseIfConditions)
					{
						string targetGuid = (string)((JValue)((item3 is JValue) ? item3 : null)).Value;
						AddLink(branch5, targetGuid);
					}
				}
				if (!string.IsNullOrEmpty(branch2.conditions.elseCondition))
				{
					AddLink(branch5, branch2.conditions.elseCondition);
				}
			}
		}
		foreach (ArcweaveConversationInfo item4 in conversationInfo)
		{
			string boardGuid2 = item4.boardGuid;
			if (!arcweaveProject.boards.TryGetValue(boardGuid2, out var value3))
			{
				continue;
			}
			foreach (string connection6 in value3.connections)
			{
				Connection connection3 = LookupArcweave<Connection>(connection6);
				if (connection3 != null)
				{
					AddLink(connection3.sourceid, connection6);
					AddLink(connection6, connection3.targetid);
				}
			}
		}
		SetElementOrderByOutputs();
		DeleteUnnecessaryConnectionEntries();
	}

	protected virtual string ExtractCode(string label, out string code)
	{
		code = string.Empty;
		if (string.IsNullOrEmpty(label) || !label.Contains("<code>"))
		{
			return label;
		}
		int num = label.IndexOf("<code>");
		int num2 = label.IndexOf("</code>");
		int num3 = num + "<code>".Length;
		code = label.Substring(num3, num2 - num3);
		return string.Empty;
	}

	protected virtual void SetElementOrderByOutputs()
	{
		foreach (ArcweaveConversationInfo item in conversationInfo)
		{
			string boardGuid = item.boardGuid;
			if (!arcweaveProject.boards.TryGetValue(boardGuid, out var value))
			{
				continue;
			}
			foreach (string element2 in value.elements)
			{
				Element element = LookupArcweave<Element>(element2);
				DialogueEntry dialogueEntry = dialogueEntryLookup[element2];
				if (element != null)
				{
					dialogueEntry?.outgoingLinks.Sort((Link x, Link y) => CompareOutputsPosition(x, y, element.outputs));
				}
			}
		}
	}

	protected virtual void DeleteUnnecessaryConnectionEntries()
	{
		foreach (Conversation conversation in database.conversations)
		{
			List<DialogueEntry> list = new List<DialogueEntry>();
			foreach (DialogueEntry dialogueEntry2 in conversation.dialogueEntries)
			{
				foreach (Link outgoingLink in dialogueEntry2.outgoingLinks)
				{
					if (outgoingLink.destinationConversationID == dialogueEntry2.conversationID)
					{
						DialogueEntry dialogueEntry = conversation.GetDialogueEntry(outgoingLink.destinationDialogueID);
						if (dialogueEntry != null && dialogueEntry.isGroup && dialogueEntry.outgoingLinks.Count > 0 && dialogueEntry.Title == "$$Delete$$")
						{
							outgoingLink.destinationDialogueID = dialogueEntry.outgoingLinks[0].destinationDialogueID;
							list.Add(dialogueEntry);
						}
					}
				}
			}
			list.ForEach(delegate(DialogueEntry entryToDelete)
			{
				conversation.dialogueEntries.Remove(entryToDelete);
			});
		}
	}

	protected virtual int CompareOutputsPosition(Link x, Link y, List<string> outputs)
	{
		if (outputs == null)
		{
			return 0;
		}
		DialogueEntry destinationX = database.GetDialogueEntry(x);
		DialogueEntry destinationY = database.GetDialogueEntry(y);
		if (destinationX == null || destinationY == null)
		{
			return 0;
		}
		string key = dialogueEntryLookup.FirstOrDefault((KeyValuePair<string, DialogueEntry> e) => e.Value == destinationX).Key;
		string key2 = dialogueEntryLookup.FirstOrDefault((KeyValuePair<string, DialogueEntry> e) => e.Value == destinationY).Key;
		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(key2))
		{
			return 0;
		}
		int num = outputs.IndexOf(key);
		int num2 = outputs.IndexOf(key2);
		if (num == -1 || num2 == -1 || num == num2)
		{
			return 0;
		}
		if (num >= num2)
		{
			return 1;
		}
		return -1;
	}

	protected virtual string GetConversationTitle(Board conversationBoard)
	{
		foreach (ArcweaveBoardNode child in rootBoardNode.children)
		{
			if (TryGetConversationTitle(conversationBoard, child, out var title))
			{
				return title;
			}
		}
		return conversationBoard.name;
	}

	protected virtual bool TryGetConversationTitle(Board conversationBoard, ArcweaveBoardNode boardNode, out string title)
	{
		if (boardNode.board == conversationBoard)
		{
			title = boardNode.board.name;
			return true;
		}
		foreach (ArcweaveBoardNode child in boardNode.children)
		{
			if (TryGetConversationTitle(conversationBoard, child, out title))
			{
				title = boardNode.board.name + "/" + title;
				return true;
			}
		}
		title = string.Empty;
		return false;
	}

	protected virtual int GetActorIDFromTitle(Element element, int currentNpcID)
	{
		if (element != null && element.title != null && element.title.Contains("Speaker:"))
		{
			string actorName = Tools.StripRichTextCodes(element.title).Substring("Speaker:".Length).Trim();
			Actor actor = database.GetActor(actorName);
			if (actor != null)
			{
				return actor.id;
			}
		}
		return currentNpcID;
	}

	protected virtual void SetActorIDsFromComponents(DialogueEntry entry, Element element)
	{
		if (entry == null || element == null || element.components == null)
		{
			return;
		}
		if (element.components.Count >= 1)
		{
			string key = element.components[0];
			if (actorLookup.TryGetValue(key, out var value))
			{
				entry.ActorID = value.id;
			}
		}
		if (element.components.Count >= 2)
		{
			string key2 = element.components[1];
			if (actorLookup.TryGetValue(key2, out var value2))
			{
				entry.ConversantID = value2.id;
			}
		}
	}

	protected virtual bool AddLink(string sourceGuid, string targetGuid)
	{
		if (!dialogueEntryLookup.TryGetValue(sourceGuid, out var value) || !dialogueEntryLookup.TryGetValue(targetGuid, out var value2))
		{
			return false;
		}
		value.outgoingLinks.Add(new Link(value.conversationID, value.id, value2.conversationID, value2.id));
		return true;
	}

	protected virtual DialogueEntry GetOrCreateDialogueEntry(Conversation conversation, string guid)
	{
		if (!dialogueEntryLookup.TryGetValue(guid, out var value))
		{
			value = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
			conversation.dialogueEntries.Add(value);
			dialogueEntryLookup[guid] = value;
			value.DialogueText = string.Empty;
			value.MenuText = string.Empty;
			value.Sequence = string.Empty;
			if (importGuids)
			{
				value.fields.Add(new Field("Guid", guid, FieldType.Text));
			}
		}
		return value;
	}

	protected virtual DialogueEntry CreateConditionEntry(Conversation conversation, string conditionGuid, ref string currentCumulativeCondition)
	{
		if (string.IsNullOrEmpty(conditionGuid))
		{
			return null;
		}
		if (!arcweaveLookup.TryGetValue(conditionGuid, out var value))
		{
			return null;
		}
		Condition condition = value as Condition;
		DialogueEntry orCreateDialogueEntry = GetOrCreateDialogueEntry(conversation, conditionGuid);
		orCreateDialogueEntry.ActorID = currentNpcID;
		orCreateDialogueEntry.ConversantID = currentPlayerID;
		orCreateDialogueEntry.isGroup = true;
		bool flag = !string.IsNullOrEmpty(condition.script);
		bool flag2 = !string.IsNullOrEmpty(currentCumulativeCondition);
		string text = (flag ? ConvertArcscriptToLua(condition.script) : string.Empty);
		if (flag && (text.Contains(" and ") || text.Contains(" or ")))
		{
			text = "(" + text + ")";
		}
		string conditionsString = text;
		if (flag2)
		{
			conditionsString = ((!flag) ? ("not (" + currentCumulativeCondition + ")") : ((!currentCumulativeCondition.StartsWith("(")) ? ("(not (" + currentCumulativeCondition + ")) and " + text) : ("(not " + currentCumulativeCondition + ") and " + text)));
		}
		if (flag)
		{
			if (!flag2)
			{
				currentCumulativeCondition = text;
			}
			else
			{
				currentCumulativeCondition = currentCumulativeCondition + " or " + text;
			}
		}
		orCreateDialogueEntry.conditionsString = conditionsString;
		return orCreateDialogueEntry;
	}

	protected virtual void ConnectJumpers()
	{
		foreach (ArcweaveConversationInfo item in conversationInfo)
		{
			string boardGuid = item.boardGuid;
			if (!arcweaveProject.boards.TryGetValue(boardGuid, out var value))
			{
				continue;
			}
			foreach (string jumper2 in value.jumpers)
			{
				Jumper jumper = LookupArcweave<Jumper>(jumper2);
				DialogueEntry dialogueEntry = dialogueEntryLookup[jumper2];
				if (jumper != null && jumper.elementId != null && dialogueEntry != null && dialogueEntryLookup.TryGetValue(jumper.elementId, out var value2) && value2 != null)
				{
					string text = value2.Title;
					if (string.IsNullOrEmpty(text))
					{
						text = value2.DialogueText;
					}
					dialogueEntry.Title = (string.IsNullOrEmpty(text) ? "Jumper" : ("Jumper: " + StripHtmlCodes(text)));
					dialogueEntry.outgoingLinks.Clear();
					AddLink(jumper2, jumper.elementId);
				}
			}
		}
	}

	protected virtual void AddQuests()
	{
		foreach (string questBoardGuid in questBoardGuids)
		{
			try
			{
				Board board = LookupArcweave<Board>(questBoardGuid);
				if (board == null)
				{
					continue;
				}
				int nextQuestID = template.GetNextQuestID(database);
				Item item = template.CreateQuest(nextQuestID, board.name);
				int num = 0;
				foreach (string element2 in board.elements)
				{
					Element element = LookupArcweave<Element>(element2);
					string text = StripHtmlCodes(element.title);
					if (string.Equals("Main", text, StringComparison.OrdinalIgnoreCase))
					{
						Field.SetValue(item.fields, "Description", StripHtmlCodes(element.content));
						foreach (string attribute3 in element.attributes)
						{
							Attribute attribute = LookupArcweave<Attribute>(attribute3);
							switch (attribute.name)
							{
							case "Trackable":
							case "Track":
								Field.SetValue(item.fields, attribute.name, attribute.value.plain);
								break;
							case "State":
								Field.SetValue(item.fields, attribute.name, ((object)attribute.value.data).ToString().ToLower());
								break;
							default:
								Field.SetValue(item.fields, attribute.name, ((object)attribute.value.data).ToString());
								break;
							}
						}
					}
					else
					{
						if (!text.StartsWith("Entry ") || !int.TryParse(text.Substring("Entry ".Length), out var result))
						{
							continue;
						}
						num = Mathf.Max(num, result);
						Field.SetValue(item.fields, text, StripHtmlCodes(element.content));
						foreach (string attribute4 in element.attributes)
						{
							Attribute attribute2 = LookupArcweave<Attribute>(attribute4);
							string text2 = ((object)attribute2.value.data).ToString();
							if (attribute2.name == "State")
							{
								text2 = text2.ToLower();
							}
							Field.SetValue(item.fields, attribute2.name, text2);
						}
					}
				}
				Field.SetValue(item.fields, "Entry Count", num);
				database.items.Add(item);
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to create quest for Arcweave GUID " + questBoardGuid + ". " + ex.Message);
			}
		}
	}

	public virtual void TouchUpDialogueDatabase(DialogueDatabase database)
	{
		SetStartCutscenesToNone(database);
		ConnectJumpers();
		AddInlineCodeNodes();
		ExtractSequences();
		TouchUpRichText();
		SplitPipes();
	}

	protected virtual void SetStartCutscenesToNone(DialogueDatabase database)
	{
		foreach (Conversation conversation in database.conversations)
		{
			SetConversationStartCutsceneToNone(conversation);
		}
	}

	protected virtual void SetConversationStartCutsceneToNone(Conversation conversation)
	{
		DialogueEntry firstDialogueEntry = conversation.GetFirstDialogueEntry();
		if (firstDialogueEntry == null)
		{
			Debug.LogWarning("Dialogue System: Conversation '" + conversation.Title + "' doesn't have a START dialogue entry.");
		}
		else if (string.IsNullOrEmpty(firstDialogueEntry.currentSequence))
		{
			Field.SetValue(firstDialogueEntry.fields, "Sequence", "None()");
		}
	}

	protected virtual string StripHtmlCodes(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Tools.StripRichTextCodes(text.Replace("\n", "").Replace("\r", "").Replace("<p>", "")
			.Replace("</p>", ""));
	}

	protected virtual void TouchUpRichText()
	{
		foreach (Conversation conversation in database.conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				dialogueEntry.Title = TouchUpRichText(dialogueEntry.Title);
				dialogueEntry.DialogueText = TouchUpRichText(dialogueEntry.DialogueText);
				if (!string.IsNullOrEmpty(dialogueEntry.userScript))
				{
					dialogueEntry.userScript = ConvertArcscriptToLua(dialogueEntry.userScript, convertIncrementors: true);
				}
				if (!string.IsNullOrEmpty(dialogueEntry.conditionsString))
				{
					dialogueEntry.conditionsString = ConvertArcscriptToLua(dialogueEntry.conditionsString);
				}
				if (!dialogueEntry.isGroup && string.IsNullOrEmpty(dialogueEntry.DialogueText) && string.IsNullOrEmpty(dialogueEntry.Sequence))
				{
					dialogueEntry.Sequence = ((dialogueEntry.id == 0) ? "None()" : "Continue()");
				}
			}
		}
	}

	protected virtual void SplitPipes()
	{
		foreach (Conversation conversation in database.conversations)
		{
			conversation.SplitPipesIntoEntries(putEndSequenceOnLastSplit: true, trimWhitespace: false, "Guid");
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				dialogueEntry.DialogueText = dialogueEntry.DialogueText.Trim();
			}
		}
	}

	protected virtual void ExtractSequences()
	{
		foreach (Conversation conversation in database.conversations)
		{
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				ExtractSequence(dialogueEntry);
			}
		}
	}

	protected virtual void ExtractSequence(DialogueEntry entry)
	{
		string dialogueText = entry.DialogueText;
		if (string.IsNullOrEmpty(dialogueText))
		{
			return;
		}
		Match match = SequenceRegex.Match(dialogueText);
		if (match.Success)
		{
			if (!string.IsNullOrEmpty(entry.Sequence))
			{
				entry.Sequence += ";\n";
			}
			entry.Sequence = dialogueText.Substring(match.Index + "[SEQUENCE:".Length, match.Length - "[SEQUENCE:]".Length).Trim();
			entry.DialogueText = dialogueText.Remove(match.Index, match.Length);
		}
	}

	protected virtual string TouchUpRichText(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return string.Empty;
		}
		if (s.Contains("<em>"))
		{
			s = s.Replace("<em>", "<i>").Replace("</em>", "</i>");
		}
		foreach (Match item in BlockRegex.Matches(s).Cast<Match>().Reverse())
		{
			if (item.Value.StartsWith("<p>") || item.Value.StartsWith("</p>"))
			{
				s = s.Insert(item.Index, "\n");
			}
		}
		s = BlockRegex.Replace(s, string.Empty);
		return Tools.RemoveHtml(s).Trim();
	}

	protected virtual void ProcessContent(DialogueEntry entry, string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			entry.DialogueText = string.Empty;
			return;
		}
		if (!content.Contains("<code>"))
		{
			entry.DialogueText = TouchUpRichText(content);
			return;
		}
		string text = content;
		List<ContentPiece> list = new List<ContentPiece>();
		Match match = CodeStartRegex.Match(text);
		while (match.Success)
		{
			string text2 = TouchUpRichText(text.Substring(0, match.Index));
			if (!string.IsNullOrEmpty(text2.Trim()))
			{
				list.Add(new ContentPiece(ContentPieceType.Text, text2));
			}
			text = text.Substring(match.Index + match.Length);
			match = CodeEndRegex.Match(text);
			if (match.Success)
			{
				string text3 = text.Substring(0, match.Index);
				list.Add(new ContentPiece(ContentPieceType.Code, text3));
				text = text.Substring(match.Index + match.Length);
			}
			match = CodeStartRegex.Match(text);
		}
		text = TouchUpRichText(text);
		if (!string.IsNullOrEmpty(text.Trim()))
		{
			list.Add(new ContentPiece(ContentPieceType.Text, text));
		}
		entry.DialogueText = string.Empty;
		bool flag = false;
		CodeState codeState = CodeState.None;
		string text4 = string.Empty;
		int num = 0;
		foreach (ContentPiece item in list)
		{
			switch (item.type)
			{
			case ContentPieceType.Text:
				switch (codeState)
				{
				case CodeState.None:
					if (flag)
					{
						entry.fields.Add(new Field("_POST_IF_TEXT", item.text, FieldType.Text));
					}
					else
					{
						entry.DialogueText += item.text;
					}
					break;
				case CodeState.InIf:
				case CodeState.InElseIf:
				case CodeState.InElse:
					entry.fields.Add(new Field(text4 + "_TEXT", item.text, FieldType.Text));
					break;
				}
				break;
			case ContentPieceType.Code:
			{
				string text5 = item.text;
				if (text5.StartsWith("if "))
				{
					flag = true;
					codeState = CodeState.InIf;
					text4 = "_IF";
					text5 = text5.Substring("if ".Length);
				}
				else if (text5.StartsWith("elseif "))
				{
					codeState = CodeState.InElseIf;
					text4 = $"_ELSEIF.{num}";
					text5 = text5.Substring("elseif ".Length);
					num++;
				}
				else if (text5.StartsWith("else "))
				{
					codeState = CodeState.InElse;
					text4 = "_ELSE";
					text5 = text5.Substring("else ".Length);
				}
				else if (text5.StartsWith("endif"))
				{
					codeState = CodeState.None;
					text4 = string.Empty;
				}
				else if (codeState == CodeState.None)
				{
					if (!string.IsNullOrEmpty(entry.userScript))
					{
						entry.userScript += "\n";
					}
					entry.userScript += text5;
				}
				else
				{
					entry.fields.Add(new Field(text4 + "_INNER_CODE", text5, FieldType.Text));
				}
				if (!string.IsNullOrEmpty(text4))
				{
					entry.fields.Add(new Field(text4 + "_CODE", text5, FieldType.Text));
				}
				break;
			}
			}
		}
		entry.fields.Add(new Field("_NUM_ELSEIF", num.ToString(), FieldType.Number));
	}

	protected virtual void AddInlineCodeNodes()
	{
		foreach (Conversation conversation in database.conversations)
		{
			for (int num = conversation.dialogueEntries.Count - 1; num >= 0; num--)
			{
				DialogueEntry dialogueEntry = conversation.dialogueEntries[num];
				if (Field.FieldExists(dialogueEntry.fields, "_IF_CODE"))
				{
					DialogueEntry dialogueEntry2 = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
					conversation.dialogueEntries.Add(dialogueEntry2);
					dialogueEntry2.ActorID = dialogueEntry.ActorID;
					dialogueEntry2.ConversantID = dialogueEntry.ConversantID;
					foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
					{
						dialogueEntry2.outgoingLinks.Add(new Link(outgoingLink));
					}
					Field field = Field.Lookup(dialogueEntry.fields, "_POST_IF_TEXT");
					dialogueEntry.fields.Remove(field);
					if (field == null || string.IsNullOrEmpty(field.value))
					{
						dialogueEntry2.isGroup = true;
					}
					else
					{
						dialogueEntry2.DialogueText = TouchUpRichText(field.value);
					}
					dialogueEntry.outgoingLinks.Clear();
					string cumulativeConditions = string.Empty;
					string[] codeFieldPrefixes = CodeFieldPrefixes;
					foreach (string text in codeFieldPrefixes)
					{
						if (text == "_ELSEIF")
						{
							int num2 = Field.LookupInt(dialogueEntry.fields, "_NUM_ELSEIF");
							for (int j = 0; j < num2; j++)
							{
								InsertCodeEntry(conversation, dialogueEntry, dialogueEntry2, $"{text}.{j}", ref cumulativeConditions);
							}
						}
						else
						{
							InsertCodeEntry(conversation, dialogueEntry, dialogueEntry2, text, ref cumulativeConditions);
						}
					}
					InsertCodeFallthroughEntry(conversation, dialogueEntry, dialogueEntry2, cumulativeConditions);
				}
			}
		}
	}

	protected virtual void InsertCodeEntry(Conversation conversation, DialogueEntry entry, DialogueEntry postIfEntry, string prefix, ref string cumulativeConditions)
	{
		Field field = Field.Lookup(entry.fields, prefix + "_CODE");
		if (field == null)
		{
			return;
		}
		Field field2 = Field.Lookup(entry.fields, prefix + "_TEXT");
		Field field3 = Field.Lookup(entry.fields, prefix + "_INNER_CODE");
		entry.fields.Remove(field);
		entry.fields.Remove(field2);
		entry.fields.Remove(field3);
		DialogueEntry dialogueEntry = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
		conversation.dialogueEntries.Add(dialogueEntry);
		dialogueEntry.ActorID = entry.ActorID;
		dialogueEntry.ConversantID = entry.ConversantID;
		dialogueEntry.Title = field.value;
		dialogueEntry.outgoingLinks.Add(new Link(conversation.id, dialogueEntry.id, conversation.id, postIfEntry.id));
		dialogueEntry.isGroup = field2 == null || string.IsNullOrEmpty(field2.value);
		dialogueEntry.Sequence = (dialogueEntry.isGroup ? string.Empty : "Continue()");
		dialogueEntry.DialogueText = ((field2 != null) ? TouchUpRichText(field2.value) : string.Empty);
		dialogueEntry.conditionsString = ConvertArcscriptToLua(field.value);
		if (string.IsNullOrEmpty(cumulativeConditions))
		{
			cumulativeConditions = dialogueEntry.conditionsString;
		}
		else
		{
			if (cumulativeConditions[0] != '(')
			{
				cumulativeConditions = "(" + cumulativeConditions + ")";
			}
			cumulativeConditions = cumulativeConditions + " and (" + dialogueEntry.conditionsString + ")";
		}
		if (field3 != null)
		{
			dialogueEntry.userScript = ConvertArcscriptToLua(field3.value, convertIncrementors: true);
		}
		entry.outgoingLinks.Add(new Link(conversation.id, entry.id, conversation.id, dialogueEntry.id));
	}

	protected virtual void InsertCodeFallthroughEntry(Conversation conversation, DialogueEntry entry, DialogueEntry postIfEntry, string cumulativePreviousConditions)
	{
		DialogueEntry dialogueEntry = template.CreateDialogueEntry(template.GetNextDialogueEntryID(conversation), conversation.id, string.Empty);
		conversation.dialogueEntries.Add(dialogueEntry);
		dialogueEntry.ActorID = entry.ActorID;
		dialogueEntry.ConversantID = entry.ConversantID;
		dialogueEntry.Title = "fallthrough";
		dialogueEntry.outgoingLinks.Add(new Link(conversation.id, dialogueEntry.id, conversation.id, postIfEntry.id));
		dialogueEntry.isGroup = true;
		dialogueEntry.Sequence = (dialogueEntry.isGroup ? string.Empty : "Continue()");
		dialogueEntry.DialogueText = string.Empty;
		dialogueEntry.conditionsString = "not (" + cumulativePreviousConditions + ")";
		entry.outgoingLinks.Add(new Link(conversation.id, entry.id, conversation.id, dialogueEntry.id));
	}

	protected virtual string ConvertArcscriptToLua(string code, bool convertIncrementors = false)
	{
		if (string.IsNullOrEmpty(code))
		{
			return code;
		}
		code = Tools.RemoveHtml(code);
		code = ConvertVisits(code);
		if (convertIncrementors)
		{
			code = ConvertIncrementors(code);
		}
		code = ConvertArcscriptVariablesToLua(code);
		code = code.Replace("!=", "~=").Replace("is not", "~=").Replace("!", "not ")
			.Replace("&&", "and")
			.Replace("||", "or");
		return code;
	}

	protected virtual string ConvertVisits(string code)
	{
		if (!code.Contains("visits("))
		{
			return code;
		}
		foreach (Match item in VisitsRegex.Matches(code).Cast<Match>().Reverse())
		{
			int num = code.Substring(item.Index, item.Length).IndexOf("data-id") + item.Index;
			string text = code.Substring(num + "data-id=\"".Length + 1);
			text = text.Substring(0, text.IndexOf("\""));
			code = Replace(code, item.Index, item.Length, "visits(\"" + text + "\")");
		}
		return code.Replace("visits()", "visits(\"\")");
	}

	protected virtual string ConvertIncrementors(string code)
	{
		foreach (Match item in IncrementorRegex.Matches(code).Cast<Match>().Reverse())
		{
			string text = item.Value.Substring(0, 1);
			string text2 = code.Substring(0, item.Index).TrimEnd();
			int num = Mathf.Max(0, Mathf.Max(text2.LastIndexOf('\n'), text2.LastIndexOf(';')));
			string text3 = code.Substring(num, item.Index - num).Trim();
			code = Replace(code, item.Index, 2, "= " + text3 + " " + text);
		}
		return code;
	}

	protected virtual string ConvertArcscriptVariablesToLua(string code)
	{
		foreach (Match item in IdentifierRegex.Matches(code).Cast<Match>().Reverse())
		{
			string identifier = item.Value;
			if (!ReservedKeywords.Contains(identifier) && database.variables.Find((Variable x) => x.Name == identifier) != null)
			{
				string replacement = "Variable[\"" + identifier + "\"]";
				if (numPlayers > 1)
				{
					replacement = ((identifier.StartsWith("global") || globalVariables.Contains(identifier)) ? ("Variable[\"" + identifier + "\"]") : ("Variable[Variable[\"ActorIndex\"] .. \"_" + identifier + "\"]"));
				}
				code = Replace(code, item.Index, item.Length, replacement);
			}
		}
		return code;
	}

	protected virtual string Replace(string s, int index, int length, string replacement)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(s.Substring(0, index));
		stringBuilder.Append(replacement);
		stringBuilder.Append(s.Substring(index + length));
		return stringBuilder.ToString();
	}
}
