using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class PersistentDataSettings
{
	[Tooltip("- All Game Objects: Send notification to all scripts on all GameObjects in the scene to record and/or apply their persistent data if supported.\n- Only Registered Game Objects: Send notification only to explicitly-registered GameObjects.\n- No Game Objects: Don't send notification to any GameObjects in the scene.")]
	public PersistentDataManager.RecordPersistentDataOn recordPersistentDataOn;

	[Tooltip("Tick to include the Actor[] table in save data.")]
	public bool includeActorData = true;

	[Tooltip("Tick to include all Item[] and Quest[] fields. If unticked, only record quest states and quest tracking states to reduce size.")]
	public bool includeAllItemData;

	[Tooltip("Tick to include the Location[] table.")]
	public bool includeLocationData;

	[Tooltip("Tick to include status and relationship tables in save data.")]
	public bool includeStatusAndRelationshipData = true;

	[Tooltip("Tick to include all conversation fields.")]
	public bool includeAllConversationFields;

	[Tooltip("Optional field to use when saving a conversation's SimStatus info (e.g., Title). If blank, uses conversation ID.")]
	public string saveConversationSimStatusWithField = string.Empty;

	[Tooltip("Optional field to use when saving a dialogue entry's SimStatus info (e.g,. Title). If blank, uses entry's ID.")]
	public string saveDialogueEntrySimStatusWithField = string.Empty;

	[Tooltip("How many scene GameObjects are sent OnRecordPersistentData each frame.")]
	public int asyncGameObjectBatchSize = 1000;

	[Tooltip("How many dialogue entries' SimStatus values are recorded each frame; only used if saving SimStatus.")]
	public int asyncDialogueEntryBatchSize = 100;

	[Tooltip("Initialize variables and quests that were added to database after saved game.")]
	public bool initializeNewVariables = true;
}
