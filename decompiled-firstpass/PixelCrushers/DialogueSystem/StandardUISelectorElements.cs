using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUISelectorElements : MonoBehaviour
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	[HelpBox("If a GameObject with a Selector or Proximity Selector has a Selector Use Standard UI Elements component, it will use these UI elements.", HelpBoxMessageType.None)]
	[Tooltip("(Optional) Main panel. Assign if you have created an entire panel for selector.")]
	public Graphic mainGraphic;

	[Tooltip("Text element for name of current selection.")]
	public UITextField nameText;

	[Tooltip("Text element for use message (e.g., 'Press spacebar to use').")]
	public UITextField useMessageText;

	[Tooltip("Use In Range and Out Of Range text colors defined below.")]
	public bool useRangeColors = true;

	[Tooltip("Set text elements to this color when selector is in range to use selection.")]
	public Color inRangeColor = Color.yellow;

	[Tooltip("Set text elements to this color when selector is out of range.")]
	public Color outOfRangeColor = Color.gray;

	[Tooltip("Optional graphic to show if selection is in range.")]
	public Graphic reticleInRange;

	[Tooltip("Optional graphic to show if selection is out of range.")]
	public Graphic reticleOutOfRange;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	private static List<StandardUISelectorElements> m_instances = new List<StandardUISelectorElements>();

	public Animator animator { get; private set; }

	public static List<StandardUISelectorElements> instances => m_instances;

	public static StandardUISelectorElements instance
	{
		get
		{
			if (m_instances.Count <= 0)
			{
				return null;
			}
			return m_instances[0];
		}
	}

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void OnEnable()
	{
		m_instances.Add(this);
	}

	private void OnDisable()
	{
		m_instances.Remove(this);
	}
}
