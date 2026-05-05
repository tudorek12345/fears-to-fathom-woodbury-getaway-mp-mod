using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandLoadLevel : SequencerCommand
{
	public void Start()
	{
		string parameter = GetParameter(0);
		string parameter2 = GetParameter(1);
		bool flag = string.Equals(GetParameter(2), "additive", StringComparison.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: LoadLevel() level name is an empty string", "Dialogue System"));
			}
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: LoadLevel({1})", "Dialogue System", GetParameters()));
			}
			DialogueLua.SetActorField("Player", "Spawnpoint", parameter2);
			if (SaveSystem.hasInstance)
			{
				if (flag)
				{
					SaveSystem.LoadAdditiveScene(parameter);
				}
				else
				{
					PersistentDataManager.LevelWillBeUnloaded();
					SaveSystem.LoadScene(string.IsNullOrEmpty(parameter2) ? parameter : (parameter + "@" + parameter2));
				}
			}
			else if (flag)
			{
				SceneManager.LoadScene(parameter, LoadSceneMode.Additive);
			}
			else
			{
				LevelManager levelManager = GameObjectUtility.FindFirstObjectByType<LevelManager>();
				if (levelManager != null)
				{
					levelManager.LoadLevel(parameter);
				}
				else
				{
					PersistentDataManager.Record();
					PersistentDataManager.LevelWillBeUnloaded();
					SceneManager.LoadScene(parameter, LoadSceneMode.Single);
					PersistentDataManager.Apply();
				}
			}
		}
		Stop();
	}
}
