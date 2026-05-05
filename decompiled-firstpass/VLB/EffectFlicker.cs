using System.Collections;
using UnityEngine;

namespace VLB;

[HelpURL("http://saladgamer.com/vlb-doc/comp-effect-flicker.html")]
public class EffectFlicker : EffectAbstractBase
{
	public new const string ClassName = "EffectFlicker";

	[Range(1f, 60f)]
	public float frequency = 10f;

	public bool performPauses;

	[MinMaxRange(0f, 10f)]
	public MinMaxRangeFloat flickeringDuration = Consts.Effects.FlickeringDurationDefault;

	[MinMaxRange(0f, 10f)]
	public MinMaxRangeFloat pauseDuration = Consts.Effects.PauseDurationDefault;

	[MinMaxRange(-5f, 5f)]
	public MinMaxRangeFloat intensityAmplitude = Consts.Effects.IntensityAmplitudeDefault;

	[Range(0f, 0.25f)]
	public float smoothing = 0.05f;

	private float m_CurrentAdditiveIntensity;

	protected override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(CoUpdate());
	}

	private IEnumerator CoUpdate()
	{
		while (true)
		{
			yield return CoFlicker();
			float remaining = pauseDuration.randomValue;
			do
			{
				remaining -= Time.deltaTime;
				yield return null;
			}
			while (performPauses && remaining > 0f);
		}
	}

	private IEnumerator CoFlicker()
	{
		float remainingDuration = flickeringDuration.randomValue;
		_ = Time.deltaTime;
		while (!performPauses || remainingDuration > 0f)
		{
			float freqDuration = 1f / frequency;
			yield return CoChangeIntensity(freqDuration, intensityAmplitude.randomValue);
			remainingDuration -= freqDuration;
		}
	}

	private IEnumerator CoChangeIntensity(float expectedDuration, float nextIntensity)
	{
		float velocity = 0f;
		float t = 0f;
		while (t < expectedDuration)
		{
			m_CurrentAdditiveIntensity = Mathf.SmoothDamp(m_CurrentAdditiveIntensity, nextIntensity, ref velocity, smoothing);
			SetAdditiveIntensity(m_CurrentAdditiveIntensity);
			t += Time.deltaTime;
			yield return null;
		}
	}
}
