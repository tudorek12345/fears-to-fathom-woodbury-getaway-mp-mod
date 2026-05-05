using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SelectorFollowTarget : MonoBehaviour
{
	public Vector3 offset = Vector3.zero;

	private Selector selector;

	private ProximitySelector proximitySelector;

	private bool previousUseDefaultGUI;

	private Usable lastUsable;

	private string heading = string.Empty;

	private string useMessage = string.Empty;

	private GameObject lastSelectionDrawn;

	private float selectionHeight;

	private Vector2 selectionHeadingSize = Vector2.zero;

	private Vector2 selectionUseMessageSize = Vector2.zero;

	private SelectorUseStandardUIElements selectorUseStandardUIElements;

	private void Awake()
	{
		selector = GetComponent<Selector>();
		proximitySelector = GetComponent<ProximitySelector>();
		selectorUseStandardUIElements = GetComponent<SelectorUseStandardUIElements>();
	}

	private void OnEnable()
	{
		if (selector != null)
		{
			previousUseDefaultGUI = selector.useDefaultGUI;
			selector.useDefaultGUI = false;
		}
		if (proximitySelector != null)
		{
			previousUseDefaultGUI = proximitySelector.useDefaultGUI;
			proximitySelector.useDefaultGUI = false;
		}
	}

	private void OnDisable()
	{
		if (selector != null)
		{
			selector.useDefaultGUI = previousUseDefaultGUI;
		}
		if (proximitySelector != null)
		{
			proximitySelector.useDefaultGUI = previousUseDefaultGUI;
		}
	}

	public virtual void Update()
	{
		if (selectorUseStandardUIElements == null || !selectorUseStandardUIElements.enabled || StandardUISelectorElements.instance == null)
		{
			return;
		}
		Usable usable = null;
		if (selector != null && selector.enabled)
		{
			usable = selector.CurrentUsable;
		}
		else if (proximitySelector != null && proximitySelector.enabled)
		{
			usable = proximitySelector.CurrentUsable;
		}
		if (!(usable == null))
		{
			Graphic mainGraphic = selectorUseStandardUIElements.elements.mainGraphic;
			GameObject gameObject = usable.gameObject;
			Vector3 position = Camera.main.WorldToScreenPoint(gameObject.transform.position + Vector3.up * selectionHeight);
			position += offset;
			position += new Vector3((0f - mainGraphic.rectTransform.sizeDelta.x) / 2f, mainGraphic.rectTransform.sizeDelta.y / 2f, 0f);
			if (!(position.z < 0f))
			{
				mainGraphic.rectTransform.position = position;
			}
		}
	}

	public virtual void OnGUI()
	{
		if (!(selectorUseStandardUIElements != null) || !selectorUseStandardUIElements.enabled)
		{
			if (selector != null && selector.useDefaultGUI)
			{
				DrawOnSelection(selector.CurrentUsable, selector.CurrentDistance, selector.reticle, selector.GuiStyle, selector.defaultUseMessage, selector.inRangeColor, selector.outOfRangeColor, selector.textStyle, selector.textStyleColor);
			}
			else if (proximitySelector != null && proximitySelector.useDefaultGUI)
			{
				DrawOnSelection(proximitySelector.CurrentUsable, 0f, null, proximitySelector.GuiStyle, proximitySelector.defaultUseMessage, proximitySelector.color, proximitySelector.color, proximitySelector.textStyle, proximitySelector.textStyleColor);
			}
		}
	}

	protected void DrawOnSelection(Usable usable, float distance, Selector.Reticle reticle, GUIStyle guiStyle, string defaultUseMessage, Color inRangeColor, Color outOfRangeColor, TextStyle textStyle, Color textStyleColor)
	{
		if (usable == null)
		{
			return;
		}
		if (usable != lastUsable || string.IsNullOrEmpty(heading))
		{
			lastUsable = usable;
			heading = usable.GetName();
			useMessage = (string.IsNullOrEmpty(usable.overrideUseMessage) ? defaultUseMessage : usable.overrideUseMessage);
		}
		GameObject gameObject = usable.gameObject;
		if (gameObject != lastSelectionDrawn)
		{
			selectionHeight = Tools.GetGameObjectHeight(gameObject);
			selectionHeadingSize = guiStyle.CalcSize(new GUIContent(heading));
			selectionUseMessageSize = guiStyle.CalcSize(new GUIContent(useMessage));
		}
		bool flag = distance <= usable.maxUseDistance;
		guiStyle.normal.textColor = (flag ? inRangeColor : outOfRangeColor);
		Vector3 vector = Camera.main.WorldToScreenPoint(gameObject.transform.position + Vector3.up * selectionHeight);
		vector += offset;
		vector = new Vector3(vector.x, vector.y + selectionUseMessageSize.y + selectionHeadingSize.y, vector.z);
		if (vector.z < 0f)
		{
			return;
		}
		UnityGUITools.DrawText(new Rect(vector.x - selectionHeadingSize.x / 2f, (float)Screen.height - vector.y - selectionHeadingSize.y / 2f, selectionHeadingSize.x, selectionHeadingSize.y), heading, guiStyle, textStyle, textStyleColor);
		vector = Camera.main.WorldToScreenPoint(gameObject.transform.position + Vector3.up * selectionHeight);
		vector += offset;
		vector = new Vector3(vector.x, vector.y + selectionUseMessageSize.y, vector.z);
		UnityGUITools.DrawText(new Rect(vector.x - selectionUseMessageSize.x / 2f, (float)Screen.height - vector.y - selectionUseMessageSize.y / 2f, selectionUseMessageSize.x, selectionUseMessageSize.y), useMessage, guiStyle, textStyle, textStyleColor);
		if (reticle != null)
		{
			Texture2D texture2D = (flag ? reticle.inRange : reticle.outOfRange);
			if (texture2D != null)
			{
				vector = Camera.main.WorldToScreenPoint(gameObject.transform.position + Vector3.up * 0.5f * selectionHeight);
				GUI.Label(new Rect(vector.x - reticle.width / 2f, (float)Screen.height - vector.y - reticle.height / 2f, reticle.width, reticle.height), texture2D);
			}
		}
	}
}
