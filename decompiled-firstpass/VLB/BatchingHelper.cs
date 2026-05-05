using UnityEngine;

namespace VLB;

public static class BatchingHelper
{
	public const bool isGpuInstancingSupported = true;

	public static bool forceEnableDepthBlend
	{
		get
		{
			if (Config.Instance.actualRenderingMode != RenderingMode.GPUInstancing)
			{
				return Config.Instance.actualRenderingMode == RenderingMode.SRPBatcher;
			}
			return true;
		}
	}

	public static bool IsGpuInstancingEnabled(Material material)
	{
		return material.enableInstancing;
	}

	public static void SetMaterialProperties(Material material, bool enableGpuInstancing)
	{
		material.enableInstancing = enableGpuInstancing;
	}

	public static bool CanBeBatched(VolumetricLightBeam beamA, VolumetricLightBeam beamB, ref string reasons)
	{
		_ = Config.Instance.renderPipeline;
		if (Config.Instance.actualRenderingMode != RenderingMode.GPUInstancing && Config.Instance.actualRenderingMode != RenderingMode.SRPBatcher)
		{
			reasons = $"Current Render Pipeline is '{Config.Instance.renderPipeline}'. To enable batching, use 'GPU Instancing'";
			if (Config.Instance.renderPipeline != RenderPipeline.BuiltIn)
			{
				reasons += " or 'SRP Batcher'";
			}
			return false;
		}
		bool result = true;
		if (!CanBeBatched(beamA, ref reasons))
		{
			result = false;
		}
		if (!CanBeBatched(beamB, ref reasons))
		{
			result = false;
		}
		if (Config.Instance.featureEnabledDynamicOcclusion && beamA.GetComponent<DynamicOcclusionAbstractBase>() == null != (beamB.GetComponent<DynamicOcclusionAbstractBase>() == null))
		{
			AppendErrorMessage(ref reasons, $"{beamA.name}/{beamB.name}: dynamically occluded and non occluded beams cannot be batched together");
			result = false;
		}
		if (Config.Instance.featureEnabledColorGradient != FeatureEnabledColorGradient.Off && beamA.colorMode != beamB.colorMode)
		{
			AppendErrorMessage(ref reasons, $"'Color Mode' mismatch: {beamA.colorMode} / {beamB.colorMode}");
			result = false;
		}
		if (beamA.blendingMode != beamB.blendingMode)
		{
			AppendErrorMessage(ref reasons, $"'Blending Mode' mismatch: {beamA.blendingMode} / {beamB.blendingMode}");
			result = false;
		}
		if (Config.Instance.featureEnabledNoise3D && beamA.isNoiseEnabled != beamB.isNoiseEnabled)
		{
			AppendErrorMessage(ref reasons, $"'3D Noise' enabled mismatch: {beamA.noiseMode} / {beamB.noiseMode}");
			result = false;
		}
		if (Config.Instance.featureEnabledDepthBlend && !forceEnableDepthBlend && beamA.depthBlendDistance > 0f != beamB.depthBlendDistance > 0f)
		{
			AppendErrorMessage(ref reasons, $"'Opaque Geometry Blending' mismatch: {beamA.depthBlendDistance} / {beamB.depthBlendDistance}");
			result = false;
		}
		if (Config.Instance.featureEnabledShaderAccuracyHigh && beamA.shaderAccuracy != beamB.shaderAccuracy)
		{
			AppendErrorMessage(ref reasons, $"'Shader Accuracy' mismatch: {beamA.shaderAccuracy} / {beamB.shaderAccuracy}");
			result = false;
		}
		return result;
	}

	public static bool CanBeBatched(VolumetricLightBeam beam, ref string reasons)
	{
		bool result = true;
		if (Config.Instance.actualRenderingMode == RenderingMode.GPUInstancing && beam.geomMeshType != MeshType.Shared)
		{
			AppendErrorMessage(ref reasons, $"{beam.name} is not using shared mesh");
			result = false;
		}
		if (Config.Instance.featureEnabledDynamicOcclusion && beam.GetComponent<DynamicOcclusionDepthBuffer>() != null)
		{
			AppendErrorMessage(ref reasons, $"{beam.name} is using the DynamicOcclusion DepthBuffer feature");
			result = false;
		}
		return result;
	}

	private static void AppendErrorMessage(ref string message, string toAppend)
	{
		if (message != "")
		{
			message += "\n";
		}
		message = message + "- " + toAppend;
	}
}
