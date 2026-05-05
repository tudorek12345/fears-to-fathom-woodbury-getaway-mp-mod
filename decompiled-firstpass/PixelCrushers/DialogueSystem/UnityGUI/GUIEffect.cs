using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

public abstract class GUIEffect : MonoBehaviour
{
	public GUIEffectTrigger trigger;

	public abstract IEnumerator Play();

	public virtual void Stop()
	{
		StopAllCoroutines();
	}

	public void OnEnable()
	{
		if (base.enabled && trigger == GUIEffectTrigger.OnEnable)
		{
			StartCoroutine(Play());
		}
	}

	public void OnDisable()
	{
		Stop();
	}
}
