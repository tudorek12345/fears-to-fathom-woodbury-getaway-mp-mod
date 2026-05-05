using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

public class GUIControl : MonoBehaviour
{
	public int depth;

	public bool depthSortChildren;

	public ScaledRect scaledRect = new ScaledRect(ScaledRect.wholeScreen);

	public AutoSize autoSize;

	public Fit fit;

	public Navigation navigation;

	public bool visible = true;

	public bool clipChildren = true;

	private string fullName;

	private List<GUIControl> children = new List<GUIControl>();

	private bool needToUpdateLayout = true;

	private Vector2 windowSize = Vector2.zero;

	private bool navigationSelectButtonClicked;

	public Rect rect { get; set; }

	public Vector2 Offset { get; set; }

	protected List<GUIControl> Children => children;

	public bool NeedToUpdateLayout
	{
		get
		{
			return needToUpdateLayout;
		}
		set
		{
			needToUpdateLayout = value;
		}
	}

	protected Vector2 WindowSize
	{
		get
		{
			return windowSize;
		}
		set
		{
			windowSize = value;
		}
	}

	public bool IsNavigationEnabled
	{
		get
		{
			if (navigation != null)
			{
				return navigation.enabled;
			}
			return false;
		}
	}

	public string FullName
	{
		get
		{
			if (string.IsNullOrEmpty(fullName))
			{
				fullName = Tools.GetFullName(base.gameObject);
			}
			return fullName;
		}
	}

	public Vector2 dRect { get; set; }

	public virtual void Awake()
	{
		dRect = Vector2.zero;
		rect = scaledRect.GetPixelRect();
		Offset = Vector2.zero;
	}

	public virtual void OnEnable()
	{
		Refresh();
		if (IsNavigationEnabled && navigation.focusFirstControlOnEnable)
		{
			navigation.FocusFirstControl();
		}
	}

	public void Draw(Vector2 relativeMousePosition)
	{
		if (visible && base.gameObject.activeSelf)
		{
			UpdateLayout();
			DrawSelf(relativeMousePosition);
			DrawChildren(relativeMousePosition);
		}
	}

	public virtual void DrawSelf(Vector2 relativeMousePosition)
	{
	}

	public virtual void DrawChildren(Vector2 relativeMousePosition)
	{
		if (Children.Count == 0)
		{
			return;
		}
		if (clipChildren)
		{
			GUI.BeginGroup(rect);
		}
		try
		{
			bool isNavigationEnabled = IsNavigationEnabled;
			if (isNavigationEnabled && navigation.click != KeyCode.Space && Event.current.type == EventType.KeyDown && Event.current.character == ' ')
			{
				return;
			}
			GUIControl gUIControl = null;
			bool flag = IsNavigationEnabled && (navigation.IsClicked || navigationSelectButtonClicked);
			Vector2 relativeMousePosition2 = new Vector2(relativeMousePosition.x - rect.x, relativeMousePosition.y - rect.y);
			if (isNavigationEnabled)
			{
				navigation.CheckNavigationInput(relativeMousePosition2);
			}
			foreach (GUIControl child in Children)
			{
				if (IsNavigationEnabled)
				{
					GUI.SetNextControlName(child.FullName);
					if (flag && string.Equals(GUI.GetNameOfFocusedControl(), child.FullName))
					{
						navigationSelectButtonClicked = false;
						gUIControl = child;
					}
				}
				child.Draw(relativeMousePosition2);
			}
			if (isNavigationEnabled)
			{
				GUI.FocusControl(navigation.FocusedControlName);
			}
			if (gUIControl != null && gUIControl is GUIButton)
			{
				(gUIControl as GUIButton).Click();
			}
		}
		finally
		{
			if (clipChildren)
			{
				GUI.EndGroup();
			}
		}
	}

	public virtual void Update()
	{
		if (IsNavigationEnabled)
		{
			navigationSelectButtonClicked = DialogueManager.getInputButtonDown(navigation.clickButton);
		}
	}

	public virtual void Refresh(Vector2 windowSize)
	{
		NeedToUpdateLayout = true;
		WindowSize = windowSize;
	}

	public virtual void Refresh()
	{
		NeedToUpdateLayout = true;
	}

	public virtual void UpdateLayout()
	{
		if (NeedToUpdateLayout)
		{
			UpdateLayoutSelf();
			FitSelf();
			UpdateLayoutChildren();
			FitChildren();
		}
	}

	public virtual void UpdateLayoutSelf()
	{
		NeedToUpdateLayout = false;
		if (WindowSize.x == 0f)
		{
			WindowSize = new Vector2(Screen.width, Screen.height);
		}
		rect = scaledRect.GetPixelRect(WindowSize);
		if (Offset.x != 0f || Offset.y != 0f)
		{
			rect = new Rect(rect.x + Offset.x, rect.y + Offset.y, rect.width, rect.height);
		}
		if (dRect.x != 0f || dRect.y != 0f)
		{
			rect = new Rect(rect.x + dRect.x, rect.y + dRect.y, rect.width, rect.height);
		}
		if (autoSize != null)
		{
			AutoSizeSelf();
		}
	}

	public virtual void AutoSizeSelf()
	{
	}

	protected virtual void FitSelf()
	{
		if (fit == null || !fit.IsSpecified)
		{
			return;
		}
		float num = rect.xMin;
		float num2 = rect.xMax;
		float num3 = rect.yMin;
		float num4 = rect.yMax;
		if (fit.above != null)
		{
			num4 = fit.above.rect.yMin;
			if (fit.below == null && !fit.expandToFit)
			{
				num3 = num4 - rect.height;
			}
		}
		if (fit.below != null)
		{
			num3 = fit.below.rect.yMax;
			if (fit.above == null && !fit.expandToFit)
			{
				num4 = num3 + rect.height;
			}
		}
		if (fit.leftOf != null)
		{
			num2 = fit.leftOf.rect.xMin;
			if (fit.rightOf == null && !fit.expandToFit)
			{
				num = num2 - rect.width;
			}
		}
		if (fit.rightOf != null)
		{
			num = fit.rightOf.rect.xMax;
			if (fit.rightOf == null && !fit.expandToFit)
			{
				num2 = num + rect.width;
			}
		}
		rect = Rect.MinMaxRect(num, num3, num2, num4);
	}

	private void UpdateLayoutChildren()
	{
		FindChildren();
		if (depthSortChildren)
		{
			SortChildren();
		}
		Vector2 childWindowSize = new Vector2(rect.width, rect.height);
		foreach (GUIControl child in Children)
		{
			UpdateLayoutChild(child, childWindowSize);
		}
	}

	private void UpdateLayoutChild(GUIControl child, Vector2 childWindowSize)
	{
		child.Refresh(childWindowSize);
		child.dRect = (clipChildren ? Vector2.zero : new Vector2(rect.x, rect.y));
		child.UpdateLayout();
	}

	private void FitChildren()
	{
		for (int i = 0; i < Children.Count; i++)
		{
			Children[i].FitSelf();
		}
	}

	private void FindChildren()
	{
		Children.Clear();
		foreach (Transform item in base.transform)
		{
			GUIControl[] components = item.GetComponents<GUIControl>();
			Children.AddRange(components);
			if (components.Length != 0)
			{
				components[0].FindChildren();
			}
		}
	}

	private void SortChildren()
	{
		Children.Sort((GUIControl x, GUIControl y) => x.depth.CompareTo(y.depth));
	}
}
