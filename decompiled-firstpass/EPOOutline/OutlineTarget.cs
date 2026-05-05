using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace EPOOutline;

[Serializable]
public class OutlineTarget
{
	public bool IsVisible;

	[SerializeField]
	public ColorMask CutoutMask = ColorMask.A;

	[SerializeField]
	private float edgeDilateAmount = 5f;

	[SerializeField]
	private float frontEdgeDilateAmount = 5f;

	[SerializeField]
	private float backEdgeDilateAmount = 5f;

	[SerializeField]
	[FormerlySerializedAs("Renderer")]
	public Renderer renderer;

	[SerializeField]
	public int SubmeshIndex;

	[SerializeField]
	public BoundsMode BoundsMode;

	[SerializeField]
	public Bounds Bounds = new Bounds(Vector3.zero, Vector3.one);

	[SerializeField]
	[Range(0f, 1f)]
	public float CutoutThreshold = 0.5f;

	[SerializeField]
	public CullMode CullMode;

	[SerializeField]
	private string cutoutTextureName;

	[SerializeField]
	public DilateRenderMode DilateRenderingMode;

	[SerializeField]
	private int cutoutTextureIndex;

	private int? cutoutTextureId;

	public Renderer Renderer => renderer;

	public bool UsesCutout => !string.IsNullOrEmpty(cutoutTextureName);

	public int CutoutTextureIndex
	{
		get
		{
			return cutoutTextureIndex;
		}
		set
		{
			cutoutTextureIndex = value;
			if (cutoutTextureIndex < 0)
			{
				Debug.LogError("Trying to set cutout texture index less than zero");
				cutoutTextureIndex = 0;
			}
		}
	}

	public int ShiftedSubmeshIndex => SubmeshIndex;

	public int CutoutTextureId
	{
		get
		{
			if (!cutoutTextureId.HasValue)
			{
				cutoutTextureId = Shader.PropertyToID(cutoutTextureName);
			}
			return cutoutTextureId.Value;
		}
	}

	public string CutoutTextureName
	{
		get
		{
			return cutoutTextureName;
		}
		set
		{
			cutoutTextureName = value;
			cutoutTextureId = null;
		}
	}

	public float EdgeDilateAmount
	{
		get
		{
			return edgeDilateAmount;
		}
		set
		{
			if (value < 0f)
			{
				edgeDilateAmount = 0f;
			}
			else
			{
				edgeDilateAmount = value;
			}
		}
	}

	public float FrontEdgeDilateAmount
	{
		get
		{
			return frontEdgeDilateAmount;
		}
		set
		{
			if (value < 0f)
			{
				frontEdgeDilateAmount = 0f;
			}
			else
			{
				frontEdgeDilateAmount = value;
			}
		}
	}

	public float BackEdgeDilateAmount
	{
		get
		{
			return backEdgeDilateAmount;
		}
		set
		{
			if (value < 0f)
			{
				backEdgeDilateAmount = 0f;
			}
			else
			{
				backEdgeDilateAmount = value;
			}
		}
	}

	public OutlineTarget()
	{
	}

	public OutlineTarget(Renderer renderer, int submesh = 0)
	{
		SubmeshIndex = submesh;
		this.renderer = renderer;
		CutoutThreshold = 0.5f;
		cutoutTextureId = null;
		cutoutTextureName = string.Empty;
		CullMode = ((!(renderer is SpriteRenderer)) ? CullMode.Back : CullMode.Off);
		DilateRenderingMode = DilateRenderMode.PostProcessing;
		frontEdgeDilateAmount = 5f;
		backEdgeDilateAmount = 5f;
		edgeDilateAmount = 5f;
	}

	public OutlineTarget(Renderer renderer, string cutoutTextureName, float cutoutThreshold = 0.5f)
	{
		SubmeshIndex = 0;
		this.renderer = renderer;
		cutoutTextureId = Shader.PropertyToID(cutoutTextureName);
		CutoutThreshold = cutoutThreshold;
		this.cutoutTextureName = cutoutTextureName;
		CullMode = ((!(renderer is SpriteRenderer)) ? CullMode.Back : CullMode.Off);
		DilateRenderingMode = DilateRenderMode.PostProcessing;
		frontEdgeDilateAmount = 5f;
		backEdgeDilateAmount = 5f;
		edgeDilateAmount = 5f;
	}
}
