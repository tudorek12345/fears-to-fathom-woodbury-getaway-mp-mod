using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class Navigation
{
	public bool enabled;

	public bool focusFirstControlOnEnable = true;

	public bool jumpToMousePosition = true;

	public GUIControl[] order;

	public string clickButton = "Fire1";

	public KeyCode click = KeyCode.Space;

	public KeyCode previous = KeyCode.UpArrow;

	public KeyCode next = KeyCode.DownArrow;

	public string axis = "Vertical";

	public bool invertAxis = true;

	public float axisRepeatDelay = 1f;

	public float mouseWheelSensitivity = 5f;

	private int current;

	private float axisRepeatTime;

	private const float AxisThreshold = 0.5f;

	private const float MinorAxisThreshold = 0.01f;

	private float mouseWheelY;

	private bool isAxisPrevDown;

	private bool isAxisNextDown;

	private float timeNextRelease;

	public string FocusedControlName
	{
		get
		{
			if (!IsCurrentValid)
			{
				return string.Empty;
			}
			return order[current].FullName;
		}
	}

	private bool IsCurrentValid
	{
		get
		{
			if (IsOrderArrayValid && 0 <= current)
			{
				return current < order.Length;
			}
			return false;
		}
	}

	private bool IsOrderArrayValid
	{
		get
		{
			if (order != null)
			{
				return order.Length != 0;
			}
			return false;
		}
	}

	public bool IsClicked
	{
		get
		{
			if (Event.current.type == EventType.KeyDown)
			{
				return Event.current.keyCode == click;
			}
			return false;
		}
	}

	public void FocusFirstControl()
	{
		if (IsOrderArrayValid && IsClickableButton(order[0]))
		{
			current = 0;
			return;
		}
		current = ((order != null) ? (order.Length + 1) : 0);
		Navigate(1);
	}

	public void CheckNavigationInput(Vector2 relativeMousePosition)
	{
		CheckMouseWheel();
		float navigationAxis = GetNavigationAxis();
		if (IsPreviousControlInputDown(navigationAxis))
		{
			Navigate(-1);
		}
		else if (IsNextControlInputDown(navigationAxis))
		{
			Navigate(1);
		}
		else if (jumpToMousePosition)
		{
			NavigateToMousePosition(relativeMousePosition);
		}
	}

	private void NavigateToMousePosition(Vector2 relativeMousePosition)
	{
		for (int i = 0; i < order.Length; i++)
		{
			if (order[i].gameObject.activeInHierarchy && order[i].visible && IsClickableButton(order[i]) && order[i].rect.Contains(relativeMousePosition))
			{
				current = i;
				break;
			}
		}
	}

	public void Navigate(int direction)
	{
		if (IsOrderArrayValid)
		{
			int num = current;
			current = NextControlIndex(direction);
			int num2 = 0;
			while (!IsClickableButton(order[current]) && current != num && num2 <= 999)
			{
				current = NextControlIndex(direction);
				num2++;
			}
		}
	}

	private bool IsClickableButton(GUIControl control)
	{
		if (control != null && control.visible && control is GUIButton)
		{
			return (control as GUIButton).clickable;
		}
		return false;
	}

	private int NextControlIndex(int direction)
	{
		if (IsOrderArrayValid)
		{
			int num = (current + direction) % order.Length;
			if (num < 0)
			{
				return order.Length - 1;
			}
			return num;
		}
		return 0;
	}

	private void CheckMouseWheel()
	{
		if (Event.current.type == EventType.ScrollWheel)
		{
			mouseWheelY += Event.current.delta.y;
		}
	}

	private bool IsNextControlInputDown(float axisValue)
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == next)
		{
			Event.current.Use();
			isAxisNextDown = true;
		}
		else if (mouseWheelY >= mouseWheelSensitivity)
		{
			mouseWheelY = 0f;
			return true;
		}
		bool flag = isAxisNextDown && axisValue <= 0.01f && Time.time >= timeNextRelease;
		isAxisNextDown = axisValue > 0.01f;
		if (axisValue > 0.5f)
		{
			if (DialogueTime.time >= axisRepeatTime)
			{
				axisRepeatTime = DialogueTime.time + axisRepeatDelay;
				timeNextRelease = Time.time + 0.5f;
				return true;
			}
		}
		else
		{
			if (axisValue >= 0f)
			{
				axisRepeatTime = 0f;
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPreviousControlInputDown(float axisValue)
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == previous)
		{
			Event.current.Use();
			isAxisPrevDown = true;
		}
		else if (mouseWheelY <= 0f - mouseWheelSensitivity)
		{
			mouseWheelY = 0f;
			return true;
		}
		bool flag = isAxisPrevDown && axisValue >= -0.01f && Time.time >= timeNextRelease;
		isAxisPrevDown = axisValue < -0.01f;
		if (axisValue < -0.5f)
		{
			if (DialogueTime.time >= axisRepeatTime)
			{
				axisRepeatTime = DialogueTime.time + axisRepeatDelay;
				timeNextRelease = Time.time + 0.5f;
				return true;
			}
		}
		else
		{
			if (axisValue <= 0f)
			{
				axisRepeatTime = 0f;
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private float GetNavigationAxis()
	{
		if (!Application.isPlaying || string.IsNullOrEmpty(axis))
		{
			return 0f;
		}
		try
		{
			return Input.GetAxis(axis) * (float)((!invertAxis) ? 1 : (-1));
		}
		catch (UnityException)
		{
			return 0f;
		}
	}
}
