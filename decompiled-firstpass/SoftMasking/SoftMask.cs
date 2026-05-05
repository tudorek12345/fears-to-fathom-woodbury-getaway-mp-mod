using System;
using System.Collections.Generic;
using System.Linq;
using SoftMasking.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SoftMasking;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("UI/Soft Mask", 14)]
[RequireComponent(typeof(RectTransform))]
[HelpURL("https://docs.google.com/document/d/1XhJFNFHNyKXwWsErLkd1FBw0YgOCeo4qkjrMW9_H-hc")]
public class SoftMask : UIBehaviour, ISoftMask, ICanvasRaycastFilter
{
	[Serializable]
	public enum MaskSource
	{
		Graphic,
		Sprite,
		Texture
	}

	[Serializable]
	public enum BorderMode
	{
		Simple,
		Sliced,
		Tiled
	}

	[Serializable]
	[Flags]
	public enum Errors
	{
		NoError = 0,
		UnsupportedShaders = 1,
		NestedMasks = 2,
		TightPackedSprite = 4,
		AlphaSplitSprite = 8,
		UnsupportedImageType = 0x10,
		UnreadableTexture = 0x20
	}

	private struct SourceParameters
	{
		public Image image;

		public Sprite sprite;

		public BorderMode spriteBorderMode;

		public Texture2D texture;

		public Rect textureUVRect;
	}

	private class MaterialReplacerImpl : IMaterialReplacer
	{
		private readonly SoftMask _owner;

		public int order => 0;

		public MaterialReplacerImpl(SoftMask owner)
		{
			_owner = owner;
		}

		public Material Replace(Material original)
		{
			if (original == null || original.HasDefaultUIShader())
			{
				return Replace(original, _owner._defaultShader);
			}
			if (original.HasDefaultETC1UIShader())
			{
				return Replace(original, _owner._defaultETC1Shader);
			}
			if (original.SupportsSoftMask())
			{
				return new Material(original);
			}
			return null;
		}

		private static Material Replace(Material original, Shader defaultReplacementShader)
		{
			Material material = (defaultReplacementShader ? new Material(defaultReplacementShader) : null);
			if ((bool)material && (bool)original)
			{
				material.CopyPropertiesFromMaterial(original);
			}
			return material;
		}
	}

	private static class Mathr
	{
		public static Vector4 ToVector(Rect r)
		{
			return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
		}

		public static Vector4 Div(Vector4 v, Vector2 s)
		{
			return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y);
		}

		public static Vector2 Div(Vector2 v, Vector2 s)
		{
			return new Vector2(v.x / s.x, v.y / s.y);
		}

		public static Vector4 Mul(Vector4 v, Vector2 s)
		{
			return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y);
		}

		public static Vector2 Size(Vector4 r)
		{
			return new Vector2(r.z - r.x, r.w - r.y);
		}

		public static Vector4 Move(Vector4 v, Vector2 o)
		{
			return new Vector4(v.x + o.x, v.y + o.y, v.z + o.x, v.w + o.y);
		}

		public static Vector4 BorderOf(Vector4 outer, Vector4 inner)
		{
			return new Vector4(inner.x - outer.x, inner.y - outer.y, outer.z - inner.z, outer.w - inner.w);
		}

		public static Vector4 ApplyBorder(Vector4 v, Vector4 b)
		{
			return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
		}

		public static Vector2 Min(Vector4 r)
		{
			return new Vector2(r.x, r.y);
		}

		public static Vector2 Max(Vector4 r)
		{
			return new Vector2(r.z, r.w);
		}

		public static Vector2 Remap(Vector2 c, Vector4 from, Vector4 to)
		{
			Vector2 s = Max(from) - Min(from);
			Vector2 b = Max(to) - Min(to);
			return Vector2.Scale(Div(c - Min(from), s), b) + Min(to);
		}

		public static bool Inside(Vector2 v, Vector4 r)
		{
			if (v.x >= r.x && v.y >= r.y && v.x <= r.z)
			{
				return v.y <= r.w;
			}
			return false;
		}
	}

	private struct MaterialParameters
	{
		private static class Ids
		{
			public static readonly int SoftMask = Shader.PropertyToID("_SoftMask");

			public static readonly int SoftMask_Rect = Shader.PropertyToID("_SoftMask_Rect");

			public static readonly int SoftMask_UVRect = Shader.PropertyToID("_SoftMask_UVRect");

			public static readonly int SoftMask_ChannelWeights = Shader.PropertyToID("_SoftMask_ChannelWeights");

			public static readonly int SoftMask_WorldToMask = Shader.PropertyToID("_SoftMask_WorldToMask");

			public static readonly int SoftMask_BorderRect = Shader.PropertyToID("_SoftMask_BorderRect");

			public static readonly int SoftMask_UVBorderRect = Shader.PropertyToID("_SoftMask_UVBorderRect");

			public static readonly int SoftMask_TileRepeat = Shader.PropertyToID("_SoftMask_TileRepeat");
		}

		public Vector4 maskRect;

		public Vector4 maskBorder;

		public Vector4 maskRectUV;

		public Vector4 maskBorderUV;

		public Vector2 tileRepeat;

		public Color maskChannelWeights;

		public Matrix4x4 worldToMask;

		public Texture2D texture;

		public BorderMode borderMode;

		public Texture2D activeTexture
		{
			get
			{
				if (!texture)
				{
					return Texture2D.whiteTexture;
				}
				return texture;
			}
		}

		public bool SampleMask(Vector2 localPos, out float mask)
		{
			Vector2 vector = XY2UV(localPos);
			try
			{
				mask = MaskValue(texture.GetPixelBilinear(vector.x, vector.y));
				return true;
			}
			catch (UnityException)
			{
				mask = 0f;
				return false;
			}
		}

		public void Apply(Material mat)
		{
			mat.SetTexture(Ids.SoftMask, activeTexture);
			mat.SetVector(Ids.SoftMask_Rect, maskRect);
			mat.SetVector(Ids.SoftMask_UVRect, maskRectUV);
			mat.SetColor(Ids.SoftMask_ChannelWeights, maskChannelWeights);
			mat.SetMatrix(Ids.SoftMask_WorldToMask, worldToMask);
			mat.EnableKeyword("SOFTMASK_SIMPLE", borderMode == BorderMode.Simple);
			mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
			mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);
			if (borderMode != BorderMode.Simple)
			{
				mat.SetVector(Ids.SoftMask_BorderRect, maskBorder);
				mat.SetVector(Ids.SoftMask_UVBorderRect, maskBorderUV);
				if (borderMode == BorderMode.Tiled)
				{
					mat.SetVector(Ids.SoftMask_TileRepeat, tileRepeat);
				}
			}
		}

		private Vector2 XY2UV(Vector2 localPos)
		{
			switch (borderMode)
			{
			case BorderMode.Simple:
				return MapSimple(localPos);
			case BorderMode.Sliced:
				return MapBorder(localPos, repeat: false);
			case BorderMode.Tiled:
				return MapBorder(localPos, repeat: true);
			default:
				Debug.LogError("Unknown BorderMode");
				return MapSimple(localPos);
			}
		}

		private Vector2 MapSimple(Vector2 localPos)
		{
			return Mathr.Remap(localPos, maskRect, maskRectUV);
		}

		private Vector2 MapBorder(Vector2 localPos, bool repeat)
		{
			return new Vector2(Inset(localPos.x, maskRect.x, maskBorder.x, maskBorder.z, maskRect.z, maskRectUV.x, maskBorderUV.x, maskBorderUV.z, maskRectUV.z, repeat ? tileRepeat.x : 1f), Inset(localPos.y, maskRect.y, maskBorder.y, maskBorder.w, maskRect.w, maskRectUV.y, maskBorderUV.y, maskBorderUV.w, maskRectUV.w, repeat ? tileRepeat.y : 1f));
		}

		private float Inset(float v, float x1, float x2, float u1, float u2, float repeat = 1f)
		{
			float num = x2 - x1;
			return Mathf.Lerp(u1, u2, (num != 0f) ? Frac((v - x1) / num * repeat) : 0f);
		}

		private float Inset(float v, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4, float repeat = 1f)
		{
			if (v < x2)
			{
				return Inset(v, x1, x2, u1, u2);
			}
			if (v < x3)
			{
				return Inset(v, x2, x3, u2, u3, repeat);
			}
			return Inset(v, x3, x4, u3, u4);
		}

		private float Frac(float v)
		{
			return v - Mathf.Floor(v);
		}

		private float MaskValue(Color mask)
		{
			Color color = mask * maskChannelWeights;
			return color.a + color.r + color.g + color.b;
		}
	}

	private struct Diagnostics(SoftMask softMask)
	{
		private SoftMask _softMask = softMask;

		private Image image => _softMask.DeduceSourceParameters().image;

		private Sprite sprite => _softMask.DeduceSourceParameters().sprite;

		private Texture2D texture => _softMask.DeduceSourceParameters().texture;

		public Errors PollErrors()
		{
			SoftMask softMask = _softMask;
			Errors errors = Errors.NoError;
			((Component)(object)softMask).GetComponentsInChildren(s_maskables);
			using (new ClearListAtExit<SoftMaskable>(s_maskables))
			{
				if (s_maskables.Any((SoftMaskable m) => m.mask == softMask && m.shaderIsNotSupported))
				{
					errors |= Errors.UnsupportedShaders;
				}
			}
			if (ThereAreNestedMasks())
			{
				errors |= Errors.NestedMasks;
			}
			errors |= CheckSprite(sprite);
			errors |= CheckImage();
			return errors | CheckTexture();
		}

		public static Errors CheckSprite(Sprite sprite)
		{
			Errors errors = Errors.NoError;
			if (!sprite)
			{
				return errors;
			}
			if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
			{
				errors |= Errors.TightPackedSprite;
			}
			if ((bool)sprite.associatedAlphaSplitTexture)
			{
				errors |= Errors.AlphaSplitSprite;
			}
			return errors;
		}

		private bool ThereAreNestedMasks()
		{
			SoftMask softMask = _softMask;
			bool flag = false;
			using (new ClearListAtExit<SoftMask>(s_masks))
			{
				((Component)(object)softMask).GetComponentsInParent(false, s_masks);
				flag |= s_masks.Any((SoftMask x) => AreCompeting(softMask, x));
				((Component)(object)softMask).GetComponentsInChildren(false, s_masks);
				return flag | s_masks.Any((SoftMask x) => AreCompeting(softMask, x));
			}
		}

		private Errors CheckImage()
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			Errors errors = Errors.NoError;
			if (!_softMask.isBasedOnGraphic)
			{
				return errors;
			}
			if ((bool)(UnityEngine.Object)(object)image && !IsSupportedImageType(image.type))
			{
				errors |= Errors.UnsupportedImageType;
			}
			return errors;
		}

		private Errors CheckTexture()
		{
			Errors errors = Errors.NoError;
			if (_softMask.isUsingRaycastFiltering && (bool)texture && !IsReadable(texture))
			{
				errors |= Errors.UnreadableTexture;
			}
			return errors;
		}

		private static bool AreCompeting(SoftMask softMask, SoftMask other)
		{
			if (softMask.isMaskingEnabled && (UnityEngine.Object)(object)softMask != (UnityEngine.Object)(object)other && other.isMaskingEnabled && softMask.canvas.rootCanvas == other.canvas.rootCanvas)
			{
				return !SelectChild<SoftMask>(softMask, other).canvas.overrideSorting;
			}
			return false;
		}

		private static T SelectChild<T>(T first, T second) where T : Component
		{
			if (!first.transform.IsChildOf(second.transform))
			{
				return second;
			}
			return first;
		}

		private static bool IsReadable(Texture2D texture)
		{
			try
			{
				texture.GetPixel(0, 0);
				return true;
			}
			catch (UnityException)
			{
				return false;
			}
		}

		private static bool IsSupportedImageType(Type type)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Invalid comparison between Unknown and I4
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Invalid comparison between Unknown and I4
			if ((int)type != 0 && (int)type != 1)
			{
				return (int)type == 2;
			}
			return true;
		}
	}

	[SerializeField]
	private Shader _defaultShader;

	[SerializeField]
	private Shader _defaultETC1Shader;

	[SerializeField]
	private MaskSource _source;

	[SerializeField]
	private RectTransform _separateMask;

	[SerializeField]
	private Sprite _sprite;

	[SerializeField]
	private BorderMode _spriteBorderMode;

	[SerializeField]
	private Texture2D _texture;

	[SerializeField]
	private Rect _textureUVRect = DefaultUVRect;

	[SerializeField]
	private Color _channelWeights = MaskChannel.alpha;

	[SerializeField]
	private float _raycastThreshold;

	private MaterialReplacements _materials;

	private MaterialParameters _parameters;

	private Sprite _lastUsedSprite;

	private Rect _lastMaskRect;

	private bool _maskingWasEnabled;

	private bool _destroyed;

	private bool _dirty;

	private RectTransform _maskTransform;

	private Graphic _graphic;

	private Canvas _canvas;

	private static readonly Rect DefaultUVRect = new Rect(0f, 0f, 1f, 1f);

	private static readonly List<SoftMask> s_masks = new List<SoftMask>();

	private static readonly List<SoftMaskable> s_maskables = new List<SoftMaskable>();

	public Shader defaultShader
	{
		get
		{
			return _defaultShader;
		}
		set
		{
			SetShader(ref _defaultShader, value);
		}
	}

	public Shader defaultETC1Shader
	{
		get
		{
			return _defaultETC1Shader;
		}
		set
		{
			SetShader(ref _defaultETC1Shader, value, warnIfNotSet: false);
		}
	}

	public MaskSource source
	{
		get
		{
			return _source;
		}
		set
		{
			if (_source != value)
			{
				Set(ref _source, value);
			}
		}
	}

	public RectTransform separateMask
	{
		get
		{
			return _separateMask;
		}
		set
		{
			if (_separateMask != value)
			{
				Set(ref _separateMask, value);
				_graphic = null;
				_maskTransform = null;
			}
		}
	}

	public Sprite sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (_sprite != value)
			{
				Set(ref _sprite, value);
			}
		}
	}

	public BorderMode spriteBorderMode
	{
		get
		{
			return _spriteBorderMode;
		}
		set
		{
			if (_spriteBorderMode != value)
			{
				Set(ref _spriteBorderMode, value);
			}
		}
	}

	public Texture2D texture
	{
		get
		{
			return _texture;
		}
		set
		{
			if (_texture != value)
			{
				Set(ref _texture, value);
			}
		}
	}

	public Rect textureUVRect
	{
		get
		{
			return _textureUVRect;
		}
		set
		{
			if (_textureUVRect != value)
			{
				Set(ref _textureUVRect, value);
			}
		}
	}

	public Color channelWeights
	{
		get
		{
			return _channelWeights;
		}
		set
		{
			if (_channelWeights != value)
			{
				Set(ref _channelWeights, value);
			}
		}
	}

	public float raycastThreshold
	{
		get
		{
			return _raycastThreshold;
		}
		set
		{
			_raycastThreshold = value;
		}
	}

	public bool isUsingRaycastFiltering => _raycastThreshold > 0f;

	public bool isMaskingEnabled
	{
		get
		{
			if (((Behaviour)this).isActiveAndEnabled)
			{
				return canvas;
			}
			return false;
		}
	}

	private RectTransform maskTransform
	{
		get
		{
			if (!_maskTransform)
			{
				return _maskTransform = (_separateMask ? _separateMask : ((Component)this).GetComponent<RectTransform>());
			}
			return _maskTransform;
		}
	}

	private Canvas canvas
	{
		get
		{
			if (!_canvas)
			{
				return _canvas = NearestEnabledCanvas();
			}
			return _canvas;
		}
	}

	private bool isBasedOnGraphic => _source == MaskSource.Graphic;

	bool ISoftMask.isAlive
	{
		get
		{
			if ((bool)(UnityEngine.Object)(object)this)
			{
				return !_destroyed;
			}
			return false;
		}
	}

	public SoftMask()
	{
		MaterialReplacerChain replacer = new MaterialReplacerChain(MaterialReplacer.globalReplacers, new MaterialReplacerImpl(this));
		_materials = new MaterialReplacements(replacer, delegate(Material m)
		{
			_parameters.Apply(m);
		});
	}

	public Errors PollErrors()
	{
		return new Diagnostics(this).PollErrors();
	}

	public bool IsRaycastLocationValid(Vector2 sp, Camera cam)
	{
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskTransform, sp, cam, out var localPoint))
		{
			return false;
		}
		if (!Mathr.Inside(localPoint, LocalMaskRect(Vector4.zero)))
		{
			return false;
		}
		if (!_parameters.texture)
		{
			return true;
		}
		if (!isUsingRaycastFiltering)
		{
			return true;
		}
		if (!_parameters.SampleMask(localPoint, out var mask))
		{
			Debug.LogErrorFormat((UnityEngine.Object)(object)this, "Raycast Threshold greater than 0 can't be used on Soft Mask with texture '{0}' because it's not readable. You can make the texture readable in the Texture Import Settings.", _parameters.activeTexture.name);
			return true;
		}
		return mask >= _raycastThreshold;
	}

	protected override void Start()
	{
		((UIBehaviour)this).Start();
		WarnIfDefaultShaderIsNotSet();
	}

	protected override void OnEnable()
	{
		((UIBehaviour)this).OnEnable();
		SubscribeOnWillRenderCanvases();
		SpawnMaskablesInChildren(((Component)this).transform);
		FindGraphic();
		if (isMaskingEnabled)
		{
			UpdateMaskParameters();
		}
		NotifyChildrenThatMaskMightChanged();
	}

	protected override void OnDisable()
	{
		((UIBehaviour)this).OnDisable();
		UnsubscribeFromWillRenderCanvases();
		if ((bool)(UnityEngine.Object)(object)_graphic)
		{
			_graphic.UnregisterDirtyVerticesCallback((UnityAction)OnGraphicDirty);
			_graphic.UnregisterDirtyMaterialCallback((UnityAction)OnGraphicDirty);
			_graphic = null;
		}
		NotifyChildrenThatMaskMightChanged();
		DestroyMaterials();
	}

	protected override void OnDestroy()
	{
		((UIBehaviour)this).OnDestroy();
		_destroyed = true;
		NotifyChildrenThatMaskMightChanged();
	}

	protected virtual void LateUpdate()
	{
		bool flag = isMaskingEnabled;
		if (flag)
		{
			if (_maskingWasEnabled != flag)
			{
				SpawnMaskablesInChildren(((Component)this).transform);
			}
			Graphic graphic = _graphic;
			FindGraphic();
			if (_lastMaskRect != maskTransform.rect || _graphic != graphic)
			{
				_dirty = true;
			}
		}
		_maskingWasEnabled = flag;
	}

	protected override void OnRectTransformDimensionsChange()
	{
		((UIBehaviour)this).OnRectTransformDimensionsChange();
		_dirty = true;
	}

	protected override void OnDidApplyAnimationProperties()
	{
		((UIBehaviour)this).OnDidApplyAnimationProperties();
		_dirty = true;
	}

	protected override void OnTransformParentChanged()
	{
		((UIBehaviour)this).OnTransformParentChanged();
		_canvas = null;
		_dirty = true;
	}

	protected override void OnCanvasHierarchyChanged()
	{
		((UIBehaviour)this).OnCanvasHierarchyChanged();
		_canvas = null;
		_dirty = true;
		NotifyChildrenThatMaskMightChanged();
	}

	private void OnTransformChildrenChanged()
	{
		SpawnMaskablesInChildren(((Component)this).transform);
	}

	private void SubscribeOnWillRenderCanvases()
	{
		Touch<CanvasUpdateRegistry>(CanvasUpdateRegistry.instance);
		Canvas.willRenderCanvases += OnWillRenderCanvases;
	}

	private void UnsubscribeFromWillRenderCanvases()
	{
		Canvas.willRenderCanvases -= OnWillRenderCanvases;
	}

	private void OnWillRenderCanvases()
	{
		if (isMaskingEnabled)
		{
			UpdateMaskParameters();
		}
	}

	private static T Touch<T>(T obj)
	{
		return obj;
	}

	Material ISoftMask.GetReplacement(Material original)
	{
		return _materials.Get(original);
	}

	void ISoftMask.ReleaseReplacement(Material replacement)
	{
		_materials.Release(replacement);
	}

	void ISoftMask.UpdateTransformChildren(Transform transform)
	{
		SpawnMaskablesInChildren(transform);
	}

	private void OnGraphicDirty()
	{
		if (isBasedOnGraphic)
		{
			_dirty = true;
		}
	}

	private void FindGraphic()
	{
		if (!(UnityEngine.Object)(object)_graphic && isBasedOnGraphic)
		{
			_graphic = maskTransform.GetComponent<Graphic>();
			if ((bool)(UnityEngine.Object)(object)_graphic)
			{
				_graphic.RegisterDirtyVerticesCallback((UnityAction)OnGraphicDirty);
				_graphic.RegisterDirtyMaterialCallback((UnityAction)OnGraphicDirty);
			}
		}
	}

	private Canvas NearestEnabledCanvas()
	{
		Canvas[] componentsInParent = ((Component)this).GetComponentsInParent<Canvas>(false);
		for (int i = 0; i < componentsInParent.Length; i++)
		{
			if (componentsInParent[i].isActiveAndEnabled)
			{
				return componentsInParent[i];
			}
		}
		return null;
	}

	private void UpdateMaskParameters()
	{
		if (_dirty || maskTransform.hasChanged)
		{
			CalculateMaskParameters();
			maskTransform.hasChanged = false;
			_lastMaskRect = maskTransform.rect;
			_dirty = false;
		}
		_materials.ApplyAll();
	}

	private void SpawnMaskablesInChildren(Transform root)
	{
		using (new ClearListAtExit<SoftMaskable>(s_maskables))
		{
			for (int i = 0; i < root.childCount; i++)
			{
				Transform child = root.GetChild(i);
				child.GetComponents(s_maskables);
				if (s_maskables.Count == 0)
				{
					child.gameObject.AddComponent<SoftMaskable>();
				}
			}
		}
	}

	private void InvalidateChildren()
	{
		ForEachChildMaskable(delegate(SoftMaskable x)
		{
			x.Invalidate();
		});
	}

	private void NotifyChildrenThatMaskMightChanged()
	{
		ForEachChildMaskable(delegate(SoftMaskable x)
		{
			x.MaskMightChanged();
		});
	}

	private void ForEachChildMaskable(Action<SoftMaskable> f)
	{
		((Component)this).transform.GetComponentsInChildren(s_maskables);
		using (new ClearListAtExit<SoftMaskable>(s_maskables))
		{
			for (int i = 0; i < s_maskables.Count; i++)
			{
				SoftMaskable softMaskable = s_maskables[i];
				if ((bool)(UnityEngine.Object)(object)softMaskable && ((Component)(object)softMaskable).gameObject != ((Component)this).gameObject)
				{
					f(softMaskable);
				}
			}
		}
	}

	private void DestroyMaterials()
	{
		_materials.DestroyAllAndClear();
	}

	private SourceParameters DeduceSourceParameters()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		SourceParameters result = default(SourceParameters);
		switch (_source)
		{
		case MaskSource.Graphic:
			if (_graphic is Image)
			{
				result.image = (Image)_graphic;
				result.sprite = result.image.sprite;
				result.spriteBorderMode = ToBorderMode(result.image.type);
				result.texture = (result.sprite ? result.sprite.texture : null);
			}
			else if (_graphic is RawImage)
			{
				RawImage val = (RawImage)_graphic;
				result.texture = val.texture as Texture2D;
				result.textureUVRect = val.uvRect;
			}
			break;
		case MaskSource.Sprite:
			result.sprite = _sprite;
			result.spriteBorderMode = _spriteBorderMode;
			result.texture = (result.sprite ? result.sprite.texture : null);
			break;
		case MaskSource.Texture:
			result.texture = _texture;
			result.textureUVRect = _textureUVRect;
			break;
		default:
			Debug.LogErrorFormat((UnityEngine.Object)(object)this, "Unknown MaskSource: {0}", _source);
			break;
		}
		return result;
	}

	private BorderMode ToBorderMode(Type imageType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected I4, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		switch ((int)imageType)
		{
		case 0:
			return BorderMode.Simple;
		case 1:
			return BorderMode.Sliced;
		case 2:
			return BorderMode.Tiled;
		default:
			Debug.LogErrorFormat((UnityEngine.Object)(object)this, "SoftMask doesn't support image type {0}. Image type Simple will be used.", imageType);
			return BorderMode.Simple;
		}
	}

	private void CalculateMaskParameters()
	{
		SourceParameters sourceParameters = DeduceSourceParameters();
		if ((bool)sourceParameters.sprite)
		{
			CalculateSpriteBased(sourceParameters.sprite, sourceParameters.spriteBorderMode);
		}
		else if ((bool)sourceParameters.texture)
		{
			CalculateTextureBased(sourceParameters.texture, sourceParameters.textureUVRect);
		}
		else
		{
			CalculateSolidFill();
		}
	}

	private void CalculateSpriteBased(Sprite sprite, BorderMode borderMode)
	{
		Sprite lastUsedSprite = _lastUsedSprite;
		_lastUsedSprite = sprite;
		Errors errors = Diagnostics.CheckSprite(sprite);
		if (errors != Errors.NoError)
		{
			if (lastUsedSprite != sprite)
			{
				WarnSpriteErrors(errors);
			}
			CalculateSolidFill();
			return;
		}
		if (!sprite)
		{
			CalculateSolidFill();
			return;
		}
		FillCommonParameters();
		Vector4 vector = Mathr.Move(Mathr.ToVector(sprite.rect), sprite.textureRect.position - sprite.rect.position - sprite.textureRectOffset);
		Vector4 vector2 = Mathr.ToVector(sprite.textureRect);
		Vector4 vector3 = Mathr.BorderOf(vector, vector2);
		Vector2 s = new Vector2(sprite.texture.width, sprite.texture.height);
		Vector4 vector4 = LocalMaskRect(Vector4.zero);
		_parameters.maskRectUV = Mathr.Div(vector2, s);
		if (borderMode == BorderMode.Simple)
		{
			Vector4 v = Mathr.Div(vector3, Mathr.Size(vector));
			_parameters.maskRect = Mathr.ApplyBorder(vector4, Mathr.Mul(v, Mathr.Size(vector4)));
		}
		else
		{
			_parameters.maskRect = Mathr.ApplyBorder(vector4, vector3 * GraphicToCanvasScale(sprite));
			Vector4 v2 = Mathr.Div(vector, s);
			Vector4 border = AdjustBorders(sprite.border * GraphicToCanvasScale(sprite), vector4);
			_parameters.maskBorder = LocalMaskRect(border);
			_parameters.maskBorderUV = Mathr.ApplyBorder(v2, Mathr.Div(sprite.border, s));
		}
		_parameters.texture = sprite.texture;
		_parameters.borderMode = borderMode;
		if (borderMode == BorderMode.Tiled)
		{
			_parameters.tileRepeat = MaskRepeat(sprite, _parameters.maskBorder);
		}
	}

	private static Vector4 AdjustBorders(Vector4 border, Vector4 rect)
	{
		Vector2 vector = Mathr.Size(rect);
		for (int i = 0; i <= 1; i++)
		{
			float num = border[i] + border[i + 2];
			if (vector[i] < num && num != 0f)
			{
				float num2 = vector[i] / num;
				border[i] *= num2;
				border[i + 2] *= num2;
			}
		}
		return border;
	}

	private void CalculateTextureBased(Texture2D texture, Rect uvRect)
	{
		FillCommonParameters();
		_parameters.maskRect = LocalMaskRect(Vector4.zero);
		_parameters.maskRectUV = Mathr.ToVector(uvRect);
		_parameters.texture = texture;
		_parameters.borderMode = BorderMode.Simple;
	}

	private void CalculateSolidFill()
	{
		CalculateTextureBased(null, DefaultUVRect);
	}

	private void FillCommonParameters()
	{
		_parameters.worldToMask = WorldToMask();
		_parameters.maskChannelWeights = _channelWeights;
	}

	private float GraphicToCanvasScale(Sprite sprite)
	{
		float num = (canvas ? canvas.referencePixelsPerUnit : 100f);
		float num2 = (sprite ? sprite.pixelsPerUnit : 100f);
		return num / num2;
	}

	private Matrix4x4 WorldToMask()
	{
		return maskTransform.worldToLocalMatrix * canvas.rootCanvas.transform.localToWorldMatrix;
	}

	private Vector4 LocalMaskRect(Vector4 border)
	{
		return Mathr.ApplyBorder(Mathr.ToVector(maskTransform.rect), border);
	}

	private Vector2 MaskRepeat(Sprite sprite, Vector4 centralPart)
	{
		Vector4 r = Mathr.ApplyBorder(Mathr.ToVector(sprite.textureRect), sprite.border);
		return Mathr.Div(Mathr.Size(centralPart) * GraphicToCanvasScale(sprite), Mathr.Size(r));
	}

	private void WarnIfDefaultShaderIsNotSet()
	{
		if (!_defaultShader)
		{
			Debug.LogWarning("SoftMask may not work because its defaultShader is not set", (UnityEngine.Object)(object)this);
		}
	}

	private void WarnSpriteErrors(Errors errors)
	{
		if ((errors & Errors.TightPackedSprite) != Errors.NoError)
		{
			Debug.LogError("SoftMask doesn't support tight packed sprites", (UnityEngine.Object)(object)this);
		}
		if ((errors & Errors.AlphaSplitSprite) != Errors.NoError)
		{
			Debug.LogError("SoftMask doesn't support sprites with an alpha split texture", (UnityEngine.Object)(object)this);
		}
	}

	private void Set<T>(ref T field, T value)
	{
		field = value;
		_dirty = true;
	}

	private void SetShader(ref Shader field, Shader value, bool warnIfNotSet = true)
	{
		if (field != value)
		{
			field = value;
			if (warnIfNotSet)
			{
				WarnIfDefaultShaderIsNotSet();
			}
			DestroyMaterials();
			InvalidateChildren();
		}
	}
}
