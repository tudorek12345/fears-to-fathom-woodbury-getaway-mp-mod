using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUIAlertControls : AbstractUIControls
{
	protected float m_alertDoneTime;

	public abstract bool isVisible { get; }

	public virtual bool isDone => DialogueTime.time > m_alertDoneTime;

	public bool IsVisible => isVisible;

	public bool IsDone => isDone;

	public abstract void SetMessage(string message, float duration);

	public virtual void ShowMessage(string message, float duration)
	{
		if (!string.IsNullOrEmpty(message))
		{
			m_alertDoneTime = ((duration >= 0f) ? (DialogueTime.time + duration) : float.PositiveInfinity);
			SetMessage(message, duration);
			Show();
		}
		else
		{
			Hide();
		}
	}
}
