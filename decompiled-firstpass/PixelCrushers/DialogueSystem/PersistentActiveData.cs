using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PersistentActiveData : MonoBehaviour
{
	[Tooltip("The GameObject to set active or inactive based on the Condition below.")]
	public GameObject target;

	[Tooltip("If true, Target is activated; otherwise deactivated.")]
	public Condition condition;

	[Tooltip("When script starts, check condition & set target GameObject active/inactive. Otherwise it only checks when a game is loaded or entering from another scene.")]
	public bool checkOnStart;

	protected virtual void Start()
	{
		if (checkOnStart)
		{
			Check();
		}
	}

	protected virtual void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	protected virtual void OnDisable()
	{
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
	}

	public void OnApplyPersistentData()
	{
		Check();
	}

	public virtual void Check()
	{
		if (!base.enabled)
		{
			return;
		}
		if (target == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: No target is assigned to Persistent Active Data component on " + base.name + ".", this);
			}
		}
		else
		{
			target.SetActive(condition.IsTrue(null));
		}
	}
}
