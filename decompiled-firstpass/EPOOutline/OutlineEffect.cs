using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EPOOutline;

public static class OutlineEffect
{
	private struct OutlineTargetGroup(Outlinable outlinable, OutlineTarget target)
	{
		public readonly Outlinable Outlinable = outlinable;

		public readonly OutlineTarget Target = target;
	}

	public static readonly int FillRefHash = Shader.PropertyToID("_FillRef");

	public static readonly int DilateShiftHash = Shader.PropertyToID("_DilateShift");

	public static readonly int ColorMaskHash = Shader.PropertyToID("_ColorMask");

	public static readonly int OutlineRefHash = Shader.PropertyToID("_OutlineRef");

	public static readonly int RefHash = Shader.PropertyToID("_Ref");

	public static readonly int ZWriteHash = Shader.PropertyToID("_ZWrite");

	public static readonly int EffectSizeHash = Shader.PropertyToID("_EffectSize");

	public static readonly int CullHash = Shader.PropertyToID("_Cull");

	public static readonly int ZTestHash = Shader.PropertyToID("_ZTest");

	public static readonly int ColorHash = Shader.PropertyToID("_EPOColor");

	public static readonly int ScaleHash = Shader.PropertyToID("_Scale");

	public static readonly int ShiftHash = Shader.PropertyToID("_Shift");

	public static readonly int InitialTexHash = Shader.PropertyToID("_InitialTex");

	public static readonly int InfoBufferHash = Shader.PropertyToID("_InfoBuffer");

	public static readonly int ComparisonHash = Shader.PropertyToID("_Comparison");

	public static readonly int ReadMaskHash = Shader.PropertyToID("_ReadMask");

	public static readonly int WriteMaskHash = Shader.PropertyToID("_WriteMask");

	public static readonly int OperationHash = Shader.PropertyToID("_Operation");

	public static readonly int CutoutThresholdHash = Shader.PropertyToID("_CutoutThreshold");

	public static readonly int CutoutMaskHash = Shader.PropertyToID("_CutoutMask");

	public static readonly int TextureIndexHash = Shader.PropertyToID("_TextureIndex");

	public static readonly int CutoutTextureHash = Shader.PropertyToID("_CutoutTexture");

	public static readonly int CutoutTextureSTHash = Shader.PropertyToID("_CutoutTexture_ST");

	public static readonly int SrcBlendHash = Shader.PropertyToID("_SrcBlend");

	public static readonly int DstBlendHash = Shader.PropertyToID("_DstBlend");

	public static readonly int TargetHash = Shader.PropertyToID("ScreenRenderTargetTexture");

	public static readonly int InfoTargetHash = Shader.PropertyToID("ScreenInfoRenderTargetTexture");

	public static readonly int PrimaryBufferHash = Shader.PropertyToID("PrimaryBuffer");

	public static readonly int HelperBufferHash = Shader.PropertyToID("HelperBuffer");

	public static readonly int PrimaryInfoBufferHash = Shader.PropertyToID("PrimaryInfoBuffer");

	public static readonly int HelperInfoBufferHash = Shader.PropertyToID("HelperInfoBuffer");

	private static Material TransparentBlitMaterial;

	private static Material EmptyFillMaterial;

	private static Material OutlineMaterial;

	private static Material PartialBlitMaterial;

	private static Material ObstacleMaterial;

	private static Material FillMaskMaterial;

	private static Material ZPrepassMaterial;

	private static Material OutlineMaskMaterial;

	private static Material DilateMaterial;

	private static Material BlurMaterial;

	private static Material FinalBlitMaterial;

	private static Material BasicBlitMaterial;

	private static Material ClearStencilMaterial;

	private static List<OutlineTargetGroup> targets = new List<OutlineTargetGroup>();

	private static List<string> keywords = new List<string>();

	public static Material LoadMaterial(string shaderName)
	{
		Material material = new Material(Resources.Load<Shader>($"Easy performant outline/Shaders/{shaderName}"));
		if (SystemInfo.supportsInstancing)
		{
			material.enableInstancing = true;
		}
		return material;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void InitMaterials()
	{
		if (PartialBlitMaterial == null)
		{
			PartialBlitMaterial = LoadMaterial("PartialBlit");
		}
		if (ObstacleMaterial == null)
		{
			ObstacleMaterial = LoadMaterial("Obstacle");
		}
		if (OutlineMaterial == null)
		{
			OutlineMaterial = LoadMaterial("Outline");
		}
		if (TransparentBlitMaterial == null)
		{
			TransparentBlitMaterial = LoadMaterial("TransparentBlit");
		}
		if (ZPrepassMaterial == null)
		{
			ZPrepassMaterial = LoadMaterial("ZPrepass");
		}
		if (OutlineMaskMaterial == null)
		{
			OutlineMaskMaterial = LoadMaterial("OutlineMask");
		}
		if (DilateMaterial == null)
		{
			DilateMaterial = LoadMaterial("Dilate");
		}
		if (BlurMaterial == null)
		{
			BlurMaterial = LoadMaterial("Blur");
		}
		if (FinalBlitMaterial == null)
		{
			FinalBlitMaterial = LoadMaterial("FinalBlit");
		}
		if (BasicBlitMaterial == null)
		{
			BasicBlitMaterial = LoadMaterial("BasicBlit");
		}
		if (EmptyFillMaterial == null)
		{
			EmptyFillMaterial = LoadMaterial("Fills/EmptyFill");
		}
		if (FillMaskMaterial == null)
		{
			FillMaskMaterial = LoadMaterial("Fills/FillMask");
		}
		if (ClearStencilMaterial == null)
		{
			ClearStencilMaterial = LoadMaterial("ClearStencil");
		}
	}

	private static void Postprocess(OutlineParameters parameters, int first, int second, Material material, int iterations, bool additionalShift, float shiftValue, ref int stencil, Rect viewport, float scale)
	{
		if (iterations > 0)
		{
			parameters.Buffer.SetGlobalInt(ComparisonHash, 3);
			for (int i = 1; i <= iterations; i++)
			{
				parameters.Buffer.SetGlobalInt(RefHash, stencil);
				float num = (additionalShift ? ((float)i) : 1f);
				parameters.Buffer.SetGlobalVector(ShiftHash, new Vector4(num * scale, 0f));
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, first), RenderTargetUtility.ComposeTarget(parameters, second), RenderTargetUtility.ComposeTarget(parameters, first), material, shiftValue, null, -1, viewport);
				stencil = (stencil + 1) % 255;
				parameters.Buffer.SetGlobalInt(RefHash, stencil);
				parameters.Buffer.SetGlobalVector(ShiftHash, new Vector4(0f, num * scale));
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, second), RenderTargetUtility.ComposeTarget(parameters, first), RenderTargetUtility.ComposeTarget(parameters, first), material, shiftValue, null, -1, viewport);
				stencil = (stencil + 1) % 255;
			}
		}
	}

	private static void Blit(OutlineParameters parameters, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier destinationDepth, Material material, float effectSize, CommandBuffer buffer, int pass = -1, Rect? viewport = null)
	{
		parameters.Buffer.SetGlobalFloat(EffectSizeHash, effectSize);
		BlitUtility.Blit(parameters, source, destination, destinationDepth, material, buffer, pass, viewport);
	}

	private static float GetBlurShift(BlurType blurType, int iterrationsCount)
	{
		switch (blurType)
		{
		case BlurType.Anisotropic:
		case BlurType.Box:
			return (float)iterrationsCount * 0.65f + 1f;
		case BlurType.Gaussian5x5:
			return 3f * (float)iterrationsCount;
		case BlurType.Gaussian9x9:
			return 5f + (float)iterrationsCount;
		case BlurType.Gaussian13x13:
			return 7f + (float)iterrationsCount;
		default:
			throw new ArgumentException("Unknown blur type");
		}
	}

	private static float GetMaskingValueForMode(OutlinableDrawingMode mode)
	{
		if ((mode & OutlinableDrawingMode.Mask) != 0)
		{
			return 0.6f;
		}
		if ((mode & OutlinableDrawingMode.Obstacle) != 0)
		{
			return 0.25f;
		}
		return 1f;
	}

	private static float ComputeEffectShift(OutlineParameters parameters)
	{
		return (GetBlurShift(parameters.BlurType, parameters.BlurIterations) * parameters.BlurShift + (float)parameters.DilateIterations * 4f * parameters.DilateShift) * 2f;
	}

	private static void PrepareTargets(OutlineParameters parameters)
	{
		targets.Clear();
		foreach (Outlinable item in parameters.OutlinablesToRender)
		{
			foreach (OutlineTarget outlineTarget in item.OutlineTargets)
			{
				Renderer renderer = outlineTarget.Renderer;
				if (outlineTarget.IsVisible || ((item.DrawingMode & OutlinableDrawingMode.GenericMask) != 0 && !(renderer == null)))
				{
					targets.Add(new OutlineTargetGroup(item, outlineTarget));
				}
			}
		}
	}

	public static void SetupOutline(OutlineParameters parameters)
	{
		parameters.Buffer.SetGlobalVector(ScaleHash, parameters.Scale);
		PrepareTargets(parameters);
		InitMaterials();
		float num = ComputeEffectShift(parameters);
		int targetWidth = parameters.TargetWidth;
		int targetHeight = parameters.TargetHeight;
		parameters.Buffer.SetGlobalInt(SrcBlendHash, 1);
		parameters.Buffer.SetGlobalInt(DstBlendHash, 0);
		int value = 1;
		parameters.Buffer.SetGlobalInt(OutlineRefHash, value);
		SetupDilateKeyword(parameters);
		RenderTargetUtility.GetTemporaryRT(parameters, TargetHash, targetWidth, targetHeight, 24, clear: true, forceNoAA: false, noFiltering: false);
		int num2 = parameters.TargetWidth / 2;
		int num3 = parameters.TargetHeight / 2;
		switch (parameters.PrimaryBufferSizeMode)
		{
		case BufferSizeMode.WidthControllsHeight:
			num2 = parameters.PrimaryBufferSizeReference;
			num3 = (int)((float)parameters.PrimaryBufferSizeReference / ((float)parameters.TargetWidth / (float)parameters.TargetHeight));
			break;
		case BufferSizeMode.HeightControlsWidth:
			num2 = (int)((float)parameters.PrimaryBufferSizeReference / ((float)parameters.TargetHeight / (float)parameters.TargetWidth));
			num3 = parameters.PrimaryBufferSizeReference;
			break;
		case BufferSizeMode.Scaled:
			num2 = (int)((float)targetWidth * parameters.PrimaryBufferScale);
			num3 = (int)((float)targetHeight * parameters.PrimaryBufferScale);
			break;
		}
		if (parameters.EyeMask != StereoTargetEyeMask.None)
		{
			if (num2 % 2 != 0)
			{
				num2++;
			}
			if (num3 % 2 != 0)
			{
				num3++;
			}
		}
		Vector2Int vector2Int = parameters.MakeScaledVector(num2, num3);
		RenderTargetUtility.GetTemporaryRT(parameters, PrimaryBufferHash, num2, num3, 24, clear: true, forceNoAA: false, noFiltering: false);
		RenderTargetUtility.GetTemporaryRT(parameters, HelperBufferHash, num2, num3, 24, clear: true, forceNoAA: false, noFiltering: false);
		if (parameters.UseInfoBuffer)
		{
			int width = num2;
			int height = num3;
			RenderTargetUtility.GetTemporaryRT(parameters, InfoTargetHash, targetWidth, targetHeight, 0, clear: false, forceNoAA: false, noFiltering: false);
			RenderTargetUtility.GetTemporaryRT(parameters, PrimaryInfoBufferHash, width, height, 0, clear: true, forceNoAA: true, noFiltering: false);
			RenderTargetUtility.GetTemporaryRT(parameters, HelperInfoBufferHash, width, height, 0, clear: true, forceNoAA: true, noFiltering: false);
		}
		BlitUtility.PrepareForRendering(parameters);
		parameters.Buffer.SetRenderTarget(RenderTargetUtility.ComposeTarget(parameters, TargetHash), RenderTargetUtility.ComposeTarget(parameters, parameters.DepthTarget));
		if (parameters.CustomViewport.HasValue)
		{
			parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
		}
		DrawOutlineables(parameters, CompareFunction.LessEqual, (Outlinable x) => true, (Outlinable x) => Color.clear, (Outlinable x) => ZPrepassMaterial, (RenderStyle)3, OutlinableDrawingMode.ZOnly);
		parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetEnabledInfoBufferKeyword());
		if (parameters.UseInfoBuffer)
		{
			parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetInfoBufferStageKeyword());
			parameters.Buffer.SetRenderTarget(RenderTargetUtility.ComposeTarget(parameters, InfoTargetHash), parameters.DepthTarget);
			parameters.Buffer.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
			if (parameters.CustomViewport.HasValue)
			{
				parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
			}
			DrawOutlineables(parameters, CompareFunction.Always, (Outlinable x) => x.OutlineParameters.Enabled, (Outlinable x) => new Color(x.OutlineParameters.DilateShift, x.OutlineParameters.BlurShift, 0f, 1f), (Outlinable x) => OutlineMaterial, RenderStyle.Single);
			DrawOutlineables(parameters, CompareFunction.NotEqual, (Outlinable x) => x.BackParameters.Enabled, (Outlinable x) => new Color(x.BackParameters.DilateShift, x.BackParameters.BlurShift, 0f, 1f), (Outlinable x) => OutlineMaterial, RenderStyle.FrontBack);
			DrawOutlineables(parameters, CompareFunction.LessEqual, (Outlinable x) => x.FrontParameters.Enabled, (Outlinable x) => new Color(x.FrontParameters.DilateShift, x.FrontParameters.BlurShift, 0f, 1f), (Outlinable x) => OutlineMaterial, RenderStyle.FrontBack);
			DrawOutlineables(parameters, CompareFunction.LessEqual, (Outlinable x) => true, (Outlinable x) => new Color(0f, 0f, GetMaskingValueForMode(x.DrawingMode), 1f), (Outlinable x) => ObstacleMaterial, (RenderStyle)3, OutlinableDrawingMode.Obstacle | OutlinableDrawingMode.Mask);
			parameters.Buffer.SetGlobalInt(ComparisonHash, 8);
			parameters.Buffer.SetGlobalInt(OperationHash, 0);
			Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, InfoTargetHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryInfoBufferHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryInfoBufferHash), BasicBlitMaterial, num, null);
			int num4 = ((parameters.DilateQuality == DilateQuality.Base) ? parameters.DilateIterations : (parameters.DilateIterations * 2)) + parameters.BlurIterations;
			if (num4 > 5)
			{
				parameters.Buffer.SetGlobalInt(ColorMaskHash, 0);
				parameters.Buffer.SetGlobalInt(ComparisonHash, 8);
				parameters.Buffer.SetGlobalInt(RefHash, 255);
				parameters.Buffer.SetGlobalInt(OperationHash, 2);
				parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetEdgeMaskKeyword());
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, InfoTargetHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryInfoBufferHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryInfoBufferHash), BasicBlitMaterial, num, null);
				parameters.Buffer.SetGlobalInt(ColorMaskHash, 255);
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetEdgeMaskKeyword());
				parameters.Buffer.SetGlobalInt(OperationHash, 0);
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, InfoTargetHash), RenderTargetUtility.ComposeTarget(parameters, HelperInfoBufferHash), RenderTargetUtility.ComposeTarget(parameters, HelperInfoBufferHash), BasicBlitMaterial, num, null);
			}
			int stencil = 0;
			Postprocess(parameters, PrimaryInfoBufferHash, HelperInfoBufferHash, DilateMaterial, num4, additionalShift: true, num, ref stencil, new Rect(0f, 0f, vector2Int.x, vector2Int.y), 1f);
			parameters.Buffer.SetRenderTarget(RenderTargetUtility.ComposeTarget(parameters, InfoTargetHash), parameters.DepthTarget);
			if (parameters.CustomViewport.HasValue)
			{
				parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
			}
			parameters.Buffer.SetGlobalTexture(InfoBufferHash, PrimaryInfoBufferHash);
			parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetInfoBufferStageKeyword());
		}
		if (parameters.UseInfoBuffer)
		{
			parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetEnabledInfoBufferKeyword());
		}
		parameters.Buffer.SetRenderTarget(RenderTargetUtility.ComposeTarget(parameters, TargetHash), parameters.DepthTarget);
		parameters.Buffer.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
		if (parameters.CustomViewport.HasValue)
		{
			parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
		}
		int num5 = 0 + DrawOutlineables(parameters, CompareFunction.Always, (Outlinable x) => x.OutlineParameters.Enabled, (Outlinable x) => x.OutlineParameters.Color, (Outlinable x) => OutlineMaterial, RenderStyle.Single) + DrawOutlineables(parameters, CompareFunction.NotEqual, (Outlinable x) => x.BackParameters.Enabled, (Outlinable x) => x.BackParameters.Color, (Outlinable x) => OutlineMaterial, RenderStyle.FrontBack) + DrawOutlineables(parameters, CompareFunction.LessEqual, (Outlinable x) => x.FrontParameters.Enabled, (Outlinable x) => x.FrontParameters.Color, (Outlinable x) => OutlineMaterial, RenderStyle.FrontBack);
		int stencil2 = 0;
		if (num5 > 0)
		{
			parameters.Buffer.SetGlobalInt(ComparisonHash, 8);
			parameters.Buffer.SetGlobalInt(OperationHash, 0);
			Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, TargetHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryBufferHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryBufferHash), BasicBlitMaterial, num, null, -1, new Rect(0f, 0f, vector2Int.x, vector2Int.y));
			if (parameters.BlurIterations + parameters.DilateIterations > 5)
			{
				parameters.Buffer.SetGlobalInt(ComparisonHash, 8);
				parameters.Buffer.SetGlobalInt(RefHash, 255);
				parameters.Buffer.SetGlobalInt(ColorMaskHash, 0);
				parameters.Buffer.SetGlobalInt(OperationHash, 2);
				parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetEdgeMaskKeyword());
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, TargetHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryBufferHash), RenderTargetUtility.ComposeTarget(parameters, PrimaryBufferHash), BasicBlitMaterial, num, null, -1, new Rect(0f, 0f, vector2Int.x, vector2Int.y));
				parameters.Buffer.SetGlobalInt(ColorMaskHash, 255);
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetEdgeMaskKeyword());
				parameters.Buffer.SetGlobalInt(OperationHash, 0);
				Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, TargetHash), RenderTargetUtility.ComposeTarget(parameters, HelperBufferHash), RenderTargetUtility.ComposeTarget(parameters, HelperBufferHash), BasicBlitMaterial, num, null, -1, new Rect(0f, 0f, vector2Int.x, vector2Int.y));
			}
			Postprocess(parameters, PrimaryBufferHash, HelperBufferHash, DilateMaterial, parameters.DilateIterations, additionalShift: false, num, ref stencil2, new Rect(0f, 0f, vector2Int.x, vector2Int.y), parameters.DilateShift);
		}
		parameters.Buffer.SetRenderTarget(RenderTargetUtility.ComposeTarget(parameters, TargetHash), parameters.DepthTarget);
		if (num5 > 0)
		{
			parameters.Buffer.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
		}
		if (parameters.CustomViewport.HasValue)
		{
			parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
		}
		if (parameters.BlurIterations > 0)
		{
			SetupBlurKeyword(parameters);
			Postprocess(parameters, PrimaryBufferHash, HelperBufferHash, BlurMaterial, parameters.BlurIterations, additionalShift: false, num, ref stencil2, new Rect(0f, 0f, vector2Int.x, vector2Int.y), parameters.BlurShift);
		}
		parameters.Buffer.SetGlobalInt(ComparisonHash, 6);
		parameters.Buffer.SetGlobalInt(ReadMaskHash, 255);
		parameters.Buffer.SetGlobalInt(OperationHash, 2);
		Blit(parameters, RenderTargetUtility.ComposeTarget(parameters, PrimaryBufferHash), parameters.Target, parameters.DepthTarget, FinalBlitMaterial, num, null, -1, parameters.CustomViewport);
		DrawFill(parameters, parameters.Target);
		parameters.Buffer.SetGlobalFloat(EffectSizeHash, num);
		BlitUtility.Draw(parameters, parameters.Target, parameters.DepthTarget, ClearStencilMaterial, parameters.CustomViewport);
		parameters.Buffer.ReleaseTemporaryRT(PrimaryBufferHash);
		parameters.Buffer.ReleaseTemporaryRT(HelperBufferHash);
		parameters.Buffer.ReleaseTemporaryRT(TargetHash);
		if (parameters.UseInfoBuffer)
		{
			parameters.Buffer.ReleaseTemporaryRT(InfoBufferHash);
			parameters.Buffer.ReleaseTemporaryRT(PrimaryInfoBufferHash);
			parameters.Buffer.ReleaseTemporaryRT(HelperInfoBufferHash);
		}
	}

	private static void SetupDilateKeyword(OutlineParameters parameters)
	{
		KeywordsUtility.GetAllDilateKeywords(keywords);
		foreach (string keyword in keywords)
		{
			parameters.Buffer.DisableShaderKeyword(keyword);
		}
		parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetDilateQualityKeyword(parameters.DilateQuality));
	}

	private static void SetupBlurKeyword(OutlineParameters parameters)
	{
		KeywordsUtility.GetAllBlurKeywords(keywords);
		foreach (string keyword in keywords)
		{
			parameters.Buffer.DisableShaderKeyword(keyword);
		}
		parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetBlurKeyword(parameters.BlurType));
	}

	private static int DrawOutlineables(OutlineParameters parameters, CompareFunction function, Func<Outlinable, bool> shouldRender, Func<Outlinable, Color> colorProvider, Func<Outlinable, Material> materialProvider, RenderStyle styleMask, OutlinableDrawingMode modeMask = OutlinableDrawingMode.Normal)
	{
		int num = 0;
		parameters.Buffer.SetGlobalInt(ZTestHash, (int)function);
		foreach (OutlineTargetGroup target2 in targets)
		{
			Outlinable outlinable = target2.Outlinable;
			if ((outlinable.RenderStyle & styleMask) != 0 && (outlinable.DrawingMode & modeMask) != 0)
			{
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetBackKeyword(ComplexMaskingMode.MaskingMode));
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetBackKeyword(ComplexMaskingMode.ObstaclesMode));
				if (function == CompareFunction.NotEqual && outlinable.ComplexMaskingEnabled)
				{
					parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetBackKeyword(outlinable.ComplexMaskingMode));
				}
				Color value = (shouldRender(outlinable) ? colorProvider(outlinable) : Color.clear);
				parameters.Buffer.SetGlobalColor(ColorHash, value);
				OutlineTarget target = target2.Target;
				parameters.Buffer.SetGlobalInt(ColorMaskHash, 255);
				SetupCutout(parameters, target);
				SetupCull(parameters, target);
				num++;
				Material material = materialProvider(outlinable);
				parameters.Buffer.DrawRenderer(target.Renderer, material, target.ShiftedSubmeshIndex);
			}
		}
		return num;
	}

	private static void DrawFill(OutlineParameters parameters, RenderTargetIdentifier targetSurface)
	{
		parameters.Buffer.SetRenderTarget(targetSurface, parameters.DepthTarget);
		if (parameters.CustomViewport.HasValue)
		{
			parameters.Buffer.SetViewport(parameters.CustomViewport.Value);
		}
		int value = 1;
		int value2 = 2;
		int value3 = 3;
		foreach (Outlinable item in parameters.OutlinablesToRender)
		{
			if ((item.DrawingMode & OutlinableDrawingMode.Normal) == 0)
			{
				continue;
			}
			parameters.Buffer.SetGlobalInt(ZTestHash, 5);
			foreach (OutlineTarget outlineTarget in item.OutlineTargets)
			{
				if (outlineTarget.IsVisible)
				{
					Renderer renderer = outlineTarget.Renderer;
					if (item.NeedFillMask)
					{
						SetupCutout(parameters, outlineTarget);
						SetupCull(parameters, outlineTarget);
						parameters.Buffer.SetGlobalInt(FillRefHash, value3);
						parameters.Buffer.DrawRenderer(renderer, FillMaskMaterial, outlineTarget.ShiftedSubmeshIndex);
					}
				}
			}
		}
		foreach (Outlinable item2 in parameters.OutlinablesToRender)
		{
			if ((item2.DrawingMode & OutlinableDrawingMode.Normal) == 0)
			{
				continue;
			}
			parameters.Buffer.SetGlobalInt(ZTestHash, 4);
			foreach (OutlineTarget outlineTarget2 in item2.OutlineTargets)
			{
				if (outlineTarget2.IsVisible && item2.NeedFillMask)
				{
					Renderer renderer2 = outlineTarget2.Renderer;
					SetupCutout(parameters, outlineTarget2);
					SetupCull(parameters, outlineTarget2);
					parameters.Buffer.SetGlobalInt(FillRefHash, value2);
					parameters.Buffer.DrawRenderer(renderer2, FillMaskMaterial, outlineTarget2.ShiftedSubmeshIndex);
				}
			}
		}
		foreach (Outlinable item3 in parameters.OutlinablesToRender)
		{
			if ((item3.DrawingMode & OutlinableDrawingMode.Normal) == 0)
			{
				continue;
			}
			if (item3.RenderStyle == RenderStyle.FrontBack)
			{
				if ((item3.BackParameters.FillPass.Material == null || !item3.BackParameters.Enabled) && (item3.FrontParameters.FillPass.Material == null || !item3.FrontParameters.Enabled))
				{
					continue;
				}
				Material material = item3.FrontParameters.FillPass.Material;
				parameters.Buffer.SetGlobalInt(FillRefHash, value2);
				if (material != null && item3.FrontParameters.Enabled)
				{
					foreach (OutlineTarget outlineTarget3 in item3.OutlineTargets)
					{
						if (outlineTarget3.IsVisible)
						{
							Renderer renderer3 = outlineTarget3.Renderer;
							SetupCutout(parameters, outlineTarget3);
							SetupCull(parameters, outlineTarget3);
							parameters.Buffer.DrawRenderer(renderer3, material, outlineTarget3.ShiftedSubmeshIndex);
						}
					}
				}
				Material material2 = item3.BackParameters.FillPass.Material;
				parameters.Buffer.SetGlobalInt(FillRefHash, value3);
				if (material2 == null || !item3.BackParameters.Enabled)
				{
					continue;
				}
				if (item3.ComplexMaskingEnabled)
				{
					parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetBackKeyword(item3.ComplexMaskingMode));
				}
				foreach (OutlineTarget outlineTarget4 in item3.OutlineTargets)
				{
					if (outlineTarget4.IsVisible)
					{
						Renderer renderer4 = outlineTarget4.Renderer;
						SetupCutout(parameters, outlineTarget4);
						SetupCull(parameters, outlineTarget4);
						parameters.Buffer.DrawRenderer(renderer4, material2, outlineTarget4.ShiftedSubmeshIndex);
					}
				}
				if (item3.ComplexMaskingEnabled)
				{
					parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetBackKeyword(item3.ComplexMaskingMode));
				}
			}
			else
			{
				if (item3.OutlineParameters.FillPass.Material == null || !item3.OutlineParameters.Enabled)
				{
					continue;
				}
				parameters.Buffer.SetGlobalInt(FillRefHash, value);
				parameters.Buffer.SetGlobalInt(ZTestHash, 8);
				foreach (OutlineTarget outlineTarget5 in item3.OutlineTargets)
				{
					if (outlineTarget5.IsVisible && item3.NeedFillMask)
					{
						Renderer renderer5 = outlineTarget5.Renderer;
						SetupCutout(parameters, outlineTarget5);
						SetupCull(parameters, outlineTarget5);
						parameters.Buffer.DrawRenderer(renderer5, FillMaskMaterial, outlineTarget5.ShiftedSubmeshIndex);
					}
				}
				parameters.Buffer.SetGlobalInt(FillRefHash, value);
				Material material3 = item3.OutlineParameters.FillPass.Material;
				if (FillMaskMaterial == null)
				{
					continue;
				}
				foreach (OutlineTarget outlineTarget6 in item3.OutlineTargets)
				{
					if (outlineTarget6.IsVisible)
					{
						Renderer renderer6 = outlineTarget6.Renderer;
						SetupCutout(parameters, outlineTarget6);
						SetupCull(parameters, outlineTarget6);
						parameters.Buffer.DrawRenderer(renderer6, material3, outlineTarget6.ShiftedSubmeshIndex);
					}
				}
			}
		}
	}

	private static void SetupCutout(OutlineParameters parameters, OutlineTarget target)
	{
		if (target.Renderer == null)
		{
			return;
		}
		Vector4 value = new Vector4(((target.CutoutMask & ColorMask.R) != ColorMask.None) ? 1f : 0f, ((target.CutoutMask & ColorMask.G) != ColorMask.None) ? 1f : 0f, ((target.CutoutMask & ColorMask.B) != ColorMask.None) ? 1f : 0f, ((target.CutoutMask & ColorMask.A) != ColorMask.None) ? 1f : 0f);
		parameters.Buffer.SetGlobalVector(CutoutMaskHash, value);
		if (target.Renderer is SpriteRenderer)
		{
			SpriteRenderer spriteRenderer = target.Renderer as SpriteRenderer;
			if (spriteRenderer.sprite == null)
			{
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetCutoutKeyword());
				return;
			}
			parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetCutoutKeyword());
			parameters.Buffer.SetGlobalFloat(CutoutThresholdHash, target.CutoutThreshold);
			parameters.Buffer.SetGlobalTexture(CutoutTextureHash, spriteRenderer.sprite.texture);
			return;
		}
		Material sharedMaterial = target.Renderer.sharedMaterial;
		if (target.UsesCutout && sharedMaterial != null && sharedMaterial.HasProperty(target.CutoutTextureId))
		{
			parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetCutoutKeyword());
			parameters.Buffer.SetGlobalFloat(CutoutThresholdHash, target.CutoutThreshold);
			Vector2 textureOffset = sharedMaterial.GetTextureOffset(target.CutoutTextureId);
			Vector2 textureScale = sharedMaterial.GetTextureScale(target.CutoutTextureId);
			parameters.Buffer.SetGlobalVector(CutoutTextureSTHash, new Vector4(textureScale.x, textureScale.y, textureOffset.x, textureOffset.y));
			Texture texture = sharedMaterial.GetTexture(target.CutoutTextureId);
			if (texture == null || texture.dimension != TextureDimension.Tex2DArray)
			{
				parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetTextureArrayCutoutKeyword());
			}
			else
			{
				parameters.Buffer.SetGlobalFloat(TextureIndexHash, target.CutoutTextureIndex);
				parameters.Buffer.EnableShaderKeyword(KeywordsUtility.GetTextureArrayCutoutKeyword());
			}
			parameters.Buffer.SetGlobalTexture(CutoutTextureHash, texture);
		}
		else
		{
			parameters.Buffer.DisableShaderKeyword(KeywordsUtility.GetCutoutKeyword());
		}
	}

	private static void SetupCull(OutlineParameters parameters, OutlineTarget target)
	{
		parameters.Buffer.SetGlobalInt(CullHash, (int)target.CullMode);
	}
}
