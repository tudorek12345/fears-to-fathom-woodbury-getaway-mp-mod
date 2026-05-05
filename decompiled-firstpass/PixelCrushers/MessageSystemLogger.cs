using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class MessageSystemLogger : MonoBehaviour
{
	[Tooltip("Log a message when this GameObject sends a message to the Message System.")]
	public bool logWhenSendingMessages;

	[Tooltip("Log a message when this GameObject receives a message from the Message System.")]
	public bool logWhenReceivingMessages;

	private void OnEnable()
	{
		if (logWhenSendingMessages)
		{
			MessageSystem.LogWhenSendingMessages(base.gameObject);
		}
		if (!logWhenReceivingMessages)
		{
			return;
		}
		if (GetComponent(typeof(IMessageHandler)) == null)
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("MessageSystem: " + base.name + " doesn't have any IMessageHandler components. Can't log when receiving messages.", this);
			}
		}
		else
		{
			MessageSystem.LogWhenReceivingMessages(base.gameObject);
		}
	}

	private void OnDisable()
	{
		if (logWhenSendingMessages)
		{
			MessageSystem.StopLoggingWhenSendingMessages(base.gameObject);
		}
		if (logWhenReceivingMessages)
		{
			MessageSystem.StopLoggingWhenReceivingMessages(base.gameObject);
		}
	}
}
