using System;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class DestructibleSaver : Saver
{
	[Serializable]
	public class DestructibleData
	{
		public bool destroyed;

		public Vector3 position;
	}

	public enum Mode
	{
		OnDisable,
		OnDestroy
	}

	public enum DestroyMode
	{
		Destroy,
		Deactivate
	}

	[Tooltip("Event to watch for.")]
	[SerializeField]
	private Mode m_mode = Mode.OnDestroy;

	[Tooltip("How to re-destroy object.")]
	[SerializeField]
	private DestroyMode m_destroyMode;

	[Tooltip("Instantiate this if already destroyed when loading game or scene.")]
	[SerializeField]
	private GameObject m_destroyedVersionPrefab;

	private DestructibleData m_data = new DestructibleData();

	private bool m_ignoreOnDestroy;

	public Mode mode
	{
		get
		{
			return m_mode;
		}
		set
		{
			m_mode = value;
		}
	}

	public DestroyMode destroyMode
	{
		get
		{
			return m_destroyMode;
		}
		set
		{
			m_destroyMode = value;
		}
	}

	public GameObject destroyedVersionPrefab
	{
		get
		{
			return m_destroyedVersionPrefab;
		}
		set
		{
			m_destroyedVersionPrefab = value;
		}
	}

	public override void OnBeforeSceneChange()
	{
		base.OnBeforeSceneChange();
		m_ignoreOnDestroy = true;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (m_mode == Mode.OnDisable)
		{
			RecordDestruction();
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (m_mode == Mode.OnDestroy)
		{
			RecordDestruction();
		}
	}

	public virtual void RecordDestruction()
	{
		if (!m_ignoreOnDestroy && SaveSystem.instance != null)
		{
			m_data.destroyed = true;
			m_data.position = base.transform.position;
			SaveSystem.UpdateSaveData(this, SaveSystem.Serialize(m_data));
		}
		m_ignoreOnDestroy = false;
	}

	public override string RecordData()
	{
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		DestructibleData destructibleData = SaveSystem.Deserialize(s, m_data);
		if (destructibleData == null)
		{
			return;
		}
		m_data = destructibleData;
		if (destructibleData.destroyed)
		{
			if (destroyedVersionPrefab != null)
			{
				UnityEngine.Object.Instantiate(destroyedVersionPrefab, destructibleData.position, base.transform.rotation);
			}
			switch (destroyMode)
			{
			case DestroyMode.Destroy:
				UnityEngine.Object.Destroy(base.gameObject);
				break;
			case DestroyMode.Deactivate:
				base.gameObject.SetActive(value: false);
				break;
			}
		}
	}
}
