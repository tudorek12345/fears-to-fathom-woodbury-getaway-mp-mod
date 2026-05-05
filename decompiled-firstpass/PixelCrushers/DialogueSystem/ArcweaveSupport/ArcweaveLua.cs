using System;
using System.Collections.Generic;
using Language.Lua;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[AddComponentMenu("")]
public class ArcweaveLua : Saver
{
	[Serializable]
	public class SaveData
	{
		public List<string> guids = new List<string>();

		public List<int> visits = new List<int>();

		public string lastTextGuid;

		public SaveData()
		{
		}

		public SaveData(Dictionary<string, int> visitsDict, string lastTextGuid)
		{
			foreach (KeyValuePair<string, int> item in visitsDict)
			{
				guids.Add(item.Key);
				visits.Add(item.Value);
			}
			this.lastTextGuid = lastTextGuid;
		}
	}

	[Serializable]
	public class MultiplayerSaveData
	{
		public List<string> actorIndices = new List<string>();

		public List<SaveData> saveData = new List<SaveData>();
	}

	[Tooltip("Typically leave unticked so temporary Dialogue Managers don't unregister your functions.")]
	public bool unregisterOnDisable;

	[Tooltip("Support multiplayer Lua functions. Currently: visits() incorporates Variable['ActorIndex'].")]
	public bool multiplayer;

	protected Dictionary<string, Dictionary<string, int>> visitsDicts = new Dictionary<string, Dictionary<string, int>>();

	protected Dictionary<string, string> lastTextGuids = new Dictionary<string, string>();

	public override void OnEnable()
	{
		base.OnEnable();
		Lua.RegisterFunction("abs", null, SymbolExtensions.GetMethodInfo(() => abs(0.0)));
		Lua.RegisterFunction("sqr", null, SymbolExtensions.GetMethodInfo(() => sqr(0.0)));
		Lua.RegisterFunction("sqrt", null, SymbolExtensions.GetMethodInfo(() => sqrt(0.0)));
		Lua.RegisterFunction("random", null, SymbolExtensions.GetMethodInfo(() => random()));
		Lua.RegisterFunction("visits", this, SymbolExtensions.GetMethodInfo(() => visits(string.Empty)));
		Lua.environment.Register("roll", roll);
		Lua.environment.Register("show", show);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (unregisterOnDisable)
		{
			Lua.UnregisterFunction("abs");
			Lua.UnregisterFunction("sqr");
			Lua.UnregisterFunction("sqrt");
			Lua.UnregisterFunction("random");
			Lua.UnregisterFunction("roll");
			Lua.UnregisterFunction("show");
		}
	}

	public static double abs(double n)
	{
		return Mathf.Abs((float)n);
	}

	public static double sqr(double n)
	{
		return n * n;
	}

	public static double sqrt(double n)
	{
		return Mathf.Sqrt((float)n);
	}

	public static double random()
	{
		return UnityEngine.Random.value;
	}

	public static LuaValue roll(LuaValue[] values)
	{
		int num = (int)(values[0] as LuaNumber).Number;
		int num2 = ((values.Length <= 1 || !(values[1] is LuaNumber)) ? 1 : ((int)(values[1] as LuaNumber).Number));
		double num3 = 0.0;
		for (int i = 0; i < num2; i++)
		{
			num3 += (double)UnityEngine.Random.Range(1, num + 1);
		}
		return new LuaNumber(num3);
	}

	public static LuaValue show(LuaValue[] values)
	{
		string text = string.Empty;
		foreach (LuaValue luaValue in values)
		{
			text += luaValue.ToString();
		}
		return new LuaString(text);
	}

	protected virtual string GetActorIndex()
	{
		if (!multiplayer)
		{
			return "Player";
		}
		return DialogueLua.GetVariable("ActorIndex").asString;
	}

	protected virtual string GetLastTextGuid(string actorIndex)
	{
		if (!lastTextGuids.ContainsKey(actorIndex))
		{
			lastTextGuids.Add(actorIndex, string.Empty);
		}
		return lastTextGuids[actorIndex];
	}

	protected virtual Dictionary<string, int> GetVisitsDict(string actorIndex)
	{
		if (!visitsDicts.ContainsKey(actorIndex))
		{
			visitsDicts.Add(actorIndex, new Dictionary<string, int>());
		}
		return visitsDicts[actorIndex];
	}

	protected virtual void OnConversationLine(Subtitle subtitle)
	{
		if (string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			return;
		}
		string value = Field.LookupValue(subtitle.dialogueEntry.fields, "Guid");
		if (!string.IsNullOrEmpty(value))
		{
			string actorIndex = GetActorIndex();
			lastTextGuids[actorIndex] = value;
			Dictionary<string, int> visitsDict = GetVisitsDict(actorIndex);
			if (visitsDict.ContainsKey(value))
			{
				visitsDict[value]++;
			}
			else
			{
				visitsDict.Add(value, 1);
			}
		}
	}

	public double visits(string id)
	{
		string actorIndex = GetActorIndex();
		string text = ((!string.IsNullOrEmpty(id)) ? id : GetLastTextGuid(actorIndex));
		int value;
		return GetVisitsDict(actorIndex).TryGetValue(text, out value) ? value : 0;
	}

	public override string RecordData()
	{
		if (!multiplayer)
		{
			string actorIndex = GetActorIndex();
			return SaveSystem.Serialize(new SaveData(GetVisitsDict(actorIndex), GetLastTextGuid(actorIndex)));
		}
		MultiplayerSaveData multiplayerSaveData = new MultiplayerSaveData();
		foreach (KeyValuePair<string, Dictionary<string, int>> visitsDict in visitsDicts)
		{
			string text = visitsDict.Key;
			SaveData item = new SaveData(visitsDict.Value, GetLastTextGuid(text));
			multiplayerSaveData.actorIndices.Add(text);
			multiplayerSaveData.saveData.Add(item);
		}
		return SaveSystem.Serialize(multiplayerSaveData);
	}

	public override void ApplyData(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		if (!multiplayer)
		{
			SaveData saveData = SaveSystem.Deserialize<SaveData>(s);
			if (saveData != null)
			{
				string actorIndex = GetActorIndex();
				Dictionary<string, int> visitsDict = GetVisitsDict(actorIndex);
				visitsDict.Clear();
				for (int i = 0; i < Mathf.Min(saveData.guids.Count, saveData.visits.Count); i++)
				{
					visitsDict.Add(saveData.guids[i], saveData.visits[i]);
				}
				lastTextGuids[actorIndex] = saveData.lastTextGuid;
			}
			return;
		}
		MultiplayerSaveData multiplayerSaveData = SaveSystem.Deserialize<MultiplayerSaveData>(s);
		if (multiplayerSaveData == null)
		{
			return;
		}
		for (int j = 0; j < Mathf.Min(multiplayerSaveData.actorIndices.Count, multiplayerSaveData.saveData.Count); j++)
		{
			string actorIndex2 = multiplayerSaveData.actorIndices[j];
			SaveData saveData2 = multiplayerSaveData.saveData[j];
			Dictionary<string, int> visitsDict2 = GetVisitsDict(actorIndex2);
			visitsDict2.Clear();
			for (int k = 0; k < Mathf.Min(saveData2.guids.Count, saveData2.visits.Count); k++)
			{
				visitsDict2.Add(saveData2.guids[k], saveData2.visits[k]);
			}
			lastTextGuids[actorIndex2] = saveData2.lastTextGuid;
		}
	}
}
