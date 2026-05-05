using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using PixelCrushers.DialogueSystem.SequencerCommands;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class Sequencer : MonoBehaviour
{
	public delegate void MessageStringDelegate(string message);

	private const string DefaultCameraAnglesResourceName = "Default Camera Angles";

	public bool disableInternalSequencerCommands;

	private bool m_hasCameraControl;

	private Camera m_originalCamera;

	private Vector3 m_originalCameraPosition = Vector3.zero;

	private Quaternion m_originalCameraRotation = Quaternion.identity;

	private float m_originalOrthographicSize = 16f;

	private bool m_keepCameraPositionOnClose;

	private Transform m_speaker;

	private Transform m_listener;

	private ConversationView m_conversationView;

	private List<QueuedSequencerCommand> m_queuedCommands = new List<QueuedSequencerCommand>();

	private List<SequencerCommand> m_activeCommands = new List<SequencerCommand>();

	private List<SequencerCommand> m_commandsToDelete = new List<SequencerCommand>();

	private float m_delayTimeLeft;

	private bool m_informParticipants;

	private bool m_closeWhenFinished;

	private Camera m_sequencerCameraSource;

	private Camera m_sequencerCamera;

	private GameObject m_alternateSequencerCameraObject;

	private GameObject m_cameraAngles;

	private bool m_isUsingMainCamera;

	private bool m_isPlaying;

	public static bool reportMissingAudioFiles = false;

	private static Dictionary<string, Type> m_cachedComponentTypes = new Dictionary<string, Type>();

	private static Dictionary<string, string> m_shortcuts = new Dictionary<string, string>();

	private static Dictionary<string, Stack<string>> m_shortcutStack = new Dictionary<string, Stack<string>>();

	private Dictionary<string, Coroutine> m_timedMessageCoroutines = new Dictionary<string, Coroutine>();

	private SequenceParser m_parser = new SequenceParser();

	private const float InstantThreshold = 0.001f;

	private static Regex ShortcutRegex = null;

	public static Sequencer s_awakeSequencer;

	public static string s_awakeEndMessage;

	public static Transform s_awakeSpeaker;

	public static Transform s_awakeListener;

	public static string[] s_awakeArgs;

	private List<string> queuedMessages = new List<string>();

	private List<int> m_setDialoguePanelPreviouslyOpenSubtitlePanels;

	private List<int> m_setDialoguePanelPreviouslyFocusedSubtitlePanels;

	private List<int> m_setDialoguePanelPreviouslyOpenMenuPanels;

	private List<bool> m_setDialoguePanelPreviousClearText;

	private List<bool> m_setDialoguePanelPreviousContinueButtonStates;

	private static DisplaySettings.SubtitleSettings.ContinueButtonMode savedContinueButtonMode = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;

	public bool isPlaying => m_isPlaying;

	public GameObject cameraAngles => m_cameraAngles;

	public Camera sequencerCamera => m_sequencerCamera;

	public Transform sequencerCameraTransform
	{
		get
		{
			if (!(m_alternateSequencerCameraObject != null))
			{
				return m_sequencerCamera.transform;
			}
			return m_alternateSequencerCameraObject.transform;
		}
	}

	public Transform speaker => m_speaker;

	public Transform listener => m_listener;

	public ConversationView conversationView
	{
		get
		{
			return m_conversationView;
		}
		set
		{
			m_conversationView = value;
		}
	}

	public Vector3 originalCameraPosition
	{
		get
		{
			return m_originalCameraPosition;
		}
		set
		{
			m_originalCameraPosition = value;
		}
	}

	public Quaternion originalCameraRotation
	{
		get
		{
			return m_originalCameraRotation;
		}
		set
		{
			m_originalCameraRotation = value;
		}
	}

	public float originalOrthographicSize
	{
		get
		{
			return m_originalOrthographicSize;
		}
		set
		{
			m_originalOrthographicSize = value;
		}
	}

	public bool keepCameraPositionOnClose
	{
		get
		{
			return m_keepCameraPositionOnClose;
		}
		set
		{
			m_keepCameraPositionOnClose = value;
		}
	}

	public float subtitleEndTime { get; set; }

	public string entrytag { get; set; }

	public string entrytaglocal
	{
		get
		{
			if (!Localization.isDefaultLanguage)
			{
				return entrytag + "_" + Localization.language;
			}
			return entrytag;
		}
	}

	public ActiveConversationRecord activeConversationRecord { get; set; }

	public bool IsPlaying => isPlaying;

	public GameObject CameraAngles => cameraAngles;

	public Camera SequencerCamera => sequencerCamera;

	public Transform SequencerCameraTransform => sequencerCameraTransform;

	public Transform Speaker => speaker;

	public Transform Listener => listener;

	public Vector3 OriginalCameraPosition => originalCameraPosition;

	public Quaternion OriginalCameraRotation => originalCameraRotation;

	public float OriginalOrthographicSize => originalOrthographicSize;

	public float SubtitleEndTime
	{
		get
		{
			return subtitleEndTime;
		}
		set
		{
			subtitleEndTime = value;
		}
	}

	public int numQueuedCommands => m_queuedCommands.Count;

	public int numActiveCommands => m_activeCommands.Count;

	public static Dictionary<string, string> shortcuts => m_shortcuts;

	public static Dictionary<string, Stack<string>> shortcutStack => m_shortcutStack;

	private DisplaySettings currentDisplaySettings
	{
		get
		{
			if (conversationView != null && conversationView.displaySettings != null)
			{
				return conversationView.displaySettings;
			}
			return DialogueManager.displaySettings;
		}
	}

	public event Action FinishedSequenceHandler;

	public event MessageStringDelegate receivedMessage;

	public static void Message(string message)
	{
		if (!(DialogueManager.instance == null))
		{
			DialogueManager.instance.SendMessage("OnSequencerMessage", message, SendMessageOptions.DontRequireReceiver);
		}
	}

	public static void RegisterShortcut(string shortcut, string value)
	{
		if (!string.IsNullOrEmpty(shortcut) && !shortcut.Equals("end") && !shortcut.Equals("default"))
		{
			string key = "{{" + shortcut + "}}";
			if (m_shortcuts.ContainsKey(key))
			{
				m_shortcuts[key] = value;
			}
			else
			{
				m_shortcuts.Add(key, value);
			}
			if (!m_shortcutStack.ContainsKey(key))
			{
				m_shortcutStack.Add(key, new Stack<string>());
			}
			m_shortcutStack[key].Push(value);
		}
	}

	public static void UnregisterShortcut(string shortcut)
	{
		string key = "{{" + shortcut + "}}";
		if (m_shortcuts.ContainsKey(key))
		{
			m_shortcuts.Remove(key);
		}
		if (!m_shortcutStack.ContainsKey(key))
		{
			return;
		}
		if (m_shortcutStack[key].Count > 0)
		{
			m_shortcutStack[key].Pop();
			if (m_shortcutStack[key].Count > 0)
			{
				string value = m_shortcutStack[key].Pop();
				m_shortcuts.Add(key, value);
			}
		}
		if (m_shortcutStack[key].Count == 0)
		{
			m_shortcutStack.Remove(key);
		}
	}

	public static string ReplaceShortcuts(string sequence)
	{
		if (!sequence.Contains("{{"))
		{
			return sequence;
		}
		foreach (KeyValuePair<string, string> shortcut in m_shortcuts)
		{
			sequence = sequence.Replace(shortcut.Key, shortcut.Value);
		}
		return sequence;
	}

	private static void ReportUnrecognizedShortcuts(string sequence)
	{
		if (ShortcutRegex == null)
		{
			ShortcutRegex = new Regex("{{.+}}");
		}
		foreach (Match item in ShortcutRegex.Matches(sequence))
		{
			if (!string.Equals("{{default}}", item.Value))
			{
				Debug.LogWarning("Dialogue System: Unrecognized shortcut " + item.Value);
			}
		}
	}

	public void UseCamera(Camera sequencerCamera, GameObject cameraAngles)
	{
		UseCamera(sequencerCamera, null, cameraAngles);
	}

	public void UseCamera(Camera sequencerCamera, GameObject alternateSequencerCameraObject, GameObject cameraAngles)
	{
		m_originalCamera = Camera.main;
		m_sequencerCameraSource = sequencerCamera;
		m_alternateSequencerCameraObject = alternateSequencerCameraObject;
		m_cameraAngles = cameraAngles;
		GetCameraAngles();
	}

	private void GetCameraAngles()
	{
		if (m_cameraAngles == null)
		{
			DialogueManager.LoadAsset("Default Camera Angles", typeof(GameObject), delegate(UnityEngine.Object asset)
			{
				m_cameraAngles = asset as GameObject;
			});
		}
	}

	private void GetCamera()
	{
		if (m_sequencerCamera == null)
		{
			if (m_alternateSequencerCameraObject != null)
			{
				m_isUsingMainCamera = true;
				m_sequencerCamera = m_alternateSequencerCameraObject.GetComponent<Camera>();
			}
			else if (m_sequencerCameraSource != null)
			{
				GameObject gameObject = m_sequencerCameraSource.gameObject;
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
				m_sequencerCamera = gameObject2.GetComponent<Camera>();
				if (m_sequencerCamera != null)
				{
					m_sequencerCamera.transform.parent = base.transform;
					m_sequencerCamera.gameObject.SetActive(value: false);
					m_isUsingMainCamera = false;
				}
				else
				{
					UnityEngine.Object.Destroy(gameObject2);
				}
			}
			if (m_sequencerCamera == null)
			{
				m_sequencerCamera = Camera.main;
				m_isUsingMainCamera = true;
			}
			if (m_sequencerCamera == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: No MainCamera found in scene. Creating one for the Sequencer Camera.", this);
				}
				GameObject gameObject3 = new GameObject("Sequencer Camera", typeof(Camera), typeof(AudioListener));
				m_sequencerCamera = gameObject3.GetComponent<Camera>();
				m_isUsingMainCamera = true;
			}
		}
		if (Camera.main == null && m_sequencerCamera != null)
		{
			m_sequencerCamera.tag = "MainCamera";
			m_isUsingMainCamera = true;
		}
	}

	private void DestroyCamera()
	{
		if (m_sequencerCamera != null && !m_isUsingMainCamera)
		{
			m_sequencerCamera.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(m_sequencerCamera.gameObject, 1f);
			m_sequencerCamera = null;
		}
	}

	private IEnumerator RestoreCamera()
	{
		yield return null;
		yield return null;
		ReleaseCameraControl();
	}

	public void SwitchCamera(Camera newCamera)
	{
		if (m_sequencerCamera != null && !m_isUsingMainCamera)
		{
			UnityEngine.Object.Destroy(m_sequencerCamera.gameObject, 1f);
		}
		ReleaseCameraControl();
		m_hasCameraControl = false;
		m_originalCamera = null;
		m_originalCameraPosition = Vector3.zero;
		m_originalCameraRotation = Quaternion.identity;
		m_originalOrthographicSize = 16f;
		m_sequencerCameraSource = null;
		m_sequencerCamera = null;
		m_alternateSequencerCameraObject = null;
		m_isUsingMainCamera = false;
		UseCamera(newCamera, m_cameraAngles);
		TakeCameraControl();
	}

	public void TakeCameraControl()
	{
		GetCamera();
		if (m_hasCameraControl)
		{
			return;
		}
		m_hasCameraControl = true;
		if (m_alternateSequencerCameraObject != null)
		{
			m_originalCamera = m_sequencerCamera;
			m_originalCameraPosition = m_alternateSequencerCameraObject.transform.position;
			m_originalCameraRotation = m_alternateSequencerCameraObject.transform.rotation;
			return;
		}
		m_originalCamera = Camera.main;
		if (Camera.main != null)
		{
			m_originalCameraPosition = Camera.main.transform.position;
			m_originalCameraRotation = Camera.main.transform.rotation;
			m_originalCamera.gameObject.SetActive(value: false);
		}
		m_originalOrthographicSize = m_sequencerCamera.orthographicSize;
		m_sequencerCamera.gameObject.SetActive(value: true);
	}

	private void ReleaseCameraControl()
	{
		if (!m_hasCameraControl)
		{
			return;
		}
		m_hasCameraControl = false;
		if (m_alternateSequencerCameraObject != null && !keepCameraPositionOnClose)
		{
			m_alternateSequencerCameraObject.transform.position = m_originalCameraPosition;
			m_alternateSequencerCameraObject.transform.rotation = m_originalCameraRotation;
			return;
		}
		if (m_sequencerCamera != null)
		{
			if (!keepCameraPositionOnClose)
			{
				m_sequencerCamera.transform.position = m_originalCameraPosition;
				m_sequencerCamera.transform.rotation = m_originalCameraRotation;
				m_sequencerCamera.orthographicSize = m_originalOrthographicSize;
			}
			m_sequencerCamera.gameObject.SetActive(value: false);
		}
		if (m_originalCamera != null)
		{
			m_originalCamera.gameObject.SetActive(value: true);
		}
	}

	public void Open()
	{
		entrytag = string.Empty;
		m_hasCameraControl = false;
		GetCameraAngles();
	}

	public void Close()
	{
		if (this.FinishedSequenceHandler != null)
		{
			this.FinishedSequenceHandler();
		}
		this.FinishedSequenceHandler = null;
		Stop();
		StartCoroutine(RestoreCamera());
		UnityEngine.Object.Destroy(this, 1f);
	}

	public void OnDestroy()
	{
		DestroyCamera();
	}

	public void Update()
	{
		if (m_isPlaying)
		{
			CheckQueuedCommands();
			CheckActiveCommands();
			if (m_delayTimeLeft > 0f)
			{
				switch (DialogueTime.mode)
				{
				case DialogueTime.TimeMode.Realtime:
					m_delayTimeLeft -= Time.unscaledDeltaTime;
					break;
				case DialogueTime.TimeMode.Gameplay:
					m_delayTimeLeft -= Time.deltaTime;
					break;
				}
			}
		}
		if (!m_isPlaying)
		{
			return;
		}
		foreach (string queuedMessage in queuedMessages)
		{
			Message(queuedMessage);
		}
		queuedMessages.Clear();
		if (m_queuedCommands.Count == 0 && m_activeCommands.Count == 0 && m_delayTimeLeft <= 0f)
		{
			FinishSequence();
		}
	}

	private void FinishSequence()
	{
		m_isPlaying = false;
		if (this.FinishedSequenceHandler != null)
		{
			this.FinishedSequenceHandler();
		}
		if (m_informParticipants)
		{
			InformParticipants("OnSequenceEnd");
		}
		if (m_closeWhenFinished)
		{
			this.FinishedSequenceHandler = null;
			Close();
		}
		s_awakeSequencer = null;
	}

	public void SetParticipants(Transform speaker, Transform listener)
	{
		m_speaker = speaker;
		m_listener = listener;
	}

	private void InformParticipants(string message)
	{
		if (m_speaker != null)
		{
			m_speaker.BroadcastMessage(message, m_speaker, SendMessageOptions.DontRequireReceiver);
			if (m_listener != null && m_listener != m_speaker)
			{
				m_listener.BroadcastMessage(message, m_speaker, SendMessageOptions.DontRequireReceiver);
			}
		}
		if (DialogueManager.instance.transform != m_speaker && DialogueManager.instance.transform != m_listener)
		{
			Transform parameter = ((m_speaker != null) ? m_speaker : ((m_listener != null) ? m_listener : DialogueManager.instance.transform));
			DialogueManager.instance.BroadcastMessage(message, parameter, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void PlaySequence(string sequence)
	{
		m_isPlaying = true;
		if (string.IsNullOrEmpty(sequence))
		{
			return;
		}
		sequence = FormattedText.ParseCode(sequence);
		if (sequence.Contains("{{"))
		{
			sequence = ReplaceShortcuts(sequence);
			sequence = FormattedText.ParseCode(sequence);
			if (DialogueDebug.logWarnings && sequence.Contains("{{"))
			{
				ReportUnrecognizedShortcuts(sequence);
			}
		}
		if (!string.IsNullOrEmpty(entrytag) && sequence.Contains("entrytag"))
		{
			sequence = sequence.Replace("entrytaglocal", entrytaglocal).Replace("entrytag", entrytag);
		}
		List<QueuedSequencerCommand> list = m_parser.Parse(sequence);
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				PlayCommand(list[i]);
			}
		}
	}

	public void PlaySequence(string sequence, bool informParticipants, bool destroyWhenDone)
	{
		m_closeWhenFinished = destroyWhenDone;
		m_informParticipants = informParticipants;
		if (informParticipants)
		{
			InformParticipants("OnSequenceStart");
		}
		PlaySequence(sequence);
	}

	public void PlaySequence(string sequence, Transform speaker, Transform listener, bool informParticipants, bool destroyWhenDone)
	{
		SetParticipants(speaker, listener);
		PlaySequence(sequence, informParticipants, destroyWhenDone);
	}

	public void PlaySequence(string sequence, Transform speaker, Transform listener, bool informParticipants, bool destroyWhenDone, bool delayOneFrame)
	{
		if (delayOneFrame)
		{
			StartCoroutine(PlaySequenceAfterOneFrame(sequence, speaker, listener, informParticipants, destroyWhenDone));
		}
		else
		{
			PlaySequence(sequence, speaker, listener, informParticipants, destroyWhenDone);
		}
	}

	public IEnumerator PlaySequenceAfterOneFrame(string sequence, Transform speaker, Transform listener, bool informParticipants, bool destroyWhenDone)
	{
		yield return null;
		PlaySequence(sequence, speaker, listener, informParticipants, destroyWhenDone);
	}

	public void PlayCommand(string commandName, bool required, float time, string message, string endMessage, params string[] args)
	{
		PlayCommand(null, commandName, required, time, message, endMessage, args);
	}

	public void PlayCommand(QueuedSequencerCommand commandRecord)
	{
		if (commandRecord != null)
		{
			PlayCommand(commandRecord, commandRecord.command, commandRecord.required, commandRecord.startTime, commandRecord.messageToWaitFor, commandRecord.endMessage, commandRecord.parameters);
		}
	}

	public void PlayCommand(QueuedSequencerCommand commandRecord, string commandName, bool required, float time, string message, string endMessage, params string[] args)
	{
		if (DialogueDebug.logInfo && args != null)
		{
			if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(endMessage))
			{
				Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer.Play( {1}{2}({3})@{4} )", "Dialogue System", required ? "required " : string.Empty, commandName, string.Join(", ", args), time));
			}
			else if (string.IsNullOrEmpty(endMessage))
			{
				Debug.Log(string.Format("{0}: Sequencer.Play( {1}{2}({3})@Message({4}) )", "Dialogue System", required ? "required " : string.Empty, commandName, string.Join(", ", args), message));
			}
			else if (string.IsNullOrEmpty(message))
			{
				Debug.Log(string.Format("{0}: Sequencer.Play( {1}{2}({3})->Message({4}) )", "Dialogue System", required ? "required " : string.Empty, commandName, string.Join(", ", args), endMessage));
			}
			else
			{
				Debug.Log(string.Format("{0}: Sequencer.Play( {1}{2}({3})@Message({4})->Message({5}) )", "Dialogue System", required ? "required " : string.Empty, commandName, string.Join(", ", args), message, endMessage));
			}
		}
		m_isPlaying = true;
		if (commandName == "Continue")
		{
			required = false;
			commandRecord.required = false;
		}
		if (time <= 0.001f && !IsTimePaused() && string.IsNullOrEmpty(message))
		{
			ActivateCommand(commandName, endMessage, speaker, listener, args);
		}
		else if (commandRecord != null)
		{
			commandRecord.startTime += DialogueTime.time;
			commandRecord.speaker = speaker;
			commandRecord.listener = listener;
			m_queuedCommands.Add(commandRecord);
		}
		else
		{
			m_queuedCommands.Add(new QueuedSequencerCommand(commandName, args, DialogueTime.time + time, message, endMessage, required, speaker, listener));
		}
	}

	private bool IsTimePaused()
	{
		return DialogueTime.isPaused;
	}

	private void ActivateCommand(string commandName, string endMessage, Transform speaker, Transform listener, string[] args)
	{
		float duration = 0f;
		if (string.IsNullOrEmpty(commandName))
		{
			return;
		}
		if (HandleCommandInternally(commandName, args, out duration))
		{
			if (!string.IsNullOrEmpty(endMessage))
			{
				string text = Guid.NewGuid().ToString();
				Coroutine value = StartCoroutine(SendTimedSequencerMessage(endMessage, duration, text));
				m_timedMessageCoroutines.Add(text, value);
			}
			return;
		}
		Type type = FindSequencerCommandType(commandName);
		s_awakeSequencer = this;
		s_awakeEndMessage = endMessage;
		s_awakeSpeaker = speaker;
		s_awakeListener = listener;
		s_awakeArgs = args;
		SequencerCommand sequencerCommand = ((type == null) ? null : (base.gameObject.AddComponent(type) as SequencerCommand));
		if (sequencerCommand != null)
		{
			sequencerCommand.Initialize(this, endMessage, speaker, listener, args);
			m_activeCommands.Add(sequencerCommand);
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Can't find any built-in sequencer command named {1}() or a sequencer command component named SequencerCommand{1}()", new object[2] { "Dialogue System", commandName }));
		}
	}

	private Type FindSequencerCommandType(string commandName)
	{
		if (m_cachedComponentTypes.ContainsKey(commandName))
		{
			return m_cachedComponentTypes[commandName];
		}
		Type typeFromName = GetTypeFromName("SequencerCommand" + commandName);
		m_cachedComponentTypes[commandName] = typeFromName;
		return typeFromName;
	}

	public static void Preload()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name.StartsWith("SequencerCommand"))
					{
						string key = type.Name.Substring("SequencerCommand".Length);
						m_cachedComponentTypes[key] = type;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		new SequenceParser().Parse("None();");
		Resources.Load<GameObject>("Default Camera Angles");
	}

	public Type GetTypeFromName(string typeName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (string.Equals(type.Name, typeName))
					{
						return type;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	private IEnumerator SendTimedSequencerMessage(string endMessage, float delay, string guid)
	{
		yield return StartCoroutine(DialogueTime.WaitForSeconds(delay));
		if (m_timedMessageCoroutines.ContainsKey(guid))
		{
			m_timedMessageCoroutines.Remove(guid);
		}
		Message(endMessage);
	}

	private void ActivateCommand(QueuedSequencerCommand queuedCommand)
	{
		ActivateCommand(queuedCommand.command, queuedCommand.endMessage, queuedCommand.speaker, queuedCommand.listener, queuedCommand.parameters);
	}

	private void CheckQueuedCommands()
	{
		if (m_queuedCommands.Count <= 0 || IsTimePaused())
		{
			return;
		}
		float now = DialogueTime.time;
		try
		{
			foreach (QueuedSequencerCommand queuedCommand in m_queuedCommands)
			{
				if (now >= queuedCommand.startTime)
				{
					ActivateCommand(queuedCommand.command, queuedCommand.endMessage, queuedCommand.speaker, queuedCommand.listener, queuedCommand.parameters);
				}
			}
		}
		catch (InvalidOperationException)
		{
		}
		m_queuedCommands.RemoveAll((QueuedSequencerCommand queuedCommand) => now >= queuedCommand.startTime);
	}

	public void OnSequencerMessage(string message)
	{
		try
		{
			if (m_queuedCommands.Count > 0 && !string.IsNullOrEmpty(message))
			{
				List<QueuedSequencerCommand> list = m_queuedCommands.FindAll((QueuedSequencerCommand x) => string.Equals(message, x.messageToWaitFor));
				for (int num = 0; num < list.Count; num++)
				{
					QueuedSequencerCommand queuedSequencerCommand = list[num];
					ActivateCommand(queuedSequencerCommand.command, queuedSequencerCommand.endMessage, queuedSequencerCommand.speaker, queuedSequencerCommand.listener, queuedSequencerCommand.parameters);
				}
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					m_queuedCommands.Remove(list[num2]);
				}
			}
		}
		catch (Exception ex)
		{
			if (!(ex is InvalidOperationException) && !(ex is ArgumentOutOfRangeException))
			{
				throw;
			}
		}
		finally
		{
			if (this.receivedMessage != null)
			{
				this.receivedMessage(message);
			}
		}
	}

	private void CheckActiveCommands()
	{
		m_commandsToDelete.Clear();
		if (m_activeCommands.Count > 0)
		{
			for (int num = m_activeCommands.Count - 1; num >= 0; num--)
			{
				SequencerCommand sequencerCommand = m_activeCommands[num];
				if (sequencerCommand != null && !sequencerCommand.isPlaying)
				{
					if (!string.IsNullOrEmpty(sequencerCommand.endMessage))
					{
						queuedMessages.Add(sequencerCommand.endMessage);
					}
					m_commandsToDelete.Add(sequencerCommand);
				}
			}
		}
		for (int i = 0; i < m_commandsToDelete.Count; i++)
		{
			m_activeCommands.Remove(m_commandsToDelete[i]);
			UnityEngine.Object.Destroy(m_commandsToDelete[i]);
		}
		m_commandsToDelete.Clear();
	}

	public void Stop()
	{
		StopTimedSequencerMessageCoroutines();
		StopQueued();
		StopActive();
	}

	private void StopTimedSequencerMessageCoroutines()
	{
		foreach (Coroutine value in m_timedMessageCoroutines.Values)
		{
			StopCoroutine(value);
		}
		m_timedMessageCoroutines.Clear();
	}

	public void StopQueued()
	{
		if (m_queuedCommands.Count == 0)
		{
			return;
		}
		List<QueuedSequencerCommand> list = new List<QueuedSequencerCommand>(m_queuedCommands);
		m_queuedCommands.Clear();
		foreach (QueuedSequencerCommand item in list)
		{
			if (item.required)
			{
				ActivateCommand(item.command, string.Empty, item.speaker, item.listener, item.parameters);
			}
		}
	}

	public void StopActive()
	{
		foreach (SequencerCommand activeCommand in m_activeCommands)
		{
			if (activeCommand != null)
			{
				if (!string.IsNullOrEmpty(activeCommand.endMessage))
				{
					OnSequencerMessage(activeCommand.endMessage);
				}
				StartCoroutine(DestroyAfterOneFrame(activeCommand));
			}
		}
		m_activeCommands.Clear();
		m_delayTimeLeft = 0f;
	}

	private IEnumerator DestroyAfterOneFrame(SequencerCommand command)
	{
		yield return null;
		UnityEngine.Object.Destroy(command);
	}

	private bool HandleCommandInternally(string commandName, string[] args, out float duration)
	{
		duration = 0f;
		if (disableInternalSequencerCommands)
		{
			return false;
		}
		if (string.Equals(commandName, "None") || string.IsNullOrEmpty(commandName))
		{
			return true;
		}
		if (string.Equals(commandName, "Delay"))
		{
			return HandleDelayInternally(commandName, args, out duration);
		}
		if (string.Equals(commandName, "Camera"))
		{
			return TryHandleCameraInternally(commandName, args);
		}
		if (string.Equals(commandName, "Animation"))
		{
			return HandleAnimationInternally(commandName, args, out duration);
		}
		if (string.Equals(commandName, "AnimatorController"))
		{
			return HandleAnimatorControllerInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorLayer"))
		{
			return TryHandleAnimatorLayerInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorBool"))
		{
			return HandleAnimatorBoolInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorInt"))
		{
			return HandleAnimatorIntInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorFloat"))
		{
			return TryHandleAnimatorFloatInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorTrigger"))
		{
			return HandleAnimatorTriggerInternally(commandName, args);
		}
		if (string.Equals(commandName, "AnimatorPlay"))
		{
			return HandleAnimatorPlayInternally(commandName, args);
		}
		if (string.Equals(commandName, "Audio"))
		{
			return HandleAudioInternally(commandName, args);
		}
		if (string.Equals(commandName, "AudioStop"))
		{
			return HandleAudioStopInternally(commandName, args);
		}
		if (string.Equals(commandName, "ClearSubtitleText"))
		{
			return HandleClearSubtitleText(commandName, args);
		}
		if (string.Equals(commandName, "MoveTo"))
		{
			return TryHandleMoveToInternally(commandName, args);
		}
		if (string.Equals(commandName, "LookAt"))
		{
			return TryHandleLookAtInternally(commandName, args);
		}
		if (string.Equals(commandName, "NavMeshAgent"))
		{
			return HandleNavMeshAgentInternally(commandName, args);
		}
		if (string.Equals(commandName, "OpenPanel"))
		{
			return HandleOpenPanelInternally(commandName, args);
		}
		if (string.Equals(commandName, "SendMessage"))
		{
			return HandleSendMessageInternally(commandName, upwards: false, args);
		}
		if (string.Equals(commandName, "SendMessageUpwards"))
		{
			return HandleSendMessageInternally(commandName, upwards: true, args);
		}
		if (string.Equals(commandName, "SetActive"))
		{
			return HandleSetActiveInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetEnabled"))
		{
			return HandleSetEnabledInternally(commandName, args);
		}
		if (string.Equals(commandName, "HidePanel"))
		{
			return HandleHidePanelInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetPanel"))
		{
			return HandleSetPanelInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetMenuPanel"))
		{
			return HandleSetMenuPanelInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetDialoguePanel"))
		{
			return HandleSetDialoguePanelInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetPortrait"))
		{
			return HandleSetPortraitInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetTimeout"))
		{
			return HandleSetTimeoutInternally(commandName, args);
		}
		if (string.Equals(commandName, "SetContinueMode"))
		{
			return HandleSetContinueModeInternally(commandName, args);
		}
		if (string.Equals(commandName, "Continue"))
		{
			return HandleContinueInternally(args);
		}
		if (string.Equals(commandName, "SetVariable"))
		{
			return HandleSetVariableInternally(commandName, args);
		}
		if (string.Equals(commandName, "ShowAlert"))
		{
			return HandleShowAlertInternally(commandName, args);
		}
		if (string.Equals(commandName, "UpdateTracker"))
		{
			return HandleUpdateTrackerInternally();
		}
		if (string.Equals(commandName, "RandomizeNextEntry"))
		{
			return HandleRandomizeNextEntryInternally();
		}
		if (string.Equals(commandName, "StopConversation"))
		{
			return HandleStopConversationInternally();
		}
		if (string.Equals(commandName, "SequencerMessage"))
		{
			return HandleSequencerMessageInternally(commandName, args);
		}
		if (string.Equals(commandName, "GotoEntry"))
		{
			return HandleGotoEntryInternally(commandName, args);
		}
		return false;
	}

	private string GetParameters(string[] args)
	{
		if (args == null)
		{
			return string.Empty;
		}
		return string.Join(",", args);
	}

	private bool HandleDelayInternally(string commandName, string[] args, out float duration)
	{
		duration = SequencerTools.GetParameterAsFloat(args, 0);
		m_delayTimeLeft = Mathf.Max(m_delayTimeLeft, duration);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: Delay({1})", new object[2] { "Dialogue System", duration }));
		}
		return true;
	}

	private bool TryHandleCameraInternally(string commandName, string[] args)
	{
		float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 2);
		if (parameterAsFloat < 0.001f)
		{
			string text = SequencerTools.GetParameter(args, 0, "default");
			Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
			if (string.Equals(text, "default"))
			{
				text = SequencerTools.GetDefaultCameraAngle(subject);
			}
			bool flag = string.Equals(text, "original");
			Transform transform = (flag ? m_originalCamera.transform : ((m_cameraAngles != null) ? m_cameraAngles.transform.Find(text) : null));
			bool flag2 = true;
			if (transform == null)
			{
				flag2 = false;
				GameObject gameObject = GameObject.Find(text);
				if (gameObject != null)
				{
					transform = gameObject.transform;
				}
			}
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: Camera({1}, {2}, {3}s)", "Dialogue System", text, Tools.GetObjectName(subject), parameterAsFloat));
			}
			if (transform == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Camera angle '{1}' wasn't found.", new object[2] { "Dialogue System", text }));
			}
			if (subject == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Camera({1}): Camera subject '{2}' wasn't found.", new object[3]
				{
					"Dialogue System",
					GetParameters(args),
					SequencerTools.GetParameter(args, 1)
				}));
			}
			TakeCameraControl();
			if (flag)
			{
				sequencerCameraTransform.rotation = originalCameraRotation;
				sequencerCameraTransform.position = originalCameraPosition;
			}
			else if (transform != null && subject != null)
			{
				Transform transform2 = sequencerCameraTransform;
				if (flag2)
				{
					transform2.rotation = subject.rotation * transform.localRotation;
					transform2.position = subject.position + subject.rotation * transform.localPosition;
				}
				else
				{
					transform2.rotation = transform.rotation;
					transform2.position = transform.position;
				}
			}
			return true;
		}
		return false;
	}

	private bool HandleAnimationInternally(string commandName, string[] args, out float duration)
	{
		duration = 0f;
		if (args != null && args.Length > 2)
		{
			return false;
		}
		string parameter = SequencerTools.GetParameter(args, 0);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
		Animation animation = ((subject == null) ? null : subject.GetComponent<Animation>());
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: Animation({1}, {2})", new object[3]
			{
				"Dialogue System",
				parameter,
				Tools.GetObjectName(subject)
			}));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Animation() command: subject is null.", new object[1] { "Dialogue System" }));
			}
		}
		else if (animation == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Animation() command: no Animation component found on {1}.", new object[2] { "Dialogue System", subject.name }));
			}
		}
		else if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Animation() command: Animation name is blank.", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			animation.CrossFade(parameter);
			duration = ((animation[parameter] != null) ? animation[parameter].length : 0f);
		}
		return true;
	}

	private bool HandleAnimatorControllerInternally(string commandName, string[] args)
	{
		string controllerName = SequencerTools.GetParameter(args, 0);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorController({1}, {2})", new object[3]
			{
				"Dialogue System",
				controllerName,
				Tools.GetObjectName(subject)
			}));
		}
		try
		{
			DialogueManager.LoadAsset(controllerName, typeof(RuntimeAnimatorController), delegate(UnityEngine.Object asset)
			{
				RuntimeAnimatorController runtimeAnimatorController = asset as RuntimeAnimatorController;
				RuntimeAnimatorController runtimeAnimatorController2 = null;
				if (runtimeAnimatorController != null)
				{
					runtimeAnimatorController2 = UnityEngine.Object.Instantiate(runtimeAnimatorController);
				}
				if (subject == null)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorController() command: subject is null.", new object[1] { "Dialogue System" }));
					}
				}
				else if (runtimeAnimatorController2 == null)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorController() command: failed to load animator controller '{1}'.", new object[2] { "Dialogue System", controllerName }));
					}
				}
				else
				{
					Animator componentInChildren = subject.GetComponentInChildren<Animator>();
					if (componentInChildren == null)
					{
						if (DialogueDebug.logWarnings)
						{
							Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorController() command: No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
						}
					}
					else
					{
						componentInChildren.runtimeAnimatorController = runtimeAnimatorController2;
					}
				}
			});
		}
		catch (Exception)
		{
		}
		return true;
	}

	private bool TryHandleAnimatorLayerInternally(string commandName, string[] args)
	{
		if (SequencerTools.GetParameterAsFloat(args, 3) < 0.001f)
		{
			int parameterAsInt = SequencerTools.GetParameterAsInt(args, 0, 1);
			float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 1, 1f);
			Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 2), m_speaker, m_listener);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: AnimatorLayer({1}, {2}, {3})", "Dialogue System", parameterAsInt, parameterAsFloat, Tools.GetObjectName(subject)));
			}
			if (subject == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorLayer() command: subject is null.", new object[1] { "Dialogue System" }));
				}
			}
			else
			{
				Animator componentInChildren = subject.GetComponentInChildren<Animator>();
				if (componentInChildren == null)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorLayer(): No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
					}
				}
				else
				{
					componentInChildren.SetLayerWeight(parameterAsInt, parameterAsFloat);
				}
			}
			return true;
		}
		return false;
	}

	private bool HandleAnimatorBoolInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		bool parameterAsBool = SequencerTools.GetParameterAsBool(args, 1, defaultValue: true);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 2), m_speaker, m_listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorBool({1}, {2}, {3})", "Dialogue System", parameter, parameterAsBool, Tools.GetObjectName(subject)));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorBool() command: subject is null.", new object[1] { "Dialogue System" }));
			}
		}
		else if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorBool() command: animator parameter name is blank.", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			Animator componentInChildren = subject.GetComponentInChildren<Animator>();
			if (componentInChildren == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
				}
			}
			else
			{
				componentInChildren.SetBool(parameter, parameterAsBool);
			}
		}
		return true;
	}

	private bool HandleAnimatorIntInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		int parameterAsInt = SequencerTools.GetParameterAsInt(args, 1, 1);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 2), m_speaker, m_listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorInt({1}, {2}, {3})", "Dialogue System", parameter, parameterAsInt, Tools.GetObjectName(subject)));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorInt() command: subject is null.", new object[1] { "Dialogue System" }));
			}
		}
		else if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorInt() command: animator parameter name is blank.", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			Animator componentInChildren = subject.GetComponentInChildren<Animator>();
			if (componentInChildren == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
				}
			}
			else
			{
				componentInChildren.SetInteger(parameter, parameterAsInt);
			}
		}
		return true;
	}

	private bool TryHandleAnimatorFloatInternally(string commandName, string[] args)
	{
		if (SequencerTools.GetParameterAsFloat(args, 3) < 0.001f)
		{
			string parameter = SequencerTools.GetParameter(args, 0);
			float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 1, 1f);
			Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 2), m_speaker, m_listener);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: AnimatorFloat({1}, {2}, {3})", "Dialogue System", parameter, parameterAsFloat, Tools.GetObjectName(subject)));
			}
			if (subject == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorFloat() command: subject is null.", new object[1] { "Dialogue System" }));
				}
			}
			else if (string.IsNullOrEmpty(parameter))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorFloat() command: animator parameter name is blank.", new object[1] { "Dialogue System" }));
				}
			}
			else
			{
				Animator componentInChildren = subject.GetComponentInChildren<Animator>();
				if (componentInChildren == null)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
					}
				}
				else
				{
					componentInChildren.SetFloat(parameter, parameterAsFloat);
				}
			}
			return true;
		}
		return false;
	}

	private bool HandleAnimatorTriggerInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
		Animator animator = ((subject != null) ? subject.GetComponentInChildren<Animator>() : null);
		string parameter2 = SequencerTools.GetParameter(args, 2);
		if (animator == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Sequencer: AnimatorTrigger({1}, {2}): No Animator found on {2}", new object[3]
				{
					"Dialogue System",
					parameter,
					(subject != null) ? subject.name : SequencerTools.GetParameter(args, 1)
				}));
			}
		}
		else if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorTrigger({1}, {2})", new object[3] { "Dialogue System", parameter, subject }));
		}
		if (animator != null)
		{
			animator.SetTrigger(parameter);
			if (!string.IsNullOrEmpty(parameter2))
			{
				animator.ResetTrigger(parameter2);
				StartCoroutine(ResetAnimatorParameterAtEndOfFrame(animator, parameter2));
			}
		}
		return true;
	}

	private IEnumerator ResetAnimatorParameterAtEndOfFrame(Animator animator, string resetParameter)
	{
		if (!(animator == null) && !string.IsNullOrEmpty(resetParameter))
		{
			yield return new WaitForEndOfFrame();
			animator.ResetTrigger(resetParameter);
		}
	}

	private bool HandleAnimatorPlayInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
		float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 2);
		int parameterAsInt = SequencerTools.GetParameterAsInt(args, 3, -1);
		bool flag = false;
		for (int i = 1; i < args.Length; i++)
		{
			if (string.Equals("noactivate", args[i], StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				break;
			}
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AnimatorPlay({1}, {2}, fade={3}, layer={4})", "Dialogue System", parameter, Tools.GetObjectName(subject), parameterAsFloat, parameterAsInt));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorPlay() command: subject is null.", new object[1] { "Dialogue System" }));
			}
		}
		else if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorPlay() command: state name is blank.", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			Animator componentInChildren = subject.GetComponentInChildren<Animator>();
			if (componentInChildren == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: No Animator component found on {1}.", new object[2] { "Dialogue System", subject.name }));
				}
			}
			else
			{
				if (!componentInChildren.gameObject.activeSelf && !flag)
				{
					componentInChildren.gameObject.SetActive(value: true);
				}
				if (componentInChildren.gameObject.activeInHierarchy)
				{
					if (Tools.ApproximatelyZero(parameterAsFloat))
					{
						componentInChildren.Play(parameter, parameterAsInt);
					}
					else
					{
						componentInChildren.CrossFadeInFixedTime(parameter, parameterAsFloat, parameterAsInt);
					}
				}
			}
		}
		return true;
	}

	private bool HandleAudioInternally(string commandName, string[] args)
	{
		string clipName = SequencerTools.GetParameter(args, 0);
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
		bool oneshot = SequencerTools.GetParameterAsBool(args, 2) || string.Equals("oneshot", SequencerTools.GetParameter(args, 2), StringComparison.OrdinalIgnoreCase);
		if (SequencerTools.IsAudioMuted())
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: Audio({1}, {2}): skipping; audio is muted", new object[3] { "Dialogue System", clipName, subject }));
			}
			return true;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: Audio({1}, {2})", new object[3] { "Dialogue System", clipName, subject }));
		}
		DialogueManager.LoadAsset(clipName, typeof(AudioClip), delegate(UnityEngine.Object asset)
		{
			AudioClip audioClip = asset as AudioClip;
			if (audioClip == null && DialogueDebug.logWarnings && reportMissingAudioFiles)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Audio({1}) command: clip '{2}' could not be found or loaded.", new object[3]
				{
					"Dialogue System",
					GetParameters(args),
					clipName
				}));
			}
			if (audioClip != null)
			{
				AudioSource audioSource = SequencerTools.GetAudioSource(subject);
				if (audioSource == null)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: Audio({1}) command: can't find or add AudioSource to {2}.", new object[3]
						{
							"Dialogue System",
							GetParameters(args),
							subject.name
						}));
					}
				}
				else if (oneshot)
				{
					audioSource.PlayOneShot(audioClip);
				}
				else
				{
					audioSource.clip = audioClip;
					audioSource.Play();
				}
			}
		});
		return true;
	}

	private bool HandleAudioStopInternally(string commandName, string[] args)
	{
		Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 0), m_speaker, m_listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: AudioStop({1})", new object[2] { "Dialogue System", subject }));
		}
		AudioSource audioSource = SequencerTools.GetAudioSource(subject);
		if (audioSource != null)
		{
			audioSource.Stop();
		}
		return true;
	}

	private bool TryHandleMoveToInternally(string commandName, string[] args)
	{
		float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 2);
		if (parameterAsFloat < 0.001f)
		{
			Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 0), m_speaker, m_listener);
			Transform subject2 = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: MoveTo({1}, {2}, {3})", "Dialogue System", subject, subject2, parameterAsFloat));
			}
			if (subject2 == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: MoveTo() command: subject is null.", new object[1] { "Dialogue System" }));
			}
			if (subject == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: MoveTo() command: target is null.", new object[1] { "Dialogue System" }));
			}
			if (subject2 != null && subject != null)
			{
				Rigidbody component = subject2.GetComponent<Rigidbody>();
				if (component != null && !component.isKinematic)
				{
					component.MoveRotation(subject.rotation);
					component.MovePosition(subject.position);
				}
				else
				{
					subject2.position = subject.position;
					subject2.rotation = subject.rotation;
				}
			}
			return true;
		}
		return false;
	}

	private bool TryHandleLookAtInternally(string commandName, string[] args)
	{
		float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 2);
		bool yAxisOnly = string.Compare(SequencerTools.GetParameter(args, 3), "allAxes", StringComparison.OrdinalIgnoreCase) != 0;
		if (parameterAsFloat < 0.001f)
		{
			if (args == null || args.Length == 0 || (args.Length == 1 && string.IsNullOrEmpty(args[0])))
			{
				if (m_speaker != null && m_listener != null)
				{
					if (DialogueDebug.logInfo)
					{
						Debug.Log(string.Format("{0}: Sequencer: LookAt() [speaker<->listener]", new object[1] { "Dialogue System" }));
					}
					DoLookAt(m_speaker, m_listener.position, yAxisOnly);
					DoLookAt(m_listener, m_speaker.position, yAxisOnly);
				}
			}
			else
			{
				Transform subject = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 0), m_speaker, m_listener);
				Transform subject2 = SequencerTools.GetSubject(SequencerTools.GetParameter(args, 1), m_speaker, m_listener);
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: LookAt({1}, {2}, {3})", "Dialogue System", subject, subject2, parameterAsFloat));
				}
				if (subject2 == null && DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: LookAt() command: subject is null.", new object[1] { "Dialogue System" }));
				}
				if (subject == null && DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: LookAt() command: target is null.", new object[1] { "Dialogue System" }));
				}
				if (subject2 != subject && subject2 != null && subject != null)
				{
					DoLookAt(subject2, subject.position, yAxisOnly);
				}
			}
			return true;
		}
		return false;
	}

	private void DoLookAt(Transform subject, Vector3 position, bool yAxisOnly)
	{
		if (yAxisOnly)
		{
			subject.LookAt(new Vector3(position.x, subject.position.y, position.z));
		}
		else
		{
			subject.LookAt(position);
		}
	}

	private bool HandleNavMeshAgentInternally(string commandName, string[] args)
	{
		if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Sequencer: NavMeshAgent() support isn't enabled. Select menu item Tools > Pixel Crushers > Common > Misc > Use NavMesh");
		}
		return true;
	}

	private bool HandleSendMessageInternally(string commandName, bool upwards, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		string parameter2 = SequencerTools.GetParameter(args, 1);
		bool flag = string.Equals(SequencerTools.GetParameter(args, 2), "everyone", StringComparison.OrdinalIgnoreCase);
		Transform transform = (flag ? DialogueManager.instance.transform : SequencerTools.GetSubject(SequencerTools.GetParameter(args, 2), m_speaker, m_listener));
		bool flag2 = string.Equals(SequencerTools.GetParameter(args, 3), "broadcast", StringComparison.OrdinalIgnoreCase);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: {1}({2}, {3}, {4}, {5})", "Dialogue System", commandName, parameter, parameter2, transform, SequencerTools.GetParameter(args, 3)));
		}
		if (transform == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: {1}({2}) command: subject is null.", new object[3]
			{
				"Dialogue System",
				commandName,
				GetParameters(args)
			}));
		}
		if (string.IsNullOrEmpty(parameter) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: {1}({2}) command: message is blank.", new object[3]
			{
				"Dialogue System",
				commandName,
				GetParameters(args)
			}));
		}
		if (upwards && flag2 && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: {1}({2}) command: 'broadcast' is ignored by SendCommandUpwards.", new object[3]
			{
				"Dialogue System",
				commandName,
				GetParameters(args)
			}));
		}
		if (upwards && flag && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: {1}({2}) command: 'everyone' is ignored by SendCommandUpwards.", new object[3]
			{
				"Dialogue System",
				commandName,
				GetParameters(args)
			}));
		}
		if (transform != null && !string.IsNullOrEmpty(parameter))
		{
			if (upwards)
			{
				transform.SendMessageUpwards(parameter, parameter2, SendMessageOptions.DontRequireReceiver);
			}
			else if (flag)
			{
				Tools.SendMessageToEveryone(parameter, parameter2);
			}
			else if (flag2)
			{
				transform.BroadcastMessage(parameter, parameter2, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				transform.SendMessage(parameter, parameter2, SendMessageOptions.DontRequireReceiver);
			}
		}
		return true;
	}

	private bool HandleSetActiveInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		string parameter2 = SequencerTools.GetParameter(args, 1);
		if (SequencerTools.SpecifierSpecifiesTag(parameter))
		{
			string specifiedTag = SequencerTools.GetSpecifiedTag(parameter);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetActive({1}, {2}): (all GameObjects matching tag)", new object[3] { "Dialogue System", parameter, parameter2 }));
			}
			if (string.Equals(parameter2, "false", StringComparison.OrdinalIgnoreCase))
			{
				GameObject[] array = GameObject.FindGameObjectsWithTag(specifiedTag);
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: false);
				}
			}
			else
			{
				GameObject[] array2 = Tools.FindGameObjectsWithTagHard(specifiedTag);
				foreach (GameObject gameObject in array2)
				{
					bool active = !string.Equals(parameter2, "flip", StringComparison.OrdinalIgnoreCase) || !gameObject.activeSelf;
					gameObject.SetActive(active);
				}
			}
			return true;
		}
		Transform subject = SequencerTools.GetSubject(parameter, speaker, listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetActive({1}, {2})", new object[3] { "Dialogue System", subject, parameter2 }));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetActive() command: subject '{1}' is null.", new object[2]
				{
					"Dialogue System",
					(args.Length != 0) ? args[0] : string.Empty
				}));
			}
		}
		else if (subject == m_speaker || subject == m_listener)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetActive() command: subject '{1}' cannot be speaker or listener.", new object[2]
				{
					"Dialogue System",
					(args.Length != 0) ? args[0] : string.Empty
				}));
			}
		}
		else
		{
			bool active2 = true;
			if (!string.IsNullOrEmpty(parameter2))
			{
				if (string.Equals(parameter2.ToLower(), "false"))
				{
					active2 = false;
				}
				else if (string.Equals(parameter2.ToLower(), "flip"))
				{
					active2 = !subject.gameObject.activeSelf;
				}
			}
			subject.gameObject.SetActive(active2);
		}
		return true;
	}

	private bool HandleSetEnabledInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		string parameter2 = SequencerTools.GetParameter(args, 1);
		string parameter3 = SequencerTools.GetParameter(args, 2);
		if (SequencerTools.SpecifierSpecifiesTag(parameter3))
		{
			string specifiedTag = SequencerTools.GetSpecifiedTag(parameter3);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetEnabled({1}, {2}, {3})", "Dialogue System", parameter, parameter2, parameter3));
			}
			GameObject[] array = GameObject.FindGameObjectsWithTag(specifiedTag);
			foreach (GameObject gameObject in array)
			{
				Component component = ((gameObject != null) ? gameObject.GetComponent(parameter) : null);
				if (!(component != null))
				{
					continue;
				}
				Toggle state = Toggle.True;
				if (!string.IsNullOrEmpty(parameter2))
				{
					if (string.Equals(parameter2.ToLower(), "false"))
					{
						state = Toggle.False;
					}
					else if (string.Equals(parameter2.ToLower(), "flip"))
					{
						state = Toggle.Flip;
					}
				}
				Tools.SetComponentEnabled(component, state);
			}
			return true;
		}
		Transform subject = SequencerTools.GetSubject(parameter3, m_speaker, m_listener);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetEnabled({1}, {2}, {3})", "Dialogue System", parameter, parameter2, subject));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetEnabled() command: subject is null.", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			Component component2 = subject.GetComponent(parameter);
			if (component2 == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: SetEnabled() command: component '{1}' not found on {2}.", new object[3] { "Dialogue System", parameter, subject.name }));
				}
			}
			else
			{
				Toggle state2 = Toggle.True;
				if (!string.IsNullOrEmpty(parameter2))
				{
					if (string.Equals(parameter2.ToLower(), "false"))
					{
						state2 = Toggle.False;
					}
					else if (string.Equals(parameter2.ToLower(), "flip"))
					{
						state2 = Toggle.Flip;
					}
				}
				Tools.SetComponentEnabled(component2, state2);
			}
		}
		return true;
	}

	private bool HandleClearSubtitleText(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		bool flag = string.Equals(parameter, "all", StringComparison.OrdinalIgnoreCase);
		int num = ((!flag) ? Tools.StringToInt(parameter) : 0);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: ClearSubtitleText({1})", new object[2] { "Dialogue System", parameter }));
		}
		StandardDialogueUI standardDialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		if (standardDialogueUI != null)
		{
			if (flag)
			{
				standardDialogueUI.conversationUIElements.ClearAllSubtitleText();
			}
			else if (0 <= num && num < standardDialogueUI.conversationUIElements.subtitlePanels.Length && standardDialogueUI.conversationUIElements.subtitlePanels[num] != null)
			{
				standardDialogueUI.conversationUIElements.subtitlePanels[num].ClearText();
			}
		}
		return true;
	}

	public void SetDialoguePanel(bool show, bool immediate)
	{
		AbstractDialogueUI abstractDialogueUI = DialogueManager.dialogueUI as AbstractDialogueUI;
		if (!(abstractDialogueUI != null))
		{
			return;
		}
		StandardDialogueUI standardDialogueUI = abstractDialogueUI as StandardDialogueUI;
		if (show)
		{
			abstractDialogueUI.dialogueControls.Show();
			if (!(standardDialogueUI != null))
			{
				return;
			}
			if (m_setDialoguePanelPreviouslyOpenSubtitlePanels != null)
			{
				for (int i = 0; i < m_setDialoguePanelPreviouslyOpenSubtitlePanels.Count; i++)
				{
					int num = m_setDialoguePanelPreviouslyOpenSubtitlePanels[i];
					standardDialogueUI.conversationUIElements.subtitlePanels[num].panelState = UIPanel.PanelState.Closed;
					standardDialogueUI.conversationUIElements.subtitlePanels[num].Open();
					standardDialogueUI.conversationUIElements.subtitlePanels[num].ActivateUIElements();
					if (m_setDialoguePanelPreviouslyFocusedSubtitlePanels != null && m_setDialoguePanelPreviouslyFocusedSubtitlePanels.Contains(num))
					{
						standardDialogueUI.conversationUIElements.subtitlePanels[num].Focus();
					}
					standardDialogueUI.conversationUIElements.subtitlePanels[num].clearTextOnClose = m_setDialoguePanelPreviousClearText[num];
					if (m_setDialoguePanelPreviousContinueButtonStates[num])
					{
						standardDialogueUI.conversationUIElements.subtitlePanels[num].ShowContinueButton();
					}
				}
			}
			if (m_setDialoguePanelPreviouslyOpenMenuPanels != null)
			{
				for (int j = 0; j < m_setDialoguePanelPreviouslyOpenMenuPanels.Count; j++)
				{
					int num2 = m_setDialoguePanelPreviouslyOpenMenuPanels[j];
					standardDialogueUI.conversationUIElements.menuPanels[num2].panelState = UIPanel.PanelState.Closed;
					standardDialogueUI.conversationUIElements.menuPanels[num2].Open();
				}
			}
			return;
		}
		if (standardDialogueUI != null)
		{
			if (m_setDialoguePanelPreviouslyOpenMenuPanels == null)
			{
				m_setDialoguePanelPreviouslyOpenMenuPanels = new List<int>();
			}
			if (m_setDialoguePanelPreviouslyOpenSubtitlePanels == null)
			{
				m_setDialoguePanelPreviouslyOpenSubtitlePanels = new List<int>();
			}
			if (m_setDialoguePanelPreviouslyFocusedSubtitlePanels == null)
			{
				m_setDialoguePanelPreviouslyFocusedSubtitlePanels = new List<int>();
			}
			if (m_setDialoguePanelPreviousClearText == null)
			{
				m_setDialoguePanelPreviousClearText = new List<bool>();
			}
			if (m_setDialoguePanelPreviousContinueButtonStates == null)
			{
				m_setDialoguePanelPreviousContinueButtonStates = new List<bool>();
			}
			m_setDialoguePanelPreviouslyOpenMenuPanels.Clear();
			m_setDialoguePanelPreviouslyOpenSubtitlePanels.Clear();
			m_setDialoguePanelPreviouslyFocusedSubtitlePanels.Clear();
			m_setDialoguePanelPreviousClearText.Clear();
			m_setDialoguePanelPreviousContinueButtonStates.Clear();
			for (int k = 0; k < standardDialogueUI.conversationUIElements.subtitlePanels.Length; k++)
			{
				if (standardDialogueUI.conversationUIElements.subtitlePanels[k] == null)
				{
					continue;
				}
				m_setDialoguePanelPreviousClearText.Add(standardDialogueUI.conversationUIElements.subtitlePanels[k].clearTextOnClose);
				m_setDialoguePanelPreviousContinueButtonStates.Add((UnityEngine.Object)(object)standardDialogueUI.conversationUIElements.subtitlePanels[k].continueButton != null && ((Component)(object)standardDialogueUI.conversationUIElements.subtitlePanels[k].continueButton).gameObject.activeInHierarchy);
				standardDialogueUI.conversationUIElements.subtitlePanels[k].clearTextOnClose = false;
				if (standardDialogueUI.conversationUIElements.subtitlePanels[k].isOpen && standardDialogueUI.conversationUIElements.subtitlePanels[k].panelState != UIPanel.PanelState.Closing)
				{
					if (standardDialogueUI.conversationUIElements.subtitlePanels[k].hasFocus)
					{
						m_setDialoguePanelPreviouslyFocusedSubtitlePanels.Add(k);
					}
					if (immediate)
					{
						standardDialogueUI.conversationUIElements.subtitlePanels[k].HideImmediate();
					}
					else
					{
						standardDialogueUI.conversationUIElements.subtitlePanels[k].Close();
					}
					m_setDialoguePanelPreviouslyOpenSubtitlePanels.Add(k);
				}
			}
			for (int l = 0; l < standardDialogueUI.conversationUIElements.menuPanels.Length; l++)
			{
				if (!(standardDialogueUI.conversationUIElements.menuPanels[l] == null) && standardDialogueUI.conversationUIElements.menuPanels[l].isOpen)
				{
					m_setDialoguePanelPreviouslyOpenMenuPanels.Add(l);
					if (immediate)
					{
						standardDialogueUI.conversationUIElements.menuPanels[l].HideImmediate();
					}
					else
					{
						standardDialogueUI.conversationUIElements.menuPanels[l].Close();
					}
				}
			}
			if (immediate)
			{
				standardDialogueUI.conversationUIElements.HideImmediate();
			}
		}
		abstractDialogueUI.dialogueControls.Hide();
	}

	private bool HandleSetDialoguePanelInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		if (!string.Equals(parameter, "true", StringComparison.OrdinalIgnoreCase) && !string.Equals(parameter, "false", StringComparison.OrdinalIgnoreCase))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetDialoguePanel({1}): Parameter must be true or false", new object[2] { "Dialogue System", parameter }));
			}
			return true;
		}
		bool flag = string.Equals(parameter, "true", StringComparison.OrdinalIgnoreCase);
		bool immediate = string.Equals(SequencerTools.GetParameter(args, 1), "immediate", StringComparison.OrdinalIgnoreCase);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetDialoguePanel({1})", new object[2] { "Dialogue System", flag }));
		}
		SetDialoguePanel(flag, immediate);
		return true;
	}

	private bool HandleOpenPanelInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		SubtitlePanelNumber subtitlePanelNumber = ((!string.Equals(parameter, "default", StringComparison.OrdinalIgnoreCase)) ? (string.Equals(parameter, "bark", StringComparison.OrdinalIgnoreCase) ? SubtitlePanelNumber.UseBarkUI : PanelNumberUtility.IntToSubtitlePanelNumber(Tools.StringToInt(parameter))) : SubtitlePanelNumber.Default);
		string text = SequencerTools.GetParameter(args, 1);
		if (string.IsNullOrEmpty(text))
		{
			text = "open";
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: OpenPanel({1}, {2})", new object[3] { "Dialogue System", subtitlePanelNumber, text }));
		}
		StandardDialogueUI standardDialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		if (standardDialogueUI != null)
		{
			StandardUISubtitlePanel[] subtitlePanels = standardDialogueUI.conversationUIElements.subtitlePanels;
			int subtitlePanelIndex = PanelNumberUtility.GetSubtitlePanelIndex(subtitlePanelNumber);
			if (0 <= subtitlePanelIndex && subtitlePanelIndex < subtitlePanels.Length)
			{
				StandardUISubtitlePanel standardUISubtitlePanel = subtitlePanels[subtitlePanelIndex];
				if (string.Equals("open", text, StringComparison.OrdinalIgnoreCase))
				{
					standardDialogueUI.conversationUIElements.standardSubtitleControls.OpenSubtitlePanelLikeStart(subtitlePanelNumber);
				}
				else if (string.Equals("close", text, StringComparison.OrdinalIgnoreCase))
				{
					standardUISubtitlePanel.Close();
				}
				else if (string.Equals("focus", text, StringComparison.OrdinalIgnoreCase))
				{
					if (!standardUISubtitlePanel.isOpen)
					{
						standardUISubtitlePanel.Open();
					}
					standardUISubtitlePanel.Focus();
				}
				else if (string.Equals("unfocus", text, StringComparison.OrdinalIgnoreCase))
				{
					standardUISubtitlePanel.Unfocus();
				}
				else if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: OpenPanel({1}, {2}): Unrecognized mode.", new object[3] { "Dialogue System", subtitlePanelNumber, text }));
				}
			}
			else if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: OpenPanel({1}, {2}): Invalid panel number.", new object[3] { "Dialogue System", subtitlePanelNumber, text }));
			}
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: OpenPanel({1}, {2}): Current dialogue UI is not a Standard Dialogue UI.", new object[3] { "Dialogue System", subtitlePanelNumber, text }));
		}
		return true;
	}

	private bool HandleHidePanelInternally(string commandName, string[] args)
	{
		int parameterAsInt = SequencerTools.GetParameterAsInt(args, 0);
		bool flag = string.Equals("portrait", SequencerTools.GetParameter(args, 1), StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals("portraitimage", SequencerTools.GetParameter(args, 1), StringComparison.OrdinalIgnoreCase);
		StandardDialogueUI standardDialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		string text = "HidePanel(" + parameterAsInt + (flag ? ", portrait" : string.Empty) + ")";
		if (standardDialogueUI == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Sequencer: " + text + " can't run. Not using a Standard Dialogue UI.");
			}
		}
		else if (0 > parameterAsInt || parameterAsInt >= standardDialogueUI.conversationUIElements.subtitlePanels.Length)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Sequencer: " + text + "dialogue UI doesn't have panel #" + parameterAsInt + ".");
			}
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Sequencer: " + text);
			}
			StandardUISubtitlePanel standardUISubtitlePanel = standardDialogueUI.conversationUIElements.subtitlePanels[parameterAsInt];
			if (standardUISubtitlePanel == null)
			{
				return true;
			}
			if (flag)
			{
				Tools.SetGameObjectActive((Component)(object)standardUISubtitlePanel.portraitImage, value: false);
				Tools.SetGameObjectActive(standardUISubtitlePanel.portraitName.gameObject, value: false);
			}
			else if (flag2)
			{
				Tools.SetGameObjectActive((Component)(object)standardUISubtitlePanel.portraitImage, value: false);
			}
			else
			{
				standardUISubtitlePanel.Close();
			}
		}
		return true;
	}

	private bool HandleSetPanelInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		Transform transform = CharacterInfo.GetRegisteredActorTransform(parameter) ?? SequencerTools.GetSubject(parameter, speaker, listener, speaker);
		string parameter2 = SequencerTools.GetParameter(args, 1);
		bool immediate = string.Equals("immediate", SequencerTools.GetParameter(args, 2), StringComparison.OrdinalIgnoreCase);
		SubtitlePanelNumber subtitlePanelNumber = ((!string.Equals(parameter2, "default", StringComparison.OrdinalIgnoreCase)) ? (string.Equals(parameter2, "bark", StringComparison.OrdinalIgnoreCase) ? SubtitlePanelNumber.UseBarkUI : PanelNumberUtility.IntToSubtitlePanelNumber(Tools.StringToInt(parameter2))) : SubtitlePanelNumber.Default);
		DialogueActor dialogueActor = ((transform != null) ? transform.GetComponent<DialogueActor>() : null);
		if (dialogueActor != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetPanel({1}, {2})", new object[3] { "Dialogue System", transform, subtitlePanelNumber }), transform);
			}
			dialogueActor.SetSubtitlePanelNumber(subtitlePanelNumber);
		}
		Actor actor = DialogueManager.masterDatabase.GetActor((dialogueActor != null && !string.IsNullOrEmpty(dialogueActor.actor)) ? dialogueActor.actor : parameter);
		if (actor == null)
		{
			if (dialogueActor == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetPanel({1}, {2}): No actor named {1}", new object[3] { "Dialogue System", parameter, subtitlePanelNumber }));
			}
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetPanel({1}, {2})", new object[3] { "Dialogue System", parameter, subtitlePanelNumber }));
			}
			if (DialogueManager.dialogueUI is IStandardDialogueUI standardDialogueUI)
			{
				standardDialogueUI.OverrideActorPanel(actor, subtitlePanelNumber, immediate);
			}
		}
		return true;
	}

	private bool HandleSetMenuPanelInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		Transform transform = null;
		if (string.IsNullOrEmpty(parameter) || string.Equals("speaker", parameter, StringComparison.OrdinalIgnoreCase))
		{
			transform = speaker;
		}
		else if (string.Equals("listener", parameter, StringComparison.OrdinalIgnoreCase))
		{
			transform = listener;
		}
		else
		{
			transform = CharacterInfo.GetRegisteredActorTransform(parameter);
			if (transform == null)
			{
				GameObject gameObject = GameObject.Find(parameter);
				if (gameObject != null)
				{
					transform = gameObject.transform;
				}
			}
		}
		string parameter2 = SequencerTools.GetParameter(args, 1);
		MenuPanelNumber menuPanelNumber = ((!string.Equals(parameter2, "default", StringComparison.OrdinalIgnoreCase)) ? PanelNumberUtility.IntToMenuPanelNumber(Tools.StringToInt(parameter2)) : MenuPanelNumber.Default);
		DialogueActor dialogueActor = ((transform != null) ? transform.GetComponent<DialogueActor>() : null);
		if (transform != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetMenuPanel({1}, {2})", new object[3] { "Dialogue System", transform, menuPanelNumber }), transform);
			}
			if (dialogueActor != null)
			{
				dialogueActor.SetMenuPanelNumber(menuPanelNumber);
			}
			else if (DialogueManager.dialogueUI is IStandardDialogueUI standardDialogueUI)
			{
				standardDialogueUI.OverrideActorMenuPanel(transform, menuPanelNumber, null);
			}
			return true;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetMenuPanel({1}, {2})", new object[3] { "Dialogue System", parameter, menuPanelNumber }));
		}
		if (string.Equals(parameter, "speaker", StringComparison.OrdinalIgnoreCase))
		{
			Conversation conversation = DialogueManager.masterDatabase.GetConversation(DialogueManager.lastConversationID);
			if (conversation != null)
			{
				parameter = DialogueManager.masterDatabase.GetActor(conversation.ActorID).Name;
			}
		}
		Actor actor = DialogueManager.masterDatabase.GetActor(parameter);
		StandardDialogueUI standardDialogueUI2 = DialogueManager.dialogueUI as StandardDialogueUI;
		if (actor != null && standardDialogueUI2 != null)
		{
			standardDialogueUI2.OverrideActorMenuPanel(actor, menuPanelNumber, null);
			return true;
		}
		if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: SetMenuPanel({1}, {2}): Requires a DialogueActor or GameObject named {1}", new object[3] { "Dialogue System", parameter, menuPanelNumber }));
		}
		return false;
	}

	private bool HandleSetPortraitInternally(string commandName, string[] args)
	{
		string actorName = SequencerTools.GetParameter(args, 0);
		string textureName = SequencerTools.GetParameter(args, 1);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetPortrait({1}, {2})", new object[3] { "Dialogue System", actorName, textureName }));
		}
		Actor actor = DialogueManager.masterDatabase.GetActor(actorName);
		if (actor == null)
		{
			DialogueActor dialogueActorComponent = DialogueActor.GetDialogueActorComponent(SequencerTools.GetSubject(actorName, speaker, listener, speaker));
			if (dialogueActorComponent != null && !string.IsNullOrEmpty(dialogueActorComponent.actor))
			{
				actorName = dialogueActorComponent.actor;
			}
			actor = DialogueManager.masterDatabase.GetActor(actorName);
			if (actor != null)
			{
				actorName = actor.Name;
			}
		}
		bool flag = string.Equals(textureName, "default");
		bool flag2 = textureName != null && textureName.StartsWith("pic=");
		Sprite sprite = null;
		if (flag)
		{
			sprite = null;
		}
		else if (flag2)
		{
			string text = textureName.Substring("pic=".Length);
			if (!int.TryParse(text, out var result))
			{
				if (DialogueLua.DoesVariableExist(text))
				{
					result = DialogueLua.GetVariable(text).asInt;
				}
				else
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: SetPortrait() command: pic variable '{1}' not found.", new object[2] { "Dialogue System", text }));
				}
			}
			sprite = actor?.GetPortraitSprite(result);
		}
		else if (actor == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetPortrait() command: actor '{1}' not found.", new object[2] { "Dialogue System", actorName }));
			}
		}
		else
		{
			if (!flag)
			{
				DialogueManager.LoadAsset(textureName, typeof(Sprite), delegate(UnityEngine.Object asset)
				{
					Sprite spriteAsset = asset as Sprite;
					if (spriteAsset != null)
					{
						DialogueLua.SetActorField(actorName, "Current Portrait", textureName);
						DialogueManager.instance.SetActorPortraitSprite(actorName, spriteAsset);
					}
					else
					{
						DialogueManager.LoadAsset(textureName, typeof(Texture2D), delegate(UnityEngine.Object textureAsset)
						{
							spriteAsset = UITools.CreateSprite(textureAsset as Texture2D);
							if (spriteAsset == null && DialogueDebug.logWarnings)
							{
								Debug.LogWarning(string.Format("{0}: Sequencer: SetPortrait() command: sprite/texture '{1}' not found.", new object[2] { "Dialogue System", textureName }));
							}
							DialogueLua.SetActorField(actorName, "Current Portrait", textureName);
							DialogueManager.instance.SetActorPortraitSprite(actorName, spriteAsset);
						});
					}
				});
				return true;
			}
			DialogueLua.SetActorField(actorName, "Current Portrait", string.Empty);
		}
		if (DialogueDebug.logWarnings)
		{
			if (actor == null)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetPortrait() command: actor '{1}' not found.", new object[2] { "Dialogue System", actorName }));
			}
			if (sprite == null && !flag)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetPortrait() command: texture '{1}' not found.", new object[2] { "Dialogue System", textureName }));
			}
		}
		if (actor != null)
		{
			if (flag)
			{
				DialogueLua.SetActorField(actorName, "Current Portrait", string.Empty);
			}
			else if (sprite != null)
			{
				DialogueLua.SetActorField(actorName, "Current Portrait", textureName);
			}
			DialogueManager.instance.SetActorPortraitSprite(actorName, sprite);
		}
		return true;
	}

	private bool HandleSetTimeoutInternally(string commandName, string[] args)
	{
		float parameterAsFloat = SequencerTools.GetParameterAsFloat(args, 0);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: SetTimeout({1})", "Dialogue System", parameterAsFloat));
		}
		if (currentDisplaySettings != null && currentDisplaySettings.inputSettings != null)
		{
			currentDisplaySettings.inputSettings.responseTimeout = parameterAsFloat;
		}
		return true;
	}

	private bool HandleSetContinueModeInternally(string commandName, string[] args)
	{
		if (args == null || args.Length < 1)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetContinueMode(true|false|original) requires a true/false/original parameter", new object[1] { "Dialogue System" }));
			}
			return true;
		}
		string parameter = SequencerTools.GetParameter(args, 0);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SetContinueMode({1})", new object[2] { "Dialogue System", parameter }));
		}
		if (DialogueManager.instance == null || DialogueManager.displaySettings == null || DialogueManager.displaySettings.subtitleSettings == null)
		{
			return true;
		}
		if (string.Equals(parameter, "original", StringComparison.OrdinalIgnoreCase))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetContinueMode({1}): Restoring original mode {2}", new object[3] { "Dialogue System", parameter, savedContinueButtonMode }));
			}
			DialogueManager.displaySettings.subtitleSettings.continueButton = savedContinueButtonMode;
		}
		else
		{
			if (!TryGetContinueMode(parameter, out var mode))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: SetContinueMode(true|false|original|...) requires a valid mode. See online manual for options.", new object[1] { "Dialogue System" }));
				}
				return true;
			}
			savedContinueButtonMode = DialogueManager.displaySettings.subtitleSettings.continueButton;
			DialogueManager.displaySettings.subtitleSettings.continueButton = mode;
		}
		if (DialogueManager.conversationView != null)
		{
			if (DialogueManager.conversationView.displaySettings.conversationOverrideSettings != null)
			{
				DialogueManager.conversationView.displaySettings.conversationOverrideSettings.continueButton = DialogueManager.displaySettings.subtitleSettings.continueButton;
			}
			DialogueManager.conversationView.SetupContinueButton();
		}
		return true;
	}

	private bool TryGetContinueMode(string arg, out DisplaySettings.SubtitleSettings.ContinueButtonMode mode)
	{
		if (string.Equals(arg, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "always", StringComparison.OrdinalIgnoreCase))
		{
			mode = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;
		}
		else if (string.Equals(arg, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "never", StringComparison.OrdinalIgnoreCase))
		{
			mode = DisplaySettings.SubtitleSettings.ContinueButtonMode.Never;
		}
		else if (string.Equals(arg, "optional", StringComparison.OrdinalIgnoreCase))
		{
			mode = DisplaySettings.SubtitleSettings.ContinueButtonMode.Optional;
		}
		else
		{
			mode = DisplaySettings.SubtitleSettings.ContinueButtonMode.Never;
			bool flag = false;
			Array values = Enum.GetValues(typeof(DisplaySettings.SubtitleSettings.ContinueButtonMode));
			for (int i = 0; i < values.Length; i++)
			{
				DisplaySettings.SubtitleSettings.ContinueButtonMode continueButtonMode = (DisplaySettings.SubtitleSettings.ContinueButtonMode)i;
				if (string.Equals(arg, continueButtonMode.ToString(), StringComparison.OrdinalIgnoreCase))
				{
					mode = continueButtonMode;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	private bool HandleContinueInternally(string[] args)
	{
		if (conversationView == null || (args != null && args.Length >= 1 && string.Equals("all", args[0], StringComparison.OrdinalIgnoreCase)))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: Continue(all)", new object[1] { "Dialogue System" }));
			}
			DialogueManager.instance.BroadcastMessage("OnConversationContinueAll", SendMessageOptions.DontRequireReceiver);
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: Continue()", new object[1] { "Dialogue System" }));
			}
			conversationView.HandleContinueButtonClick();
		}
		return true;
	}

	private bool HandleSetVariableInternally(string commandName, string[] args)
	{
		if (args == null || args.Length < 2)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: SetVariable(variableName, value) requires two parameters", new object[1] { "Dialogue System" }));
			}
		}
		else
		{
			string parameter = SequencerTools.GetParameter(args, 0);
			string parameter2 = SequencerTools.GetParameter(args, 1);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sequencer: SetVariable({1}, {2})", new object[3] { "Dialogue System", parameter, parameter2 }));
			}
			float result2;
			if (bool.TryParse(parameter2, out var result))
			{
				DialogueLua.SetVariable(parameter, result);
			}
			else if (float.TryParse(parameter2, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				DialogueLua.SetVariable(parameter, result2);
			}
			else
			{
				DialogueLua.SetVariable(parameter, parameter2);
			}
		}
		return true;
	}

	private bool HandleShowAlertInternally(string commandName, string[] args)
	{
		bool flag = args.Length != 0 && !string.IsNullOrEmpty(args[0]);
		float num = (flag ? SequencerTools.GetParameterAsFloat(args, 0) : 0f);
		if (DialogueDebug.logInfo)
		{
			if (flag)
			{
				Debug.Log(string.Format("{0}: Sequencer: ShowAlert({1})", new object[2] { "Dialogue System", num }));
			}
			else
			{
				Debug.Log(string.Format("{0}: Sequencer: ShowAlert()", new object[1] { "Dialogue System" }));
			}
		}
		try
		{
			string asString = Lua.Run("return Variable['Alert']").asString;
			if (!string.IsNullOrEmpty(asString))
			{
				Lua.Run("Variable['Alert'] = ''");
				if (flag)
				{
					DialogueManager.ShowAlert(asString, num);
				}
				else
				{
					DialogueManager.ShowAlert(asString);
				}
			}
		}
		catch (Exception)
		{
		}
		return true;
	}

	private bool HandleUpdateTrackerInternally()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: UpdateTracker()", new object[1] { "Dialogue System" }));
		}
		DialogueManager.SendUpdateTracker();
		return true;
	}

	private bool HandleRandomizeNextEntryInternally()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: RandomizeNextEntry()", new object[1] { "Dialogue System" }));
		}
		if (DialogueManager.conversationController != null)
		{
			DialogueManager.conversationController.randomizeNextEntry = true;
		}
		return true;
	}

	private bool HandleStopConversationInternally()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: StopConversation()", new object[1] { "Dialogue System" }));
		}
		DialogueManager.StopConversation();
		return true;
	}

	private bool HandleSequencerMessageInternally(string commandName, string[] args)
	{
		string parameter = SequencerTools.GetParameter(args, 0);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: SequencerMessage({1})", new object[2] { "Dialogue System", parameter }));
		}
		if (!string.IsNullOrEmpty(parameter))
		{
			Message(parameter);
		}
		return true;
	}

	private bool HandleGotoEntryInternally(string commandName, string[] args)
	{
		string entryTitle = SequencerTools.GetParameter(args, 0);
		string parameter = SequencerTools.GetParameter(args, 1);
		if (!DialogueManager.isConversationActive)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: GotoEntry({1}, {2}): No conversation is active.", new object[3] { "Dialogue System", entryTitle, parameter }));
			}
			return true;
		}
		Conversation conversation = (string.IsNullOrEmpty(parameter) ? DialogueManager.masterDatabase.GetConversation(DialogueManager.currentConversationState.subtitle.dialogueEntry.conversationID) : DialogueManager.masterDatabase.GetConversation(parameter));
		if (conversation == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: GotoEntry({1}, {2}): Conversation '{2}' not found.", new object[3] { "Dialogue System", entryTitle, parameter }));
			}
			return true;
		}
		DialogueEntry dialogueEntry = conversation.dialogueEntries.Find((DialogueEntry x) => x.Title == entryTitle) ?? conversation.dialogueEntries.Find((DialogueEntry x) => x.DialogueText == entryTitle);
		if (dialogueEntry == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: GotoEntry({1}, {2}): Entry '{1}' not found.", new object[3] { "Dialogue System", entryTitle, parameter }));
			}
			return true;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: GotoEntry({1}, {2})", new object[3] { "Dialogue System", entryTitle, parameter }));
		}
		ConversationState state = DialogueManager.conversationModel.GetState(dialogueEntry);
		DialogueManager.conversationController.GotoState(state);
		return true;
	}
}
