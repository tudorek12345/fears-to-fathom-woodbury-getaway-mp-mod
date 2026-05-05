using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class DialogueSystemController : MonoBehaviour
{
	public enum WarmUpMode
	{
		On,
		Extra,
		Off
	}

	[Tooltip("This dialogue database is loaded automatically. Use an Extra Databases component to load additional databases.")]
	public DialogueDatabase initialDatabase;

	public DisplaySettings displaySettings = new DisplaySettings();

	public PersistentDataSettings persistentDataSettings = new PersistentDataSettings();

	[Tooltip("Allow more than one conversation to play simultaneously.")]
	public bool allowSimultaneousConversations;

	[Tooltip("If not allowing simultaneous conversations and a conversation is active, stop it if another conversation wants to start.")]
	public bool interruptActiveConversations;

	[Tooltip("Tick if your conversations reference Dialog[x].SimStatus.")]
	public bool includeSimStatus;

	[Tooltip("Use a copy of the dialogue database at runtime instead of the asset file directly. This allows you to change the database without affecting the asset.")]
	public bool instantiateDatabase = true;

	[Tooltip("Preload the dialogue database and dialogue UI at Start. Otherwise they're loaded at first use.")]
	public bool preloadResources = true;

	[Tooltip("Warm up conversation engine and dialogue UI at Start to avoid a small amount of overhead on first use. 'Extra' performs deeper warmup that takes 1.25s at startup.")]
	public WarmUpMode warmUpConversationController;

	[Tooltip("Don't run HideImmediate on dialogue UI when warming it up on start.")]
	public bool dontHideImmediateDuringWarmup;

	[Tooltip("Retain this GameObject when changing levels. Note: If InputDeviceManager's Singleton checkbox is ticked or GameObject has SaveSystem, GameObject will still be marked Don't Destroy On Load.")]
	public bool dontDestroyOnLoad = true;

	[Tooltip("Ensure only one Dialogue Manager in the scene.")]
	public bool allowOnlyOneInstance = true;

	[Tooltip("Dialogue System Triggers set to OnStart should wait until save data has been applied or variables initialized.")]
	public bool onStartTriggerWaitForSaveDataApplied;

	[Tooltip("Time mode to use for conversations.\nRealtime: Independent of Time.timeScale.\nGameplay: Observe Time.timeScale.\nCustom: You must manually set DialogueTime.time.")]
	public DialogueTime.TimeMode dialogueTimeMode;

	[Tooltip("Set to higher levels for troubleshooting.")]
	public DialogueDebug.DebugLevel debugLevel = DialogueDebug.DebugLevel.Warning;

	public GetLocalizedTextDelegate overrideGetLocalizedText;

	private bool m_isInitialized;

	private const string DefaultDialogueUIResourceName = "Default Dialogue UI";

	private DatabaseManager m_databaseManager;

	private IDialogueUI m_currentDialogueUI;

	[HideInInspector]
	private IDialogueUI m_originalDialogueUI;

	[HideInInspector]
	private DisplaySettings m_originalDisplaySettings;

	private bool m_overrodeDisplaySettings;

	private bool m_isOverrideUIPrefab;

	private bool m_dontDestroyOverrideUI;

	private OverrideDialogueUI m_overrideDialogueUI;

	private ConversationController m_conversationController;

	private IsDialogueEntryValidDelegate m_isDialogueEntryValid;

	private Action m_customResponseTimeoutHandler;

	private GetInputButtonDownDelegate m_savedGetInputButtonDownDelegate;

	private LuaWatchers m_luaWatchers = new LuaWatchers();

	private AssetBundleManager m_assetBundleManager = new AssetBundleManager();

	private bool m_started;

	private DialogueDebug.DebugLevel m_lastDebugLevelSet;

	private List<ActiveConversationRecord> m_activeConversations = new List<ActiveConversationRecord>();

	private UILocalizationManager m_uiLocalizationManager;

	private bool m_calledRandomizeNextEntry;

	private bool m_isDuplicateBeingDestroyed;

	private Coroutine warmupCoroutine;

	public static bool isWarmingUp;

	public static bool applicationIsQuitting;

	public static string lastInitialDatabaseName;

	private bool m_unloadAddressablesOnSceneChange = true;

	private Conversation fakeConversation;

	private StandardDialogueUI warmupStandardDialogueUI;

	private ConversationController warmupController;

	private bool addTempCanvasGroup;

	private CanvasGroup warmupCanvasGroup;

	private DialogueDebug.DebugLevel warmupPreviousLogLevel;

	private float warmupPreviousAlpha;

	private const int FakeConversationID = -1;

	private const string FakeConversationTitle = "__Internal_Warmup__";

	private bool m_disableInput;

	protected Dictionary<string, List<AssetLoadedDelegate>> m_assetsBeingLoaded = new Dictionary<string, List<AssetLoadedDelegate>>();

	public bool isInitialized => m_isInitialized;

	public DatabaseManager databaseManager => m_databaseManager;

	public DialogueDatabase masterDatabase => m_databaseManager.masterDatabase;

	public IDialogueUI dialogueUI
	{
		get
		{
			return GetDialogueUI();
		}
		set
		{
			SetDialogueUI(value);
		}
	}

	public StandardDialogueUI standardDialogueUI
	{
		get
		{
			return dialogueUI as StandardDialogueUI;
		}
		set
		{
			SetDialogueUI(value);
		}
	}

	public IsDialogueEntryValidDelegate isDialogueEntryValid
	{
		get
		{
			return m_isDialogueEntryValid;
		}
		set
		{
			m_isDialogueEntryValid = value;
			if (m_conversationController != null)
			{
				m_conversationController.isDialogueEntryValid = value;
			}
		}
	}

	public Action customResponseTimeoutHandler
	{
		get
		{
			return m_customResponseTimeoutHandler;
		}
		set
		{
			m_customResponseTimeoutHandler = value;
		}
	}

	public GetInputButtonDownDelegate getInputButtonDown { get; set; }

	public bool isConversationActive
	{
		get
		{
			if (!isAlternateConversationActive)
			{
				if (m_conversationController != null)
				{
					return m_conversationController.isActive;
				}
				return false;
			}
			return true;
		}
	}

	public bool isAlternateConversationActive { get; set; }

	public Transform currentActor { get; private set; }

	public Transform currentConversant { get; private set; }

	public ConversationState currentConversationState { get; set; }

	public string lastConversationStarted { get; private set; }

	public string lastConversationEnded { get; set; }

	public int lastConversationID { get; private set; }

	public ConversationController conversationController => m_conversationController;

	public ConversationModel conversationModel
	{
		get
		{
			if (m_conversationController == null)
			{
				return null;
			}
			return m_conversationController.conversationModel;
		}
	}

	public ConversationView conversationView
	{
		get
		{
			if (m_conversationController == null)
			{
				return null;
			}
			return m_conversationController.conversationView;
		}
	}

	public List<ActiveConversationRecord> activeConversations => m_activeConversations;

	public ActiveConversationRecord activeConversation { get; set; }

	public DatabaseManager DatabaseManager => databaseManager;

	public DialogueDatabase MasterDatabase => masterDatabase;

	public IDialogueUI DialogueUI
	{
		get
		{
			return dialogueUI;
		}
		set
		{
			dialogueUI = value;
		}
	}

	public IsDialogueEntryValidDelegate IsDialogueEntryValid
	{
		get
		{
			return isDialogueEntryValid;
		}
		set
		{
			isDialogueEntryValid = value;
		}
	}

	public GetInputButtonDownDelegate GetInputButtonDown
	{
		get
		{
			return getInputButtonDown;
		}
		set
		{
			getInputButtonDown = value;
		}
	}

	public bool IsConversationActive => isConversationActive;

	public Transform CurrentActor
	{
		get
		{
			return currentActor;
		}
		private set
		{
			currentActor = value;
		}
	}

	public Transform CurrentConversant
	{
		get
		{
			return currentConversant;
		}
		set
		{
			currentConversant = value;
		}
	}

	public ConversationState CurrentConversationState
	{
		get
		{
			return currentConversationState;
		}
		set
		{
			currentConversationState = value;
		}
	}

	public string LastConversationStarted
	{
		get
		{
			return lastConversationStarted;
		}
		set
		{
			lastConversationStarted = value;
		}
	}

	public int LastConversationID
	{
		get
		{
			return lastConversationID;
		}
		set
		{
			lastConversationID = value;
		}
	}

	public ConversationController ConversationController => conversationController;

	public ConversationModel ConversationModel => conversationModel;

	public ConversationView ConversationView => conversationView;

	public List<ActiveConversationRecord> ActiveConversations => activeConversations;

	public bool allowLuaExceptions { get; set; }

	public bool warnIfActorAndConversantSame { get; set; }

	public bool unloadAddressablesOnSceneChange
	{
		get
		{
			return m_unloadAddressablesOnSceneChange;
		}
		set
		{
			m_unloadAddressablesOnSceneChange = value;
		}
	}

	public event Action receivedUpdateTracker = delegate
	{
	};

	public event TransformDelegate conversationStarted = delegate
	{
	};

	public event TransformDelegate conversationEnded = delegate
	{
	};

	public event Action stoppingAllConversations = delegate
	{
	};

	public event Action initializationComplete = delegate
	{
	};

	public void OnDestroy()
	{
		if (dontDestroyOnLoad && allowOnlyOneInstance)
		{
			applicationIsQuitting = true;
		}
		if (!applicationIsQuitting && !m_isDuplicateBeingDestroyed && DialogueTime.isPaused)
		{
			Unpause();
		}
		SceneManager.sceneLoaded -= OnSceneLoaded;
		UILocalizationManager.languageChanged -= OnLanguageChanged;
	}

	public void Awake()
	{
		if (allowOnlyOneInstance && GameObjectUtility.FindObjectsByType<DialogueSystemController>().Length > 1)
		{
			m_isDuplicateBeingDestroyed = true;
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		getInputButtonDown = StandardGetInputButtonDown;
		if ((instantiateDatabase || warmUpConversationController != WarmUpMode.Off) && initialDatabase != null)
		{
			DialogueDatabase dialogueDatabase = UnityEngine.Object.Instantiate(initialDatabase);
			dialogueDatabase.name = initialDatabase.name;
			initialDatabase = dialogueDatabase;
		}
		bool flag = displaySettings != null && displaySettings.inputSettings != null && displaySettings.inputSettings.emTagForOldResponses != EmTag.None;
		DialogueLua.includeSimStatus = includeSimStatus || flag;
		Sequencer.reportMissingAudioFiles = displaySettings.cameraSettings.reportMissingAudioFiles;
		PersistentDataManager.includeSimStatus = DialogueLua.includeSimStatus;
		PersistentDataManager.includeActorData = persistentDataSettings.includeActorData;
		PersistentDataManager.includeAllItemData = persistentDataSettings.includeAllItemData;
		PersistentDataManager.includeLocationData = persistentDataSettings.includeLocationData;
		PersistentDataManager.includeRelationshipAndStatusData = persistentDataSettings.includeStatusAndRelationshipData;
		PersistentDataManager.includeAllConversationFields = persistentDataSettings.includeAllConversationFields;
		PersistentDataManager.initializeNewVariables = persistentDataSettings.initializeNewVariables;
		PersistentDataManager.saveConversationSimStatusWithField = persistentDataSettings.saveConversationSimStatusWithField;
		PersistentDataManager.saveDialogueEntrySimStatusWithField = persistentDataSettings.saveDialogueEntrySimStatusWithField;
		PersistentDataManager.recordPersistentDataOn = persistentDataSettings.recordPersistentDataOn;
		PersistentDataManager.asyncGameObjectBatchSize = persistentDataSettings.asyncGameObjectBatchSize;
		PersistentDataManager.asyncDialogueEntryBatchSize = persistentDataSettings.asyncDialogueEntryBatchSize;
		if (dontDestroyOnLoad)
		{
			if (base.transform.parent != null)
			{
				base.transform.SetParent(null, worldPositionStays: false);
			}
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			SaveSystem component = GetComponent<SaveSystem>();
			InputDeviceManager component2 = GetComponent<InputDeviceManager>();
			if (component != null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: The Dialogue Manager's Don't Destroy On Load checkbox is UNticked, but the GameObject has a Save System which will mark it Don't Destroy On Load anyway. You may want to tick Don't Destroy On Load or move the Save System to another GameObject.", this);
				}
			}
			else if (component2 != null && component2.singleton && DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: The Dialogue Manager's Don't Destroy On Load checkbox is UNticked, but the GameObject has an Input Device Manager whose Singleton checkbox is ticked, which will mark it Don't Destroy On Load anyway. You may want to tick Don't Destroy On Load or untick the Input Device Manager's Singleton checkbox.", this);
			}
		}
		allowLuaExceptions = false;
		warnIfActorAndConversantSame = false;
		DialogueTime.mode = dialogueTimeMode;
		DialogueDebug.level = debugLevel;
		m_lastDebugLevelSet = debugLevel;
		lastConversationStarted = string.Empty;
		lastConversationEnded = string.Empty;
		lastConversationID = -1;
		currentActor = null;
		currentConversant = null;
		currentConversationState = null;
		InitializeDatabase();
		InitializeDisplaySettings();
		InitializeLocalization();
		QuestLog.RegisterQuestLogFunctions();
		RegisterLuaFunctions();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode == LoadSceneMode.Single)
		{
			if (unloadAddressablesOnSceneChange)
			{
				UnloadAssets();
			}
			else
			{
				ClearLoadedAssetHashes();
			}
		}
	}

	public void Start()
	{
		StartCoroutine(MonitorAlerts());
		m_started = true;
		if (preloadResources)
		{
			PreloadResources();
		}
		if (warmUpConversationController != WarmUpMode.Off)
		{
			WarmUpConversationController();
		}
		m_isInitialized = true;
		this.initializationComplete();
	}

	private void InitializeDisplaySettings()
	{
		if (displaySettings == null)
		{
			displaySettings = new DisplaySettings();
			displaySettings.cameraSettings = new DisplaySettings.CameraSettings();
			displaySettings.inputSettings = new DisplaySettings.InputSettings();
			displaySettings.inputSettings.cancel = new InputTrigger(KeyCode.Escape);
			displaySettings.inputSettings.qteButtons = new string[2] { "Fire1", "Fire2" };
			displaySettings.subtitleSettings = new DisplaySettings.SubtitleSettings();
			displaySettings.localizationSettings = new DisplaySettings.LocalizationSettings();
		}
	}

	private void InitializeLocalization()
	{
		if (displaySettings.localizationSettings.useSystemLanguage)
		{
			displaySettings.localizationSettings.language = Localization.GetLanguage(Application.systemLanguage);
		}
		UILocalizationManager uILocalizationManager = GetComponent<UILocalizationManager>() ?? GameObjectUtility.FindFirstObjectByType<UILocalizationManager>();
		if ((!string.IsNullOrEmpty(displaySettings.localizationSettings.language) || displaySettings.localizationSettings.textTable != null) && uILocalizationManager == null)
		{
			uILocalizationManager = base.gameObject.AddComponent<UILocalizationManager>();
		}
		if (uILocalizationManager != null)
		{
			uILocalizationManager.Initialize();
			if (uILocalizationManager.textTable == null)
			{
				uILocalizationManager.textTable = displaySettings.localizationSettings.textTable;
			}
		}
		if (uILocalizationManager != null && !string.IsNullOrEmpty(uILocalizationManager.currentLanguage))
		{
			SetLanguage(uILocalizationManager.currentLanguage);
		}
		else
		{
			SetLanguage(displaySettings.localizationSettings.language);
		}
		UILocalizationManager.languageChanged += OnLanguageChanged;
	}

	public void SetLanguage(string language)
	{
		if (m_uiLocalizationManager == null)
		{
			m_uiLocalizationManager = GetComponent<UILocalizationManager>() ?? GameObjectUtility.FindFirstObjectByType<UILocalizationManager>();
			if (m_uiLocalizationManager == null)
			{
				m_uiLocalizationManager = base.gameObject.AddComponent<UILocalizationManager>();
			}
			if (m_uiLocalizationManager.textTable == null)
			{
				m_uiLocalizationManager.textTable = displaySettings.localizationSettings.textTable;
			}
		}
		m_uiLocalizationManager.currentLanguage = language;
		displaySettings.localizationSettings.language = language;
		Localization.language = language;
	}

	private void OnLanguageChanged(string newLanguage)
	{
		displaySettings.localizationSettings.language = newLanguage;
		UpdateLocalizationOnActiveConversations();
	}

	public void PreloadMasterDatabase()
	{
		DialogueDatabase dialogueDatabase = masterDatabase;
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Loaded master database '{1}'", new object[2] { "Dialogue System", dialogueDatabase.name }));
		}
	}

	public void PreloadDialogueUI()
	{
		if (dialogueUI == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Unable to load the dialogue UI.", "Dialogue System"));
		}
	}

	public void PreloadResources()
	{
		PreloadMasterDatabase();
		PreloadDialogueUI();
		Sequencer.Preload();
	}

	public void WarmUpConversationController()
	{
		if (isConversationActive)
		{
			return;
		}
		AbstractDialogueUI abstractDialogueUI = dialogueUI as AbstractDialogueUI;
		if (abstractDialogueUI == null || abstractDialogueUI.GetComponentInParent<Canvas>() == null)
		{
			return;
		}
		warmupStandardDialogueUI = abstractDialogueUI as StandardDialogueUI;
		isWarmingUp = true;
		warmupCanvasGroup = abstractDialogueUI.GetComponent<CanvasGroup>();
		addTempCanvasGroup = warmupCanvasGroup == null;
		if (addTempCanvasGroup)
		{
			warmupCanvasGroup = abstractDialogueUI.gameObject.AddComponent<CanvasGroup>();
		}
		warmupPreviousAlpha = warmupCanvasGroup.alpha;
		warmupCanvasGroup.alpha = 0f;
		warmupPreviousLogLevel = DialogueDebug.level;
		DialogueDebug.level = DialogueDebug.DebugLevel.None;
		if (warmUpConversationController == WarmUpMode.Extra)
		{
			warmupCoroutine = StartCoroutine(WarmUpConversationControllerExtra());
			return;
		}
		try
		{
			Conversation item = CreateFakeConversation();
			masterDatabase.conversations.Add(item);
			try
			{
				ConversationModel conversationModel = new ConversationModel(databaseManager.masterDatabase, "__Internal_Warmup__", null, null, allowLuaExceptions: false, null);
				ConversationView conversationView = base.gameObject.AddComponent<ConversationView>();
				conversationView.Initialize(dialogueUI, GetNewSequencer(), displaySettings, OnDialogueEntrySpoken);
				conversationView.SetPCPortrait(conversationModel.GetPCSprite(), conversationModel.GetPCName());
				ConversationController obj = new ConversationController();
				obj.Initialize(conversationModel, conversationView, displaySettings.inputSettings.alwaysForceResponseMenu, OnEndConversation);
				StandardDialogueUI standardDialogueUI = abstractDialogueUI as StandardDialogueUI;
				if (standardDialogueUI != null && !dontHideImmediateDuringWarmup)
				{
					standardDialogueUI.conversationUIElements.HideImmediate();
				}
				obj.Close();
			}
			finally
			{
				masterDatabase.conversations.Remove(item);
			}
		}
		finally
		{
			isWarmingUp = false;
			DialogueDebug.level = warmupPreviousLogLevel;
			if (addTempCanvasGroup)
			{
				UnityEngine.Object.Destroy(warmupCanvasGroup);
			}
			else
			{
				warmupCanvasGroup.alpha = warmupPreviousAlpha;
			}
			ResetWarmupVariables();
		}
	}

	private IEnumerator WarmUpConversationControllerExtra()
	{
		fakeConversation = CreateFakeConversation();
		masterDatabase.conversations.Add(fakeConversation);
		ConversationModel conversationModel = new ConversationModel(databaseManager.masterDatabase, "__Internal_Warmup__", null, null, allowLuaExceptions: false, null);
		ConversationView conversationView = base.gameObject.AddComponent<ConversationView>();
		conversationView.Initialize(dialogueUI, GetNewSequencer(), displaySettings, OnDialogueEntrySpoken);
		conversationView.SetPCPortrait(conversationModel.GetPCSprite(), conversationModel.GetPCName());
		warmupController = new ConversationController();
		warmupController.Initialize(conversationModel, conversationView, displaySettings.inputSettings.alwaysForceResponseMenu, OnEndConversation);
		warmupController.GotoState(conversationModel.GetState(fakeConversation.dialogueEntries[1]));
		Canvas.ForceUpdateCanvases();
		yield return new WaitForSeconds(1.25f);
		FinishExtraWarmup();
	}

	private void InterruptExtraWarmup()
	{
		StopCoroutine(warmupCoroutine);
		FinishExtraWarmup();
	}

	private void FinishExtraWarmup()
	{
		if (warmupStandardDialogueUI != null && !dontHideImmediateDuringWarmup)
		{
			warmupStandardDialogueUI.conversationUIElements.HideImmediate();
		}
		warmupController.Close();
		if (warmupStandardDialogueUI != null && warmupStandardDialogueUI.conversationUIElements != null && warmupStandardDialogueUI.conversationUIElements.subtitlePanels != null)
		{
			StandardUISubtitlePanel[] subtitlePanels = warmupStandardDialogueUI.conversationUIElements.subtitlePanels;
			foreach (StandardUISubtitlePanel standardUISubtitlePanel in subtitlePanels)
			{
				if (standardUISubtitlePanel != null)
				{
					if (standardUISubtitlePanel.subtitleText != null)
					{
						standardUISubtitlePanel.subtitleText.text = string.Empty;
					}
					standardUISubtitlePanel.panelState = UIPanel.PanelState.Closed;
				}
			}
		}
		masterDatabase.conversations.Remove(fakeConversation);
		DialogueDebug.level = warmupPreviousLogLevel;
		if (addTempCanvasGroup)
		{
			UnityEngine.Object.Destroy(warmupCanvasGroup);
		}
		else if (isWarmingUp)
		{
			warmupCanvasGroup.alpha = warmupPreviousAlpha;
		}
		ResetWarmupVariables();
	}

	private void ResetWarmupVariables()
	{
		isWarmingUp = false;
		warmupCoroutine = null;
		fakeConversation = null;
		warmupStandardDialogueUI = null;
		warmupController = null;
		warmupCanvasGroup = null;
	}

	private Conversation CreateFakeConversation()
	{
		Conversation conversation = new Conversation();
		conversation.id = -1;
		conversation.fields = new List<Field>();
		conversation.fields.Add(new Field("Title", "__Internal_Warmup__", FieldType.Text));
		DialogueEntry item = new DialogueEntry
		{
			conversationID = -1,
			id = 0,
			fields = new List<Field>(),
			Sequence = "None()",
			outgoingLinks = 
			{
				new Link(-1, 0, -1, 1)
			}
		};
		conversation.dialogueEntries.Add(item);
		item = new DialogueEntry
		{
			conversationID = -1,
			fields = new List<Field>(),
			id = 1,
			DialogueText = " ",
			Sequence = "Delay(2)"
		};
		conversation.dialogueEntries.Add(item);
		return conversation;
	}

	private void CheckDebugLevel()
	{
		if (debugLevel != m_lastDebugLevelSet)
		{
			DialogueDebug.level = debugLevel;
			m_lastDebugLevelSet = debugLevel;
		}
	}

	public bool StandardGetInputButtonDown(string buttonName)
	{
		return InputDeviceManager.IsButtonDown(buttonName);
	}

	private bool DisabledGetInputButtonDown(string buttonName)
	{
		return false;
	}

	public bool IsDialogueSystemInputDisabled()
	{
		return m_disableInput;
	}

	public void SetDialogueSystemInput(bool value)
	{
		if (value)
		{
			if (IsDialogueSystemInputDisabled())
			{
				getInputButtonDown = m_savedGetInputButtonDownDelegate ?? new GetInputButtonDownDelegate(StandardGetInputButtonDown);
				m_disableInput = false;
			}
		}
		else if (!IsDialogueSystemInputDisabled())
		{
			m_savedGetInputButtonDownDelegate = getInputButtonDown;
			getInputButtonDown = DisabledGetInputButtonDown;
			m_disableInput = true;
		}
	}

	private void InitializeDatabase()
	{
		m_databaseManager = new DatabaseManager(initialDatabase);
		if (initialDatabase != null && initialDatabase.name == lastInitialDatabaseName)
		{
			m_databaseManager.Add(initialDatabase);
		}
		else
		{
			m_databaseManager.Reset(DatabaseResetOptions.KeepAllLoaded);
		}
		if (initialDatabase != null)
		{
			lastInitialDatabaseName = initialDatabase.name;
		}
		if (DialogueDebug.logWarnings && initialDatabase == null)
		{
			Debug.LogWarning(string.Format("{0}: No dialogue database is assigned.", new object[1] { "Dialogue System" }));
		}
	}

	public void AddDatabase(DialogueDatabase database)
	{
		if (m_databaseManager != null)
		{
			m_databaseManager.Add(database);
		}
	}

	public void RemoveDatabase(DialogueDatabase database)
	{
		if (m_databaseManager != null)
		{
			m_databaseManager.Remove(database);
		}
	}

	public void ResetDatabase(DatabaseResetOptions databaseResetOptions)
	{
		if (m_databaseManager != null)
		{
			m_databaseManager.Reset(databaseResetOptions);
		}
	}

	public void ResetDatabase()
	{
		if (m_databaseManager != null)
		{
			m_databaseManager.Reset(DatabaseResetOptions.KeepAllLoaded);
		}
	}

	public bool ConversationHasValidEntry(string title, Transform actor, Transform conversant, int initialDialogueEntryID = -1)
	{
		if (string.IsNullOrEmpty(title))
		{
			return false;
		}
		Transform transform = currentActor;
		Transform transform2 = currentConversant;
		currentActor = actor;
		currentConversant = conversant;
		ConversationModel obj = new ConversationModel(m_databaseManager.masterDatabase, title, actor, conversant, allowLuaExceptions, isDialogueEntryValid, initialDialogueEntryID, stopAtFirstValid: true, skipExecution: true);
		currentActor = transform;
		currentConversant = transform2;
		return obj.hasValidEntry;
	}

	public bool ConversationHasValidEntry(string title, Transform actor)
	{
		return ConversationHasValidEntry(title, actor, null);
	}

	public bool ConversationHasValidEntry(string title)
	{
		return ConversationHasValidEntry(title, null, null);
	}

	public void StartConversation(string title, Transform actor, Transform conversant, int initialDialogueEntryID)
	{
		if (warmupCoroutine != null)
		{
			InterruptExtraWarmup();
		}
		if (isConversationActive && !allowSimultaneousConversations && interruptActiveConversations)
		{
			StopConversation();
		}
		if (isConversationActive && !allowSimultaneousConversations)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Another conversation is already active. Not starting '{1}'.", new object[2] { "Dialogue System", title }));
			}
			return;
		}
		if (dialogueUI == null)
		{
			if (DialogueDebug.logErrors)
			{
				Debug.LogError(string.Format("{0}: No Dialogue UI is assigned. Can't start conversation '{1}'.", new object[2] { "Dialogue System", title }));
			}
			return;
		}
		if (actor == null)
		{
			actor = FindActorTransformFromConversation(title, "Actor");
		}
		if (conversant == null)
		{
			conversant = FindActorTransformFromConversation(title, "Conversant");
		}
		CheckDebugLevel();
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Starting conversation '{1}', actor={2}, conversant={3}.", "Dialogue System", title, actor, conversant), actor);
		}
		if (warnIfActorAndConversantSame && actor != null && actor == conversant && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Actor and conversant are the same GameObject.", new object[1] { "Dialogue System" }));
		}
		Transform transform = currentActor;
		Transform transform2 = currentConversant;
		string text = lastConversationStarted;
		currentActor = actor;
		currentConversant = conversant;
		lastConversationStarted = title;
		if ((m_overrodeDisplaySettings && m_originalDisplaySettings != null) || m_originalDialogueUI != null)
		{
			RestoreOriginalUI();
		}
		SetConversationUI(actor, conversant);
		m_calledRandomizeNextEntry = false;
		m_conversationController = new ConversationController();
		ConversationModel conversationModel = new ConversationModel(m_databaseManager.masterDatabase, title, actor, conversant, allowLuaExceptions, isDialogueEntryValid, initialDialogueEntryID);
		bool calledRandomizeNextEntry = m_calledRandomizeNextEntry;
		m_calledRandomizeNextEntry = false;
		if (!conversationModel.hasValidEntry)
		{
			currentActor = transform;
			currentConversant = transform2;
			lastConversationStarted = text;
			return;
		}
		if (conversationModel.firstState != null && conversationModel.firstState.subtitle != null && conversationModel.firstState.subtitle.dialogueEntry != null)
		{
			lastConversationID = conversationModel.firstState.subtitle.dialogueEntry.conversationID;
			if (conversationModel.firstState.subtitle.dialogueEntry.isGroup && conversationModel.firstState.subtitle.sequence == string.Empty)
			{
				conversationModel.firstState.isGroup = false;
				conversationModel.firstState.subtitle.sequence = "Continue()";
			}
		}
		Sequencer newSequencer = GetNewSequencer();
		newSequencer.keepCameraPositionOnClose = displaySettings.cameraSettings.keepCameraPositionAtConversationEnd;
		ConversationView conversationView = (newSequencer.conversationView = base.gameObject.AddComponent<ConversationView>());
		conversationView.Initialize(dialogueUI, newSequencer, displaySettings, OnDialogueEntrySpoken);
		conversationView.SetPCPortrait(conversationModel.GetPCSprite(), conversationModel.GetPCName());
		m_conversationController.Initialize(conversationModel, conversationView, displaySettings.inputSettings.alwaysForceResponseMenu, OnEndConversation);
		if (calledRandomizeNextEntry)
		{
			RandomizeNextEntry();
		}
		ActiveConversationRecord activeConversationRecord = new ActiveConversationRecord();
		activeConversationRecord.conversationTitle = title;
		activeConversationRecord.actor = actor;
		activeConversationRecord.conversant = conversant;
		activeConversationRecord.conversationController = m_conversationController;
		activeConversationRecord.conversationController.activeConversationRecord = activeConversationRecord;
		activeConversationRecord.originalDialogueUI = m_originalDialogueUI;
		activeConversationRecord.originalDisplaySettings = m_originalDisplaySettings;
		activeConversationRecord.isOverrideUIPrefab = m_isOverrideUIPrefab;
		activeConversationRecord.dontDestroyPrefabInstance = m_dontDestroyOverrideUI;
		m_activeConversations.Add(activeConversationRecord);
		activeConversation = activeConversationRecord;
		conversationView.sequencer.activeConversationRecord = activeConversationRecord;
		Transform parameter = ((actor != null) ? actor : base.transform);
		if (actor != base.transform)
		{
			base.gameObject.BroadcastMessage("OnConversationStart", parameter, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void StartConversation(string title, Transform actor, Transform conversant)
	{
		StartConversation(title, actor, conversant, -1);
	}

	public void StartConversation(string title, Transform actor)
	{
		StartConversation(title, actor, null, -1);
	}

	public void StartConversation(string title)
	{
		StartConversation(title, null, null, -1);
	}

	public void StopConversation()
	{
		if (m_conversationController != null)
		{
			m_conversationController.Close();
			m_conversationController = null;
		}
	}

	public void StopAllConversations()
	{
		this.stoppingAllConversations?.Invoke();
		for (int num = DialogueManager.instance.activeConversations.Count - 1; num >= 0; num--)
		{
			DialogueManager.instance.activeConversations[num].conversationController.Close();
		}
	}

	public void UpdateResponses()
	{
		if (m_conversationController != null)
		{
			m_conversationController.UpdateResponses();
		}
	}

	public Transform FindActorTransformFromConversation(string conversationTitle, string actorField)
	{
		Conversation conversation = masterDatabase.GetConversation(conversationTitle);
		if (conversation == null)
		{
			return null;
		}
		Actor actor = masterDatabase.GetActor(conversation.LookupInt(actorField));
		if (actor == null)
		{
			return null;
		}
		GameObject gameObject = SequencerTools.FindSpecifier(actor.Name, onlyActiveInScene: true);
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform;
	}

	public void SetPortrait(string actorName, string portraitName)
	{
		Actor actor = masterDatabase.GetActor(actorName);
		if (actor == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: SetPortrait({1}, {2}): actor '{1}' not found.", new object[3] { "Dialogue System", actorName, portraitName }));
			}
			return;
		}
		Sprite sprite = null;
		if (string.IsNullOrEmpty(portraitName) || string.Equals(portraitName, "default"))
		{
			DialogueLua.SetActorField(actorName, "Current Portrait", string.Empty);
			sprite = actor.GetPortraitSprite();
		}
		else
		{
			DialogueLua.SetActorField(actorName, "Current Portrait", portraitName);
			if (!portraitName.StartsWith("pic="))
			{
				LoadAsset(portraitName, typeof(Sprite), delegate(UnityEngine.Object asset)
				{
					Sprite sprite2 = asset as Sprite;
					if (sprite2 != null)
					{
						SetActorPortraitSprite(actorName, sprite2);
					}
					else
					{
						LoadAsset(portraitName, typeof(Texture2D), delegate(UnityEngine.Object textureAsset)
						{
							SetActorPortraitSprite(actorName, UITools.CreateSprite(textureAsset as Texture2D));
						});
					}
				});
				return;
			}
			sprite = actor.GetPortraitSprite(Tools.StringToInt(portraitName.Substring("pic=".Length)));
		}
		if (DialogueDebug.logWarnings && sprite == null)
		{
			Debug.LogWarning(string.Format("{0}: SetPortrait({1}, {2}): portrait image not found.", new object[3] { "Dialogue System", actorName, portraitName }));
		}
		SetActorPortraitSprite(actorName, sprite);
	}

	public void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
		if (isConversationActive && m_conversationController != null)
		{
			m_conversationController.SetActorPortraitSprite(actorName, sprite);
		}
	}

	private void SetConversationUI(Transform actor, Transform conversant)
	{
		OverrideUIBase overrideUIBase = FindHighestPriorityOverrideUI(actor, conversant);
		if (overrideUIBase != null)
		{
			ApplyOverrideUI(overrideUIBase);
		}
		ValidateCurrentDialogueUI();
		if (isWarmingUp && displaySettings.dialogueUI != null)
		{
			isWarmingUp = false;
			CanvasGroup component = displaySettings.dialogueUI.GetComponent<CanvasGroup>();
			if (component != null)
			{
				component.alpha = 1f;
			}
		}
	}

	private OverrideUIBase FindHighestPriorityOverrideUI(Transform actor, Transform conversant)
	{
		OverrideUIBase overrideUIBase = FindOverrideUI(actor);
		OverrideUIBase overrideUIBase2 = FindOverrideUI(conversant);
		if (overrideUIBase != null)
		{
			if (overrideUIBase2 != null)
			{
				if (overrideUIBase.priority <= overrideUIBase2.priority)
				{
					return overrideUIBase2;
				}
				return overrideUIBase;
			}
			return overrideUIBase;
		}
		return overrideUIBase2;
	}

	private OverrideUIBase FindOverrideUI(Transform character)
	{
		if (character == null)
		{
			return null;
		}
		OverrideUIBase componentInChildren = character.GetComponentInChildren<OverrideUIBase>();
		OverrideDialogueUI overrideDialogueUI = componentInChildren as OverrideDialogueUI;
		OverrideDisplaySettings overrideDisplaySettings = componentInChildren as OverrideDisplaySettings;
		if (!(componentInChildren != null) || !componentInChildren.enabled || ((!(overrideDialogueUI != null) || !(overrideDialogueUI.ui != null)) && !(overrideDisplaySettings != null)))
		{
			return null;
		}
		return componentInChildren;
	}

	private void ApplyOverrideUI(OverrideUIBase overrideUI)
	{
		m_overrideDialogueUI = overrideUI as OverrideDialogueUI;
		OverrideDisplaySettings overrideDisplaySettings = overrideUI as OverrideDisplaySettings;
		if (m_overrideDialogueUI != null)
		{
			m_isOverrideUIPrefab = Tools.IsPrefab(m_overrideDialogueUI.ui);
			m_dontDestroyOverrideUI = m_overrideDialogueUI.dontDestroyPrefabIntance;
			m_originalDialogueUI = dialogueUI;
			displaySettings.dialogueUI = m_overrideDialogueUI.ui;
			m_currentDialogueUI = null;
		}
		else if ((bool)overrideDisplaySettings)
		{
			if (overrideDisplaySettings.displaySettings.dialogueUI != null)
			{
				m_isOverrideUIPrefab = Tools.IsPrefab(overrideDisplaySettings.displaySettings.dialogueUI);
				m_originalDialogueUI = dialogueUI;
				m_dontDestroyOverrideUI = false;
				m_currentDialogueUI = null;
			}
			m_overrodeDisplaySettings = true;
			m_originalDisplaySettings = displaySettings;
			displaySettings = overrideDisplaySettings.displaySettings;
			if (overrideDisplaySettings.displaySettings.dialogueUI == null)
			{
				overrideDisplaySettings.displaySettings.dialogueUI = m_originalDisplaySettings.dialogueUI;
			}
		}
	}

	private void RestoreOriginalUI()
	{
		if (m_overrodeDisplaySettings && m_originalDisplaySettings != null)
		{
			displaySettings = m_originalDisplaySettings;
		}
		if (m_originalDialogueUI != null)
		{
			if (m_isOverrideUIPrefab)
			{
				MonoBehaviour monoBehaviour = m_currentDialogueUI as MonoBehaviour;
				if (monoBehaviour != null)
				{
					HideAndDestroyUI(monoBehaviour);
				}
			}
			m_currentDialogueUI = m_originalDialogueUI;
			displaySettings.dialogueUI = (m_originalDialogueUI as MonoBehaviour).gameObject;
		}
		m_isOverrideUIPrefab = false;
		m_originalDialogueUI = null;
		m_originalDisplaySettings = null;
		m_overrodeDisplaySettings = false;
	}

	private void HideAndDestroyUI(MonoBehaviour uiBehaviour)
	{
		StandardDialogueUI standardDialogueUI = uiBehaviour as StandardDialogueUI;
		if (standardDialogueUI != null && standardDialogueUI.conversationUIElements.mainPanel != null)
		{
			StartCoroutine(HideAndDestroyUICoroutine(standardDialogueUI));
		}
		else if (!m_dontDestroyOverrideUI)
		{
			UnityEngine.Object.Destroy(uiBehaviour.gameObject);
		}
	}

	private IEnumerator HideAndDestroyUICoroutine(StandardDialogueUI standardDialogueUI)
	{
		float timeout = Time.realtimeSinceStartup + 8f;
		standardDialogueUI.Close();
		while (standardDialogueUI.conversationUIElements.mainPanel.panelState != UIPanel.PanelState.Closed && Time.realtimeSinceStartup < timeout)
		{
			yield return null;
		}
		if (!m_dontDestroyOverrideUI)
		{
			UnityEngine.Object.Destroy(standardDialogueUI.gameObject);
		}
	}

	private void OnDialogueEntrySpoken(Subtitle subtitle)
	{
		m_luaWatchers.NotifyObservers(LuaWatchFrequency.EveryDialogueEntry);
	}

	public void OnEndConversation(ConversationController endingConversationController)
	{
		activeConversation = null;
		ActiveConversationRecord activeConversationRecord = m_activeConversations.Find((ActiveConversationRecord r) => r.conversationController == endingConversationController);
		if (activeConversationRecord != null)
		{
			m_activeConversations.Remove(activeConversationRecord);
			if (m_activeConversations.Count > 0)
			{
				ActiveConversationRecord activeConversationRecord2 = m_activeConversations[0];
				m_conversationController = activeConversationRecord2.conversationController;
				currentActor = activeConversationRecord2.actor;
				currentConversant = activeConversationRecord2.conversant;
			}
			else
			{
				m_conversationController = null;
				currentActor = null;
				currentConversant = null;
			}
			m_originalDialogueUI = activeConversationRecord.originalDialogueUI;
			m_originalDisplaySettings = activeConversationRecord.originalDisplaySettings;
			m_isOverrideUIPrefab = activeConversationRecord.isOverrideUIPrefab;
			RestoreOriginalUI();
		}
		m_luaWatchers.NotifyObservers(LuaWatchFrequency.EndOfConversation);
		CheckAlerts();
	}

	public void OnConversationTimeout()
	{
		if (isConversationActive)
		{
			if (displaySettings.inputSettings.responseTimeoutAction == ResponseTimeoutAction.EndConversation)
			{
				StopConversation();
			}
			else
			{
				StartCoroutine(ChooseResponseAfterOneFrame(displaySettings.inputSettings.responseTimeoutAction));
			}
		}
	}

	private void OnConversationStart(Transform actor)
	{
		this.conversationStarted(actor);
	}

	private void OnConversationEnd(Transform actor)
	{
		this.conversationEnded(actor);
	}

	private IEnumerator ChooseResponseAfterOneFrame(ResponseTimeoutAction responseTimeoutAction)
	{
		yield return null;
		if (!isConversationActive)
		{
			yield break;
		}
		switch (responseTimeoutAction)
		{
		default:
			m_conversationController.GotoFirstResponse();
			break;
		case ResponseTimeoutAction.ChooseRandomResponse:
			m_conversationController.GotoRandomResponse();
			break;
		case ResponseTimeoutAction.ChooseCurrentResponse:
			m_conversationController.GotoCurrentResponse();
			break;
		case ResponseTimeoutAction.ChooseLastResponse:
			m_conversationController.GotoLastResponse();
			break;
		case ResponseTimeoutAction.Custom:
			if (customResponseTimeoutHandler != null)
			{
				customResponseTimeoutHandler();
			}
			break;
		}
	}

	public void UpdateLocalizationOnActiveConversations()
	{
		foreach (ActiveConversationRecord activeConversation in activeConversations)
		{
			UpdateLocalizationOnConversation(activeConversation);
		}
	}

	private void UpdateLocalizationOnConversation(ActiveConversationRecord record)
	{
		if (record == null || record.conversationView == null || record.conversationView.dialogueUI == null)
		{
			return;
		}
		StandardDialogueUI standardDialogueUI = record.conversationView.dialogueUI as StandardDialogueUI;
		if (standardDialogueUI == null)
		{
			return;
		}
		ConversationState currentState = record.conversationController.currentState;
		Subtitle subtitle = currentState.subtitle;
		subtitle.formattedText.text = FormattedText.Parse(subtitle.dialogueEntry.currentDialogueText).text;
		DialogueActor dialogueActor;
		StandardUISubtitlePanel panel = standardDialogueUI.conversationUIElements.standardSubtitleControls.GetPanel(subtitle, out dialogueActor);
		panel.subtitleText.text = subtitle.formattedText.text;
		if (panel.portraitName != null)
		{
			record.conversationModel.OverrideCharacterInfo(subtitle.speakerInfo.id, subtitle.speakerInfo.transform);
			CharacterInfo characterInfo = record.conversationModel.GetCharacterInfo(subtitle.speakerInfo.id);
			if (characterInfo != null)
			{
				panel.portraitName.text = characterInfo.Name;
			}
		}
		StandardUIMenuPanel defaultMenuPanel = standardDialogueUI.conversationUIElements.defaultMenuPanel;
		if (defaultMenuPanel != null && defaultMenuPanel.isOpen)
		{
			Response[] pcResponses = currentState.pcResponses;
			foreach (Response response in pcResponses)
			{
				response.formattedText.text = FormattedText.Parse(response.destinationEntry.currentMenuText).text;
			}
			defaultMenuPanel.ShowResponses(subtitle, currentState.pcResponses, standardDialogueUI.transform);
		}
	}

	public void Bark(string conversationTitle, Transform speaker, Transform listener, BarkHistory barkHistory)
	{
		CheckDebugLevel();
		StartCoroutine(BarkController.Bark(conversationTitle, speaker, listener, barkHistory));
	}

	public void Bark(string conversationTitle, Transform speaker, Transform listener, int entryID)
	{
		CheckDebugLevel();
		if (speaker == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning($"Dialogue System: Can't bark '{conversationTitle}:[{entryID}]. No barker specified.");
			}
		}
		else
		{
			IBarkUI barkUI = speaker.GetComponentInChildren(typeof(IBarkUI)) as IBarkUI;
			ConversationState firstState = new ConversationModel(DialogueManager.masterDatabase, conversationTitle, speaker, listener, DialogueManager.allowLuaExceptions, DialogueManager.isDialogueEntryValid, entryID).firstState;
			StartCoroutine(BarkController.Bark(firstState.subtitle, speaker, listener, barkUI));
		}
	}

	public void Bark(string conversationTitle, Transform speaker, Transform listener)
	{
		Bark(conversationTitle, speaker, listener, new BarkHistory(BarkOrder.Random));
	}

	public void Bark(string conversationTitle, Transform speaker)
	{
		Bark(conversationTitle, speaker, null, new BarkHistory(BarkOrder.Random));
	}

	public void Bark(string conversationTitle, Transform speaker, BarkHistory barkHistory)
	{
		Bark(conversationTitle, speaker, null, barkHistory);
	}

	public void BarkString(string barkText, Transform speaker, Transform listener = null, string sequence = null)
	{
		if (speaker == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Can't bark '" + barkText + "'. No speaker specified.");
			}
			return;
		}
		CharacterInfo characterInfoFromTransform = GetCharacterInfoFromTransform(speaker);
		CharacterInfo characterInfoFromTransform2 = GetCharacterInfoFromTransform(listener);
		FormattedText formattedText = FormattedText.Parse(GetLocalizedText(barkText), masterDatabase.emphasisSettings);
		if (sequence == null)
		{
			sequence = string.Empty;
		}
		string empty = string.Empty;
		DialogueEntry dialogueEntry = null;
		Subtitle subtitle = new Subtitle(characterInfoFromTransform, characterInfoFromTransform2, formattedText, sequence, empty, dialogueEntry);
		if (DialogueDebug.logInfo)
		{
			string text = ((characterInfoFromTransform != null) ? (" (" + characterInfoFromTransform.Name + ")") : string.Empty);
			Debug.Log("Dialogue System: " + speaker?.ToString() + text + " barking string '" + barkText + "'.");
		}
		StartCoroutine(BarkController.Bark(subtitle));
	}

	public float GetBarkDuration(string barkText)
	{
		float num = displaySettings.barkSettings.minBarkSeconds;
		if (Mathf.Approximately(0f, num))
		{
			num = displaySettings.subtitleSettings.minSubtitleSeconds;
		}
		float num2 = displaySettings.barkSettings.barkCharsPerSecond;
		if (Mathf.Approximately(0f, num2))
		{
			num2 = displaySettings.subtitleSettings.subtitleCharsPerSecond;
		}
		if (!string.IsNullOrEmpty(barkText))
		{
			return Mathf.Max(num, (float)barkText.Length / num2);
		}
		return 0f;
	}

	private CharacterInfo GetCharacterInfoFromTransform(Transform actorTransform)
	{
		string actorName = DialogueActor.GetActorName(actorTransform);
		Actor actor = masterDatabase.GetActor(actorName);
		int id = actor?.id ?? 1;
		Sprite portrait = actor?.GetPortraitSprite();
		return new CharacterInfo(id, actorName, actorTransform, CharacterType.NPC, portrait);
	}

	public void ShowAlert(string message, float duration)
	{
		if (message != null && dialogueUI != null && (displaySettings.alertSettings.allowAlertsDuringConversations || !isConversationActive))
		{
			if (message.Contains("\\n"))
			{
				message = message.Replace("\\n", "\n");
			}
			base.gameObject.BroadcastMessage("OnShowAlert", message, SendMessageOptions.DontRequireReceiver);
			dialogueUI.ShowAlert(GetLocalizedText(FormattedText.ParseCode(message)), duration);
		}
	}

	public void ShowAlert(string message)
	{
		float num = displaySettings.alertSettings.minAlertSeconds;
		if (Mathf.Approximately(0f, num))
		{
			num = displaySettings.subtitleSettings.minSubtitleSeconds;
		}
		float num2 = displaySettings.alertSettings.alertCharsPerSecond;
		if (Mathf.Approximately(0f, num2))
		{
			num2 = displaySettings.subtitleSettings.subtitleCharsPerSecond;
		}
		float duration = (string.IsNullOrEmpty(message) ? 0f : Mathf.Max(num, (float)message.Length / num2));
		ShowAlert(message, duration);
	}

	public void CheckAlerts()
	{
		if (displaySettings.alertSettings.allowAlertsDuringConversations || !isConversationActive)
		{
			string asString = DialogueLua.GetVariable("Alert").asString;
			if (!string.IsNullOrEmpty(asString) && !string.Equals(asString, "nil"))
			{
				Lua.Run("Variable['Alert'] = ''");
				ShowAlert(asString);
			}
		}
	}

	private IEnumerator MonitorAlerts()
	{
		if (displaySettings == null || displaySettings.alertSettings == null || Tools.ApproximatelyZero(displaySettings.alertSettings.alertCheckFrequency))
		{
			yield break;
		}
		float currentFrequency = displaySettings.alertSettings.alertCheckFrequency;
		WaitForSeconds waitForSeconds = new WaitForSeconds(displaySettings.alertSettings.alertCheckFrequency);
		while (true)
		{
			if (currentFrequency != displaySettings.alertSettings.alertCheckFrequency)
			{
				waitForSeconds = new WaitForSeconds(displaySettings.alertSettings.alertCheckFrequency);
			}
			yield return waitForSeconds;
			CheckAlerts();
		}
	}

	public void HideAlert()
	{
		if (dialogueUI != null)
		{
			dialogueUI.HideAlert();
		}
	}

	public string GetLocalizedText(string s)
	{
		if (overrideGetLocalizedText != null)
		{
			return overrideGetLocalizedText(s);
		}
		TextTable textTable = displaySettings.localizationSettings.textTable;
		string fieldTextForLanguage;
		if (textTable != null)
		{
			int currentLanguageID = Localization.GetCurrentLanguageID(textTable);
			if (textTable.HasFieldTextForLanguage(s, currentLanguageID))
			{
				fieldTextForLanguage = textTable.GetFieldTextForLanguage(s, currentLanguageID);
				if (!string.IsNullOrEmpty(fieldTextForLanguage))
				{
					return fieldTextForLanguage;
				}
			}
		}
		fieldTextForLanguage = UILocalizationManager.instance.GetLocalizedText(s);
		if (!string.IsNullOrEmpty(fieldTextForLanguage))
		{
			return fieldTextForLanguage;
		}
		return s;
	}

	public Sequencer PlaySequence(string sequence, Transform speaker, Transform listener, bool informParticipants, bool destroyWhenDone, string entrytag)
	{
		CheckDebugLevel();
		Sequencer newSequencer = GetNewSequencer();
		newSequencer.Open();
		newSequencer.entrytag = entrytag;
		newSequencer.PlaySequence(sequence, speaker, listener, informParticipants, destroyWhenDone);
		return newSequencer;
	}

	public Sequencer PlaySequence(string sequence, Transform speaker, Transform listener, bool informParticipants, bool destroyWhenDone)
	{
		return PlaySequence(sequence, speaker, listener, informParticipants, destroyWhenDone, string.Empty);
	}

	public Sequencer PlaySequence(string sequence, Transform speaker, Transform listener, bool informParticipants)
	{
		return PlaySequence(sequence, speaker, listener, informParticipants, destroyWhenDone: true, string.Empty);
	}

	public Sequencer PlaySequence(string sequence, Transform speaker, Transform listener)
	{
		return PlaySequence(sequence, speaker, listener, informParticipants: true);
	}

	public Sequencer PlaySequence(string sequence)
	{
		return PlaySequence(sequence, null, null, informParticipants: false);
	}

	public void StopSequence(Sequencer sequencer)
	{
		if (sequencer != null)
		{
			sequencer.Close();
		}
	}

	private Sequencer GetNewSequencer()
	{
		Sequencer sequencer = base.gameObject.AddComponent<Sequencer>();
		if (sequencer != null)
		{
			sequencer.UseCamera(displaySettings.cameraSettings.sequencerCamera, displaySettings.cameraSettings.alternateCameraObject, displaySettings.cameraSettings.cameraAngles);
			sequencer.disableInternalSequencerCommands = displaySettings.cameraSettings.disableInternalSequencerCommands;
		}
		return sequencer;
	}

	public void Pause()
	{
		DialogueTime.isPaused = true;
		BroadcastDialogueSystemMessage("OnDialogueSystemPause");
	}

	public void Unpause()
	{
		DialogueTime.isPaused = false;
		BroadcastDialogueSystemMessage("OnDialogueSystemUnpause");
	}

	private void BroadcastDialogueSystemMessage(string message)
	{
		BroadcastMessage(message, SendMessageOptions.DontRequireReceiver);
		if (isConversationActive)
		{
			if (currentActor != null)
			{
				currentActor.BroadcastMessage(message, SendMessageOptions.DontRequireReceiver);
			}
			if (currentConversant != null)
			{
				currentConversant.BroadcastMessage(message, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	public void UseDialogueUI(GameObject gameObject)
	{
		m_currentDialogueUI = null;
		displaySettings.dialogueUI = gameObject;
		ValidateCurrentDialogueUI();
	}

	private IDialogueUI GetDialogueUI()
	{
		ValidateCurrentDialogueUI();
		return m_currentDialogueUI;
	}

	private void SetDialogueUI(IDialogueUI ui)
	{
		MonoBehaviour monoBehaviour = ui as MonoBehaviour;
		if (!(monoBehaviour != null))
		{
			return;
		}
		if (Tools.IsPrefab(monoBehaviour.gameObject))
		{
			GameObject gameObject;
			if (monoBehaviour is CanvasDialogueUI && monoBehaviour.GetComponentInChildren<Canvas>() == null)
			{
				Canvas orCreateDefaultCanvas = GetOrCreateDefaultCanvas();
				gameObject = UnityEngine.Object.Instantiate(monoBehaviour.gameObject, orCreateDefaultCanvas.transform, worldPositionStays: false);
				monoBehaviour = gameObject.GetComponent(typeof(IDialogueUI)) as MonoBehaviour;
			}
			else
			{
				gameObject = UnityEngine.Object.Instantiate(monoBehaviour.gameObject, base.transform);
				monoBehaviour = gameObject.GetComponent(typeof(IDialogueUI)) as MonoBehaviour;
				if (monoBehaviour is CanvasDialogueUI && monoBehaviour.GetComponentInChildren<Canvas>() == null)
				{
					Canvas orCreateDefaultCanvas2 = GetOrCreateDefaultCanvas();
					gameObject.transform.SetParent(orCreateDefaultCanvas2.transform, worldPositionStays: false);
				}
			}
			gameObject.name = monoBehaviour.gameObject.name;
			if (m_overrideDialogueUI != null)
			{
				if (m_dontDestroyOverrideUI)
				{
					m_overrideDialogueUI.ui = gameObject;
				}
				m_overrideDialogueUI = null;
			}
		}
		displaySettings.dialogueUI = monoBehaviour.gameObject;
		m_currentDialogueUI = null;
		ValidateCurrentDialogueUI();
	}

	private void ValidateCurrentDialogueUI()
	{
		if (m_currentDialogueUI == null)
		{
			GetDialogueUIFromDisplaySettings();
			if (m_currentDialogueUI == null)
			{
				m_currentDialogueUI = LoadDefaultDialogueUI();
			}
			MonoBehaviour monoBehaviour = m_currentDialogueUI as MonoBehaviour;
			if (monoBehaviour != null)
			{
				displaySettings.dialogueUI = monoBehaviour.gameObject;
			}
		}
	}

	private void GetDialogueUIFromDisplaySettings()
	{
		if (displaySettings.dialogueUI != null)
		{
			m_currentDialogueUI = (Tools.IsPrefab(displaySettings.dialogueUI) ? LoadDialogueUIPrefab(displaySettings.dialogueUI) : (displaySettings.dialogueUI.GetComponentInChildren(typeof(IDialogueUI)) as IDialogueUI));
			if (m_currentDialogueUI == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: No Dialogue UI found on '{1}'. Is the GameObject active and the dialogue UI script enabled? (Will load default UI for now.)", new object[2] { "Dialogue System", displaySettings.dialogueUI }), displaySettings.dialogueUI);
			}
		}
		else
		{
			m_currentDialogueUI = GetComponentInChildren(typeof(IDialogueUI)) as IDialogueUI;
		}
	}

	private IDialogueUI LoadDefaultDialogueUI()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Loading default Dialogue UI '{1}'.", new object[2] { "Dialogue System", "Default Dialogue UI" }));
		}
		GameObject gameObject = DialogueManager.LoadAsset("Default Dialogue UI") as GameObject;
		if (gameObject == null)
		{
			Debug.LogError("Dialogue System: Can't load Default Dialogue UI! Did you delete this file from Dialogue System/Prefabs/Legacy Unity GUI Prefabs/Default/Resources? If so, add it back, or assign a dialogue UI to the Dialogue Manager so it doesn't have to try to load the default UI.");
			return null;
		}
		return LoadDialogueUIPrefab(gameObject);
	}

	private IDialogueUI LoadDialogueUIPrefab(GameObject prefab)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.name = prefab.name;
		IDialogueUI dialogueUI = null;
		if (gameObject != null)
		{
			dialogueUI = gameObject.GetComponentInChildren(typeof(IDialogueUI)) as IDialogueUI;
			if (dialogueUI == null)
			{
				if (DialogueDebug.logErrors)
				{
					Debug.LogError(string.Format("{0}: No Dialogue UI component found on {1}.", new object[2] { "Dialogue System", prefab }));
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			else if (dialogueUI is CanvasDialogueUI)
			{
				Canvas orCreateDefaultCanvas = GetOrCreateDefaultCanvas();
				gameObject.transform.SetParent(orCreateDefaultCanvas.transform, worldPositionStays: false);
			}
			else
			{
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			}
		}
		if (m_overrideDialogueUI != null)
		{
			if (m_dontDestroyOverrideUI)
			{
				m_overrideDialogueUI.ui = gameObject;
			}
			m_overrideDialogueUI = null;
		}
		return dialogueUI;
	}

	private Canvas GetOrCreateDefaultCanvas()
	{
		if (displaySettings.defaultCanvas != null)
		{
			return displaySettings.defaultCanvas;
		}
		Canvas canvas = GetComponentInChildren<Canvas>();
		if (canvas == null)
		{
			GameObject obj = new GameObject("Canvas");
			obj.layer = 5;
			obj.AddComponent<Canvas>();
			obj.AddComponent<CanvasScaler>();
			obj.AddComponent<GraphicRaycaster>();
			canvas = obj.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			obj.transform.SetParent(base.transform);
		}
		return canvas;
	}

	public void SetDialoguePanel(bool show, bool immediate = false)
	{
		if (conversationView == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: SetDialoguePanel() is only valid when a conversation is active.");
			}
		}
		else
		{
			conversationView.sequencer.SetDialoguePanel(show, immediate);
		}
	}

	private void RegisterLuaFunctions()
	{
		Lua.UnregisterFunction("RandomizeNextEntry");
		Lua.UnregisterFunction("UpdateTracker");
		Lua.RegisterFunction("ShowAlert", null, SymbolExtensions.GetMethodInfo(() => LuaShowAlert(string.Empty)));
		Lua.RegisterFunction("HideAlert", null, SymbolExtensions.GetMethodInfo(() => LuaHideAlert()));
		Lua.RegisterFunction("RandomizeNextEntry", this, SymbolExtensions.GetMethodInfo(() => RandomizeNextEntry()));
		Lua.RegisterFunction("UpdateTracker", this, SymbolExtensions.GetMethodInfo(() => SendUpdateTracker()));
		Lua.RegisterFunction("GetEntryText", null, SymbolExtensions.GetMethodInfo(() => GetEntryText(0.0, string.Empty)));
		Lua.RegisterFunction("GetEntryBool", null, SymbolExtensions.GetMethodInfo(() => GetEntryBool(0.0, string.Empty)));
		Lua.RegisterFunction("GetEntryNumber", null, SymbolExtensions.GetMethodInfo(() => GetEntryNumber(0.0, string.Empty)));
		Lua.RegisterFunction("Conditional", null, SymbolExtensions.GetMethodInfo(() => Conditional(condition: false, string.Empty)));
		Lua.RegisterFunction("ChangeActorName", this, SymbolExtensions.GetMethodInfo(() => ChangeActorName(string.Empty, string.Empty)));
		Lua.RegisterFunction("ActorIDToName", this, SymbolExtensions.GetMethodInfo(() => ActorIDToName(0.0)));
		Lua.RegisterFunction("ItemIDToName", this, SymbolExtensions.GetMethodInfo(() => ItemIDToName(0.0)));
		Lua.RegisterFunction("QuestIDToName", this, SymbolExtensions.GetMethodInfo(() => ItemIDToName(0.0)));
		Lua.RegisterFunction("GetTextTableValue", this, SymbolExtensions.GetMethodInfo(() => GetLocalizedText(string.Empty)));
		DialogueLua.RegisterLuaFunctions();
	}

	private void UnregisterLuaFunctions()
	{
		Lua.UnregisterFunction("ShowAlert");
		Lua.UnregisterFunction("HideAlert");
		Lua.UnregisterFunction("RandomizeNextEntry");
		Lua.UnregisterFunction("UpdateTracker");
		Lua.UnregisterFunction("GetEntryText");
		Lua.UnregisterFunction("GetEntryBool");
		Lua.UnregisterFunction("GetEntryNumber");
		Lua.UnregisterFunction("Conditional");
		Lua.UnregisterFunction("ChangeActorName");
		Lua.UnregisterFunction("ActorIDToName");
		Lua.UnregisterFunction("ItemIDToName");
		Lua.UnregisterFunction("QuestIDToName");
	}

	public void SendUpdateTracker()
	{
		BroadcastMessage("UpdateTracker", SendMessageOptions.DontRequireReceiver);
	}

	public static void LuaShowAlert(string message)
	{
		DialogueManager.ShowAlert(message);
	}

	public static void LuaHideAlert()
	{
		DialogueManager.HideAlert();
	}

	private static DialogueEntry GetDialogueEntryInCurrentConversation(double entryID)
	{
		if (!DialogueManager.isConversationActive)
		{
			return null;
		}
		return DialogueManager.masterDatabase.GetDialogueEntry(DialogueManager.lastConversationID, (int)entryID);
	}

	private static string GetEntryText(double entryID, string fieldName)
	{
		DialogueEntry dialogueEntryInCurrentConversation = GetDialogueEntryInCurrentConversation(entryID);
		if (dialogueEntryInCurrentConversation == null)
		{
			return string.Empty;
		}
		return Field.LookupValue(dialogueEntryInCurrentConversation.fields, fieldName);
	}

	private static bool GetEntryBool(double entryID, string fieldName)
	{
		DialogueEntry dialogueEntryInCurrentConversation = GetDialogueEntryInCurrentConversation(entryID);
		if (dialogueEntryInCurrentConversation == null)
		{
			return false;
		}
		return Field.LookupBool(dialogueEntryInCurrentConversation.fields, fieldName);
	}

	private static double GetEntryNumber(double entryID, string fieldName)
	{
		DialogueEntry dialogueEntryInCurrentConversation = GetDialogueEntryInCurrentConversation(entryID);
		return (dialogueEntryInCurrentConversation != null) ? Field.LookupFloat(dialogueEntryInCurrentConversation.fields, fieldName) : 0f;
	}

	public void RandomizeNextEntry()
	{
		m_calledRandomizeNextEntry = true;
		if (conversationController != null)
		{
			conversationController.randomizeNextEntry = true;
		}
	}

	public static string Conditional(bool condition, string value)
	{
		if (!condition)
		{
			return string.Empty;
		}
		return value;
	}

	public static void ChangeActorName(string actorName, string newDisplayName)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Changing " + actorName + "'s Display Name to " + newDisplayName);
		}
		DialogueLua.SetActorField(actorName, "Display Name", newDisplayName);
		if (!DialogueManager.isConversationActive)
		{
			return;
		}
		Actor actor = DialogueManager.MasterDatabase.GetActor(actorName);
		if (actor == null)
		{
			return;
		}
		CharacterInfo characterInfo = DialogueManager.ConversationModel.GetCharacterInfo(actor.id);
		if (characterInfo != null)
		{
			characterInfo.Name = newDisplayName;
		}
		StandardUISubtitlePanel[] subtitlePanels = DialogueManager.standardDialogueUI.conversationUIElements.subtitlePanels;
		foreach (StandardUISubtitlePanel standardUISubtitlePanel in subtitlePanels)
		{
			if (standardUISubtitlePanel.portraitActorName == actorName)
			{
				if (standardUISubtitlePanel.portraitName.gameObject != null)
				{
					standardUISubtitlePanel.portraitName.text = newDisplayName;
				}
				break;
			}
		}
	}

	public static string ActorIDToName(double id)
	{
		return DialogueManager.masterDatabase.GetActor((int)id)?.Name;
	}

	public static string ItemIDToName(double id)
	{
		return DialogueManager.masterDatabase.GetItem((int)id)?.Name;
	}

	public void AddLuaObserver(string luaExpression, LuaWatchFrequency frequency, LuaChangedDelegate luaChangedHandler)
	{
		StartCoroutine(AddLuaObserverAfterStart(luaExpression, frequency, luaChangedHandler));
	}

	private IEnumerator AddLuaObserverAfterStart(string luaExpression, LuaWatchFrequency frequency, LuaChangedDelegate luaChangedHandler)
	{
		int MaxFramesToWait = 10;
		int framesWaited = 0;
		while (!m_started && framesWaited < MaxFramesToWait)
		{
			framesWaited++;
			yield return null;
		}
		yield return null;
		m_luaWatchers.AddObserver(luaExpression, frequency, luaChangedHandler);
	}

	public void RemoveLuaObserver(string luaExpression, LuaWatchFrequency frequency, LuaChangedDelegate luaChangedHandler)
	{
		m_luaWatchers.RemoveObserver(luaExpression, frequency, luaChangedHandler);
	}

	public void RemoveAllObservers(LuaWatchFrequency frequency)
	{
		m_luaWatchers.RemoveAllObservers(frequency);
	}

	public void RemoveAllObservers()
	{
		m_luaWatchers.RemoveAllObservers();
	}

	private void Update()
	{
		if (Lua.wasInvoked)
		{
			m_luaWatchers.NotifyObservers(LuaWatchFrequency.EveryUpdate);
			Lua.wasInvoked = false;
		}
	}

	private void UpdateTracker()
	{
		this.receivedUpdateTracker();
	}

	public void RegisterAssetBundle(AssetBundle bundle)
	{
		m_assetBundleManager.RegisterAssetBundle(bundle);
	}

	public void UnregisterAssetBundle(AssetBundle bundle)
	{
		m_assetBundleManager.UnregisterAssetBundle(bundle);
	}

	public UnityEngine.Object LoadAsset(string name)
	{
		UnityEngine.Object obj = m_assetBundleManager.Load(name);
		if (obj != null)
		{
			return obj;
		}
		return null;
	}

	public UnityEngine.Object LoadAsset(string name, Type type)
	{
		UnityEngine.Object obj = m_assetBundleManager.Load(name, type);
		if (obj != null)
		{
			return obj;
		}
		return null;
	}

	protected void AddDelegateForAssetBeingLoaded(string name, AssetLoadedDelegate assetLoaded)
	{
		if (!m_assetsBeingLoaded.ContainsKey(name))
		{
			m_assetsBeingLoaded.Add(name, new List<AssetLoadedDelegate>());
		}
		m_assetsBeingLoaded[name].Add(assetLoaded);
	}

	protected void RemoveDelegatesForAssetBeingLoaded(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			if (m_assetsBeingLoaded.Count > 0)
			{
				m_assetsBeingLoaded.Remove(m_assetsBeingLoaded.First().Key);
			}
		}
		else
		{
			m_assetsBeingLoaded.Remove(name);
		}
	}

	protected void CallDelegatesForAssetBeingLoaded(string name, UnityEngine.Object asset)
	{
		List<AssetLoadedDelegate> value = null;
		if (string.IsNullOrEmpty(name))
		{
			if (m_assetsBeingLoaded.Count > 0)
			{
				value = m_assetsBeingLoaded.First().Value;
			}
		}
		else if (!m_assetsBeingLoaded.TryGetValue(name, out value))
		{
			value = null;
		}
		if (value != null)
		{
			int count = value.Count;
			for (int i = 0; i < count; i++)
			{
				value[i](asset);
			}
		}
	}

	public void LoadAsset(string name, Type type, AssetLoadedDelegate assetLoaded)
	{
		UnityEngine.Object obj = m_assetBundleManager.Load(name, type);
		if (obj != null)
		{
			assetLoaded(obj);
		}
		else
		{
			assetLoaded(null);
		}
	}

	public void UnloadAsset(object obj)
	{
	}

	public void ClearLoadedAssetHashes()
	{
	}

	public void UnloadAssets()
	{
	}
}
