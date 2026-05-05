using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class GUIProgressBar : GUIVisibleControl
{
	public enum Origin
	{
		Top,
		Bottom,
		Left,
		Right,
		HorizontalCenter,
		VerticalCenter
	}

	public Origin origin;

	public Texture2D emptyImage;

	public Texture2D fullImage;

	public float progress;

	public override void DrawSelf(Vector2 relativeMousePosition)
	{
		if (emptyImage != null)
		{
			GUI.DrawTexture(base.rect, emptyImage);
		}
		float num = Mathf.Clamp(progress, 0f, 1f);
		Rect position;
		Rect texCoords;
		switch (origin)
		{
		case Origin.Top:
		{
			float height = base.rect.height * num;
			position = new Rect(base.rect.x, base.rect.y, base.rect.width, height);
			texCoords = new Rect(0f, 1f - num, 1f, num);
			break;
		}
		case Origin.Bottom:
		{
			float num5 = base.rect.height * num;
			position = new Rect(base.rect.x, base.rect.yMax - num5, base.rect.width, num5);
			texCoords = new Rect(0f, 0f, 1f, num);
			break;
		}
		default:
			position = new Rect(base.rect.x, base.rect.y, base.rect.width * num, base.rect.height);
			texCoords = new Rect(0f, 0f, num, 1f);
			break;
		case Origin.Right:
		{
			float num4 = base.rect.width * num;
			position = new Rect(base.rect.xMax - num4, base.rect.y, num4, base.rect.height);
			texCoords = new Rect(1f - num, 0f, num, 1f);
			break;
		}
		case Origin.HorizontalCenter:
		{
			float num3 = base.rect.width * num;
			position = new Rect(base.rect.x + 0.5f * (base.rect.width - num3), base.rect.y, num3, base.rect.height);
			texCoords = new Rect(0.5f * (1f - num), 0f, num, 1f);
			break;
		}
		case Origin.VerticalCenter:
		{
			float num2 = base.rect.height * num;
			position = new Rect(base.rect.x, base.rect.y + 0.5f * (base.rect.height - num2), base.rect.width, num2);
			texCoords = new Rect(0f, 0.5f * (1f - num), 1f, num);
			break;
		}
		}
		if (fullImage != null)
		{
			GUI.DrawTextureWithTexCoords(position, fullImage, texCoords);
		}
	}
}
