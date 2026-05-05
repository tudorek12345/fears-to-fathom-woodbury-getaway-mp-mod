using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

public abstract class SequencerCommand : MonoBehaviour
{
	[HideInInspector]
	public bool isPlaying = true;

	private Sequencer m_sequencer;

	private string[] m_parameters;

	private string m_endMessage;

	private Transform m_speaker;

	private Transform m_listener;

	protected Sequencer sequencer
	{
		get
		{
			if (m_sequencer == null)
			{
				m_sequencer = Sequencer.s_awakeSequencer;
			}
			return m_sequencer;
		}
		private set
		{
			m_sequencer = value;
		}
	}

	protected string[] parameters
	{
		get
		{
			if (m_parameters == null)
			{
				m_parameters = Sequencer.s_awakeArgs;
			}
			return m_parameters;
		}
		private set
		{
			m_parameters = value;
		}
	}

	public string endMessage
	{
		get
		{
			if (m_endMessage == null)
			{
				m_endMessage = Sequencer.s_awakeEndMessage;
			}
			return m_endMessage;
		}
		private set
		{
			m_endMessage = value;
		}
	}

	protected Transform speaker
	{
		get
		{
			if (!(m_speaker != null))
			{
				if (!(Sequencer != null))
				{
					return null;
				}
				return Sequencer.Speaker;
			}
			return m_speaker;
		}
	}

	protected Transform listener
	{
		get
		{
			if (!(m_listener != null))
			{
				if (!(Sequencer != null))
				{
					return null;
				}
				return Sequencer.Listener;
			}
			return m_listener;
		}
	}

	public bool IsPlaying
	{
		get
		{
			return isPlaying;
		}
		protected set
		{
			isPlaying = value;
		}
	}

	protected Sequencer Sequencer
	{
		get
		{
			return sequencer;
		}
		private set
		{
			sequencer = value;
		}
	}

	protected string[] Parameters
	{
		get
		{
			return parameters;
		}
		private set
		{
			parameters = value;
		}
	}

	public void Initialize(Sequencer sequencer, string endMessage, Transform speaker, Transform listener, params string[] parameters)
	{
		this.sequencer = sequencer;
		this.endMessage = endMessage;
		this.parameters = parameters;
		m_speaker = speaker;
		m_listener = listener;
	}

	public void Initialize(Sequencer sequencer, Transform speaker, Transform listener, params string[] parameters)
	{
		Initialize(sequencer, null, speaker, listener, parameters);
	}

	protected void Stop()
	{
		isPlaying = false;
	}

	protected Transform GetSubject(string specifier, Transform defaultSubject = null)
	{
		return SequencerTools.GetSubject(specifier, speaker, listener, defaultSubject);
	}

	protected Transform GetSubject(int i, Transform defaultSubject = null)
	{
		return GetSubject(GetParameter(i), defaultSubject);
	}

	protected string GetParameter(int i, string defaultValue = null)
	{
		return SequencerTools.GetParameter(parameters, i, defaultValue);
	}

	protected T GetParameterAs<T>(int i, T defaultValue)
	{
		return SequencerTools.GetParameterAs(parameters, i, defaultValue);
	}

	protected float GetParameterAsFloat(int i, float defaultValue = 0f)
	{
		return GetParameterAs(i, defaultValue);
	}

	protected int GetParameterAsInt(int i, int defaultValue = 0)
	{
		return GetParameterAs(i, defaultValue);
	}

	protected bool GetParameterAsBool(int i, bool defaultValue = false)
	{
		return GetParameterAs(i, defaultValue);
	}

	protected string GetParameters()
	{
		if (parameters == null)
		{
			return string.Empty;
		}
		return string.Join(",", parameters);
	}

	protected bool IsAudioMuted()
	{
		return SequencerTools.IsAudioMuted();
	}
}
