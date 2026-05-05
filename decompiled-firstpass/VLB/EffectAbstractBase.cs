using System;
using UnityEngine;

namespace VLB;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class EffectAbstractBase : MonoBehaviour
{
	[Flags]
	public enum ComponentsToChange
	{
		UnityLight = 1,
		VolumetricLightBeam = 2,
		VolumetricDustParticles = 4
	}

	public const string ClassName = "EffectAbstractBase";

	public ComponentsToChange componentsToChange = (ComponentsToChange)2147483647;

	public bool restoreBaseIntensity = true;

	protected VolumetricLightBeam m_Beam;

	protected Light m_Light;

	protected VolumetricDustParticles m_Particles;

	protected float m_BaseIntensityBeamInside;

	protected float m_BaseIntensityBeamOutside;

	protected float m_BaseIntensityLight;

	protected void SetAdditiveIntensity(float additive)
	{
		if (componentsToChange.HasFlag(ComponentsToChange.VolumetricLightBeam) && (bool)m_Beam)
		{
			m_Beam.intensityInside = Mathf.Max(0f, m_BaseIntensityBeamInside + additive);
			m_Beam.intensityOutside = Mathf.Max(0f, m_BaseIntensityBeamOutside + additive);
		}
		if (componentsToChange.HasFlag(ComponentsToChange.UnityLight) && (bool)m_Light)
		{
			m_Light.intensity = Mathf.Max(0f, m_BaseIntensityLight + additive);
		}
		if (componentsToChange.HasFlag(ComponentsToChange.VolumetricDustParticles) && (bool)m_Particles)
		{
			m_Particles.alphaAdditionalRuntime = 1f + additive;
		}
	}

	private void Awake()
	{
		m_Beam = GetComponent<VolumetricLightBeam>();
		m_Light = GetComponent<Light>();
		m_Particles = GetComponent<VolumetricDustParticles>();
		m_BaseIntensityBeamInside = (m_Beam ? m_Beam.intensityInside : 0f);
		m_BaseIntensityBeamOutside = (m_Beam ? m_Beam.intensityOutside : 0f);
		m_BaseIntensityLight = (m_Light ? m_Light.intensity : 0f);
	}

	protected virtual void OnEnable()
	{
		StopAllCoroutines();
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		if (restoreBaseIntensity)
		{
			SetAdditiveIntensity(0f);
		}
	}
}
