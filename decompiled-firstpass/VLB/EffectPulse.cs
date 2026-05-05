using System.Collections;
using UnityEngine;

namespace VLB;

[HelpURL("http://saladgamer.com/vlb-doc/comp-effect-pulse.html")]
public class EffectPulse : EffectAbstractBase
{
	public new const string ClassName = "EffectPulse";

	[Range(0.1f, 60f)]
	public float frequency = 10f;

	[MinMaxRange(-5f, 5f)]
	public MinMaxRangeFloat intensityAmplitude = Consts.Effects.IntensityAmplitudeDefault;

	protected override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(CoUpdate());
	}

	private IEnumerator CoUpdate()
	{
		float t = 0f;
		while (true)
		{
			float num = Mathf.Sin(frequency * t);
			float lerpedValue = intensityAmplitude.GetLerpedValue(num * 0.5f + 0.5f);
			SetAdditiveIntensity(lerpedValue);
			yield return null;
			t += Time.deltaTime;
		}
	}
}
