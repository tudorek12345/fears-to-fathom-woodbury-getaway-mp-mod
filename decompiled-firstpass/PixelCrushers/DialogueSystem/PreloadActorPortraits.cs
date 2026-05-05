using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PreloadActorPortraits : MonoBehaviour
{
	[Tooltip("Preload for Unity UI.")]
	public bool supportUnityUI;

	[Tooltip("If preloading for Unity UI, collapse legacy textures to save memory. Dialogue Manager's Instantiate Database must be ticked.")]
	public bool collapseLegacyTextures;

	private List<Texture2D> legacyPortraits = new List<Texture2D>();

	private void Start()
	{
		if (DialogueManager.instance == null || DialogueManager.databaseManager == null || DialogueManager.masterDatabase == null)
		{
			return;
		}
		if (collapseLegacyTextures && !DialogueManager.instance.instantiateDatabase)
		{
			Debug.LogWarning("Dialogue System: Dialogue Manager's Instantiate Database checkbox isn't ticked. Can't collapse legacy textures.", DialogueManager.instance);
			collapseLegacyTextures = false;
		}
		List<Actor> actors = DialogueManager.masterDatabase.actors;
		if (actors != null)
		{
			for (int i = 0; i < actors.Count; i++)
			{
				PreloadActor(actors[i]);
			}
		}
	}

	public void PreloadActor(Actor actor)
	{
		if (actor == null)
		{
			return;
		}
		actor.portrait = PreloadTexture(actor.portrait);
		if (actor.alternatePortraits != null)
		{
			for (int i = 0; i < actor.alternatePortraits.Count; i++)
			{
				actor.alternatePortraits[i] = PreloadTexture(actor.alternatePortraits[i]);
			}
		}
	}

	public Texture2D PreloadTexture(Texture2D texture)
	{
		if (texture == null)
		{
			return null;
		}
		if (supportUnityUI)
		{
			Sprite value = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
			if (collapseLegacyTextures)
			{
				texture = new Texture2D(2, 2);
			}
			UITools.spriteCache.Add(texture, value);
		}
		legacyPortraits.Add(texture);
		return texture;
	}
}
