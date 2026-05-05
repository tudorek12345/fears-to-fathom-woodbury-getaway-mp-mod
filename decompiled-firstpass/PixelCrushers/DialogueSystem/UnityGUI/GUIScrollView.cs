using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class GUIScrollView : GUIControl
{
	public bool showVerticalScrollbar = true;

	public bool showHorizontalScrollbar;

	public int padding = 2;

	private Vector2 scrollViewVector = Vector2.zero;

	public float contentWidth { get; set; }

	public float contentHeight { get; set; }

	public event Action MeasureContentHandler;

	public event Action DrawContentHandler;

	public void ResetScrollPosition()
	{
		scrollViewVector = Vector2.zero;
	}

	public override void DrawChildren(Vector2 relativeMousePosition)
	{
		clipChildren = false;
		Rect scrollContentRect = GetScrollContentRect();
		GUIStyle horizontalScrollbar = (showHorizontalScrollbar ? GUI.skin.horizontalScrollbar : GUIStyle.none);
		GUIStyle verticalScrollbar = (showVerticalScrollbar ? GUI.skin.verticalScrollbar : GUIStyle.none);
		scrollViewVector = GUI.BeginScrollView(base.rect, scrollViewVector, scrollContentRect, horizontalScrollbar, verticalScrollbar);
		try
		{
			if (this.DrawContentHandler != null)
			{
				this.DrawContentHandler();
			}
			base.DrawChildren(relativeMousePosition);
		}
		finally
		{
			GUI.EndScrollView();
		}
	}

	private Rect GetScrollContentRect()
	{
		float num = ((GUI.skin.verticalSlider.normal.background != null) ? ((float)GUI.skin.verticalSlider.normal.background.width) : 16f);
		contentWidth = base.rect.width - num;
		MeasureChildrenAsContent();
		if (this.MeasureContentHandler != null)
		{
			this.MeasureContentHandler();
		}
		return new Rect(0f, 0f, contentWidth, contentHeight);
	}

	private void MeasureChildrenAsContent()
	{
		if (base.Children == null)
		{
			return;
		}
		foreach (GUIControl child in base.Children)
		{
			contentWidth = Mathf.Max(contentWidth, GetChildXMax(child));
			contentHeight = Mathf.Max(contentHeight, GetChildYMax(child));
		}
	}

	private float GetChildXMax(GUIControl child)
	{
		return child.rect.xMax;
	}

	private float GetChildYMax(GUIControl child)
	{
		if (child is GUILabel)
		{
			GUILabel gUILabel = child as GUILabel;
			if (gUILabel.autoSize != null && gUILabel.autoSize.autoSizeHeight)
			{
				gUILabel.Refresh(new Vector2(base.rect.width, base.rect.height));
				gUILabel.UpdateLayout();
			}
		}
		return child.rect.yMax;
	}
}
