using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UI.ThreeDimensional;

[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteInEditMode]
[AddComponentMenu("UI/UIObject3D/UIObject3D")]
public class UIObject3D : MonoBehaviour
{
	[Header("Target")]
	[SerializeField]
	private Transform _ObjectPrefab;

	public bool UseTargetRotation = true;

	[SerializeField]
	private Vector3 _TargetRotation = Vector3.zero;

	[SerializeField]
	[Range(-10f, 10f)]
	private float _TargetOffsetX;

	[SerializeField]
	[Range(-10f, 10f)]
	private float _TargetOffsetY;

	[SerializeField]
	[Tooltip("By default, the target object will be scaled automatically by UIObject3D to fit within the viewable area. This option allows you to override that behaviour and set the scaling value manually.")]
	private bool _OverrideCalculatedTargetScale;

	[SerializeField]
	private float _CalculatedTargetScaleOverride = 1f;

	[Header("Camera Settings")]
	[SerializeField]
	[Range(20f, 100f)]
	private float _CameraFOV = 35f;

	[SerializeField]
	[Range(-10f, -1f)]
	private float _CameraDistance = -3.5f;

	[SerializeField]
	[Tooltip("If this property is set, and the target has an offset, then the camera will turn to face it.")]
	private bool _AlwaysLookAtTarget;

	[SerializeField]
	[HideInInspector]
	private Vector2 _textureSize;

	[SerializeField]
	private Color _BackgroundColor = Color.clear;

	[SerializeField]
	[Tooltip("Enabling this option may help prevent ghosting issues - although it may cause flickering on some rendering devices, such as Metal on iOS.")]
	public bool ClearGLBufferBeforeRendering;

	[Header("Performance")]
	[SerializeField]
	[Tooltip("Should this UIObject3D limit itself to a particular framerate?")]
	public bool LimitFrameRate;

	[SerializeField]
	[Tooltip("Maximum number of frames to render per second.")]
	public float FrameRateLimit = 30f;

	[Tooltip("If this is enabled, then this UIObject3D will render every frame (optionally limited by FrameRateLimit) even if none of the UIObject3D properties change. This should only be enabled if your target has animations of its own which are not controlled by UIObject3D.")]
	public bool RenderConstantly;

	[SerializeField]
	[Tooltip("Set this to a lower value to have UIObject3D render at a lower resolution, or higher to have UIObject3D render at a higher resolution.")]
	private float _RenderScale = 1f;

	private float timeSinceLastRender;

	[Header("Lighting")]
	[SerializeField]
	private bool _EnableCameraLight;

	[SerializeField]
	private Color _LightColor = Color.white;

	[SerializeField]
	[Range(0f, 8f)]
	private float _LightIntensity = 1f;

	[SerializeField]
	public UnityEvent OnUpdateTarget = new UnityEvent();

	[NonSerialized]
	private bool started;

	[NonSerialized]
	private bool hardUpdateQueued;

	[NonSerialized]
	private bool renderQueued;

	[NonSerialized]
	private Bounds targetBounds;

	private static bool copyTextureSupportedPopulated = false;

	private static bool _copyTextureSupported = false;

	private RectTransform _rectTransform;

	[SerializeField]
	[HideInInspector]
	private UIObject3DImage _imageComponent;

	private Texture2D _texture2D;

	private Sprite _sprite;

	public RenderTexture _renderTexture;

	public RawImage rawImage;

	private static Transform _parentContainer;

	private Transform _container;

	private Transform _targetContainer;

	private Transform _target;

	private Camera _targetCamera;

	private Light _cameraLight;

	private static int _objectLayer = -1;

	public Transform ObjectPrefab
	{
		get
		{
			return _ObjectPrefab;
		}
		set
		{
			_ObjectPrefab = value;
			HardUpdateDisplay();
		}
	}

	public Vector3 TargetRotation
	{
		get
		{
			return _TargetRotation;
		}
		set
		{
			_TargetRotation = UIObject3DUtilities.NormalizeRotation(value);
			UpdateDisplay();
		}
	}

	[SerializeField]
	public Vector2 TargetOffset
	{
		get
		{
			return new Vector2(_TargetOffsetX, _TargetOffsetY);
		}
		set
		{
			_TargetOffsetX = value.x;
			_TargetOffsetY = value.y;
			UpdateDisplay();
		}
	}

	public bool OverrideCalculatedTargetScale
	{
		get
		{
			return _OverrideCalculatedTargetScale;
		}
		set
		{
			_OverrideCalculatedTargetScale = value;
			UpdateDisplay();
		}
	}

	public float CalculatedTargetScaleOverride
	{
		get
		{
			return _CalculatedTargetScaleOverride;
		}
		set
		{
			_CalculatedTargetScaleOverride = value;
			UpdateDisplay();
		}
	}

	public float CameraFOV
	{
		get
		{
			return _CameraFOV;
		}
		set
		{
			_CameraFOV = value;
			UpdateDisplay();
		}
	}

	public float CameraDistance
	{
		get
		{
			return _CameraDistance;
		}
		set
		{
			_CameraDistance = value;
			UpdateDisplay();
		}
	}

	public bool AlwaysLookAtTarget
	{
		get
		{
			return _AlwaysLookAtTarget;
		}
		set
		{
			_AlwaysLookAtTarget = value;
			UpdateDisplay();
		}
	}

	public Vector2 TextureSize
	{
		get
		{
			if (_textureSize != default(Vector2))
			{
				return _textureSize;
			}
			if (target != null)
			{
				Vector2 vector = new Vector2(Mathf.Abs(Mathf.Floor(rectTransform.rect.width)), Mathf.Abs(Mathf.Floor(rectTransform.rect.height))) * RenderScale;
				if (vector.x == 0f || vector.y == 0f)
				{
					vector = new Vector2(256f, 256f);
				}
				_textureSize = vector;
				return vector;
			}
			return Vector2.one;
		}
	}

	public Color BackgroundColor
	{
		get
		{
			return _BackgroundColor;
		}
		set
		{
			_BackgroundColor = value;
			UpdateDisplay();
		}
	}

	public float RenderScale
	{
		get
		{
			return _RenderScale;
		}
		set
		{
			_RenderScale = value;
			HardUpdateDisplay();
		}
	}

	internal float timeBetweenFrames => 1f / FrameRateLimit;

	public bool EnableCameraLight
	{
		get
		{
			return _EnableCameraLight;
		}
		set
		{
			_EnableCameraLight = value;
			UpdateDisplay();
		}
	}

	public Color LightColor
	{
		get
		{
			return _LightColor;
		}
		set
		{
			_LightColor = value;
			UpdateDisplay();
		}
	}

	public float LightIntensity
	{
		get
		{
			return _LightIntensity;
		}
		set
		{
			_LightIntensity = value;
			UpdateDisplay();
		}
	}

	private static bool copyTextureSupported
	{
		get
		{
			if (!copyTextureSupportedPopulated)
			{
				_copyTextureSupported = (SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) == CopyTextureSupport.RTToTexture;
				copyTextureSupportedPopulated = true;
			}
			return _copyTextureSupported;
		}
	}

	protected RectTransform rectTransform
	{
		get
		{
			if (_rectTransform == null)
			{
				_rectTransform = GetComponent<RectTransform>();
			}
			return _rectTransform;
		}
	}

	public UIObject3DImage imageComponent
	{
		get
		{
			bool flag = false;
			if ((UnityEngine.Object)(object)_imageComponent == null)
			{
				_imageComponent = GetComponent<UIObject3DImage>();
				flag = true;
			}
			if ((UnityEngine.Object)(object)_imageComponent == null)
			{
				_imageComponent = base.gameObject.AddComponent<UIObject3DImage>();
				flag = true;
			}
			if (flag)
			{
				((Image)_imageComponent).type = (Type)0;
				((Image)_imageComponent).preserveAspect = true;
			}
			return _imageComponent;
		}
	}

	protected Texture2D texture2D
	{
		get
		{
			if (_texture2D == null)
			{
				_texture2D = new Texture2D((int)TextureSize.x, (int)TextureSize.y, TextureFormat.ARGB32, mipChain: false, linear: false);
			}
			return _texture2D;
		}
	}

	protected Sprite sprite
	{
		get
		{
			if (_sprite == null)
			{
				_sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (int)TextureSize.x, (int)TextureSize.y), new Vector2(0.5f, 0.5f));
			}
			return _sprite;
		}
	}

	protected RenderTexture renderTexture
	{
		get
		{
			if (_renderTexture == null)
			{
				_renderTexture = new RenderTexture((int)TextureSize.x, (int)TextureSize.y, 16, RenderTextureFormat.ARGB32);
				if (QualitySettings.anisotropicFiltering > AnisotropicFiltering.Disable)
				{
					_renderTexture.anisoLevel = (int)QualitySettings.anisotropicFiltering;
				}
				_renderTexture.filterMode = FilterMode.Trilinear;
				_renderTexture.useMipMap = false;
			}
			return _renderTexture;
		}
	}

	private static Transform parentContainer
	{
		get
		{
			if (_parentContainer == null)
			{
				GameObject gameObject = GameObject.Find("UIObject3D Scenes");
				if (gameObject != null)
				{
					_parentContainer = gameObject.transform;
				}
				else
				{
					_parentContainer = new GameObject().transform;
					_parentContainer.name = "UIObject3D Scenes";
				}
				if (_parentContainer.GetComponent<UIObject3DSceneManager>() == null)
				{
					_parentContainer.gameObject.AddComponent<UIObject3DSceneManager>();
				}
			}
			return _parentContainer;
		}
	}

	internal Transform container
	{
		get
		{
			if (_container == null)
			{
				if (ObjectPrefab == null)
				{
					return null;
				}
				_container = new GameObject().transform;
				_container.SetParent(parentContainer);
				_container.position = Vector3.zero;
				_container.localScale = Vector3.one;
				_container.localRotation = Quaternion.identity;
				_container.gameObject.layer = objectLayer;
				_container.name = "__UIObject3D_" + ObjectPrefab.name;
				_container.localPosition = UIObject3DUtilities.GetTargetContainerPosition(this);
				_container.gameObject.AddComponent<UIObject3DScene>().UIObject3D = this;
				UIObject3DUtilities.RegisterTargetContainerPosition(this, _container.localPosition);
			}
			return _container;
		}
	}

	internal Transform targetContainer
	{
		get
		{
			if (_targetContainer == null)
			{
				if (container == null)
				{
					return null;
				}
				_targetContainer = new GameObject().transform;
				_targetContainer.SetParent(container);
				_targetContainer.localPosition = Vector3.zero;
				_targetContainer.localScale = Vector3.one;
				_targetContainer.localRotation = Quaternion.identity;
				_targetContainer.name = "Target Container";
				_targetContainer.gameObject.layer = objectLayer;
			}
			return _targetContainer;
		}
	}

	protected Transform target
	{
		get
		{
			if (_target == null && started)
			{
				SetupTarget();
			}
			return _target;
		}
	}

	protected Camera targetCamera
	{
		get
		{
			if (_targetCamera == null)
			{
				SetupTargetCamera();
			}
			return _targetCamera;
		}
	}

	protected Light cameraLight
	{
		get
		{
			if (_cameraLight == null)
			{
				SetupCameraLight();
			}
			return _cameraLight;
		}
	}

	internal static int objectLayer
	{
		get
		{
			if (_objectLayer == -1)
			{
				_objectLayer = LayerMask.NameToLayer("UIObject3D");
			}
			return _objectLayer;
		}
	}

	private void DestroyResources()
	{
		if (_targetCamera != null)
		{
			_targetCamera.targetTexture = null;
		}
		if (_texture2D != null)
		{
			_Destroy(_texture2D);
		}
		if (_sprite != null)
		{
			_Destroy(_sprite);
		}
		if (_renderTexture != null)
		{
			_Destroy(_renderTexture);
		}
	}

	public void HardUpdateDisplay()
	{
		Color color = ((Graphic)imageComponent).color;
		if (Application.isPlaying)
		{
			((Graphic)imageComponent).color = new Color(0f, 0f, 0f, 0f);
		}
		DestroyResources();
		Cleanup();
		UIObject3DTimer.AtEndOfFrame(delegate
		{
			UpdateDisplay();
		}, this);
		UIObject3DTimer.DelayedCall(0.05f, delegate
		{
			((Graphic)imageComponent).color = color;
		}, this, forceEvenIfObjectIsInactive: true);
	}

	private void _Destroy(UnityEngine.Object o)
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(o);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(o);
		}
	}

	private void Start()
	{
		Color color = ((Graphic)imageComponent).color;
		if (Application.isPlaying)
		{
			((Graphic)imageComponent).color = new Color(0f, 0f, 0f, 0f);
		}
		UIObject3DTimer.AtEndOfFrame(delegate
		{
			SetStarted();
		}, this, forceEvenIfObjectIsInactive: true);
		UIObject3DTimer.AtEndOfFrame(delegate
		{
			OnEnable();
		}, this);
		UIObject3DTimer.DelayedCall(0.01f, delegate
		{
			Cleanup();
			UpdateDisplay();
			UIObject3DTimer.DelayedCall(0.05f, delegate
			{
				((Graphic)imageComponent).color = color;
			}, this, forceEvenIfObjectIsInactive: true);
		}, this, forceEvenIfObjectIsInactive: true);
	}

	public void SetStarted()
	{
		started = true;
	}

	public void UpdateDisplay(bool instantRender = false)
	{
		if (!Application.isPlaying && !started)
		{
			Start();
			SetStarted();
		}
		if (started)
		{
			Prepare();
			UpdateTargetPositioningAndScale();
			UpdateTargetCameraPositioningEtc();
			if (OnUpdateTarget != null)
			{
				OnUpdateTarget.Invoke();
			}
			Render(instantRender);
		}
	}

	private void OnEnable()
	{
		if (started)
		{
			if (objectLayer != -1)
			{
				ClearObjectLayerFromCameras();
				ClearObjectLayerFromLights();
			}
			UIObject3DTimer.AtEndOfFrame(delegate
			{
				UpdateDisplay(instantRender: true);
			}, this);
		}
	}

	private void ClearObjectLayerFromCameras()
	{
		Camera[] array = UnityEngine.Object.FindObjectsOfType<Camera>();
		foreach (Camera camera in array)
		{
			if (!(camera.GetComponent<UIObject3DCamera>() != null))
			{
				camera.cullingMask &= ~(1 << objectLayer);
			}
		}
	}

	private void ClearObjectLayerFromLights()
	{
		Light[] array = UnityEngine.Object.FindObjectsOfType<Light>();
		foreach (Light light in array)
		{
			if (light.type != LightType.Directional && !(light.name == "UIObject3DLight") && !(light.GetComponent<UIObject3DCamera>() != null))
			{
				light.cullingMask &= ~(1 << objectLayer);
			}
		}
	}

	private void OnDisable()
	{
		Cleanup();
	}

	private void OnDestroy()
	{
		UIObject3DUtilities.UnRegisterTargetContainer(this);
	}

	private void Prepare()
	{
		if (((Image)imageComponent).sprite != sprite)
		{
			((Image)imageComponent).sprite = sprite;
		}
		SetupTargetCamera();
	}

	public void Cleanup()
	{
		_texture2D = null;
		_sprite = null;
		_renderTexture = null;
		_target = null;
		_targetContainer = null;
		targetBounds = default(Bounds);
		_textureSize = default(Vector2);
		if (_container != null)
		{
			UIObject3DUtilities.UnRegisterTargetContainer(this);
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(_container.gameObject);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(_container.gameObject);
			}
			_container = null;
		}
	}

	public Transform GetTargetInstance()
	{
		return target;
	}

	internal void Render(bool instant = false)
	{
		if (Application.isPlaying && !instant)
		{
			renderQueued = true;
		}
		else if (!(targetCamera == null))
		{
			RenderTexture active = RenderTexture.active;
			if (!copyTextureSupported)
			{
				RenderTexture.active = renderTexture;
			}
			if (ClearGLBufferBeforeRendering)
			{
				GL.Clear(clearDepth: true, clearColor: true, BackgroundColor);
			}
			if (targetCamera.targetTexture != renderTexture)
			{
				targetCamera.targetTexture = renderTexture;
			}
			targetCamera.Render();
			if (copyTextureSupported)
			{
				Graphics.CopyTexture(renderTexture, texture2D);
			}
			else
			{
				Rect source = new Rect(0f, 0f, (int)TextureSize.x, (int)TextureSize.y);
				texture2D.ReadPixels(source, 0, 0);
				texture2D.Apply();
			}
			if (!copyTextureSupported)
			{
				RenderTexture.active = active;
			}
			renderQueued = false;
		}
	}

	private void Update()
	{
		if (!Application.isPlaying || !started)
		{
			return;
		}
		timeSinceLastRender += Time.unscaledDeltaTime;
		if (hardUpdateQueued)
		{
			hardUpdateQueued = false;
			HardUpdateDisplay();
		}
		else if (!LimitFrameRate || !(timeSinceLastRender < timeBetweenFrames))
		{
			if (renderQueued || RenderConstantly)
			{
				Render(instant: true);
				timeSinceLastRender = 0f;
			}
			if ((bool)(UnityEngine.Object)(object)rawImage)
			{
				rawImage.texture = _renderTexture;
			}
		}
	}

	private void SetupTarget()
	{
		if (_target == null)
		{
			if (ObjectPrefab == null)
			{
				if (Application.isPlaying)
				{
					Debug.LogWarning("[UIObject3D] No prefab set.");
				}
				return;
			}
			_target = UnityEngine.Object.Instantiate(ObjectPrefab);
		}
		UpdateTargetPositioningAndScale();
	}

	public void RefreshTarget()
	{
		if (_target != null)
		{
			Cleanup();
		}
		UIObject3DTimer.AtEndOfFrame(delegate
		{
			UpdateDisplay();
		}, this);
	}

	private void UpdateTargetPositioningAndScale()
	{
		if (_target == null)
		{
			return;
		}
		Renderer componentInChildren = _target.GetComponentInChildren<Renderer>();
		_target.name = "Target";
		bool flag = targetBounds == default(Bounds);
		if (flag)
		{
			bool flag2 = false;
			if (ObjectPrefab.gameObject.scene.name != "Null")
			{
				flag2 = true;
			}
			_target.transform.SetParent(targetContainer);
			if (flag2)
			{
				_target.transform.localPosition = Vector3.zero;
				_target.transform.localScale = Vector3.one;
				_target.localRotation = Quaternion.identity;
			}
			else
			{
				_target.transform.localPosition = ObjectPrefab.localPosition;
				_target.transform.localScale = ObjectPrefab.localScale;
				_target.transform.localRotation = ObjectPrefab.localRotation;
			}
			SetLayerRecursively(_target.transform, objectLayer);
		}
		if (componentInChildren != null)
		{
			if (flag)
			{
				Vector3 localPosition = _target.transform.localPosition;
				_target.transform.position = Vector3.zero;
				targetBounds = new Bounds(componentInChildren.bounds.center, componentInChildren.bounds.size);
				_target.transform.localPosition = localPosition;
				_target.transform.localPosition -= targetBounds.center;
			}
			if (!OverrideCalculatedTargetScale)
			{
				double num = 4.0 * Math.Tan((double)targetCamera.fieldOfView * 0.5 * 0.01745329238474369);
				double num2 = num * (double)targetCamera.aspect;
				double num3 = 1f / Math.Max(targetBounds.size.x, targetBounds.size.y);
				bool num4 = targetBounds.size.x > targetBounds.size.y;
				bool flag3 = targetBounds.size.y > targetBounds.size.x;
				if (num4)
				{
					num3 = num2 / (double)targetBounds.size.x;
				}
				else if (flag3)
				{
					num3 = num / (double)targetBounds.size.y;
				}
				double num5 = (double)targetBounds.size.y * num3;
				bool num6 = (double)targetBounds.size.x * num3 > num2;
				bool flag4 = num5 > num;
				if (num6)
				{
					num3 = num2 / (double)targetBounds.size.x;
				}
				if (flag4)
				{
					num3 = num / (double)targetBounds.size.y;
				}
				targetContainer.transform.localScale = Vector3.one * (float)num3;
				_CalculatedTargetScaleOverride = (float)num3;
			}
			else
			{
				targetContainer.transform.localScale = Vector3.one * CalculatedTargetScaleOverride;
			}
		}
		targetContainer.transform.localPosition = new Vector3(TargetOffset.x, TargetOffset.y, 0f);
		if (UseTargetRotation)
		{
			targetContainer.transform.localEulerAngles = TargetRotation;
		}
	}

	private void SetLayerRecursively(Transform transform, int layer)
	{
		transform.gameObject.layer = layer;
		foreach (Transform item in transform)
		{
			SetLayerRecursively(item, layer);
		}
	}

	private void SetupTargetCamera()
	{
		if (_targetCamera == null)
		{
			if (ObjectPrefab == null)
			{
				return;
			}
			GameObject gameObject = new GameObject();
			gameObject.transform.SetParent(container);
			_targetCamera = gameObject.AddComponent<Camera>();
			_targetCamera.enabled = false;
			_targetCamera.allowHDR = false;
			gameObject.AddComponent<UIObject3DCamera>();
		}
		UpdateTargetCameraPositioningEtc();
	}

	private void SetupCameraLight()
	{
		if (!(targetCamera == null))
		{
			if (_cameraLight == null)
			{
				_cameraLight = targetCamera.gameObject.AddComponent<Light>();
			}
			_cameraLight.enabled = EnableCameraLight;
			if (EnableCameraLight)
			{
				_cameraLight.gameObject.layer = objectLayer;
				_cameraLight.cullingMask = LayerMask.GetMask(LayerMask.LayerToName(objectLayer));
				_cameraLight.type = LightType.Point;
				_cameraLight.intensity = LightIntensity;
				_cameraLight.range = 200f;
				_cameraLight.color = LightColor;
				_cameraLight.bounceIntensity = 0f;
			}
		}
	}

	private void UpdateTargetCameraPositioningEtc()
	{
		if (!(_targetCamera == null))
		{
			_targetCamera.transform.localPosition = Vector3.zero + new Vector3(0f, 0f, CameraDistance);
			if (AlwaysLookAtTarget)
			{
				_targetCamera.transform.LookAt(_target);
			}
			else
			{
				_targetCamera.transform.rotation = Quaternion.identity;
			}
			_targetCamera.name = "Camera";
			_targetCamera.targetTexture = renderTexture;
			_targetCamera.clearFlags = CameraClearFlags.Color;
			_targetCamera.backgroundColor = Color.clear;
			_targetCamera.nearClipPlane = 0.1f;
			_targetCamera.farClipPlane = 50f;
			_targetCamera.fieldOfView = CameraFOV;
			_targetCamera.gameObject.layer = objectLayer;
			_targetCamera.cullingMask = LayerMask.GetMask(LayerMask.LayerToName(objectLayer));
			_targetCamera.backgroundColor = BackgroundColor;
			SetupCameraLight();
		}
	}
}
