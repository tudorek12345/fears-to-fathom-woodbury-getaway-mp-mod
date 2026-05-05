using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class ImageAnimation
{
	public bool animate;

	public int frameWidth = 64;

	public float framesPerSecond = 1f;

	private int numFrames = 1;

	private float frameNormalWidth = 1f;

	private int currentFrame;

	private float nextFrameTime;

	private Rect texCoords;

	private float lastDialogueTime;

	public void RefreshAnimation(Texture2D image)
	{
		if (!(image == null) && Application.isPlaying)
		{
			if (image != null)
			{
				numFrames = image.width / Mathf.Max(frameWidth, 1);
				frameNormalWidth = 1f / (float)Mathf.Max(numFrames, 1);
				nextFrameTime = DialogueTime.time + 1f / Mathf.Max(framesPerSecond, 0.05f);
				lastDialogueTime = DialogueTime.time;
			}
			else
			{
				nextFrameTime = float.PositiveInfinity;
			}
		}
	}

	public void DrawAnimation(Rect rect, Texture2D image)
	{
		if (Application.isPlaying)
		{
			if (DialogueTime.time >= nextFrameTime || DialogueTime.time < lastDialogueTime)
			{
				if (numFrames == 0 || frameNormalWidth == 0f)
				{
					numFrames = image.width / Mathf.Max(frameWidth, 1);
					frameNormalWidth = 1f / (float)Mathf.Max(numFrames, 1);
				}
				currentFrame = (currentFrame + 1) % Mathf.Max(numFrames, 1);
				texCoords = new Rect((float)currentFrame * frameNormalWidth, 0f, frameNormalWidth, 1f);
				nextFrameTime = DialogueTime.time + 1f / Mathf.Max(framesPerSecond, 0.05f);
			}
			lastDialogueTime = DialogueTime.time;
		}
		else
		{
			texCoords = new Rect(0f, 0f, (float)frameWidth / (float)Mathf.Max(image.width, 1), 1f);
		}
		if (texCoords.width > 0f)
		{
			GUI.DrawTextureWithTexCoords(rect, image, texCoords);
		}
	}
}
