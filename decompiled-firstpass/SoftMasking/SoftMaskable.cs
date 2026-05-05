using System.Collections.Generic;
using SoftMasking.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoftMasking;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("")]
public class SoftMaskable : UIBehaviour, IMaterialModifier
{
	private ISoftMask _mask;

	private Graphic _graphic;

	private Material _replacement;

	private bool _affectedByMask;

	private bool _destroyed;

	private static List<ISoftMask> s_softMasks = new List<ISoftMask>();

	private static List<Canvas> s_canvases = new List<Canvas>();

	public bool shaderIsNotSupported { get; private set; }

	public bool isMaskingEnabled
	{
		get
		{
			if (mask != null && mask.isAlive && mask.isMaskingEnabled)
			{
				return _affectedByMask;
			}
			return false;
		}
	}

	public ISoftMask mask
	{
		get
		{
			return _mask;
		}
		private set
		{
			if (_mask != value)
			{
				if (_mask != null)
				{
					replacement = null;
				}
				_mask = ((value != null && value.isAlive) ? value : null);
				Invalidate();
			}
		}
	}

	private Graphic graphic
	{
		get
		{
			if (!(Object)(object)_graphic)
			{
				return _graphic = ((Component)this).GetComponent<Graphic>();
			}
			return _graphic;
		}
	}

	private Material replacement
	{
		get
		{
			return _replacement;
		}
		set
		{
			if (_replacement != value)
			{
				if (_replacement != null && mask != null)
				{
					mask.ReleaseReplacement(_replacement);
				}
				_replacement = value;
			}
		}
	}

	public Material GetModifiedMaterial(Material baseMaterial)
	{
		if (isMaskingEnabled)
		{
			Material material = mask.GetReplacement(baseMaterial);
			replacement = material;
			if ((bool)replacement)
			{
				shaderIsNotSupported = false;
				return replacement;
			}
			if (!baseMaterial.HasDefaultUIShader())
			{
				SetShaderNotSupported(baseMaterial);
			}
		}
		else
		{
			shaderIsNotSupported = false;
			replacement = null;
		}
		return baseMaterial;
	}

	public void Invalidate()
	{
		if ((bool)(Object)(object)graphic)
		{
			graphic.SetMaterialDirty();
		}
	}

	public void MaskMightChanged()
	{
		if (FindMaskOrDie())
		{
			Invalidate();
		}
	}

	protected override void Awake()
	{
		((UIBehaviour)this).Awake();
		((Object)this).hideFlags = HideFlags.HideInInspector;
	}

	protected override void OnEnable()
	{
		((UIBehaviour)this).OnEnable();
		if (FindMaskOrDie())
		{
			RequestChildTransformUpdate();
		}
	}

	protected override void OnDisable()
	{
		((UIBehaviour)this).OnDisable();
		mask = null;
	}

	protected override void OnDestroy()
	{
		((UIBehaviour)this).OnDestroy();
		_destroyed = true;
	}

	protected override void OnTransformParentChanged()
	{
		((UIBehaviour)this).OnTransformParentChanged();
		FindMaskOrDie();
	}

	protected override void OnCanvasHierarchyChanged()
	{
		((UIBehaviour)this).OnCanvasHierarchyChanged();
		FindMaskOrDie();
	}

	private void OnTransformChildrenChanged()
	{
		RequestChildTransformUpdate();
	}

	private void RequestChildTransformUpdate()
	{
		if (mask != null)
		{
			mask.UpdateTransformChildren(((Component)this).transform);
		}
	}

	private bool FindMaskOrDie()
	{
		if (_destroyed)
		{
			return false;
		}
		mask = NearestMask(((Component)this).transform, out _affectedByMask) ?? NearestMask(((Component)this).transform, out _affectedByMask, enabledOnly: false);
		if (mask == null)
		{
			_destroyed = true;
			Object.DestroyImmediate((Object)(object)this);
			return false;
		}
		return true;
	}

	private static ISoftMask NearestMask(Transform transform, out bool processedByThisMask, bool enabledOnly = true)
	{
		processedByThisMask = true;
		Transform transform2 = transform;
		ISoftMask iSoftMask;
		while (true)
		{
			if (!transform2)
			{
				return null;
			}
			if (transform2 != transform)
			{
				iSoftMask = GetISoftMask(transform2, enabledOnly);
				if (iSoftMask != null)
				{
					break;
				}
			}
			if (IsOverridingSortingCanvas(transform2))
			{
				processedByThisMask = false;
			}
			transform2 = transform2.parent;
		}
		return iSoftMask;
	}

	private static ISoftMask GetISoftMask(Transform current, bool shouldBeEnabled = true)
	{
		ISoftMask component = GetComponent(current, s_softMasks);
		if (component != null && component.isAlive && (!shouldBeEnabled || component.isMaskingEnabled))
		{
			return component;
		}
		return null;
	}

	private static bool IsOverridingSortingCanvas(Transform transform)
	{
		Canvas component = GetComponent(transform, s_canvases);
		if ((bool)component && component.overrideSorting)
		{
			return true;
		}
		return false;
	}

	private static T GetComponent<T>(Component component, List<T> cachedList) where T : class
	{
		component.GetComponents(cachedList);
		using (new ClearListAtExit<T>(cachedList))
		{
			return (cachedList.Count > 0) ? cachedList[0] : null;
		}
	}

	private void SetShaderNotSupported(Material material)
	{
		if (!shaderIsNotSupported)
		{
			Debug.LogWarningFormat(((Component)this).gameObject, "SoftMask will not work on {0} because material {1} doesn't support masking. Add masking support to your material or set Graphic's material to None to use a default one.", graphic, material);
			shaderIsNotSupported = true;
		}
	}
}
