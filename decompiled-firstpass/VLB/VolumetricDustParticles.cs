using System;
using UnityEngine;

namespace VLB;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(VolumetricLightBeam))]
[HelpURL("http://saladgamer.com/vlb-doc/comp-dustparticles.html")]
public class VolumetricDustParticles : MonoBehaviour
{
	public const string ClassName = "VolumetricDustParticles";

	[Range(0f, 1f)]
	public float alpha = 0.5f;

	[Range(0.0001f, 0.1f)]
	public float size = 0.01f;

	public ParticlesDirection direction;

	public Vector3 velocity = Consts.DustParticles.VelocityDefault;

	[Obsolete("Use 'velocity' instead")]
	public float speed = 0.03f;

	public float density = 5f;

	[MinMaxRange(0f, 1f)]
	public MinMaxRangeFloat spawnDistanceRange = Consts.DustParticles.SpawnDistanceRangeDefault;

	[Obsolete("Use 'spawnDistanceRange' instead")]
	public float spawnMinDistance;

	[Obsolete("Use 'spawnDistanceRange' instead")]
	public float spawnMaxDistance = 0.7f;

	public bool cullingEnabled;

	public float cullingMaxDistance = 10f;

	[SerializeField]
	private float m_AlphaAdditionalRuntime = 1f;

	private ParticleSystem m_Particles;

	private ParticleSystemRenderer m_Renderer;

	private Material m_Material;

	private Gradient m_GradientCached = new Gradient();

	private bool m_RuntimePropertiesDirty = true;

	private static bool ms_NoMainCameraLogged;

	private static Camera ms_MainCamera;

	private VolumetricLightBeam m_Master;

	public bool isCulled { get; private set; }

	public float alphaAdditionalRuntime
	{
		get
		{
			return m_AlphaAdditionalRuntime;
		}
		set
		{
			if (m_AlphaAdditionalRuntime != value)
			{
				m_AlphaAdditionalRuntime = value;
				m_RuntimePropertiesDirty = true;
			}
		}
	}

	public bool particlesAreInstantiated => (UnityEngine.Object)(object)m_Particles;

	public int particlesCurrentCount
	{
		get
		{
			if (!(UnityEngine.Object)(object)m_Particles)
			{
				return 0;
			}
			return m_Particles.particleCount;
		}
	}

	public int particlesMaxCount
	{
		get
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (!(UnityEngine.Object)(object)m_Particles)
			{
				return 0;
			}
			MainModule main = m_Particles.main;
			return ((MainModule)(ref main)).maxParticles;
		}
	}

	public Camera mainCamera
	{
		get
		{
			if (!ms_MainCamera)
			{
				ms_MainCamera = Camera.main;
				if (!ms_MainCamera && !ms_NoMainCameraLogged)
				{
					Debug.LogErrorFormat(base.gameObject, "In order to use 'VolumetricDustParticles' culling, you must have a MainCamera defined in your scene.");
					ms_NoMainCameraLogged = true;
				}
			}
			return ms_MainCamera;
		}
	}

	private void Start()
	{
		isCulled = false;
		m_Master = GetComponent<VolumetricLightBeam>();
		HandleBackwardCompatibility(m_Master._INTERNAL_pluginVersion, 1960);
		InstantiateParticleSystem();
		SetActiveAndPlay();
	}

	private void InstantiateParticleSystem()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int num = componentsInChildren.Length - 1; num >= 0; num--)
		{
			UnityEngine.Object.DestroyImmediate(((Component)(object)componentsInChildren[num]).gameObject);
		}
		m_Particles = Config.Instance.NewVolumetricDustParticles();
		if ((bool)(UnityEngine.Object)(object)m_Particles)
		{
			((Component)(object)m_Particles).transform.SetParent(base.transform, worldPositionStays: false);
			((Component)(object)m_Particles).transform.localRotation = m_Master.beamInternalLocalRotation;
			m_Renderer = ((Component)(object)m_Particles).GetComponent<ParticleSystemRenderer>();
			m_Material = new Material(((Renderer)(object)m_Renderer).sharedMaterial);
			((Renderer)(object)m_Renderer).material = m_Material;
		}
	}

	private void OnEnable()
	{
		SetActiveAndPlay();
	}

	private void SetActive(bool active)
	{
		if ((bool)(UnityEngine.Object)(object)m_Particles)
		{
			((Component)(object)m_Particles).gameObject.SetActive(active);
		}
	}

	private void SetActiveAndPlay()
	{
		SetActive(active: true);
		Play();
	}

	private void Play()
	{
		if ((bool)(UnityEngine.Object)(object)m_Particles)
		{
			SetParticleProperties();
			m_Particles.Simulate(0f);
			m_Particles.Play(true);
		}
	}

	private void OnDisable()
	{
		SetActive(active: false);
	}

	private void OnDestroy()
	{
		if ((bool)(UnityEngine.Object)(object)m_Particles)
		{
			UnityEngine.Object.DestroyImmediate(((Component)(object)m_Particles).gameObject);
			m_Particles = null;
		}
		if ((bool)m_Material)
		{
			UnityEngine.Object.DestroyImmediate(m_Material);
			m_Material = null;
		}
	}

	private void Update()
	{
		UpdateCulling();
		if (m_Master.trackChangesDuringPlaytime)
		{
			SetParticleProperties();
		}
		if (m_RuntimePropertiesDirty && m_Material != null)
		{
			m_Material.SetColor(ShaderProperties.ParticlesTintColor, new Color(1f, 1f, 1f, alphaAdditionalRuntime));
			m_RuntimePropertiesDirty = false;
		}
	}

	private void SetParticleProperties()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		if (!(UnityEngine.Object)(object)m_Particles || !((Component)(object)m_Particles).gameObject.activeSelf)
		{
			return;
		}
		float t = Mathf.Clamp01(1f - m_Master.fresnelPow / 10f);
		float num = m_Master.fallOffEnd * (spawnDistanceRange.maxValue - spawnDistanceRange.minValue);
		float num2 = num * density;
		int maxParticles = (int)(num2 * 4f);
		MainModule main = m_Particles.main;
		MinMaxCurve startLifetime = ((MainModule)(ref main)).startLifetime;
		((MinMaxCurve)(ref startLifetime)).mode = (ParticleSystemCurveMode)3;
		((MinMaxCurve)(ref startLifetime)).constantMin = 4f;
		((MinMaxCurve)(ref startLifetime)).constantMax = 6f;
		((MainModule)(ref main)).startLifetime = startLifetime;
		MinMaxCurve startSize = ((MainModule)(ref main)).startSize;
		((MinMaxCurve)(ref startSize)).mode = (ParticleSystemCurveMode)3;
		((MinMaxCurve)(ref startSize)).constantMin = size * 0.9f;
		((MinMaxCurve)(ref startSize)).constantMax = size * 1.1f;
		((MainModule)(ref main)).startSize = startSize;
		MinMaxGradient startColor = ((MainModule)(ref main)).startColor;
		if (m_Master.usedColorMode == ColorMode.Flat)
		{
			((MinMaxGradient)(ref startColor)).mode = (ParticleSystemGradientMode)0;
			Color color = m_Master.color;
			color.a *= alpha;
			((MinMaxGradient)(ref startColor)).color = color;
		}
		else
		{
			((MinMaxGradient)(ref startColor)).mode = (ParticleSystemGradientMode)1;
			Gradient colorGradient = m_Master.colorGradient;
			GradientColorKey[] colorKeys = colorGradient.colorKeys;
			GradientAlphaKey[] alphaKeys = colorGradient.alphaKeys;
			for (int i = 0; i < alphaKeys.Length; i++)
			{
				alphaKeys[i].alpha *= alpha;
			}
			m_GradientCached.SetKeys(colorKeys, alphaKeys);
			((MinMaxGradient)(ref startColor)).gradient = m_GradientCached;
		}
		((MainModule)(ref main)).startColor = startColor;
		MinMaxCurve startSpeed = ((MainModule)(ref main)).startSpeed;
		((MinMaxCurve)(ref startSpeed)).constant = ((direction == ParticlesDirection.Random) ? Mathf.Abs(velocity.z) : 0f);
		((MainModule)(ref main)).startSpeed = startSpeed;
		VelocityOverLifetimeModule velocityOverLifetime = m_Particles.velocityOverLifetime;
		((VelocityOverLifetimeModule)(ref velocityOverLifetime)).enabled = direction != ParticlesDirection.Random;
		((VelocityOverLifetimeModule)(ref velocityOverLifetime)).space = (ParticleSystemSimulationSpace)(direction != ParticlesDirection.LocalSpace);
		((VelocityOverLifetimeModule)(ref velocityOverLifetime)).xMultiplier = velocity.x;
		((VelocityOverLifetimeModule)(ref velocityOverLifetime)).yMultiplier = velocity.y;
		((VelocityOverLifetimeModule)(ref velocityOverLifetime)).zMultiplier = velocity.z;
		((MainModule)(ref main)).maxParticles = maxParticles;
		ShapeModule shape = m_Particles.shape;
		((ShapeModule)(ref shape)).shapeType = (ParticleSystemShapeType)8;
		float num3 = m_Master.coneAngle * Mathf.Lerp(0.7f, 1f, t);
		((ShapeModule)(ref shape)).angle = num3 * 0.5f;
		float a = m_Master.coneRadiusStart * Mathf.Lerp(0.3f, 1f, t);
		float b = Utils.ComputeConeRadiusEnd(m_Master.fallOffEnd, num3);
		((ShapeModule)(ref shape)).radius = Mathf.Lerp(a, b, spawnDistanceRange.minValue);
		((ShapeModule)(ref shape)).length = num;
		float z = m_Master.fallOffEnd * spawnDistanceRange.minValue;
		((ShapeModule)(ref shape)).position = new Vector3(0f, 0f, z);
		((ShapeModule)(ref shape)).arc = 360f;
		((ShapeModule)(ref shape)).randomDirectionAmount = ((direction == ParticlesDirection.Random) ? 1f : 0f);
		EmissionModule emission = m_Particles.emission;
		MinMaxCurve rateOverTime = ((EmissionModule)(ref emission)).rateOverTime;
		((MinMaxCurve)(ref rateOverTime)).constant = num2;
		((EmissionModule)(ref emission)).rateOverTime = rateOverTime;
		if ((bool)(UnityEngine.Object)(object)m_Renderer)
		{
			((Renderer)(object)m_Renderer).sortingLayerID = m_Master.sortingLayerID;
			((Renderer)(object)m_Renderer).sortingOrder = m_Master.sortingOrder;
		}
	}

	private void HandleBackwardCompatibility(int serializedVersion, int newVersion)
	{
		if (serializedVersion == -1 || serializedVersion == newVersion)
		{
			return;
		}
		if (serializedVersion < 1880)
		{
			if (direction == ParticlesDirection.Random)
			{
				direction = ParticlesDirection.LocalSpace;
			}
			else
			{
				direction = ParticlesDirection.Random;
			}
			velocity = new Vector3(0f, 0f, speed);
		}
		if (serializedVersion < 1940)
		{
			spawnDistanceRange = new MinMaxRangeFloat(spawnMinDistance, spawnMaxDistance);
		}
		Utils.MarkCurrentSceneDirty();
	}

	private void UpdateCulling()
	{
		if (!(UnityEngine.Object)(object)m_Particles)
		{
			return;
		}
		bool flag = true;
		if ((cullingEnabled || m_Master.isFadeOutEnabled) && m_Master.hasGeometry)
		{
			if ((bool)mainCamera)
			{
				float num = cullingMaxDistance;
				if (m_Master.isFadeOutEnabled)
				{
					num = Mathf.Min(num, m_Master.fadeOutEnd);
				}
				float num2 = num * num;
				flag = m_Master.bounds.SqrDistance(mainCamera.transform.position) <= num2;
			}
			else
			{
				cullingEnabled = false;
			}
		}
		if (((Component)(object)m_Particles).gameObject.activeSelf != flag)
		{
			SetActive(flag);
			isCulled = !flag;
		}
		if (flag && !m_Particles.isPlaying)
		{
			m_Particles.Play();
		}
	}
}
