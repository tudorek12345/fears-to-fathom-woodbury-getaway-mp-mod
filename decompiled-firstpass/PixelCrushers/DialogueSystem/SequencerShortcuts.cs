using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SequencerShortcuts : MonoBehaviour
{
	[Serializable]
	public class Shortcut
	{
		[Tooltip("Shortcut. Wrap in double braces to reference in sequences, such as {{foo}}.")]
		public string shortcut;

		[Tooltip("Value to replace shortcut with.")]
		[TextArea]
		public string value;

		[Tooltip("Menu of the shortcut.")]
		[TextArea]
		public string subMenu;
	}

	public Shortcut[] shortcuts = new Shortcut[0];

	[Tooltip("Optionally assign GameObjects referenced by name in sequencer commands below. Prevents having to search for them at runtime.")]
	public Transform[] referencedSubjects = new Transform[0];

	private void OnEnable()
	{
		for (int i = 0; i < shortcuts.Length; i++)
		{
			Sequencer.RegisterShortcut(shortcuts[i].shortcut, shortcuts[i].value);
		}
		for (int j = 0; j < referencedSubjects.Length; j++)
		{
			SequencerTools.RegisterSubject(referencedSubjects[j]);
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < shortcuts.Length; i++)
		{
			Sequencer.UnregisterShortcut(shortcuts[i].shortcut);
		}
		for (int j = 0; j < referencedSubjects.Length; j++)
		{
			SequencerTools.UnregisterSubject(referencedSubjects[j]);
		}
	}
}
