using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EPOOutline;

public class OutlineParameters
{
	public readonly MeshPool MeshPool = new MeshPool();

	public Camera Camera;

	public RenderTargetIdentifier Target;

	public RenderTargetIdentifier DepthTarget;

	public CommandBuffer Buffer;

	public DilateQuality DilateQuality;

	public int DilateIterations = 2;

	public int BlurIterations = 5;

	public Vector2 Scale = Vector2.one;

	public Rect? CustomViewport;

	public long OutlineLayerMask = -1L;

	public int TargetWidth;

	public int TargetHeight;

	public float BlurShift = 1f;

	public float DilateShift = 1f;

	public bool UseHDR;

	public bool UseInfoBuffer;

	public bool IsEditorCamera;

	public BufferSizeMode PrimaryBufferSizeMode;

	public int PrimaryBufferSizeReference;

	public float PrimaryBufferScale = 0.1f;

	public StereoTargetEyeMask EyeMask;

	public int Antialiasing = 1;

	public BlurType BlurType = BlurType.Gaussian13x13;

	public LayerMask Mask = -1;

	public Mesh BlitMesh;

	public List<Outlinable> OutlinablesToRender = new List<Outlinable>();

	private bool isInitialized;

	public Vector2Int MakeScaledVector(int x, int y)
	{
		float num = x;
		float num2 = y;
		return new Vector2Int(Mathf.FloorToInt(num * Scale.x), Mathf.FloorToInt(num2 * Scale.y));
	}

	public void CheckInitialization()
	{
		if (!isInitialized)
		{
			Buffer = new CommandBuffer();
			isInitialized = true;
		}
	}

	public void Prepare()
	{
		if (OutlinablesToRender.Count == 0)
		{
			return;
		}
		UseInfoBuffer = OutlinablesToRender.Find((Outlinable x) => x != null && ((x.DrawingMode & (OutlinableDrawingMode.Obstacle | OutlinableDrawingMode.Mask)) != 0 || x.ComplexMaskingEnabled)) != null;
		if (UseInfoBuffer)
		{
			return;
		}
		foreach (Outlinable item in OutlinablesToRender)
		{
			if ((item.DrawingMode & OutlinableDrawingMode.Normal) != 0 && CheckDiffers(item))
			{
				UseInfoBuffer = true;
				break;
			}
		}
	}

	private static bool CheckDiffers(Outlinable outlinable)
	{
		if (outlinable.RenderStyle == RenderStyle.Single)
		{
			return CheckIfNonOne(outlinable.OutlineParameters);
		}
		if (!CheckIfNonOne(outlinable.FrontParameters))
		{
			return CheckIfNonOne(outlinable.BackParameters);
		}
		return true;
	}

	private static bool CheckIfNonOne(Outlinable.OutlineProperties parameters)
	{
		if (parameters.BlurShift == 1f)
		{
			return parameters.DilateShift != 1f;
		}
		return true;
	}
}
