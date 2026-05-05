using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkGroupMember : MonoBehaviour
{
	[Tooltip("Member of this group. Can be a Lua expression.")]
	public string groupId;

	[Tooltip("Evaluate Group Id before every bark. Useful if Id is a Lua expression that can change value.")]
	public bool evaluateIdEveryBark;

	[Tooltip("When another group member forces this member's bark to hide, delay this many seconds before hiding.")]
	public float forcedHideDelay;

	[Tooltip("If another group member is barking, wait in a queue to bark instead of cancelling the other member's bark.")]
	public bool queueBarks;

	public float minDelayBetweenQueuedBarks;

	public float maxDelayBetweenQueuedBarks = 1f;

	[Tooltip("Hide bark when conversations start.")]
	public bool hideBarkOnConversationStart;

	private string m_currentIdValue = string.Empty;

	private IBarkUI m_barkUI;

	private bool m_applicationIsQuitting;

	public string currentIdValue => m_currentIdValue;

	private IBarkUI barkUI
	{
		get
		{
			if (m_barkUI == null)
			{
				m_barkUI = GetComponentInChildren(typeof(IBarkUI)) as IBarkUI;
			}
			return m_barkUI;
		}
	}

	protected virtual void Awake()
	{
		m_currentIdValue = groupId;
	}

	protected virtual void Start()
	{
		if (!string.IsNullOrEmpty(groupId))
		{
			BarkGroupManager.instance.AddToGroup(groupId, this);
		}
	}

	private void OnApplicationQuit()
	{
		m_applicationIsQuitting = true;
	}

	private void OnEnable()
	{
		if (hideBarkOnConversationStart)
		{
			DialogueManager.instance.conversationStarted += OnConversationStarted;
		}
	}

	private void OnDisable()
	{
		if (!m_applicationIsQuitting && !(BarkGroupManager.instance == null))
		{
			BarkGroupManager.instance.RemoveFromGroup(m_currentIdValue, this);
			if (hideBarkOnConversationStart)
			{
				DialogueManager.instance.conversationStarted -= OnConversationStarted;
			}
		}
	}

	private void OnConversationStarted(Transform actor)
	{
		CancelBark();
	}

	public void GroupBark(string conversation, Transform listener, BarkHistory barkHistory, float delayTime = -1f)
	{
		BarkGroupManager.instance.GroupBark(conversation, this, listener, barkHistory, delayTime);
	}

	public void GroupBark(string conversation, Transform listener, int entryID, float delayTime = -1f)
	{
		BarkGroupManager.instance.GroupBark(conversation, this, listener, entryID, delayTime);
	}

	public void GroupBarkString(string barkText, Transform listener, string sequence, float delayTime = -1f)
	{
		BarkGroupManager.instance.GroupBarkString(barkText, this, listener, sequence, delayTime);
	}

	private void OnBarkStart(Transform listener)
	{
		if (string.IsNullOrEmpty(m_currentIdValue) || evaluateIdEveryBark)
		{
			UpdateMembership();
		}
		BarkGroupManager.instance.MutexBark(m_currentIdValue, this);
	}

	public void UpdateMembership()
	{
		string asString = Lua.Run("return " + groupId, DialogueDebug.logInfo, allowExceptions: false).asString;
		if (string.Equals(asString, "nil"))
		{
			asString = groupId;
		}
		if (asString != m_currentIdValue)
		{
			BarkGroupManager.instance.RemoveFromGroup(m_currentIdValue, this);
			BarkGroupManager.instance.AddToGroup(asString, this);
			m_currentIdValue = asString;
		}
	}

	public void CancelBark()
	{
		if (barkUI != null && barkUI.isPlaying)
		{
			CancelInvoke("HideBarkNow");
			Invoke("HideBarkNow", forcedHideDelay);
		}
	}

	private void HideBarkNow()
	{
		if (barkUI == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Didn't find a bark UI on " + base.name, this);
			}
		}
		else if (barkUI.isPlaying)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Hiding bark on " + base.name, this);
			}
			barkUI.Hide();
		}
	}
}
