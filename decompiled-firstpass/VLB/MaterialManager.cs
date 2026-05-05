using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLB;

public static class MaterialManager
{
	public enum BlendingMode
	{
		Additive,
		SoftAdditive,
		TraditionalTransparency,
		Count
	}

	public enum Noise3D
	{
		Off,
		On,
		Count
	}

	public enum DepthBlend
	{
		Off,
		On,
		Count
	}

	public enum ColorGradient
	{
		Off,
		MatrixLow,
		MatrixHigh,
		Count
	}

	public enum DynamicOcclusion
	{
		Off,
		ClippingPlane,
		DepthTexture,
		Count
	}

	public enum MeshSkewing
	{
		Off,
		On,
		Count
	}

	public enum ShaderAccuracy
	{
		Fast,
		High,
		Count
	}

	public struct StaticProperties
	{
		public BlendingMode blendingMode;

		public Noise3D noise3D;

		public DepthBlend depthBlend;

		public ColorGradient colorGradient;

		public DynamicOcclusion dynamicOcclusion;

		public MeshSkewing meshSkewing;

		public ShaderAccuracy shaderAccuracy;

		private int blendingModeID => (int)blendingMode;

		private int noise3DID
		{
			get
			{
				if (!Config.Instance.featureEnabledNoise3D)
				{
					return 0;
				}
				return (int)noise3D;
			}
		}

		private int depthBlendID
		{
			get
			{
				if (!Config.Instance.featureEnabledDepthBlend)
				{
					return 0;
				}
				return (int)depthBlend;
			}
		}

		private int colorGradientID
		{
			get
			{
				if (Config.Instance.featureEnabledColorGradient == FeatureEnabledColorGradient.Off)
				{
					return 0;
				}
				return (int)colorGradient;
			}
		}

		private int dynamicOcclusionID
		{
			get
			{
				if (!Config.Instance.featureEnabledDynamicOcclusion)
				{
					return 0;
				}
				return (int)dynamicOcclusion;
			}
		}

		private int meshSkewingID
		{
			get
			{
				if (!Config.Instance.featureEnabledMeshSkewing)
				{
					return 0;
				}
				return (int)meshSkewing;
			}
		}

		private int shaderAccuracyID
		{
			get
			{
				if (!Config.Instance.featureEnabledShaderAccuracyHigh)
				{
					return 0;
				}
				return (int)shaderAccuracy;
			}
		}

		public int materialID => (((((blendingModeID * 2 + noise3DID) * 2 + depthBlendID) * 3 + colorGradientID) * 3 + dynamicOcclusionID) * 2 + meshSkewingID) * 2 + shaderAccuracyID;

		public void ApplyToMaterial(Material mat)
		{
			mat.SetKeywordEnabled("VLB_ALPHA_AS_BLACK", BlendingMode_AlphaAsBlack[(int)blendingMode]);
			mat.SetKeywordEnabled("VLB_COLOR_GRADIENT_MATRIX_LOW", colorGradient == ColorGradient.MatrixLow);
			mat.SetKeywordEnabled("VLB_COLOR_GRADIENT_MATRIX_HIGH", colorGradient == ColorGradient.MatrixHigh);
			mat.SetKeywordEnabled("VLB_DEPTH_BLEND", depthBlend == DepthBlend.On);
			mat.SetKeywordEnabled("VLB_NOISE_3D", noise3D == Noise3D.On);
			mat.SetKeywordEnabled("VLB_OCCLUSION_CLIPPING_PLANE", dynamicOcclusion == DynamicOcclusion.ClippingPlane);
			mat.SetKeywordEnabled("VLB_OCCLUSION_DEPTH_TEXTURE", dynamicOcclusion == DynamicOcclusion.DepthTexture);
			mat.SetKeywordEnabled("VLB_MESH_SKEWING", meshSkewing == MeshSkewing.On);
			mat.SetKeywordEnabled("VLB_SHADER_ACCURACY_HIGH", shaderAccuracy == ShaderAccuracy.High);
			mat.SetInt(ShaderProperties.BlendSrcFactor, (int)BlendingMode_SrcFactor[(int)blendingMode]);
			mat.SetInt(ShaderProperties.BlendDstFactor, (int)BlendingMode_DstFactor[(int)blendingMode]);
		}
	}

	private class MaterialsGroup
	{
		public Material[] materials = new Material[kStaticPropertiesCount];
	}

	public static MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

	private static readonly BlendMode[] BlendingMode_SrcFactor = new BlendMode[3]
	{
		BlendMode.One,
		BlendMode.OneMinusDstColor,
		BlendMode.SrcAlpha
	};

	private static readonly BlendMode[] BlendingMode_DstFactor = new BlendMode[3]
	{
		BlendMode.One,
		BlendMode.One,
		BlendMode.OneMinusSrcAlpha
	};

	private static readonly bool[] BlendingMode_AlphaAsBlack = new bool[3] { true, true, false };

	private static int kStaticPropertiesCount = 432;

	private static Hashtable ms_MaterialsGroup = new Hashtable(1);

	public static Material NewMaterialTransient(bool gpuInstanced)
	{
		Material material = NewMaterialPersistent(Config.Instance.beamShader, gpuInstanced);
		if ((bool)material)
		{
			material.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
			material.renderQueue = Config.Instance.geometryRenderQueue;
		}
		return material;
	}

	public static Material NewMaterialPersistent(Shader shader, bool gpuInstanced)
	{
		if (!shader)
		{
			Debug.LogError("Invalid VLB Shader. Please try to reset the VLB Config asset or reinstall the plugin.");
			return null;
		}
		Material material = new Material(shader);
		BatchingHelper.SetMaterialProperties(material, gpuInstanced);
		return material;
	}

	public static Material GetInstancedMaterial(uint groupID, ref StaticProperties staticProps)
	{
		MaterialsGroup materialsGroup = (MaterialsGroup)ms_MaterialsGroup[groupID];
		if (materialsGroup == null)
		{
			materialsGroup = new MaterialsGroup();
			ms_MaterialsGroup[groupID] = materialsGroup;
		}
		int materialID = staticProps.materialID;
		Material material = materialsGroup.materials[materialID];
		if (material == null)
		{
			material = NewMaterialTransient(gpuInstanced: true);
			if ((bool)material)
			{
				materialsGroup.materials[materialID] = material;
				staticProps.ApplyToMaterial(material);
			}
		}
		return material;
	}
}
