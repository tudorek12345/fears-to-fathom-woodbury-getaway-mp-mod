using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class CharacterInfo
{
	public int id;

	public string nameInDatabase;

	public CharacterType characterType;

	public Transform transform;

	public Sprite portrait;

	private static Dictionary<string, Transform> registeredActorTransforms = new Dictionary<string, Transform>();

	public bool isPlayer => characterType == CharacterType.PC;

	public bool isNPC => characterType == CharacterType.NPC;

	public string Name { get; set; }

	public bool IsPlayer => isPlayer;

	public bool IsNPC => isNPC;

	public CharacterInfo(int id, string nameInDatabase, Transform transform, CharacterType characterType, Sprite portrait)
	{
		this.id = id;
		this.nameInDatabase = nameInDatabase;
		this.characterType = characterType;
		this.portrait = portrait;
		this.transform = transform;
		if (transform == null && !string.IsNullOrEmpty(nameInDatabase))
		{
			GameObject gameObject = SequencerTools.FindSpecifier(nameInDatabase, onlyActiveInScene: true);
			if (gameObject != null)
			{
				this.transform = gameObject.transform;
			}
		}
		DialogueActor dialogueActorComponent = DialogueActor.GetDialogueActorComponent(transform);
		if (dialogueActorComponent == null)
		{
			Name = GetLocalizedDisplayNameInDatabase(nameInDatabase);
			return;
		}
		Name = dialogueActorComponent.GetActorName();
		Actor actor = DialogueManager.masterDatabase.GetActor(dialogueActorComponent.actor);
		Sprite portraitSprite = dialogueActorComponent.GetPortraitSprite();
		if (portraitSprite != null)
		{
			this.portrait = portraitSprite;
		}
		else if (actor != null && portrait == null)
		{
			this.portrait = actor.GetPortraitSprite();
		}
	}

	public static string GetLocalizedDisplayNameInDatabase(string nameInDatabase)
	{
		string text = DialogueLua.GetLocalizedActorField(nameInDatabase, "Display Name").asString;
		if (string.IsNullOrEmpty(text) || string.Equals(text, "nil"))
		{
			text = DialogueLua.GetLocalizedActorField(nameInDatabase, "Name").asString;
		}
		if (string.IsNullOrEmpty(text) || string.Equals(text, "nil"))
		{
			text = nameInDatabase;
		}
		return FormattedText.ParseCode(text);
	}

	public Sprite GetPicOverride(int picNum)
	{
		if (picNum < 2)
		{
			return portrait;
		}
		int num = picNum - 2;
		Actor actor = DialogueManager.masterDatabase.GetActor(id);
		if (actor == null || num >= actor.alternatePortraits.Count)
		{
			if (actor == null || num >= actor.spritePortraits.Count)
			{
				return portrait;
			}
			return actor.spritePortraits[num];
		}
		return UITools.CreateSprite(actor.alternatePortraits[num]);
	}

	public static void RegisterActorTransform(string actorName, Transform actorTransform)
	{
		if (string.IsNullOrEmpty(actorName) || actorTransform == null)
		{
			return;
		}
		if (registeredActorTransforms.ContainsKey(actorName))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.LogWarning("Dialogue System: Registering transform " + actorTransform.name + " as actor '" + actorName + "' but another transform is already registered. Overwriting with new transform.", actorTransform);
			}
			registeredActorTransforms[actorName] = actorTransform;
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Registering transform " + actorTransform.name + " as actor '" + actorName + "'.", actorTransform);
			}
			registeredActorTransforms.Add(actorName, actorTransform);
		}
	}

	public static void UnregisterActorTransform(string actorName, Transform actorTransform)
	{
		if (!string.IsNullOrEmpty(actorName) && !(actorTransform == null) && registeredActorTransforms.ContainsKey(actorName))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Unregistering transform " + actorTransform.name + " from actor '" + actorName + "'.", actorTransform);
			}
			registeredActorTransforms.Remove(actorName);
		}
	}

	public static Transform GetRegisteredActorTransform(string actorName)
	{
		if (!registeredActorTransforms.ContainsKey(actorName))
		{
			return null;
		}
		return registeredActorTransforms[actorName];
	}

	public static List<Transform> GetAllRegisteredActorTransforms()
	{
		return new List<Transform>(registeredActorTransforms.Values);
	}
}
