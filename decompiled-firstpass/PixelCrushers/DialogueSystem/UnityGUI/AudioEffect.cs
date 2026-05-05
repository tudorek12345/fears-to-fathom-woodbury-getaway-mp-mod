using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class AudioEffect : GUIEffect
{
	private AudioSource myAudio;

	public void Awake()
	{
		myAudio = GetComponent<AudioSource>();
	}

	public override IEnumerator Play()
	{
		if (myAudio != null)
		{
			myAudio.Play();
		}
		yield return null;
	}
}
