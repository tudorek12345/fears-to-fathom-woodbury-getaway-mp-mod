using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace EPOOutline;

public static class RenderTargetUtility
{
	public struct RenderTextureInfo(RenderTextureDescriptor descriptor, FilterMode filterMode)
	{
		public readonly RenderTextureDescriptor Descriptor = descriptor;

		public readonly FilterMode FilterMode = filterMode;
	}

	private static RenderTextureFormat? hdrFormat;

	public static int GetDepthSliceForEye(StereoTargetEyeMask mask)
	{
		switch (mask)
		{
		case StereoTargetEyeMask.Both:
			return -1;
		case StereoTargetEyeMask.None:
		case StereoTargetEyeMask.Left:
			return 0;
		case StereoTargetEyeMask.Right:
			return 1;
		default:
			throw new ArgumentException("Unknown mode");
		}
	}

	public static RenderTargetIdentifier ComposeTarget(OutlineParameters parameters, RenderTargetIdentifier target)
	{
		return new RenderTargetIdentifier(target, 0, CubemapFace.Unknown, GetDepthSliceForEye(parameters.EyeMask));
	}

	public static bool IsUsingVR(OutlineParameters parameters)
	{
		if (XRSettings.enabled && !parameters.IsEditorCamera)
		{
			return parameters.EyeMask != StereoTargetEyeMask.None;
		}
		return false;
	}

	public static RenderTextureInfo GetTargetInfo(OutlineParameters parameters, int width, int height, int depthBuffer, bool forceNoAA, bool noFiltering)
	{
		FilterMode filterMode = ((!noFiltering) ? FilterMode.Bilinear : FilterMode.Point);
		RenderTextureFormat colorFormat = (parameters.UseHDR ? GetHDRFormat() : RenderTextureFormat.ARGB32);
		if (IsUsingVR(parameters))
		{
			RenderTextureDescriptor eyeTextureDesc = XRSettings.eyeTextureDesc;
			eyeTextureDesc.colorFormat = colorFormat;
			eyeTextureDesc.width = width;
			eyeTextureDesc.height = height;
			eyeTextureDesc.depthBufferBits = depthBuffer;
			eyeTextureDesc.msaaSamples = (forceNoAA ? 1 : Mathf.Max(parameters.Antialiasing, 1));
			VRTextureUsage vrUsage = ((parameters.EyeMask != StereoTargetEyeMask.Both) ? VRTextureUsage.OneEye : VRTextureUsage.TwoEyes);
			eyeTextureDesc.vrUsage = vrUsage;
			return new RenderTextureInfo(eyeTextureDesc, filterMode);
		}
		RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, colorFormat, depthBuffer);
		descriptor.dimension = TextureDimension.Tex2D;
		descriptor.msaaSamples = (forceNoAA ? 1 : Mathf.Max(parameters.Antialiasing, 1));
		return new RenderTextureInfo(descriptor, filterMode);
	}

	public static void GetTemporaryRT(OutlineParameters parameters, int id, int width, int height, int depthBuffer, bool clear, bool forceNoAA, bool noFiltering)
	{
		RenderTextureInfo targetInfo = GetTargetInfo(parameters, width, height, depthBuffer, forceNoAA, noFiltering);
		parameters.Buffer.GetTemporaryRT(id, targetInfo.Descriptor, targetInfo.FilterMode);
		parameters.Buffer.SetRenderTarget(ComposeTarget(parameters, id));
		if (clear)
		{
			parameters.Buffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
		}
	}

	private static RenderTextureFormat GetHDRFormat()
	{
		if (hdrFormat.HasValue)
		{
			return hdrFormat.Value;
		}
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
		{
			hdrFormat = RenderTextureFormat.ARGBHalf;
		}
		else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
		{
			hdrFormat = RenderTextureFormat.ARGBFloat;
		}
		else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB64))
		{
			hdrFormat = RenderTextureFormat.ARGB64;
		}
		else
		{
			hdrFormat = RenderTextureFormat.ARGB32;
		}
		return hdrFormat.Value;
	}
}
