using System;
using UnityEngine;

namespace PixelCrushers;

[Serializable]
[AddComponentMenu("")]
public class MessageEvents : MonoBehaviour, IMessageHandler
{
	[Serializable]
	public class MessageEvent
	{
		[Tooltip("(Optional) If set, only react to messages sent from this sender.")]
		public GameObject requiredSender;

		[Tooltip("(Optional) If set, only react to messages sent to this target.")]
		public GameObject requiredTarget;

		[Tooltip("Listen for this message.")]
		public StringField message;

		[Tooltip("(Optional) If set, listen for this parameter with the message.")]
		public StringField parameter;

		public MessageArgsEvent onMessage = new MessageArgsEvent();
	}

	[Serializable]
	public class MessageToSend
	{
		[Tooltip("(Optional) If set, specify this GameObject as the message target.")]
		public GameObject target;

		[Tooltip("Send this message.")]
		public StringField message;

		[Tooltip("(Optional) If set, send this parameter with the message.")]
		public StringField parameter;
	}

	[SerializeField]
	private MessageEvent[] m_messagesToListenFor;

	[SerializeField]
	private MessageToSend[] m_messagesToSend;

	public MessageEvent[] messagesToListenFor
	{
		get
		{
			return m_messagesToListenFor;
		}
		set
		{
			m_messagesToListenFor = value;
		}
	}

	public MessageToSend[] messagesToSend
	{
		get
		{
			return m_messagesToSend;
		}
		set
		{
			m_messagesToSend = value;
		}
	}

	protected virtual void OnEnable()
	{
		for (int i = 0; i < messagesToListenFor.Length; i++)
		{
			MessageEvent messageEvent = messagesToListenFor[i];
			MessageSystem.AddListener(this, messageEvent.message, messageEvent.parameter);
		}
	}

	protected virtual void OnDisable()
	{
		for (int i = 0; i < messagesToListenFor.Length; i++)
		{
			MessageEvent messageEvent = messagesToListenFor[i];
			MessageSystem.RemoveListener(this, messageEvent.message, messageEvent.parameter);
		}
	}

	public virtual void OnMessage(MessageArgs messageArgs)
	{
		for (int i = 0; i < messagesToListenFor.Length; i++)
		{
			MessageEvent messageEvent = messagesToListenFor[i];
			if (IsParticipantOk(messageEvent.requiredSender, messageArgs.sender) && IsParticipantOk(messageEvent.requiredTarget, messageArgs.target) && object.Equals(messageEvent.message, messageArgs.message) && (StringField.IsNullOrEmpty(messageEvent.parameter) || object.Equals(messageEvent.parameter, messageArgs.parameter)))
			{
				messageEvent.onMessage.Invoke(messageArgs);
			}
		}
	}

	protected virtual bool IsParticipantOk(GameObject requiredParticipant, object actualParticipant)
	{
		if (requiredParticipant == null)
		{
			return true;
		}
		if (actualParticipant == null)
		{
			return false;
		}
		if (!(actualParticipant as GameObject == requiredParticipant) && !(actualParticipant as Transform == requiredParticipant.transform) && (!(actualParticipant is MonoBehaviour) || !((actualParticipant as MonoBehaviour).gameObject == requiredParticipant)) && (!(actualParticipant.GetType() == typeof(string)) || !((string)actualParticipant == requiredParticipant.name)))
		{
			if (actualParticipant.GetType() == typeof(StringField))
			{
				return StringField.GetStringValue(actualParticipant as StringField) == requiredParticipant.name;
			}
			return false;
		}
		return true;
	}

	public virtual void SendToMessageSystem(int index)
	{
		if (messagesToSend != null && 0 <= index && index < messagesToSend.Length)
		{
			MessageSystem.SendMessageWithTarget(this, messagesToSend[index].target, messagesToSend[index].message, messagesToSend[index].parameter);
		}
	}
}
