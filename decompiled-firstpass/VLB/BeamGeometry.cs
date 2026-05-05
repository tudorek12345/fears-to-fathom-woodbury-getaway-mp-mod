using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLB;

[AddComponentMenu("")]
[ExecuteInEditMode]
[HelpURL("http://saladgamer.com/vlb-doc/comp-lightbeam.html")]
public class BeamGeometry : MonoBehaviour, MaterialModifier.Interface
{
	private VolumetricLightBeam m_Master;

	private Matrix4x4 m_ColorGradientMatrix;

	private MeshType m_CurrentMeshType;

	private Material m_CustomMaterial;

	private MaterialModifier.Callback m_MaterialModifierCallback;

	private Coroutine m_CoFadeOut;

	private Camera m_CurrentCameraRenderingSRP;

	public MeshRenderer meshRenderer { get; private set; }

	public MeshFilter meshFilter { get; private set; }

	public Mesh coneMesh { get; private set; }

	public bool visible
	{
		get
		{
			return meshRenderer.enabled;
		}
		set
		{
			meshRenderer.enabled = value;
		}
	}

	public int sortingLayerID
	{
		get
		{
			return meshRenderer.sortingLayerID;
		}
		set
		{
			meshRenderer.sortingLayerID = value;
		}
	}

	public int sortingOrder
	{
		get
		{
			return meshRenderer.sortingOrder;
		}
		set
		{
			meshRenderer.sortingOrder = value;
		}
	}

	public bool _INTERNAL_IsFadeOutCoroutineRunning => m_CoFadeOut != null;

	public static bool isCustomRenderPipelineSupported => true;

	private bool shouldUseGPUInstancedMaterial
	{
		get
		{
			if (m_Master._INTERNAL_DynamicOcclusionMode != MaterialManager.DynamicOcclusion.DepthTexture)
			{
				return Config.Instance.actualRenderingMode == RenderingMode.GPUInstancing;
			}
			return false;
		}
	}

	private bool isNoiseEnabled
	{
		get
		{
			if (m_Master.isNoiseEnabled && m_Master.noiseIntensity > 0f)
			{
				return Noise3D.isSupported;
			}
			return false;
		}
	}

	private bool isDepthBlendEnabled
	{
		get
		{
			if (!BatchingHelper.forceEnableDepthBlend)
			{
				return m_Master.depthBlendDistance > 0f;
			}
			return true;
		}
	}

	private float ComputeFadeOutFactor(Transform camTransform)
	{
		if (m_Master.isFadeOutEnabled)
		{
			float value = Vector3.SqrMagnitude(meshRenderer.bounds.center - camTransform.position);
			return Mathf.InverseLerp(m_Master.fadeOutEnd * m_Master.fadeOutEnd, m_Master.fadeOutBegin * m_Master.fadeOutBegin, value);
		}
		return 1f;
	}

	private IEnumerator CoUpdateFadeOut()
	{
		while (m_Master.isFadeOutEnabled)
		{
			ComputeFadeOutFactor();
			yield return null;
		}
		SetFadeOutFactorProp(1f);
		m_CoFadeOut = null;
	}

	private void ComputeFadeOutFactor()
	{
		Transform fadeOutCameraTransform = Config.Instance.fadeOutCameraTransform;
		if ((bool)fadeOutCameraTransform)
		{
			float fadeOutFactorProp = ComputeFadeOutFactor(fadeOutCameraTransform);
			SetFadeOutFactorProp(fadeOutFactorProp);
		}
		else
		{
			SetFadeOutFactorProp(1f);
		}
	}

	private void SetFadeOutFactorProp(float value)
	{
		if (value > 0f)
		{
			meshRenderer.enabled = true;
			MaterialChangeStart();
			SetMaterialProp(ShaderProperties.FadeOutFactor, value);
			MaterialChangeStop();
		}
		else
		{
			meshRenderer.enabled = false;
		}
	}

	public void RestartFadeOutCoroutine()
	{
		if (m_CoFadeOut != null)
		{
			StopCoroutine(m_CoFadeOut);
			m_CoFadeOut = null;
		}
		if ((bool)m_Master && m_Master.isFadeOutEnabled)
		{
			m_CoFadeOut = StartCoroutine(CoUpdateFadeOut());
		}
	}

	private void Start()
	{
		if (!m_Master)
		{
			UnityEngine.Object.DestroyImmediate(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if ((bool)m_CustomMaterial)
		{
			UnityEngine.Object.DestroyImmediate(m_CustomMaterial);
			m_CustomMaterial = null;
		}
	}

	private void OnDisable()
	{
		SRPHelper.UnregisterOnBeginCameraRendering(OnBeginCameraRenderingSRP);
		m_CurrentCameraRenderingSRP = null;
	}

	private void OnEnable()
	{
		RestartFadeOutCoroutine();
		SRPHelper.RegisterOnBeginCameraRendering(OnBeginCameraRenderingSRP);
	}

	public void Initialize(VolumetricLightBeam master)
	{
		HideFlags proceduralObjectsHideFlags = Consts.Internal.ProceduralObjectsHideFlags;
		m_Master = master;
		base.transform.SetParent(master.transform, worldPositionStays: false);
		meshRenderer = base.gameObject.GetOrAddComponent<MeshRenderer>();
		meshRenderer.hideFlags = proceduralObjectsHideFlags;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;
		meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		meshRenderer.lightProbeUsage = LightProbeUsage.Off;
		if (!shouldUseGPUInstancedMaterial)
		{
			m_CustomMaterial = MaterialManager.NewMaterialTransient(gpuInstanced: false);
			ApplyMaterial();
		}
		if (SortingLayer.IsValid(m_Master.sortingLayerID))
		{
			sortingLayerID = m_Master.sortingLayerID;
		}
		else
		{
			Debug.LogError($"Beam '{Utils.GetPath(m_Master.transform)}' has an invalid sortingLayerID ({m_Master.sortingLayerID}). Please fix it by setting a valid layer.");
		}
		sortingOrder = m_Master.sortingOrder;
		meshFilter = base.gameObject.GetOrAddComponent<MeshFilter>();
		meshFilter.hideFlags = proceduralObjectsHideFlags;
		base.gameObject.hideFlags = proceduralObjectsHideFlags;
		RestartFadeOutCoroutine();
	}

	public void RegenerateMesh()
	{
		if (Config.Instance.geometryOverrideLayer)
		{
			base.gameObject.layer = Config.Instance.geometryLayerID;
		}
		else
		{
			base.gameObject.layer = m_Master.gameObject.layer;
		}
		base.gameObject.tag = Config.Instance.geometryTag;
		if ((bool)coneMesh && m_CurrentMeshType == MeshType.Custom)
		{
			UnityEngine.Object.DestroyImmediate(coneMesh);
		}
		m_CurrentMeshType = m_Master.geomMeshType;
		switch (m_Master.geomMeshType)
		{
		case MeshType.Custom:
			coneMesh = MeshGenerator.GenerateConeZ_Radius(1f, 1f, 1f, m_Master.geomCustomSides, m_Master.geomCustomSegments, m_Master.geomCap, Config.Instance.requiresDoubleSidedMesh);
			coneMesh.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
			meshFilter.mesh = coneMesh;
			break;
		case MeshType.Shared:
			coneMesh = GlobalMesh.Get();
			meshFilter.sharedMesh = coneMesh;
			break;
		default:
			Debug.LogError("Unsupported MeshType");
			break;
		}
		UpdateMaterialAndBounds();
	}

	private Vector3 ComputeLocalMatrix()
	{
		float num = Mathf.Max(m_Master.coneRadiusStart, m_Master.coneRadiusEnd);
		base.transform.localScale = new Vector3(num, num, m_Master.maxGeometryDistance);
		base.transform.localRotation = m_Master.beamInternalLocalRotation;
		return base.transform.localScale;
	}

	private bool ApplyMaterial()
	{
		MaterialManager.ColorGradient colorGradient = MaterialManager.ColorGradient.Off;
		if (m_Master.colorMode == ColorMode.Gradient)
		{
			colorGradient = ((Utils.GetFloatPackingPrecision() != Utils.FloatPackingPrecision.High) ? MaterialManager.ColorGradient.MatrixLow : MaterialManager.ColorGradient.MatrixHigh);
		}
		MaterialManager.StaticProperties staticProps = new MaterialManager.StaticProperties
		{
			blendingMode = (MaterialManager.BlendingMode)m_Master.blendingMode,
			noise3D = (isNoiseEnabled ? MaterialManager.Noise3D.On : MaterialManager.Noise3D.Off),
			depthBlend = (isDepthBlendEnabled ? MaterialManager.DepthBlend.On : MaterialManager.DepthBlend.Off),
			colorGradient = colorGradient,
			dynamicOcclusion = m_Master._INTERNAL_DynamicOcclusionMode_Runtime,
			meshSkewing = (m_Master.hasMeshSkewing ? MaterialManager.MeshSkewing.On : MaterialManager.MeshSkewing.Off),
			shaderAccuracy = ((m_Master.shaderAccuracy != ShaderAccuracy.Fast) ? MaterialManager.ShaderAccuracy.High : MaterialManager.ShaderAccuracy.Fast)
		};
		Material material = null;
		if (!shouldUseGPUInstancedMaterial)
		{
			material = m_CustomMaterial;
			if ((bool)material)
			{
				staticProps.ApplyToMaterial(material);
			}
		}
		else
		{
			material = MaterialManager.GetInstancedMaterial(m_Master._INTERNAL_InstancedMaterialGroupID, ref staticProps);
		}
		meshRenderer.material = material;
		return material != null;
	}

	public void SetMaterialProp(int nameID, float value)
	{
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetFloat(nameID, value);
		}
		else
		{
			MaterialManager.materialPropertyBlock.SetFloat(nameID, value);
		}
	}

	public void SetMaterialProp(int nameID, Vector4 value)
	{
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetVector(nameID, value);
		}
		else
		{
			MaterialManager.materialPropertyBlock.SetVector(nameID, value);
		}
	}

	public void SetMaterialProp(int nameID, Color value)
	{
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetColor(nameID, value);
		}
		else
		{
			MaterialManager.materialPropertyBlock.SetColor(nameID, value);
		}
	}

	public void SetMaterialProp(int nameID, Matrix4x4 value)
	{
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetMatrix(nameID, value);
		}
		else
		{
			MaterialManager.materialPropertyBlock.SetMatrix(nameID, value);
		}
	}

	public void SetMaterialProp(int nameID, Texture value)
	{
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetTexture(nameID, value);
		}
		else
		{
			Debug.LogError("Setting a Texture property to a GPU instanced material is not supported");
		}
	}

	private void MaterialChangeStart()
	{
		if (m_CustomMaterial == null)
		{
			meshRenderer.GetPropertyBlock(MaterialManager.materialPropertyBlock);
		}
	}

	private void MaterialChangeStop()
	{
		if (m_CustomMaterial == null)
		{
			meshRenderer.SetPropertyBlock(MaterialManager.materialPropertyBlock);
		}
	}

	public void SetDynamicOcclusionCallback(string shaderKeyword, MaterialModifier.Callback cb)
	{
		m_MaterialModifierCallback = cb;
		if ((bool)m_CustomMaterial)
		{
			m_CustomMaterial.SetKeywordEnabled(shaderKeyword, cb != null);
			cb?.Invoke(this);
		}
		else
		{
			UpdateMaterialAndBounds();
		}
	}

	public void UpdateMaterialAndBounds()
	{
		if (!ApplyMaterial())
		{
			return;
		}
		MaterialChangeStart();
		if (m_CustomMaterial == null && m_MaterialModifierCallback != null)
		{
			m_MaterialModifierCallback(this);
		}
		float f = m_Master.coneAngle * (MathF.PI / 180f) / 2f;
		SetMaterialProp(ShaderProperties.ConeSlopeCosSin, new Vector2(Mathf.Cos(f), Mathf.Sin(f)));
		SetMaterialProp(value: new Vector2(Mathf.Max(m_Master.coneRadiusStart, 0.0001f), Mathf.Max(m_Master.coneRadiusEnd, 0.0001f)), nameID: ShaderProperties.ConeRadius);
		float value = Mathf.Sign(m_Master.coneApexOffsetZ) * Mathf.Max(Mathf.Abs(m_Master.coneApexOffsetZ), 0.0001f);
		SetMaterialProp(ShaderProperties.ConeApexOffsetZ, value);
		if (m_Master.usedColorMode == ColorMode.Flat)
		{
			SetMaterialProp(ShaderProperties.ColorFlat, m_Master.color);
		}
		else
		{
			Utils.FloatPackingPrecision floatPackingPrecision = Utils.GetFloatPackingPrecision();
			m_ColorGradientMatrix = m_Master.colorGradient.SampleInMatrix((int)floatPackingPrecision);
		}
		m_Master.GetInsideAndOutsideIntensity(out var inside, out var outside);
		SetMaterialProp(ShaderProperties.AlphaInside, inside);
		SetMaterialProp(ShaderProperties.AlphaOutside, outside);
		SetMaterialProp(ShaderProperties.AttenuationLerpLinearQuad, m_Master.attenuationLerpLinearQuad);
		SetMaterialProp(ShaderProperties.DistanceFallOff, new Vector3(m_Master.fallOffStart, m_Master.fallOffEnd, m_Master.maxGeometryDistance));
		SetMaterialProp(ShaderProperties.DistanceCamClipping, m_Master.cameraClippingDistance);
		SetMaterialProp(ShaderProperties.FresnelPow, Mathf.Max(0.001f, m_Master.fresnelPow));
		SetMaterialProp(ShaderProperties.GlareBehind, m_Master.glareBehind);
		SetMaterialProp(ShaderProperties.GlareFrontal, m_Master.glareFrontal);
		SetMaterialProp(ShaderProperties.DrawCap, m_Master.geomCap ? 1 : 0);
		SetMaterialProp(ShaderProperties.TiltVector, m_Master.tiltFactor);
		SetMaterialProp(ShaderProperties.AdditionalClippingPlaneWS, m_Master.additionalClippingPlane);
		if (isDepthBlendEnabled)
		{
			SetMaterialProp(ShaderProperties.DepthBlendDistance, m_Master.depthBlendDistance);
		}
		if (isNoiseEnabled)
		{
			Noise3D.LoadIfNeeded();
			Vector3 vector = (m_Master.noiseVelocityUseGlobal ? Config.Instance.globalNoiseVelocity : m_Master.noiseVelocityLocal);
			float w = (m_Master.noiseScaleUseGlobal ? Config.Instance.globalNoiseScale : m_Master.noiseScaleLocal);
			SetMaterialProp(ShaderProperties.NoiseVelocityAndScale, new Vector4(vector.x, vector.y, vector.z, w));
			SetMaterialProp(ShaderProperties.NoiseParam, new Vector2(m_Master.noiseIntensity, (m_Master.noiseMode == NoiseMode.WorldSpace) ? 0f : 1f));
		}
		Vector3 vector2 = ComputeLocalMatrix();
		if (m_Master.hasMeshSkewing)
		{
			Vector3 skewingLocalForwardDirectionNormalized = m_Master.skewingLocalForwardDirectionNormalized;
			SetMaterialProp(ShaderProperties.LocalForwardDirection, skewingLocalForwardDirectionNormalized);
			if (coneMesh != null)
			{
				Vector3 vector3 = skewingLocalForwardDirectionNormalized;
				vector3 /= vector3.z;
				vector3 *= m_Master.fallOffEnd;
				vector3.x /= vector2.x;
				vector3.y /= vector2.y;
				Bounds bounds = MeshGenerator.ComputeBounds(1f, 1f, 1f);
				Vector3 min = bounds.min;
				Vector3 max = bounds.max;
				if (vector3.x > 0f)
				{
					max.x += vector3.x;
				}
				else
				{
					min.x += vector3.x;
				}
				if (vector3.y > 0f)
				{
					max.y += vector3.y;
				}
				else
				{
					min.y += vector3.y;
				}
				bounds.min = min;
				bounds.max = max;
				coneMesh.bounds = bounds;
			}
		}
		UpdateMatricesPropertiesForGPUInstancingSRP();
		MaterialChangeStop();
	}

	private void UpdateMatricesPropertiesForGPUInstancingSRP()
	{
		if (SRPHelper.IsUsingCustomRenderPipeline() && Config.Instance.actualRenderingMode == RenderingMode.GPUInstancing)
		{
			SetMaterialProp(ShaderProperties.LocalToWorldMatrix, base.transform.localToWorldMatrix);
			SetMaterialProp(ShaderProperties.WorldToLocalMatrix, base.transform.worldToLocalMatrix);
		}
	}

	private void OnBeginCameraRenderingSRP(ScriptableRenderContext context, Camera cam)
	{
		m_CurrentCameraRenderingSRP = cam;
	}

	private void OnWillRenderObject()
	{
		Camera camera = null;
		camera = ((!SRPHelper.IsUsingCustomRenderPipeline()) ? Camera.current : m_CurrentCameraRenderingSRP);
		OnWillCameraRenderThisBeam(camera);
	}

	private void OnWillCameraRenderThisBeam(Camera cam)
	{
		if ((bool)m_Master && (bool)cam && cam.enabled)
		{
			UpdateCameraRelatedProperties(cam);
			m_Master._INTERNAL_OnWillCameraRenderThisBeam(cam);
		}
	}

	private void UpdateCameraRelatedProperties(Camera cam)
	{
		if ((bool)cam && (bool)m_Master)
		{
			MaterialChangeStart();
			Vector3 posOS = m_Master.transform.InverseTransformPoint(cam.transform.position);
			Vector3 normalized = base.transform.InverseTransformDirection(cam.transform.forward).normalized;
			float w = (cam.orthographic ? (-1f) : m_Master.GetInsideBeamFactorFromObjectSpacePos(posOS));
			SetMaterialProp(ShaderProperties.CameraParams, new Vector4(normalized.x, normalized.y, normalized.z, w));
			UpdateMatricesPropertiesForGPUInstancingSRP();
			if (m_Master.usedColorMode == ColorMode.Gradient)
			{
				SetMaterialProp(ShaderProperties.ColorGradientMatrix, m_ColorGradientMatrix);
			}
			MaterialChangeStop();
			if (m_Master.depthBlendDistance > 0f)
			{
				cam.depthTextureMode |= DepthTextureMode.Depth;
			}
		}
	}
}
