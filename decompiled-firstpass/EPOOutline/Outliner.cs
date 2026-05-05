using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace EPOOutline;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class Outliner : MonoBehaviour
{
	private static List<Outlinable> temporaryOutlinables = new List<Outlinable>();

	private OutlineParameters parameters = new OutlineParameters();

	private Camera targetCamera;

	[SerializeField]
	private RenderStage stage = RenderStage.AfterTransparents;

	[SerializeField]
	private OutlineRenderingStrategy renderingStrategy;

	[SerializeField]
	private RenderingMode renderingMode;

	[SerializeField]
	private long outlineLayerMask = -1L;

	[SerializeField]
	private BufferSizeMode primaryBufferSizeMode;

	[SerializeField]
	[Range(0.15f, 1f)]
	private float primaryRendererScale = 0.75f;

	[SerializeField]
	private int primarySizeReference = 800;

	[SerializeField]
	[Range(0f, 2f)]
	private float blurShift = 1f;

	[SerializeField]
	[Range(0f, 2f)]
	private float dilateShift = 1f;

	[SerializeField]
	[FormerlySerializedAs("dilateIterrations")]
	private int dilateIterations = 1;

	[SerializeField]
	private DilateQuality dilateQuality;

	[SerializeField]
	[FormerlySerializedAs("blurIterrations")]
	private int blurIterations = 1;

	[SerializeField]
	private BlurType blurType = BlurType.Box;

	[Obsolete]
	public float InfoRendererScale
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public int PrimarySizeReference
	{
		get
		{
			return primarySizeReference;
		}
		set
		{
			primarySizeReference = value;
		}
	}

	public BufferSizeMode PrimaryBufferSizeMode
	{
		get
		{
			return primaryBufferSizeMode;
		}
		set
		{
			primaryBufferSizeMode = value;
		}
	}

	private CameraEvent Event
	{
		get
		{
			if (stage != RenderStage.BeforeTransparents)
			{
				return CameraEvent.BeforeImageEffects;
			}
			return CameraEvent.AfterForwardOpaque;
		}
	}

	public OutlineRenderingStrategy RenderingStrategy
	{
		get
		{
			return renderingStrategy;
		}
		set
		{
			renderingStrategy = value;
		}
	}

	public RenderStage RenderStage
	{
		get
		{
			return stage;
		}
		set
		{
			stage = value;
		}
	}

	public DilateQuality DilateQuality
	{
		get
		{
			return dilateQuality;
		}
		set
		{
			dilateQuality = value;
		}
	}

	private RenderingMode RenderingMode
	{
		get
		{
			return renderingMode;
		}
		set
		{
			renderingMode = value;
		}
	}

	public float BlurShift
	{
		get
		{
			return blurShift;
		}
		set
		{
			blurShift = Mathf.Clamp(value, 0f, 2f);
		}
	}

	public float DilateShift
	{
		get
		{
			return dilateShift;
		}
		set
		{
			dilateShift = Mathf.Clamp(value, 0f, 2f);
		}
	}

	public long OutlineLayerMask
	{
		get
		{
			return outlineLayerMask;
		}
		set
		{
			outlineLayerMask = value;
		}
	}

	public float PrimaryRendererScale
	{
		get
		{
			return primaryRendererScale;
		}
		set
		{
			primaryRendererScale = Mathf.Clamp01(value);
		}
	}

	[Obsolete("Fixed incorrect spelling. Use BlurIterations instead")]
	public int BlurIterrations
	{
		get
		{
			return BlurIterations;
		}
		set
		{
			BlurIterations = value;
		}
	}

	public int BlurIterations
	{
		get
		{
			return blurIterations;
		}
		set
		{
			blurIterations = ((value > 0) ? value : 0);
		}
	}

	public BlurType BlurType
	{
		get
		{
			return blurType;
		}
		set
		{
			blurType = value;
		}
	}

	[Obsolete("Fixed incorrect spelling. Use DilateIterations instead")]
	public int DilateIterration
	{
		get
		{
			return DilateIterations;
		}
		set
		{
			DilateIterations = value;
		}
	}

	public int DilateIterations
	{
		get
		{
			return dilateIterations;
		}
		set
		{
			dilateIterations = ((value > 0) ? value : 0);
		}
	}

	private void OnValidate()
	{
		if (blurIterations < 0)
		{
			blurIterations = 0;
		}
		if (dilateIterations < 0)
		{
			dilateIterations = 0;
		}
	}

	private void OnEnable()
	{
		if (targetCamera == null)
		{
			targetCamera = GetComponent<Camera>();
		}
		targetCamera.forceIntoRenderTexture = targetCamera.stereoTargetEye == StereoTargetEyeMask.None || !XRSettings.enabled;
		parameters.CheckInitialization();
		parameters.Buffer.name = "Outline";
	}

	private void OnDestroy()
	{
		UnityEngine.Object.DestroyImmediate(parameters.BlitMesh);
		if (parameters.Buffer != null)
		{
			parameters.Buffer.Dispose();
		}
	}

	private void OnDisable()
	{
		if (targetCamera != null)
		{
			UpdateBuffer(targetCamera, parameters.Buffer, removeOnly: true);
		}
	}

	private void UpdateBuffer(Camera targetCamera, CommandBuffer buffer, bool removeOnly)
	{
		targetCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, buffer);
		targetCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, buffer);
		if (!removeOnly)
		{
			targetCamera.AddCommandBuffer(Event, buffer);
		}
	}

	private void OnPreRender()
	{
		if (!(GraphicsSettings.renderPipelineAsset != null))
		{
			parameters.OutlinablesToRender.Clear();
			SetupOutline(targetCamera, parameters, isEditor: false);
		}
	}

	private void SetupOutline(Camera cameraToUse, OutlineParameters parametersToUse, bool isEditor)
	{
		UpdateBuffer(cameraToUse, parametersToUse.Buffer, removeOnly: false);
		UpdateParameters(parametersToUse, cameraToUse, isEditor);
		parametersToUse.Buffer.Clear();
		if (renderingStrategy == OutlineRenderingStrategy.Default)
		{
			OutlineEffect.SetupOutline(parametersToUse);
			parametersToUse.BlitMesh = null;
			parametersToUse.MeshPool.ReleaseAllMeshes();
			return;
		}
		temporaryOutlinables.Clear();
		temporaryOutlinables.AddRange(parametersToUse.OutlinablesToRender);
		parametersToUse.OutlinablesToRender.Clear();
		parametersToUse.OutlinablesToRender.Add(null);
		foreach (Outlinable temporaryOutlinable in temporaryOutlinables)
		{
			parametersToUse.OutlinablesToRender[0] = temporaryOutlinable;
			OutlineEffect.SetupOutline(parametersToUse);
			parametersToUse.BlitMesh = null;
		}
		parametersToUse.MeshPool.ReleaseAllMeshes();
	}

	public void UpdateSharedParameters(OutlineParameters parameters, Camera camera, bool editorCamera)
	{
		parameters.DilateQuality = DilateQuality;
		parameters.Camera = camera;
		parameters.IsEditorCamera = editorCamera;
		parameters.PrimaryBufferScale = primaryRendererScale;
		parameters.PrimaryBufferSizeMode = primaryBufferSizeMode;
		parameters.PrimaryBufferSizeReference = primarySizeReference;
		parameters.BlurIterations = blurIterations;
		parameters.BlurType = blurType;
		parameters.DilateIterations = dilateIterations;
		parameters.BlurShift = blurShift;
		parameters.DilateShift = dilateShift;
		parameters.UseHDR = camera.allowHDR && RenderingMode == RenderingMode.HDR;
		parameters.EyeMask = camera.stereoTargetEye;
		parameters.OutlineLayerMask = outlineLayerMask;
		parameters.Prepare();
	}

	private void UpdateParameters(OutlineParameters parameters, Camera camera, bool editorCamera)
	{
		parameters.DepthTarget = RenderTargetUtility.ComposeTarget(parameters, BuiltinRenderTextureType.CameraTarget);
		RenderTexture renderTexture = ((camera.targetTexture == null) ? camera.activeTexture : camera.targetTexture);
		if (XRSettings.enabled && !parameters.IsEditorCamera && parameters.EyeMask != StereoTargetEyeMask.None)
		{
			RenderTextureDescriptor eyeTextureDesc = XRSettings.eyeTextureDesc;
			parameters.TargetWidth = eyeTextureDesc.width;
			parameters.TargetHeight = eyeTextureDesc.height;
		}
		else
		{
			parameters.TargetWidth = ((renderTexture != null) ? renderTexture.width : camera.scaledPixelWidth);
			parameters.TargetHeight = ((renderTexture != null) ? renderTexture.height : camera.scaledPixelHeight);
		}
		parameters.Antialiasing = ((!editorCamera) ? CameraUtility.GetMSAA(targetCamera) : ((renderTexture == null) ? 1 : renderTexture.antiAliasing));
		parameters.Target = RenderTargetUtility.ComposeTarget(parameters, BuiltinRenderTextureType.CameraTarget);
		parameters.Camera = camera;
		Outlinable.GetAllActiveOutlinables(parameters.Camera, parameters.OutlinablesToRender);
		RendererFilteringUtility.Filter(parameters.Camera, parameters);
		UpdateSharedParameters(parameters, camera, editorCamera);
	}
}
