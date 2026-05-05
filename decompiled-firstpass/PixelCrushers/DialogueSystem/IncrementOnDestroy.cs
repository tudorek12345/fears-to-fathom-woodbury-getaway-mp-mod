using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class IncrementOnDestroy : MonoBehaviour
{
	public enum IncrementOn
	{
		Destroy,
		Disable,
		Manually
	}

	[Tooltip("Increment on Destroy or Disable.")]
	public IncrementOn incrementOn;

	[Tooltip("Increment this Dialogue System variable.")]
	[VariablePopup(true)]
	public string variable = string.Empty;

	[Tooltip("Increment the variable by this amount. Use a negative value to decrement.")]
	public int increment = 1;

	[Tooltip("After incrementing, ensure that the variable is at least this value.")]
	public int min;

	[Tooltip("After incrementing, ensure that the variable is no more than this value.")]
	public int max = 100;

	[Tooltip("Optional alert message to show when incrementing.")]
	public string alertMessage = string.Empty;

	[Tooltip("Duration to show alert, or 0 to use default duration.")]
	public float alertDuration;

	[Tooltip("If set, only increment if the conditions are true.")]
	public Condition condition = new Condition();

	public UnityEvent onIncrement = new UnityEvent();

	protected bool listenForOnDestroy;

	protected bool awakeMarkedForDestroy;

	protected virtual string actualVariableName
	{
		get
		{
			if (!string.IsNullOrEmpty(variable))
			{
				return variable;
			}
			return DialogueActor.GetPersistentDataName(base.transform);
		}
	}

	protected string ActualVariableName => actualVariableName;

	protected virtual void Awake()
	{
		DestructibleSaver component = GetComponent<DestructibleSaver>();
		if (!(component != null) || !(GameObjectUtility.FindFirstObjectByType<SaveSystem>() != null) || SaveSystem.currentSavedGameData == null)
		{
			return;
		}
		string key = component.key;
		string data = SaveSystem.currentSavedGameData.GetData(key);
		if (!string.IsNullOrEmpty(data))
		{
			DestructibleSaver.DestructibleData destructibleData = SaveSystem.Deserialize<DestructibleSaver.DestructibleData>(data);
			if (destructibleData != null && destructibleData.destroyed)
			{
				listenForOnDestroy = false;
				awakeMarkedForDestroy = true;
			}
		}
	}

	public virtual void OnEnable()
	{
		listenForOnDestroy = !awakeMarkedForDestroy;
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	public virtual void OnLevelWillBeUnloaded()
	{
		listenForOnDestroy = false;
	}

	public virtual void OnApplicationQuit()
	{
		listenForOnDestroy = false;
	}

	public virtual void OnDestroy()
	{
		if (incrementOn == IncrementOn.Destroy)
		{
			TryIncrement();
		}
	}

	public virtual void OnDisable()
	{
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
		if (incrementOn == IncrementOn.Disable)
		{
			TryIncrement();
		}
	}

	public virtual void TryIncrement()
	{
		if (CanIncrement())
		{
			IncrementNow();
		}
	}

	protected virtual bool CanIncrement()
	{
		if (Application.isPlaying && listenForOnDestroy && DialogueManager.Instance != null && DialogueManager.DatabaseManager != null && DialogueManager.MasterDatabase != null)
		{
			return condition.IsTrue(null);
		}
		return false;
	}

	protected virtual void IncrementNow()
	{
		int num = Mathf.Clamp(DialogueLua.GetVariable(actualVariableName).asInt + increment, min, max);
		DialogueLua.SetVariable(actualVariableName, num);
		DialogueManager.SendUpdateTracker();
		if (!string.IsNullOrEmpty(alertMessage) && !(DialogueManager.instance == null))
		{
			if (Mathf.Approximately(0f, alertDuration))
			{
				DialogueManager.ShowAlert(alertMessage);
			}
			else
			{
				DialogueManager.ShowAlert(alertMessage, alertDuration);
			}
		}
		onIncrement.Invoke();
	}
}
