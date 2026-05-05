using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveImportPrefs
{
	public string sourceFilename = string.Empty;

	public string outputFolder = "Assets";

	public string databaseFilename = "Dialogue Database";

	public bool overwrite;

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

	public bool importPortraits = true;

	public bool importGuids;

	public int numPlayers = 1;

	public string globalVariables;

	public string prefsPath;
}
