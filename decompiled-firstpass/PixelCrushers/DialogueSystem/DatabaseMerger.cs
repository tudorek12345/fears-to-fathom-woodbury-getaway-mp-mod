using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class DatabaseMerger
{
	public enum ConflictingIDRule
	{
		ReplaceConflictingIDs,
		AllowConflictingIDs,
		AssignUniqueIDs
	}

	private class NewIDs
	{
		public bool destinationHasPlayerActor;

		public bool destinationHasAlertVariable;

		public Dictionary<int, int> actor = new Dictionary<int, int>();

		public Dictionary<int, int> item = new Dictionary<int, int>();

		public Dictionary<int, int> location = new Dictionary<int, int>();

		public Dictionary<int, int> variable = new Dictionary<int, int>();

		public Dictionary<int, int> conversation = new Dictionary<int, int>();
	}

	public static void Merge(DialogueDatabase destination, DialogueDatabase source, ConflictingIDRule conflictingIDRule, bool mergeProperties, bool mergeActors, bool mergeItems, bool mergeLocations, bool mergeVariables, bool mergeConversations)
	{
		Merge(destination, source, conflictingIDRule, mergeProperties, mergeEmphases: true, mergeActors, mergeItems, mergeLocations, mergeVariables, mergeConversations);
	}

	public static void Merge(DialogueDatabase destination, DialogueDatabase source, ConflictingIDRule conflictingIDRule, bool mergeProperties, bool mergeEmphases, bool mergeActors, bool mergeItems, bool mergeLocations, bool mergeVariables, bool mergeConversations)
	{
		if (destination != null && source != null)
		{
			switch (conflictingIDRule)
			{
			case ConflictingIDRule.ReplaceConflictingIDs:
				MergeReplaceConflictingIDs(destination, source, mergeProperties, mergeEmphases, mergeActors, mergeItems, mergeLocations, mergeVariables, mergeConversations);
				return;
			case ConflictingIDRule.AllowConflictingIDs:
				MergeAllowConflictingIDs(destination, source, mergeProperties, mergeEmphases, mergeActors, mergeItems, mergeLocations, mergeVariables, mergeConversations);
				return;
			case ConflictingIDRule.AssignUniqueIDs:
				MergeAssignUniqueIDs(destination, source, mergeProperties, mergeEmphases, mergeActors, mergeItems, mergeLocations, mergeVariables, mergeConversations);
				return;
			}
			Debug.LogError(string.Format("{0}: Internal error. Unsupported merge type: {1}", new object[2] { "Dialogue System", conflictingIDRule }));
		}
	}

	public static void Merge(DialogueDatabase destination, DialogueDatabase source, ConflictingIDRule conflictingIDRule)
	{
		Merge(destination, source, conflictingIDRule, mergeProperties: true, mergeActors: true, mergeItems: true, mergeLocations: true, mergeVariables: true, mergeConversations: true);
	}

	private static void MergeDatabaseProperties(DialogueDatabase destination, DialogueDatabase source, bool mergeEmphases)
	{
		if (string.IsNullOrEmpty(destination.author))
		{
			destination.author = source.author;
		}
		if (string.IsNullOrEmpty(destination.version))
		{
			destination.version = source.version;
		}
		if (string.IsNullOrEmpty(destination.description))
		{
			destination.description = source.description;
		}
		if (!string.IsNullOrEmpty(source.globalUserScript))
		{
			if (string.IsNullOrEmpty(destination.globalUserScript))
			{
				destination.globalUserScript = source.description;
			}
			else
			{
				destination.globalUserScript = string.Format("{0}; {1}", new object[2] { destination.globalUserScript, source.globalUserScript });
			}
		}
		if (mergeEmphases)
		{
			if (source.emphasisSettings.Length > destination.emphasisSettings.Length)
			{
				destination.emphasisSettings = new EmphasisSetting[source.emphasisSettings.Length];
			}
			for (int i = 0; i < source.emphasisSettings.Length; i++)
			{
				EmphasisSetting emphasisSetting = source.emphasisSettings[i];
				destination.emphasisSettings[i] = new EmphasisSetting(emphasisSetting.color, emphasisSetting.bold, emphasisSetting.italic, emphasisSetting.underline);
			}
		}
	}

	private static void MergeReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source, bool mergeProperties, bool mergeEmphases, bool mergeActors, bool mergeItems, bool mergeLocations, bool mergeVariables, bool mergeConversations)
	{
		if (mergeProperties)
		{
			MergeDatabaseProperties(destination, source, mergeEmphases);
		}
		if (mergeActors)
		{
			MergeActorsReplaceConflictingIDs(destination, source);
		}
		if (mergeItems)
		{
			MergeItemsReplaceConflictingIDs(destination, source);
		}
		if (mergeLocations)
		{
			MergeLocationsReplaceConflictingIDs(destination, source);
		}
		if (mergeVariables)
		{
			MergeVariablesReplaceConflictingIDs(destination, source);
		}
		if (mergeConversations)
		{
			MergeConversationsReplaceConflictingIDs(destination, source);
		}
	}

	private static void MergeActorsReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Actor actor in source.actors)
		{
			destination.actors.RemoveAll((Actor x) => x.id == actor.id);
			destination.actors.Add(actor);
		}
	}

	private static void MergeItemsReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Item item in source.items)
		{
			destination.items.RemoveAll((Item x) => x.id == item.id);
			destination.items.Add(item);
		}
	}

	private static void MergeLocationsReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Location location in source.locations)
		{
			destination.locations.RemoveAll((Location x) => x.id == location.id);
			destination.locations.Add(location);
		}
	}

	private static void MergeVariablesReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Variable variable in source.variables)
		{
			destination.variables.RemoveAll((Variable x) => x.id == variable.id);
			destination.variables.Add(variable);
		}
	}

	private static void MergeConversationsReplaceConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Conversation conversation in source.conversations)
		{
			destination.conversations.RemoveAll((Conversation x) => x.id == conversation.id);
			destination.conversations.Add(conversation);
		}
	}

	private static void MergeAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source, bool mergeProperties, bool mergeEmphases, bool mergeActors, bool mergeItems, bool mergeLocations, bool mergeVariables, bool mergeConversations)
	{
		if (mergeProperties)
		{
			MergeDatabaseProperties(destination, source, mergeEmphases);
		}
		if (mergeActors)
		{
			MergeActorsAllowConflictingIDs(destination, source);
		}
		if (mergeItems)
		{
			MergeItemsAllowConflictingIDs(destination, source);
		}
		if (mergeLocations)
		{
			MergeLocationsAllowConflictingIDs(destination, source);
		}
		if (mergeVariables)
		{
			MergeVariablesAllowConflictingIDs(destination, source);
		}
		if (mergeConversations)
		{
			MergeConversationsAllowConflictingIDs(destination, source);
		}
	}

	private static void MergeActorsAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Actor actor in source.actors)
		{
			destination.actors.Add(actor);
		}
	}

	private static void MergeItemsAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Item item in source.items)
		{
			destination.items.Add(item);
		}
	}

	private static void MergeLocationsAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Location location in source.locations)
		{
			destination.locations.Add(location);
		}
	}

	private static void MergeVariablesAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Variable variable in source.variables)
		{
			destination.variables.Add(variable);
		}
	}

	private static void MergeConversationsAllowConflictingIDs(DialogueDatabase destination, DialogueDatabase source)
	{
		foreach (Conversation conversation in source.conversations)
		{
			if (destination.conversations.Find((Conversation c) => string.Equals(c.Title, conversation.Title)) != null)
			{
				Conversation conversation2 = new Conversation(conversation);
				conversation2.Title = conversation.Title + " Copy";
				destination.conversations.Add(conversation2);
			}
			else
			{
				destination.conversations.Add(conversation);
			}
		}
	}

	private static void MergeAssignUniqueIDs(DialogueDatabase destination, DialogueDatabase source, bool mergeProperties, bool mergeEmphases, bool mergeActors, bool mergeItems, bool mergeLocations, bool mergeVariables, bool mergeConversations)
	{
		if (mergeProperties)
		{
			MergeDatabaseProperties(destination, source, mergeEmphases);
		}
		NewIDs newIDs = new NewIDs();
		GetNewActorIDs(destination, source, newIDs);
		GetNewItemIDs(destination, source, newIDs);
		GetNewLocationIDs(destination, source, newIDs);
		GetNewVariableIDs(destination, source, newIDs);
		GetNewConversationIDs(destination, source, newIDs);
		if (mergeActors)
		{
			MergeActors(destination, source, newIDs);
		}
		if (mergeItems)
		{
			MergeItems(destination, source, newIDs);
		}
		if (mergeLocations)
		{
			MergeLocations(destination, source, newIDs);
		}
		if (mergeVariables)
		{
			MergeVariables(destination, source, newIDs);
		}
		if (mergeConversations)
		{
			MergeConversations(destination, source, newIDs);
		}
	}

	private static void GetNewActorIDs(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		int num = destination.baseID - 1;
		foreach (Actor actor2 in destination.actors)
		{
			num = Mathf.Max(actor2.id, num);
			if (actor2.IsPlayer)
			{
				newIDs.destinationHasPlayerActor = true;
			}
		}
		int num2 = num + 1;
		foreach (Actor actor in source.actors)
		{
			if ((!actor.IsPlayer || !newIDs.destinationHasPlayerActor) && destination.actors.Find((Actor x) => string.Equals(x.Name, actor.Name)) == null)
			{
				newIDs.actor[actor.id] = num2;
				num2++;
			}
		}
	}

	private static void GetNewItemIDs(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		int num = destination.baseID - 1;
		foreach (Item item2 in destination.items)
		{
			num = Mathf.Max(item2.id, num);
		}
		int num2 = num + 1;
		foreach (Item item in source.items)
		{
			if (destination.items.Find((Item x) => string.Equals(x.Name, item.Name)) == null)
			{
				newIDs.item[item.id] = num2;
				num2++;
			}
		}
	}

	private static void GetNewLocationIDs(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		int num = destination.baseID - 1;
		foreach (Location location2 in destination.locations)
		{
			num = Mathf.Max(location2.id, num);
		}
		int num2 = num + 1;
		foreach (Location location in source.locations)
		{
			if (destination.locations.Find((Location x) => string.Equals(x.Name, location.Name)) == null)
			{
				newIDs.location[location.id] = num2;
				num2++;
			}
		}
	}

	private static void GetNewVariableIDs(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		int num = destination.baseID - 1;
		foreach (Variable variable2 in destination.variables)
		{
			num = Mathf.Max(variable2.id, num);
			if (string.Equals(variable2.Name, "Alert"))
			{
				newIDs.destinationHasAlertVariable = true;
			}
		}
		int num2 = num + 1;
		foreach (Variable variable in source.variables)
		{
			if ((!string.Equals(variable.Name, "Alert") || !newIDs.destinationHasAlertVariable) && destination.variables.Find((Variable x) => string.Equals(x.Name, variable.Name)) == null)
			{
				newIDs.variable[variable.id] = num2;
				num2++;
			}
		}
	}

	private static void GetNewConversationIDs(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		int num = destination.baseID - 1;
		foreach (Conversation conversation in destination.conversations)
		{
			num = Mathf.Max(conversation.id, num);
		}
		int num2 = num + 1;
		foreach (Conversation conversation2 in source.conversations)
		{
			newIDs.conversation[conversation2.id] = num2;
			num2++;
		}
	}

	private static void ConvertFieldIDs(List<Field> fields, NewIDs newIDs)
	{
		foreach (Field field in fields)
		{
			int key = Tools.StringToInt(field.value);
			switch (field.type)
			{
			case FieldType.Actor:
				if (newIDs.actor.ContainsKey(key))
				{
					field.value = newIDs.actor[key].ToString();
				}
				break;
			case FieldType.Item:
				if (newIDs.item.ContainsKey(key))
				{
					field.value = newIDs.item[key].ToString();
				}
				break;
			case FieldType.Location:
				if (newIDs.location.ContainsKey(key))
				{
					field.value = newIDs.location[key].ToString();
				}
				break;
			}
		}
	}

	private static void MergeActors(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		foreach (Actor actor2 in source.actors)
		{
			if (newIDs.actor.ContainsKey(actor2.id))
			{
				Actor actor = new Actor(actor2);
				actor.id = newIDs.actor[actor2.id];
				ConvertFieldIDs(actor.fields, newIDs);
				destination.actors.Add(actor);
			}
		}
	}

	private static void MergeItems(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		foreach (Item item2 in source.items)
		{
			if (newIDs.item.ContainsKey(item2.id))
			{
				Item item = new Item(item2);
				item.id = newIDs.item[item2.id];
				ConvertFieldIDs(item.fields, newIDs);
				destination.items.Add(item);
			}
		}
	}

	private static void MergeLocations(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		foreach (Location location2 in source.locations)
		{
			if (newIDs.location.ContainsKey(location2.id))
			{
				Location location = new Location(location2);
				location.id = newIDs.location[location2.id];
				ConvertFieldIDs(location.fields, newIDs);
				destination.locations.Add(location);
			}
		}
	}

	private static void MergeVariables(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		foreach (Variable variable2 in source.variables)
		{
			if (newIDs.variable.ContainsKey(variable2.id))
			{
				Variable variable = new Variable(variable2);
				variable.id = newIDs.variable[variable2.id];
				ConvertFieldIDs(variable.fields, newIDs);
				destination.variables.Add(variable);
			}
		}
	}

	private static void MergeConversations(DialogueDatabase destination, DialogueDatabase source, NewIDs newIDs)
	{
		foreach (Conversation conversation in source.conversations)
		{
			if (!newIDs.conversation.ContainsKey(conversation.id))
			{
				continue;
			}
			Conversation conversation2 = new Conversation(conversation);
			conversation2.id = newIDs.conversation[conversation.id];
			if (destination.conversations.Find((Conversation c) => string.Equals(c.Title, conversation.Title)) != null)
			{
				conversation2.Title = conversation.Title + " Copy";
			}
			ConvertFieldIDs(conversation2.fields, newIDs);
			foreach (DialogueEntry dialogueEntry in conversation2.dialogueEntries)
			{
				dialogueEntry.conversationID = conversation2.id;
				foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
				{
					if (newIDs.conversation.ContainsKey(outgoingLink.originConversationID))
					{
						outgoingLink.originConversationID = newIDs.conversation[outgoingLink.originConversationID];
					}
					if (newIDs.conversation.ContainsKey(outgoingLink.destinationConversationID))
					{
						outgoingLink.destinationConversationID = newIDs.conversation[outgoingLink.destinationConversationID];
					}
				}
			}
			destination.conversations.Add(conversation2);
		}
	}
}
