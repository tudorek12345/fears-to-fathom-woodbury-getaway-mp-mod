using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class UnityBarkUI : AbstractBarkUI
{
	public Transform textPosition;

	public GUISkin guiSkin;

	public string guiStyleName;

	public bool includeName;

	public float duration = 4f;

	public float fadeDuration = 0.5f;

	public TextStyle textStyle = TextStyle.Shadow;

	public Color textStyleColor = Color.black;

	public BarkSubtitleSetting textDisplaySetting;

	public bool waitUntilSequenceEnds;

	public bool checkIfPlayerVisible = true;

	public LayerMask visibilityLayerMask = 1;

	protected UnityBarkUIOnGUI unityBarkUIOnGUI;

	protected Transform playerCameraTransform;

	protected Collider playerCameraCollider;

	protected float secondsLeft;

	public bool showText
	{
		get
		{
			if (textDisplaySetting != BarkSubtitleSetting.Show)
			{
				if (textDisplaySetting == BarkSubtitleSetting.SameAsDialogueManager)
				{
					return DialogueManager.displaySettings.subtitleSettings.showNPCSubtitlesDuringLine;
				}
				return false;
			}
			return true;
		}
	}

	public override bool isPlaying => secondsLeft > 0f;

	public virtual void Awake()
	{
		CheckUnityBarkUIOnGUI();
	}

	public virtual void OnDestroy()
	{
		Object.Destroy(unityBarkUIOnGUI);
		unityBarkUIOnGUI = null;
	}

	protected void CheckUnityBarkUIOnGUI()
	{
		if (unityBarkUIOnGUI == null)
		{
			unityBarkUIOnGUI = GetComponent<UnityBarkUIOnGUI>();
			if (unityBarkUIOnGUI == null)
			{
				unityBarkUIOnGUI = base.gameObject.AddComponent<UnityBarkUIOnGUI>();
			}
		}
	}

	public override void Bark(Subtitle subtitle)
	{
		if (showText && subtitle != null && !string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			if (Camera.main == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: There is no camera in the scene marked MainCamera, but UnityBarkUI requires one.", "Dialogue System"));
			}
			CheckUnityBarkUIOnGUI();
			unityBarkUIOnGUI.Show(subtitle, duration, guiSkin, guiStyleName, textStyle, textStyleColor, includeName, textPosition);
			CheckPlayerCameraTransform();
			StopAllCoroutines();
			secondsLeft = (Mathf.Approximately(0f, duration) ? DialogueManager.GetBarkDuration(subtitle.formattedText.text) : duration);
		}
	}

	public virtual void Update()
	{
		if (secondsLeft > 0f)
		{
			secondsLeft -= Time.deltaTime;
			if (checkIfPlayerVisible)
			{
				CheckPlayerVisibility();
			}
			if (secondsLeft <= 0f && !waitUntilSequenceEnds)
			{
				Hide();
			}
		}
	}

	public void OnBarkEnd(Transform actor)
	{
		if (waitUntilSequenceEnds)
		{
			Hide();
		}
	}

	public override void Hide()
	{
		if (unityBarkUIOnGUI.enabled)
		{
			StartCoroutine(unityBarkUIOnGUI.FadeOut(fadeDuration));
		}
		secondsLeft = 0f;
	}

	protected void CheckPlayerVisibility()
	{
		CheckPlayerCameraTransform();
		bool flag = true;
		if (playerCameraTransform != null && Physics.Linecast(unityBarkUIOnGUI.BarkPosition, playerCameraTransform.position, out var hitInfo, visibilityLayerMask))
		{
			flag = hitInfo.collider == playerCameraCollider;
		}
		if (unityBarkUIOnGUI != null)
		{
			if (unityBarkUIOnGUI.enabled && !flag)
			{
				unityBarkUIOnGUI.enabled = false;
			}
			else if (!unityBarkUIOnGUI.enabled && flag)
			{
				unityBarkUIOnGUI.enabled = true;
			}
		}
	}

	protected void CheckPlayerCameraTransform()
	{
		if (playerCameraTransform == null && Camera.main != null)
		{
			playerCameraTransform = Camera.main.transform;
			playerCameraCollider = ((playerCameraTransform != null) ? playerCameraTransform.GetComponent<Collider>() : null);
		}
	}
}
