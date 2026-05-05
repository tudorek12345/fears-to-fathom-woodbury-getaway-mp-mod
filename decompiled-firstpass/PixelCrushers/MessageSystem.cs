using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

public static class MessageSystem
{
	public class ListenerInfo
	{
		public IMessageHandler listener;

		public string message;

		public string parameter;

		public int frameAdded;

		public bool removed;

		public ListenerInfo()
		{
		}

		public ListenerInfo(IMessageHandler listener, string message, string parameter)
		{
			this.listener = listener;
			this.message = message;
			this.parameter = parameter;
			frameAdded = Time.frameCount;
			removed = false;
		}

		public void Assign(IMessageHandler listener, string message, string parameter)
		{
			this.listener = listener;
			this.message = message;
			this.parameter = parameter;
			frameAdded = Time.frameCount;
			removed = false;
		}

		public void Clear()
		{
			listener = null;
			message = null;
			parameter = null;
			removed = false;
		}
	}

	private static List<ListenerInfo> s_listenerInfo = new List<ListenerInfo>();

	private static Pool<ListenerInfo> s_listenerInfoPool = new Pool<ListenerInfo>();

	private static HashSet<GameObject> s_sendersToLog = new HashSet<GameObject>();

	private static HashSet<GameObject> s_listenersToLog = new HashSet<GameObject>();

	private static bool s_sendInEditMode = false;

	private static bool s_allowReceiveSameFrameAdded = true;

	private static bool s_debug = false;

	private static int s_sendMessageDepth = 0;

	public static bool sendInEditMode
	{
		get
		{
			return s_sendInEditMode;
		}
		set
		{
			s_sendInEditMode = value;
		}
	}

	public static bool allowReceiveSameFrameAdded
	{
		get
		{
			return s_allowReceiveSameFrameAdded;
		}
		set
		{
			s_allowReceiveSameFrameAdded = value;
		}
	}

	public static bool debug
	{
		get
		{
			if (s_debug)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
		set
		{
			s_debug = value;
		}
	}

	private static List<ListenerInfo> listenerInfo => s_listenerInfo;

	private static Pool<ListenerInfo> listenerInfoPool => s_listenerInfoPool;

	private static int sendMessageDepth
	{
		get
		{
			return s_sendMessageDepth;
		}
		set
		{
			s_sendMessageDepth = value;
		}
	}

	public static bool IsListenerRegistered(IMessageHandler listener, string message, string parameter)
	{
		for (int i = 0; i < MessageSystem.listenerInfo.Count; i++)
		{
			ListenerInfo listenerInfo = MessageSystem.listenerInfo[i];
			if (!listenerInfo.removed && listenerInfo.listener == listener && string.Equals(listenerInfo.message, message) && (string.Equals(listenerInfo.parameter, parameter) || string.IsNullOrEmpty(listenerInfo.parameter)))
			{
				return true;
			}
		}
		return false;
	}

	public static void AddListener(IMessageHandler listener, string message, string parameter)
	{
		if (debug)
		{
			Debug.Log("MessageSystem.AddListener(listener=" + listener?.ToString() + ": " + message + "," + parameter + ")");
		}
		for (int i = 0; i < MessageSystem.listenerInfo.Count; i++)
		{
			ListenerInfo listenerInfo = MessageSystem.listenerInfo[i];
			if (listenerInfo.listener == listener && string.Equals(listenerInfo.message, message) && (string.Equals(listenerInfo.parameter, parameter) || string.IsNullOrEmpty(listenerInfo.parameter)))
			{
				listenerInfo.removed = false;
				return;
			}
		}
		ListenerInfo listenerInfo2 = listenerInfoPool.Get();
		listenerInfo2.Assign(listener, message, parameter);
		MessageSystem.listenerInfo.Add(listenerInfo2);
	}

	public static void AddListener(IMessageHandler listener, StringField message, StringField parameter)
	{
		AddListener(listener, StringField.GetStringValue(message), StringField.GetStringValue(parameter));
	}

	public static void AddListener(IMessageHandler listener, StringField message, string parameter)
	{
		AddListener(listener, StringField.GetStringValue(message), parameter);
	}

	public static void AddListener(IMessageHandler listener, string message, StringField parameter)
	{
		AddListener(listener, message, StringField.GetStringValue(parameter));
	}

	public static void RemoveListener(IMessageHandler listener, string message, string parameter)
	{
		if (debug)
		{
			Debug.Log("MessageSystem.RemoveListener(listener=" + listener?.ToString() + ": " + message + "," + parameter + ")");
		}
		if (MessageSystem.listenerInfo.Count <= 0)
		{
			return;
		}
		for (int num = MessageSystem.listenerInfo.Count - 1; num >= 0; num--)
		{
			ListenerInfo listenerInfo = MessageSystem.listenerInfo[num];
			if (listenerInfo.listener == listener && (string.Equals(listenerInfo.message, message) || string.IsNullOrEmpty(message)) && (string.Equals(listenerInfo.parameter, parameter) || string.IsNullOrEmpty(parameter)))
			{
				listenerInfo.removed = true;
				if (sendMessageDepth == 0)
				{
					MessageSystem.listenerInfo.RemoveAt(num);
					listenerInfo.Clear();
					listenerInfoPool.Release(listenerInfo);
				}
			}
		}
	}

	private static void RemoveMarkedListenerInfo()
	{
		List<ListenerInfo> list = MessageSystem.listenerInfo.FindAll((ListenerInfo x) => x.removed);
		MessageSystem.listenerInfo.RemoveAll((ListenerInfo x) => x.removed);
		for (int num = 0; num < list.Count; num++)
		{
			ListenerInfo listenerInfo = list[num];
			listenerInfo.Clear();
			listenerInfoPool.Release(listenerInfo);
		}
	}

	public static void RemoveListener(IMessageHandler listener, StringField message, StringField parameter)
	{
		RemoveListener(listener, StringField.GetStringValue(message), StringField.GetStringValue(parameter));
	}

	public static void RemoveListener(IMessageHandler listener, StringField message, string parameter)
	{
		RemoveListener(listener, StringField.GetStringValue(message), parameter);
	}

	public static void RemoveListener(IMessageHandler listener, string message, StringField parameter)
	{
		RemoveListener(listener, message, StringField.GetStringValue(parameter));
	}

	public static void RemoveListener(IMessageHandler listener)
	{
		RemoveListener(listener, string.Empty, string.Empty);
	}

	public static void LogWhenSendingMessages(GameObject sender)
	{
		if (!(sender == null))
		{
			s_sendersToLog.Add(sender);
		}
	}

	public static void StopLoggingWhenSendingMessages(GameObject sender)
	{
		if (!(sender == null))
		{
			s_sendersToLog.Remove(sender);
		}
	}

	public static void LogWhenReceivingMessages(GameObject listener)
	{
		if (!(listener == null))
		{
			s_listenersToLog.Add(listener);
		}
	}

	public static void StopLoggingWhenReceivingMessages(GameObject listener)
	{
		if (!(listener == null))
		{
			s_listenersToLog.Add(listener);
		}
	}

	private static bool ShouldLogSender(object sender)
	{
		if (sender is UnityEngine.Object && sender as UnityEngine.Object == null)
		{
			return false;
		}
		if (!(sender is GameObject) || !s_sendersToLog.Contains(sender as GameObject))
		{
			if (sender is Component)
			{
				return s_sendersToLog.Contains((sender as Component).gameObject);
			}
			return false;
		}
		return true;
	}

	private static bool ShouldLogReceiver(IMessageHandler receiver)
	{
		if (receiver is Component && receiver as Component != null)
		{
			return s_listenersToLog.Contains((receiver as Component).gameObject);
		}
		return false;
	}

	public static void SendMessageWithTarget(object sender, object target, string message, string parameter, params object[] values)
	{
		if (!Application.isPlaying && !sendInEditMode)
		{
			return;
		}
		if (debug || ShouldLogSender(sender))
		{
			Debug.Log("MessageSystem.SendMessage(sender=" + sender?.ToString() + ((target == null) ? string.Empty : (" target=" + target)) + ": " + message + "," + parameter + ")");
		}
		MessageArgs messageArgs = new MessageArgs(sender, target, message, parameter, values);
		try
		{
			sendMessageDepth++;
			for (int i = 0; i < MessageSystem.listenerInfo.Count; i++)
			{
				ListenerInfo listenerInfo = MessageSystem.listenerInfo[i];
				if (listenerInfo == null || listenerInfo.removed)
				{
					continue;
				}
				if (listenerInfo.listener == null)
				{
					listenerInfo.removed = true;
				}
				else
				{
					if ((!allowReceiveSameFrameAdded && listenerInfo.frameAdded == Time.frameCount) || !string.Equals(listenerInfo.message, message) || (!string.Equals(listenerInfo.parameter, parameter) && !string.IsNullOrEmpty(listenerInfo.parameter)))
					{
						continue;
					}
					try
					{
						if (ShouldLogReceiver(listenerInfo.listener))
						{
							Debug.Log("MessageSystem.SendMessage(sender=" + sender?.ToString() + ((target == null) ? string.Empty : (" target=" + target)) + ": " + message + "," + parameter + ")");
						}
						listenerInfo.listener.OnMessage(messageArgs);
					}
					catch (Exception ex)
					{
						Debug.LogError("Message System exception sending '" + message + "'/'" + parameter + "' to " + listenerInfo.listener?.ToString() + ": " + ex.Message);
					}
				}
			}
		}
		finally
		{
			sendMessageDepth--;
			if (sendMessageDepth == 0)
			{
				RemoveMarkedListenerInfo();
			}
		}
	}

	public static void SendMessageWithTarget(object sender, object target, StringField message, string parameter, params object[] values)
	{
		SendMessageWithTarget(sender, target, StringField.GetStringValue(message), parameter, values);
	}

	public static void SendMessageWithTarget(object sender, object target, StringField message, StringField parameter, params object[] values)
	{
		SendMessageWithTarget(sender, target, StringField.GetStringValue(message), StringField.GetStringValue(parameter), values);
	}

	public static void SendMessageWithTarget(object sender, object target, string message, StringField parameter, params object[] values)
	{
		SendMessageWithTarget(sender, target, message, StringField.GetStringValue(parameter), values);
	}

	public static void SendMessage(object sender, string message, string parameter, params object[] values)
	{
		SendMessageWithTarget(sender, null, message, parameter, values);
	}

	public static void SendMessage(object sender, StringField message, StringField parameter, params object[] values)
	{
		SendMessageWithTarget(sender, null, StringField.GetStringValue(message), StringField.GetStringValue(parameter), values);
	}

	public static void SendMessage(object sender, StringField message, string parameter, params object[] values)
	{
		SendMessageWithTarget(sender, null, StringField.GetStringValue(message), parameter, values);
	}

	public static void SendMessage(object sender, string message, StringField parameter, params object[] values)
	{
		SendMessageWithTarget(sender, null, message, StringField.GetStringValue(parameter), values);
	}

	public static void SendCompositeMessage(object sender, string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		string text = string.Empty;
		object obj = null;
		if (message.Contains(":"))
		{
			int num = message.IndexOf(':');
			text = message.Substring(num + 1);
			message = message.Substring(0, num);
			if (text.Contains(":"))
			{
				num = text.IndexOf(':');
				string text2 = text.Substring(num + 1);
				text = text.Substring(0, num);
				obj = ((!int.TryParse(text2, out var result)) ? text2 : ((object)result));
			}
		}
		if (obj == null)
		{
			SendMessage(sender, message, text);
			return;
		}
		SendMessage(sender, message, text, obj);
	}
}
