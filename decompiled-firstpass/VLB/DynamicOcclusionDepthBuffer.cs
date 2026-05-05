using UnityEngine;

namespace VLB;

[ExecuteInEditMode]
[HelpURL("http://saladgamer.com/vlb-doc/comp-dynocclusion-depthbuffer.html")]
public class DynamicOcclusionDepthBuffer : DynamicOcclusionAbstractBase
{
	public new const string ClassName = "DynamicOcclusionDepthBuffer";

	public LayerMask layerMask = Consts.DynOcclusion.LayerMaskDefault;

	public bool useOcclusionCulling = true;

	public int depthMapResolution = 32;

	public float fadeDistanceToSurface = 0.25f;

	private Camera m_DepthCamera;

	private bool m_NeedToUpdateOcclusionNextFrame;

	protected override string GetShaderKeyword()
	{
		return "VLB_OCCLUSION_DEPTH_TEXTURE";
	}

	protected override MaterialManager.DynamicOcclusion GetDynamicOcclusionMode()
	{
		return MaterialManager.DynamicOcclusion.DepthTexture;
	}

	private void ProcessOcclusionInternal()
	{
		UpdateDepthCameraPropertiesAccordingToBeam();
		m_DepthCamera.Render();
	}

	protected override bool OnProcessOcclusion(ProcessOcclusionSource source)
	{
		if (source == ProcessOcclusionSource.RenderLoop && SRPHelper.IsUsingCustomRenderPipeline())
		{
			m_NeedToUpdateOcclusionNextFrame = true;
		}
		else
		{
			ProcessOcclusionInternal();
		}
		return true;
	}

	private void Update()
	{
		if (m_NeedToUpdateOcclusionNextFrame && (bool)m_Master && (bool)m_DepthCamera)
		{
			ProcessOcclusionInternal();
			m_NeedToUpdateOcclusionNextFrame = false;
		}
	}

	private void UpdateDepthCameraPropertiesAccordingToBeam()
	{
		float coneApexOffsetZ = m_Master.coneApexOffsetZ;
		m_DepthCamera.transform.localPosition = m_Master.beamLocalForward * (0f - coneApexOffsetZ);
		m_DepthCamera.transform.localRotation = m_Master.beamInternalLocalRotation;
		Vector3 lossyScale = m_Master.lossyScale;
		if (!Mathf.Approximately(lossyScale.y * lossyScale.z, 0f))
		{
			m_DepthCamera.nearClipPlane = Mathf.Max(coneApexOffsetZ, 0.1f) * lossyScale.z;
			m_DepthCamera.farClipPlane = (m_Master.maxGeometryDistance + coneApexOffsetZ) * lossyScale.z;
			m_DepthCamera.aspect = lossyScale.x / lossyScale.y;
			m_DepthCamera.fieldOfView = lossyScale.y * m_Master.coneAngle / lossyScale.z;
		}
	}

	public bool HasLayerMaskIssues()
	{
		if (Config.Instance.geometryOverrideLayer)
		{
			int num = 1 << Config.Instance.geometryLayerID;
			return (layerMask.value & num) == num;
		}
		return false;
	}

	protected override void OnValidateProperties()
	{
		base.OnValidateProperties();
		depthMapResolution = Mathf.Clamp(Mathf.NextPowerOfTwo(depthMapResolution), 8, 2048);
		fadeDistanceToSurface = Mathf.Max(fadeDistanceToSurface, 0f);
	}

	private void InstantiateOrActivateDepthCamera()
	{
		if (m_DepthCamera != null)
		{
			m_DepthCamera.gameObject.SetActive(value: true);
			return;
		}
		m_DepthCamera = new GameObject("Depth Camera").AddComponent<Camera>();
		if ((bool)m_DepthCamera && (bool)m_Master)
		{
			m_DepthCamera.enabled = false;
			m_DepthCamera.cullingMask = layerMask;
			m_DepthCamera.clearFlags = CameraClearFlags.Depth;
			m_DepthCamera.depthTextureMode = DepthTextureMode.Depth;
			m_DepthCamera.renderingPath = RenderingPath.VertexLit;
			m_DepthCamera.useOcclusionCulling = useOcclusionCulling;
			m_DepthCamera.gameObject.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
			m_DepthCamera.transform.SetParent(base.transform, worldPositionStays: false);
			RenderTexture targetTexture = new RenderTexture(depthMapResolution, depthMapResolution, 16, RenderTextureFormat.Depth);
			m_DepthCamera.targetTexture = targetTexture;
			UpdateDepthCameraPropertiesAccordingToBeam();
		}
	}

	protected override void OnEnablePostValidate()
	{
		InstantiateOrActivateDepthCamera();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if ((bool)m_DepthCamera)
		{
			m_DepthCamera.gameObject.SetActive(value: false);
		}
	}

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		DestroyDepthCamera();
	}

	private void DestroyDepthCamera()
	{
		if ((bool)m_DepthCamera)
		{
			if ((bool)m_DepthCamera.targetTexture)
			{
				m_DepthCamera.targetTexture.Release();
				Object.DestroyImmediate(m_DepthCamera.targetTexture);
				m_DepthCamera.targetTexture = null;
			}
			Object.DestroyImmediate(m_DepthCamera.gameObject);
			m_DepthCamera = null;
		}
	}

	protected override void OnModifyMaterialCallback(MaterialModifier.Interface owner)
	{
		owner.SetMaterialProp(ShaderProperties.DynamicOcclusionDepthTexture, m_DepthCamera.targetTexture);
		owner.SetMaterialProp(ShaderProperties.DynamicOcclusionDepthProps, fadeDistanceToSurface);
	}
}
