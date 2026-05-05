using UnityEngine;

namespace PixelCrushers;

public abstract class Saver : MonoBehaviour
{
	[Tooltip("Save data under this key. If blank, use GameObject name.")]
	[SerializeField]
	private string m_key;

	[Tooltip("Append the name of this saver type to the key.")]
	[SerializeField]
	private bool m_appendSaverTypeToKey;

	[Tooltip("Save when changing scenes to be able to restore saved state when returning to scene.")]
	[SerializeField]
	private bool m_saveAcrossSceneChanges = true;

	[Tooltip("When starting, restore this saver's state from current saved game data. Normally the save system restores state when loading games or changing scenes without this checkbox.")]
	[SerializeField]
	private bool m_restoreStateOnStart;

	protected string m_runtimeKey;

	public bool appendSaverTypeToKey
	{
		get
		{
			return m_appendSaverTypeToKey;
		}
		set
		{
			m_appendSaverTypeToKey = value;
		}
	}

	public virtual string key
	{
		get
		{
			if (string.IsNullOrEmpty(m_runtimeKey))
			{
				m_runtimeKey = ((!string.IsNullOrEmpty(m_key)) ? m_key : base.name);
				if (appendSaverTypeToKey)
				{
					string text = GetType().Name;
					if (text.EndsWith("Saver"))
					{
						text.Remove(text.Length - "Saver".Length);
					}
					m_runtimeKey += text;
				}
			}
			return m_runtimeKey;
		}
		set
		{
			m_key = value;
			m_runtimeKey = value;
		}
	}

	public string _internalKeyValue
	{
		get
		{
			return m_key;
		}
		set
		{
			m_key = value;
		}
	}

	public virtual bool saveAcrossSceneChanges
	{
		get
		{
			return m_saveAcrossSceneChanges;
		}
		set
		{
			m_saveAcrossSceneChanges = value;
		}
	}

	public virtual bool restoreStateOnStart
	{
		get
		{
			return m_restoreStateOnStart;
		}
		set
		{
			m_restoreStateOnStart = value;
		}
	}

	public virtual void Awake()
	{
	}

	public virtual void Start()
	{
		if (restoreStateOnStart)
		{
			ApplyData(SaveSystem.currentSavedGameData.GetData(key));
		}
	}

	public virtual void Reset()
	{
	}

	public virtual void OnEnable()
	{
		SaveSystem.RegisterSaver(this);
	}

	public virtual void OnDisable()
	{
		SaveSystem.UnregisterSaver(this);
	}

	public virtual void OnDestroy()
	{
	}

	public abstract string RecordData();

	public abstract void ApplyData(string s);

	public virtual void ApplyDataImmediate()
	{
	}

	public virtual void OnBeforeSceneChange()
	{
	}

	public virtual void OnRestartGame()
	{
	}
}
