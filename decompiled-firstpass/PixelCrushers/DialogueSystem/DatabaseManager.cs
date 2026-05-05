using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem;

public class DatabaseManager
{
	private DialogueDatabase m_masterDatabase;

	private List<DialogueDatabase> m_loadedDatabases = new List<DialogueDatabase>();

	public DialogueDatabase defaultDatabase { get; set; }

	public DialogueDatabase masterDatabase => GetMasterDatabase();

	public DialogueDatabase DefaultDatabase
	{
		get
		{
			return defaultDatabase;
		}
		set
		{
			defaultDatabase = value;
		}
	}

	public DialogueDatabase MasterDatabase => masterDatabase;

	public List<DialogueDatabase> loadedDatabases => m_loadedDatabases;

	public DatabaseManager(DialogueDatabase defaultDatabase = null)
	{
		m_masterDatabase = DatabaseUtility.CreateDialogueDatabaseInstance();
		this.defaultDatabase = defaultDatabase;
	}

	private DialogueDatabase GetMasterDatabase()
	{
		if (m_loadedDatabases.Count == 0)
		{
			Add(defaultDatabase);
		}
		return m_masterDatabase;
	}

	public void Add(DialogueDatabase database)
	{
		if (database != null && !m_loadedDatabases.Contains(database))
		{
			if (m_loadedDatabases.Count == 0)
			{
				DialogueLua.InitializeChatMapperVariables();
			}
			m_masterDatabase.Add(database);
			DialogueLua.AddChatMapperVariables(m_masterDatabase, m_loadedDatabases);
			m_loadedDatabases.Add(database);
		}
	}

	public void Remove(DialogueDatabase database)
	{
		if (database != null)
		{
			m_loadedDatabases.Remove(database);
			m_masterDatabase.Remove(database, m_loadedDatabases);
			DialogueLua.RemoveChatMapperVariables(database, m_loadedDatabases);
		}
	}

	public void Clear()
	{
		DialogueLua.InitializeChatMapperVariables();
		m_masterDatabase.Clear();
		m_loadedDatabases.Clear();
	}

	public void Reset(DatabaseResetOptions databaseResetOptions = DatabaseResetOptions.RevertToDefault)
	{
		switch (databaseResetOptions)
		{
		case DatabaseResetOptions.RevertToDefault:
			ResetToDefaultDatabase();
			break;
		case DatabaseResetOptions.KeepAllLoaded:
			ResetToLoadedDatabases();
			break;
		}
	}

	private void ResetToDefaultDatabase()
	{
		Clear();
		Add(defaultDatabase);
	}

	private void ResetToLoadedDatabases()
	{
		List<DialogueDatabase> list = new List<DialogueDatabase>(m_loadedDatabases);
		Clear();
		Add(defaultDatabase);
		foreach (DialogueDatabase item in list)
		{
			if (item != defaultDatabase)
			{
				Add(item);
			}
		}
	}
}
