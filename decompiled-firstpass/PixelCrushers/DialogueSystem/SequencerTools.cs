using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers.DialogueSystem;

public static class SequencerTools
{
	private static Dictionary<string, Transform> registeredSubjects = new Dictionary<string, Transform>();

	private static bool hasHookedIntoSceneLoaded = false;

	public static void HookIntoSceneLoaded()
	{
		if (!hasHookedIntoSceneLoaded)
		{
			hasHookedIntoSceneLoaded = true;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}
	}

	private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
	{
		CleanNullSubjects();
	}

	public static void RegisterSubject(Transform subject)
	{
		if (!(subject == null))
		{
			registeredSubjects[subject.name] = subject;
			HookIntoSceneLoaded();
		}
	}

	public static void UnregisterSubject(Transform subject)
	{
		if (!(subject == null) && registeredSubjects.ContainsKey(subject.name))
		{
			registeredSubjects.Remove(subject.name);
		}
	}

	public static void CleanNullSubjects()
	{
		registeredSubjects.RemoveAll((string x) => x == null);
	}

	public static Transform GetSubject(string specifier, Transform speaker, Transform listener, Transform defaultSubject = null)
	{
		if (string.IsNullOrEmpty(specifier))
		{
			return defaultSubject ?? speaker;
		}
		if (string.Compare(specifier, "speaker", StringComparison.OrdinalIgnoreCase) == 0)
		{
			return speaker;
		}
		if (string.Compare(specifier, "listener", StringComparison.OrdinalIgnoreCase) == 0)
		{
			return listener;
		}
		if (string.Compare(specifier, "speakerportrait", StringComparison.OrdinalIgnoreCase) == 0)
		{
			return GetPortraitImage(speaker);
		}
		if (string.Compare(specifier, "listenerportrait", StringComparison.OrdinalIgnoreCase) == 0)
		{
			return GetPortraitImage(listener);
		}
		if (specifier.StartsWith("actor:"))
		{
			return CharacterInfo.GetRegisteredActorTransform(specifier.Substring("actor:".Length));
		}
		GameObject gameObject = FindSpecifier(specifier);
		if (!(gameObject != null))
		{
			return defaultSubject;
		}
		return gameObject.transform;
	}

	public static bool SpecifierSpecifiesTag(string specifier)
	{
		if (!string.IsNullOrEmpty(specifier))
		{
			return specifier.StartsWith("tag=", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static string GetSpecifiedTag(string specifier)
	{
		return specifier.Substring("tag=".Length);
	}

	public static GameObject FindSpecifier(string specifier, bool onlyActiveInScene = false)
	{
		if (string.IsNullOrEmpty(specifier))
		{
			return null;
		}
		if (SpecifierSpecifiesTag(specifier))
		{
			string specifiedTag = GetSpecifiedTag(specifier);
			GameObject gameObject = GameObject.FindGameObjectWithTag(specifiedTag);
			if (gameObject != null)
			{
				return gameObject;
			}
			GameObject[] array = Tools.FindGameObjectsWithTagHard(specifiedTag);
			if (array.Length == 0)
			{
				return null;
			}
			return array[0];
		}
		Transform value = CharacterInfo.GetRegisteredActorTransform(specifier);
		if (value != null)
		{
			return value.gameObject;
		}
		if (registeredSubjects.TryGetValue(specifier, out value) && value != null)
		{
			return value.gameObject;
		}
		GameObject gameObject2 = GameObject.Find(specifier);
		if (gameObject2 != null)
		{
			return gameObject2;
		}
		if (onlyActiveInScene)
		{
			return null;
		}
		gameObject2 = Tools.GameObjectHardFind(specifier);
		if (gameObject2 != null)
		{
			return gameObject2;
		}
		GameObject[] array2 = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
		foreach (GameObject gameObject3 in array2)
		{
			if (string.Compare(specifier, gameObject3.name, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return gameObject3;
			}
		}
		return null;
	}

	public static Transform GetPortraitImage(Transform subject)
	{
		if (DialogueManager.standardDialogueUI == null)
		{
			return null;
		}
		if (subject == null)
		{
			return null;
		}
		StandardUISubtitleControls standardSubtitleControls = DialogueManager.standardDialogueUI.conversationUIElements.standardSubtitleControls;
		StandardUISubtitlePanel standardUISubtitlePanel = null;
		if (DialogueManager.isConversationActive && DialogueManager.currentConversationState != null)
		{
			Subtitle subtitle = DialogueManager.currentConversationState.subtitle;
			if (subtitle.speakerInfo != null && subtitle.speakerInfo.transform == subject)
			{
				standardUISubtitlePanel = standardSubtitleControls.GetPanel(subtitle, out var _);
			}
		}
		if (standardUISubtitlePanel == null)
		{
			StandardUISubtitlePanel defaultPanel = standardSubtitleControls.defaultNPCPanel;
			DialogueActor dialogueActor = DialogueActor.GetDialogueActorComponent(subject);
			if (dialogueActor != null)
			{
				Actor actor = DialogueManager.masterDatabase.GetActor(dialogueActor.actor);
				if (actor != null)
				{
					defaultPanel = (actor.IsPlayer ? standardSubtitleControls.defaultPCPanel : standardSubtitleControls.defaultNPCPanel);
				}
			}
			standardUISubtitlePanel = standardSubtitleControls.GetActorTransformPanel(subject, defaultPanel, out dialogueActor);
		}
		if (!(standardUISubtitlePanel != null) || !((UnityEngine.Object)(object)standardUISubtitlePanel.portraitImage != null))
		{
			return null;
		}
		return ((Component)(object)standardUISubtitlePanel.portraitImage).transform;
	}

	public static string GetDefaultCameraAngle(Transform subject)
	{
		DefaultCameraAngle defaultCameraAngle = ((subject != null) ? subject.GetComponentInChildren<DefaultCameraAngle>() : null);
		if (!(defaultCameraAngle != null))
		{
			return "Closeup";
		}
		return defaultCameraAngle.cameraAngle;
	}

	public static string GetParameter(string[] parameters, int i, string defaultValue = null)
	{
		if (parameters == null || i >= parameters.Length)
		{
			return defaultValue;
		}
		return parameters[i];
	}

	public static T GetParameterAs<T>(string[] parameters, int i, T defaultValue)
	{
		try
		{
			return (parameters != null && i < parameters.Length) ? ((T)Convert.ChangeType(parameters[i], typeof(T), CultureInfo.InvariantCulture)) : defaultValue;
		}
		catch (Exception)
		{
			return defaultValue;
		}
	}

	public static float GetParameterAsFloat(string[] parameters, int i, float defaultValue = 0f)
	{
		return GetParameterAs(parameters, i, defaultValue);
	}

	public static int GetParameterAsInt(string[] parameters, int i, int defaultValue = 0)
	{
		return GetParameterAs(parameters, i, defaultValue);
	}

	public static bool GetParameterAsBool(string[] parameters, int i, bool defaultValue = false)
	{
		return GetParameterAs(parameters, i, defaultValue);
	}

	public static AudioSource GetAudioSource(Transform subject)
	{
		GameObject gameObject = ((subject != null) ? subject.gameObject : DialogueManager.instance.gameObject);
		AudioSource audioSource = gameObject.GetComponentInChildren<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.loop = false;
		}
		return audioSource;
	}

	public static bool IsAudioMuted()
	{
		return DialogueLua.GetVariable("Mute").asBool;
	}
}
