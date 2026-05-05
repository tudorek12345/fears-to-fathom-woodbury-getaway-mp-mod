using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace VLB;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[SelectionBase]
[HelpURL("http://saladgamer.com/vlb-doc/comp-lightbeam.html")]
public class VolumetricLightBeam : MonoBehaviour
{
	public delegate void OnWillCameraRenderCB(Camera cam);

	public delegate void OnBeamGeometryInitialized();

	public enum AttachedLightType
	{
		NoLight,
		OtherLight,
		SpotLight
	}

	public const string ClassName = "VolumetricLightBeam";

	public bool colorFromLight = true;

	public ColorMode colorMode;

	[ColorUsage(false, true)]
	[FormerlySerializedAs("colorValue")]
	public Color color = Consts.Beam.FlatColor;

	public Gradient colorGradient;

	public bool intensityFromLight = true;

	public bool intensityModeAdvanced;

	[FormerlySerializedAs("alphaInside")]
	public float intensityInside = 1f;

	[FormerlySerializedAs("alphaOutside")]
	[FormerlySerializedAs("alpha")]
	public float intensityOutside = 1f;

	public BlendingMode blendingMode;

	[FormerlySerializedAs("angleFromLight")]
	public bool spotAngleFromLight = true;

	[Range(0.1f, 179.9f)]
	public float spotAngle = 35f;

	[FormerlySerializedAs("radiusStart")]
	public float coneRadiusStart = 0.1f;

	public ShaderAccuracy shaderAccuracy;

	public MeshType geomMeshType;

	[FormerlySerializedAs("geomSides")]
	public int geomCustomSides = 18;

	public int geomCustomSegments = 5;

	public Vector3 skewingLocalForwardDirection = Consts.Beam.SkewingLocalForwardDirectionDefault;

	public Transform clippingPlaneTransform;

	public bool geomCap;

	[FormerlySerializedAs("fadeEndFromLight")]
	public bool fallOffEndFromLight = true;

	public AttenuationEquation attenuationEquation = AttenuationEquation.Quadratic;

	[Range(0f, 1f)]
	public float attenuationCustomBlending = 0.5f;

	[FormerlySerializedAs("fadeStart")]
	public float fallOffStart;

	[FormerlySerializedAs("fadeEnd")]
	public float fallOffEnd = 3f;

	public float depthBlendDistance = 2f;

	public float cameraClippingDistance = 0.5f;

	[Range(0f, 1f)]
	public float glareFrontal = 0.5f;

	[Range(0f, 1f)]
	public float glareBehind = 0.5f;

	[FormerlySerializedAs("fresnelPowOutside")]
	public float fresnelPow = 8f;

	public NoiseMode noiseMode;

	[Range(0f, 1f)]
	public float noiseIntensity = 0.5f;

	public bool noiseScaleUseGlobal = true;

	[Range(0.01f, 2f)]
	public float noiseScaleLocal = 0.5f;

	public bool noiseVelocityUseGlobal = true;

	public Vector3 noiseVelocityLocal = Consts.Beam.NoiseVelocityDefault;

	public Dimensions dimensions;

	public Vector2 tiltFactor = Consts.Beam.TiltDefault;

	private MaterialManager.DynamicOcclusion m_INTERNAL_DynamicOcclusionMode;

	private bool m_INTERNAL_DynamicOcclusionMode_Runtime;

	private OnBeamGeometryInitialized m_OnBeamGeometryInitialized;

	[SerializeField]
	private int pluginVersion = -1;

	[FormerlySerializedAs("trackChangesDuringPlaytime")]
	[SerializeField]
	private bool _TrackChangesDuringPlaytime;

	[SerializeField]
	private int _SortingLayerID;

	[SerializeField]
	private int _SortingOrder;

	[FormerlySerializedAs("fadeOutBegin")]
	[SerializeField]
	private float _FadeOutBegin = -150f;

	[FormerlySerializedAs("fadeOutEnd")]
	[SerializeField]
	private float _FadeOutEnd = -200f;

	private BeamGeometry m_BeamGeom;

	private Coroutine m_CoPlaytimeUpdate;

	private Light m_CachedLight;

	public ColorMode usedColorMode
	{
		get
		{
			if (Config.Instance.featureEnabledColorGradient == FeatureEnabledColorGradient.Off)
			{
				return ColorMode.Flat;
			}
			return colorMode;
		}
	}

	[Obsolete("Use 'intensityGlobal' or 'intensityInside' instead")]
	public float alphaInside
	{
		get
		{
			return intensityInside;
		}
		set
		{
			intensityInside = value;
		}
	}

	[Obsolete("Use 'intensityGlobal' or 'intensityOutside' instead")]
	public float alphaOutside
	{
		get
		{
			return intensityOutside;
		}
		set
		{
			intensityOutside = value;
		}
	}

	public float intensityGlobal
	{
		get
		{
			return intensityOutside;
		}
		set
		{
			intensityInside = value;
			intensityOutside = value;
		}
	}

	public float coneAngle => Mathf.Atan2(coneRadiusEnd - coneRadiusStart, maxGeometryDistance) * 57.29578f * 2f;

	public float coneRadiusEnd
	{
		get
		{
			return Utils.ComputeConeRadiusEnd(maxGeometryDistance, spotAngle);
		}
		set
		{
			spotAngle = Utils.ComputeSpotAngle(maxGeometryDistance, value);
		}
	}

	public float coneVolume
	{
		get
		{
			float num = coneRadiusStart;
			float num2 = coneRadiusEnd;
			return MathF.PI / 3f * (num * num + num * num2 + num2 * num2) * fallOffEnd;
		}
	}

	public float coneApexOffsetZ
	{
		get
		{
			float num = coneRadiusStart / coneRadiusEnd;
			if (num != 1f)
			{
				return maxGeometryDistance * num / (1f - num);
			}
			return float.MaxValue;
		}
	}

	public Vector3 coneApexPositionLocal => new Vector3(0f, 0f, 0f - coneApexOffsetZ);

	public Vector3 coneApexPositionGlobal => base.transform.localToWorldMatrix.MultiplyPoint(coneApexPositionLocal);

	public int geomSides
	{
		get
		{
			if (geomMeshType != MeshType.Custom)
			{
				return Config.Instance.sharedMeshSides;
			}
			return geomCustomSides;
		}
		set
		{
			geomCustomSides = value;
			Debug.LogWarning("The setter VLB.VolumetricLightBeam.geomSides is OBSOLETE and has been renamed to geomCustomSides.");
		}
	}

	public int geomSegments
	{
		get
		{
			if (geomMeshType != MeshType.Custom)
			{
				return Config.Instance.sharedMeshSegments;
			}
			return geomCustomSegments;
		}
		set
		{
			geomCustomSegments = value;
			Debug.LogWarning("The setter VLB.VolumetricLightBeam.geomSegments is OBSOLETE and has been renamed to geomCustomSegments.");
		}
	}

	public Vector3 skewingLocalForwardDirectionNormalized
	{
		get
		{
			if (Mathf.Approximately(skewingLocalForwardDirection.z, 0f))
			{
				Debug.LogErrorFormat("Beam {0} has a skewingLocalForwardDirection with a null Z, which is forbidden", base.name);
				return Vector3.forward;
			}
			return skewingLocalForwardDirection.normalized;
		}
	}

	public Vector4 additionalClippingPlane
	{
		get
		{
			if (!(clippingPlaneTransform == null))
			{
				return Utils.PlaneEquation(clippingPlaneTransform.forward, clippingPlaneTransform.position);
			}
			return Vector4.zero;
		}
	}

	public bool canHaveMeshSkewing => geomMeshType == MeshType.Custom;

	public bool hasMeshSkewing
	{
		get
		{
			if (!Config.Instance.featureEnabledMeshSkewing)
			{
				return false;
			}
			if (!canHaveMeshSkewing)
			{
				return false;
			}
			if (Mathf.Approximately(Vector3.Dot(skewingLocalForwardDirectionNormalized, Vector3.forward), 1f))
			{
				return false;
			}
			return true;
		}
	}

	[Obsolete("Use 'fallOffEndFromLight' instead")]
	public bool fadeEndFromLight
	{
		get
		{
			return fallOffEndFromLight;
		}
		set
		{
			fallOffEndFromLight = value;
		}
	}

	public float attenuationLerpLinearQuad
	{
		get
		{
			if (attenuationEquation == AttenuationEquation.Linear)
			{
				return 0f;
			}
			if (attenuationEquation == AttenuationEquation.Quadratic)
			{
				return 1f;
			}
			return attenuationCustomBlending;
		}
	}

	[Obsolete("Use 'fallOffStart' instead")]
	public float fadeStart
	{
		get
		{
			return fallOffStart;
		}
		set
		{
			fallOffStart = value;
		}
	}

	[Obsolete("Use 'fallOffEnd' instead")]
	public float fadeEnd
	{
		get
		{
			return fallOffEnd;
		}
		set
		{
			fallOffEnd = value;
		}
	}

	public float maxGeometryDistance => fallOffEnd + Mathf.Max(Mathf.Abs(tiltFactor.x), Mathf.Abs(tiltFactor.y));

	public bool isNoiseEnabled => noiseMode != NoiseMode.Disabled;

	[Obsolete("Use 'noiseMode' instead")]
	public bool noiseEnabled
	{
		get
		{
			return isNoiseEnabled;
		}
		set
		{
			noiseMode = (value ? NoiseMode.WorldSpace : NoiseMode.Disabled);
		}
	}

	public float fadeOutBegin
	{
		get
		{
			return _FadeOutBegin;
		}
		set
		{
			SetFadeOutValue(ref _FadeOutBegin, value);
		}
	}

	public float fadeOutEnd
	{
		get
		{
			return _FadeOutEnd;
		}
		set
		{
			SetFadeOutValue(ref _FadeOutEnd, value);
		}
	}

	public bool isFadeOutEnabled
	{
		get
		{
			if (_FadeOutBegin >= 0f)
			{
				return _FadeOutEnd >= 0f;
			}
			return false;
		}
	}

	public bool isTilted => !tiltFactor.Approximately(Vector2.zero);

	public int sortingLayerID
	{
		get
		{
			return _SortingLayerID;
		}
		set
		{
			_SortingLayerID = value;
			if ((bool)m_BeamGeom)
			{
				m_BeamGeom.sortingLayerID = value;
			}
		}
	}

	public string sortingLayerName
	{
		get
		{
			return SortingLayer.IDToName(sortingLayerID);
		}
		set
		{
			sortingLayerID = SortingLayer.NameToID(value);
		}
	}

	public int sortingOrder
	{
		get
		{
			return _SortingOrder;
		}
		set
		{
			_SortingOrder = value;
			if ((bool)m_BeamGeom)
			{
				m_BeamGeom.sortingOrder = value;
			}
		}
	}

	public bool trackChangesDuringPlaytime
	{
		get
		{
			return _TrackChangesDuringPlaytime;
		}
		set
		{
			_TrackChangesDuringPlaytime = value;
			StartPlaytimeUpdateIfNeeded();
		}
	}

	public bool isCurrentlyTrackingChanges => m_CoPlaytimeUpdate != null;

	public bool hasGeometry => m_BeamGeom != null;

	public Bounds bounds
	{
		get
		{
			if (!(m_BeamGeom != null))
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
			return m_BeamGeom.meshRenderer.bounds;
		}
	}

	public int blendingModeAsInt => Mathf.Clamp((int)blendingMode, 0, Enum.GetValues(typeof(BlendingMode)).Length);

	public Quaternion beamInternalLocalRotation
	{
		get
		{
			if (dimensions != Dimensions.Dim3D)
			{
				return Quaternion.LookRotation(Vector3.right, Vector3.up);
			}
			return Quaternion.identity;
		}
	}

	public Vector3 beamLocalForward
	{
		get
		{
			if (dimensions != Dimensions.Dim3D)
			{
				return Vector3.right;
			}
			return Vector3.forward;
		}
	}

	public Vector3 beamGlobalForward => base.transform.localToWorldMatrix.MultiplyVector(beamLocalForward);

	public Vector3 lossyScale
	{
		get
		{
			if (dimensions != Dimensions.Dim3D)
			{
				return new Vector3(base.transform.lossyScale.z, base.transform.lossyScale.y, base.transform.lossyScale.x);
			}
			return base.transform.lossyScale;
		}
	}

	public float raycastDistance
	{
		get
		{
			if (!hasMeshSkewing)
			{
				return maxGeometryDistance;
			}
			float z = skewingLocalForwardDirectionNormalized.z;
			if (!Mathf.Approximately(z, 0f))
			{
				return maxGeometryDistance / z;
			}
			return maxGeometryDistance;
		}
	}

	public Vector3 raycastGlobalForward
	{
		get
		{
			Vector3 vector = base.transform.forward;
			if (hasMeshSkewing)
			{
				vector = base.transform.TransformDirection(skewingLocalForwardDirectionNormalized);
			}
			return beamInternalLocalRotation * vector;
		}
	}

	public Vector3 raycastGlobalUp => beamInternalLocalRotation * base.transform.up;

	public Vector3 raycastGlobalRight => beamInternalLocalRotation * base.transform.right;

	public MaterialManager.DynamicOcclusion _INTERNAL_DynamicOcclusionMode
	{
		get
		{
			if (!Config.Instance.featureEnabledDynamicOcclusion)
			{
				return MaterialManager.DynamicOcclusion.Off;
			}
			return m_INTERNAL_DynamicOcclusionMode;
		}
		set
		{
			m_INTERNAL_DynamicOcclusionMode = value;
		}
	}

	public MaterialManager.DynamicOcclusion _INTERNAL_DynamicOcclusionMode_Runtime
	{
		get
		{
			if (!m_INTERNAL_DynamicOcclusionMode_Runtime)
			{
				return MaterialManager.DynamicOcclusion.Off;
			}
			return _INTERNAL_DynamicOcclusionMode;
		}
	}

	public int _INTERNAL_pluginVersion => pluginVersion;

	public uint _INTERNAL_InstancedMaterialGroupID { get; protected set; }

	public string meshStats
	{
		get
		{
			Mesh mesh = (m_BeamGeom ? m_BeamGeom.coneMesh : null);
			if ((bool)mesh)
			{
				return $"Cone angle: {coneAngle:0.0} degrees\nMesh: {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles";
			}
			return "no mesh available";
		}
	}

	public int meshVerticesCount
	{
		get
		{
			if (!m_BeamGeom || !m_BeamGeom.coneMesh)
			{
				return 0;
			}
			return m_BeamGeom.coneMesh.vertexCount;
		}
	}

	public int meshTrianglesCount
	{
		get
		{
			if (!m_BeamGeom || !m_BeamGeom.coneMesh)
			{
				return 0;
			}
			return m_BeamGeom.coneMesh.triangles.Length / 3;
		}
	}

	private Light lightSpotAttached => m_CachedLight;

	public event OnWillCameraRenderCB onWillCameraRenderThisBeam;

	public void GetInsideAndOutsideIntensity(out float inside, out float outside)
	{
		if (intensityModeAdvanced)
		{
			inside = intensityInside;
			outside = intensityOutside;
		}
		else
		{
			inside = (outside = intensityOutside);
		}
	}

	public void _INTERNAL_SetDynamicOcclusionCallback(string shaderKeyword, MaterialModifier.Callback cb)
	{
		m_INTERNAL_DynamicOcclusionMode_Runtime = cb != null;
		if ((bool)m_BeamGeom)
		{
			m_BeamGeom.SetDynamicOcclusionCallback(shaderKeyword, cb);
		}
	}

	public void _INTERNAL_OnWillCameraRenderThisBeam(Camera cam)
	{
		if (this.onWillCameraRenderThisBeam != null)
		{
			this.onWillCameraRenderThisBeam(cam);
		}
	}

	public void RegisterOnBeamGeometryInitializedCallback(OnBeamGeometryInitialized cb)
	{
		m_OnBeamGeometryInitialized = (OnBeamGeometryInitialized)Delegate.Combine(m_OnBeamGeometryInitialized, cb);
		if ((bool)m_BeamGeom)
		{
			CallOnBeamGeometryInitializedCallback();
		}
	}

	private void CallOnBeamGeometryInitializedCallback()
	{
		if (m_OnBeamGeometryInitialized != null)
		{
			m_OnBeamGeometryInitialized();
			m_OnBeamGeometryInitialized = null;
		}
	}

	private void SetFadeOutValue(ref float propToChange, float value)
	{
		bool flag = isFadeOutEnabled;
		propToChange = value;
		if (isFadeOutEnabled != flag)
		{
			OnFadeOutStateChanged();
		}
	}

	private void OnFadeOutStateChanged()
	{
		if (isFadeOutEnabled && (bool)m_BeamGeom)
		{
			m_BeamGeom.RestartFadeOutCoroutine();
		}
	}

	public Light GetLightSpotAttachedSlow(out AttachedLightType lightType)
	{
		Light component = GetComponent<Light>();
		if ((bool)component)
		{
			if (component.type == LightType.Spot)
			{
				lightType = AttachedLightType.SpotLight;
				return component;
			}
			lightType = AttachedLightType.OtherLight;
			return null;
		}
		lightType = AttachedLightType.NoLight;
		return null;
	}

	private void InitLightSpotAttachedCached()
	{
		m_CachedLight = GetLightSpotAttachedSlow(out var _);
	}

	public float GetInsideBeamFactor(Vector3 posWS)
	{
		return GetInsideBeamFactorFromObjectSpacePos(base.transform.InverseTransformPoint(posWS));
	}

	public float GetInsideBeamFactorFromObjectSpacePos(Vector3 posOS)
	{
		if (dimensions == Dimensions.Dim2D)
		{
			posOS = new Vector3(posOS.z, posOS.y, posOS.x);
		}
		if (posOS.z < 0f)
		{
			return -1f;
		}
		Vector2 vector = posOS.xy();
		if (hasMeshSkewing)
		{
			Vector3 aVector = skewingLocalForwardDirectionNormalized;
			vector -= aVector.xy() * (posOS.z / aVector.z);
		}
		Vector2 normalized = new Vector2(vector.magnitude, posOS.z + coneApexOffsetZ).normalized;
		return Mathf.Clamp((Mathf.Abs(Mathf.Sin(coneAngle * (MathF.PI / 180f) / 2f)) - Mathf.Abs(normalized.x)) / 0.1f, -1f, 1f);
	}

	[Obsolete("Use 'GenerateGeometry()' instead")]
	public void Generate()
	{
		GenerateGeometry();
	}

	public virtual void GenerateGeometry()
	{
		HandleBackwardCompatibility(pluginVersion, 1960);
		pluginVersion = 1960;
		ValidateProperties();
		if (m_BeamGeom == null)
		{
			m_BeamGeom = Utils.NewWithComponent<BeamGeometry>("Beam Geometry");
			m_BeamGeom.Initialize(this);
			CallOnBeamGeometryInitializedCallback();
		}
		m_BeamGeom.RegenerateMesh();
		m_BeamGeom.visible = base.enabled;
	}

	public virtual void UpdateAfterManualPropertyChange()
	{
		ValidateProperties();
		if ((bool)m_BeamGeom)
		{
			m_BeamGeom.UpdateMaterialAndBounds();
		}
	}

	private void Start()
	{
		InitLightSpotAttachedCached();
		GenerateGeometry();
	}

	private void OnEnable()
	{
		if ((bool)m_BeamGeom)
		{
			m_BeamGeom.visible = true;
		}
		StartPlaytimeUpdateIfNeeded();
	}

	private void OnDisable()
	{
		if ((bool)m_BeamGeom)
		{
			m_BeamGeom.visible = false;
		}
		m_CoPlaytimeUpdate = null;
	}

	private void StartPlaytimeUpdateIfNeeded()
	{
		if (Application.isPlaying && trackChangesDuringPlaytime && m_CoPlaytimeUpdate == null)
		{
			m_CoPlaytimeUpdate = StartCoroutine(CoPlaytimeUpdate());
		}
	}

	private IEnumerator CoPlaytimeUpdate()
	{
		while (trackChangesDuringPlaytime && base.enabled)
		{
			UpdateAfterManualPropertyChange();
			yield return null;
		}
		m_CoPlaytimeUpdate = null;
	}

	private void OnDestroy()
	{
		DestroyBeam();
	}

	private void DestroyBeam()
	{
		if ((bool)m_BeamGeom)
		{
			UnityEngine.Object.DestroyImmediate(m_BeamGeom.gameObject);
		}
		m_BeamGeom = null;
	}

	private void AssignPropertiesFromSpotLight(Light lightSpot)
	{
		if ((bool)lightSpot && lightSpot.type == LightType.Spot)
		{
			if (intensityFromLight)
			{
				intensityModeAdvanced = false;
				intensityGlobal = lightSpot.intensity;
			}
			if (fallOffEndFromLight)
			{
				fallOffEnd = lightSpot.range;
			}
			if (spotAngleFromLight)
			{
				spotAngle = lightSpot.spotAngle;
			}
			if (colorFromLight)
			{
				colorMode = ColorMode.Flat;
				color = lightSpot.color;
			}
		}
	}

	private void ClampProperties()
	{
		intensityInside = Mathf.Max(intensityInside, 0f);
		intensityOutside = Mathf.Max(intensityOutside, 0f);
		attenuationCustomBlending = Mathf.Clamp01(attenuationCustomBlending);
		fallOffEnd = Mathf.Max(0.01f, fallOffEnd);
		fallOffStart = Mathf.Clamp(fallOffStart, 0f, fallOffEnd - 0.01f);
		spotAngle = Mathf.Clamp(spotAngle, 0.1f, 179.9f);
		coneRadiusStart = Mathf.Max(coneRadiusStart, 0f);
		depthBlendDistance = Mathf.Max(depthBlendDistance, 0f);
		cameraClippingDistance = Mathf.Max(cameraClippingDistance, 0f);
		geomCustomSides = Mathf.Clamp(geomCustomSides, 3, 256);
		geomCustomSegments = Mathf.Clamp(geomCustomSegments, 0, 64);
		fresnelPow = Mathf.Max(0f, fresnelPow);
		glareBehind = Mathf.Clamp01(glareBehind);
		glareFrontal = Mathf.Clamp01(glareFrontal);
		noiseIntensity = Mathf.Clamp(noiseIntensity, 0f, 1f);
	}

	private void ValidateProperties()
	{
		AssignPropertiesFromSpotLight(lightSpotAttached);
		ClampProperties();
	}

	private void HandleBackwardCompatibility(int serializedVersion, int newVersion)
	{
		if (serializedVersion != -1 && serializedVersion != newVersion)
		{
			if (serializedVersion < 1301)
			{
				attenuationEquation = AttenuationEquation.Linear;
			}
			if (serializedVersion < 1501)
			{
				geomMeshType = MeshType.Custom;
				geomCustomSegments = 5;
			}
			if (serializedVersion < 1610)
			{
				intensityFromLight = false;
				intensityModeAdvanced = !Mathf.Approximately(intensityInside, intensityOutside);
			}
			if (serializedVersion < 1910 && !intensityModeAdvanced && !Mathf.Approximately(intensityInside, intensityOutside))
			{
				intensityInside = intensityOutside;
			}
			Utils.MarkCurrentSceneDirty();
		}
	}
}
