using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class GUIImageParams
{
	public Rect pixelRect;

	public Texture2D texture;

	public bool useTexCoords;

	public Rect texCoords = new Rect(0f, 0f, 1f, 1f);

	public ScaleMode scaleMode = ScaleMode.ScaleToFit;

	public bool alphaBlend = true;

	public Color color = Color.white;

	public float aspect;

	public void Draw(Rect rect)
	{
		Draw(rect, hasAlpha: false, 1f);
	}

	public void Draw(Rect rect, bool hasAlpha, float alpha)
	{
		if (texture != null)
		{
			Color obj = GUI.color;
			GUI.color = color;
			if (hasAlpha)
			{
				GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);
			}
			Rect position = new Rect(rect.x + pixelRect.x, rect.y + pixelRect.y, Tools.ApproximatelyZero(pixelRect.width) ? rect.width : pixelRect.width, Tools.ApproximatelyZero(pixelRect.width) ? rect.height : pixelRect.height);
			if (useTexCoords)
			{
				GUI.DrawTextureWithTexCoords(position, texture, texCoords, alphaBlend);
			}
			else
			{
				GUI.DrawTexture(position, texture, scaleMode, alphaBlend, aspect);
			}
			GUI.color = obj;
		}
	}
}
