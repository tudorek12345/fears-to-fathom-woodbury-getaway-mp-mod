using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class UnityBarkUIOnGUI : MonoBehaviour
{
	public Vector3 offset = Vector3.zero;

	public float maxWidth;

	protected GUISkin guiSkin;

	protected string guiStyleName;

	protected GUIStyle guiStyle;

	protected FormattedText formattingToApply;

	protected TextStyle textStyle;

	protected Color textStyleColor = Color.black;

	protected Vector2 size;

	protected string message;

	protected float alpha = 1f;

	protected Transform myTransform;

	protected Transform absolutePosition;

	protected Vector3 offsetToHead = Vector3.zero;

	protected Vector3 screenPos = Vector3.zero;

	public bool IsPlaying => base.enabled;

	public Vector3 BarkPosition { get; private set; }

	public virtual void Awake()
	{
		myTransform = base.transform;
	}

	public virtual void Start()
	{
		ComputeOffsetToHead();
		base.enabled = false;
	}

	protected void ComputeOffsetToHead()
	{
		CharacterController component = GetComponent<CharacterController>();
		if (component != null)
		{
			offsetToHead = new Vector3(0f, component.height, 0f);
		}
		else
		{
			CapsuleCollider component2 = GetComponent<CapsuleCollider>();
			if (component2 != null)
			{
				offsetToHead = new Vector3(0f, component2.height, 0f);
			}
			else
			{
				BoxCollider component3 = GetComponent<BoxCollider>();
				if (component3 != null)
				{
					offsetToHead = new Vector3(0f, component3.center.y + component3.size.y, 0f);
				}
				else
				{
					SphereCollider component4 = GetComponent<SphereCollider>();
					if (component4 != null)
					{
						offsetToHead = new Vector3(0f, component4.center.y + component4.radius, 0f);
					}
					else
					{
						offsetToHead = Vector3.zero;
					}
				}
			}
		}
		offsetToHead += offset;
	}

	public virtual void Show(Subtitle subtitle, float duration, GUISkin guiSkin, string guiStyleName, TextStyle textStyle, bool includeName, Transform textPosition)
	{
		Show(subtitle, duration, guiSkin, guiStyleName, textStyle, Color.black, includeName, textPosition);
	}

	public virtual void Show(Subtitle subtitle, float duration, GUISkin guiSkin, string guiStyleName, TextStyle textStyle, Color textStyleColor, bool includeName, Transform textPosition)
	{
		message = (includeName ? string.Format("{0}: {1}", new object[2]
		{
			subtitle.speakerInfo.Name,
			subtitle.formattedText.text
		}) : subtitle.formattedText.text);
		formattingToApply = subtitle.formattedText;
		this.guiSkin = guiSkin;
		this.guiStyleName = guiStyleName;
		guiStyle = null;
		this.textStyle = textStyle;
		this.textStyleColor = textStyleColor;
		alpha = 1f;
		absolutePosition = textPosition;
		UpdateBarkPosition();
		base.enabled = true;
	}

	public IEnumerator FadeOut(float fadeDuration)
	{
		float startTime = Time.time;
		float endTime = startTime + fadeDuration;
		while (Time.time < endTime)
		{
			float num = Time.time - startTime;
			alpha = 1f - Mathf.Clamp(num / fadeDuration, 0f, 1f);
			yield return null;
		}
		base.enabled = false;
	}

	public virtual void OnGUI()
	{
		GUI.skin = UnityGUITools.GetValidGUISkin(guiSkin);
		if (guiStyle == null)
		{
			guiStyle = UnityGUITools.ApplyFormatting(formattingToApply, new GUIStyle(UnityGUITools.GetGUIStyle(guiStyleName, GUI.skin.label)));
			guiStyle.alignment = TextAnchor.UpperCenter;
			size = guiStyle.CalcSize(new GUIContent(message));
			if (maxWidth >= 1f && size.x > maxWidth)
			{
				size = new Vector2(maxWidth, guiStyle.CalcHeight(new GUIContent(message), maxWidth));
			}
		}
		UpdateBarkPosition();
		guiStyle.normal.textColor = UnityGUITools.ColorWithAlpha(guiStyle.normal.textColor, alpha);
		if (!(screenPos.z < 0f))
		{
			UnityGUITools.DrawText(new Rect(screenPos.x - size.x / 2f, (float)Screen.height - screenPos.y - size.y / 2f, size.x, size.y), message, guiStyle, textStyle, textStyleColor);
		}
	}

	protected void UpdateBarkPosition()
	{
		if (!(Camera.main == null))
		{
			if (myTransform == null)
			{
				myTransform = base.transform;
			}
			BarkPosition = ((absolutePosition != null) ? (absolutePosition.position + offset) : (myTransform.position + offsetToHead));
			screenPos = Camera.main.WorldToScreenPoint(BarkPosition);
		}
	}
}
