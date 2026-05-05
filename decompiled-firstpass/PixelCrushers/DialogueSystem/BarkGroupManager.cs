using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkGroupManager : MonoBehaviour
{
	private class BarkRequest
	{
		public BarkGroupMember member;

		public Transform listener;

		public string conversation;

		public BarkHistory barkHistory;

		public int entryID;

		public string barkText;

		public string sequence;

		public AbstractBarkUI barkUI;

		public float delayTime;

		public bool isPlaying;

		public BarkRequest(string conversation, BarkGroupMember member, Transform listener, BarkHistory barkHistory, int entryID = -1, float delayTime = -1f)
		{
			this.member = member;
			this.listener = listener;
			this.conversation = conversation;
			this.barkHistory = barkHistory;
			this.entryID = entryID;
			barkText = null;
			sequence = null;
			barkUI = GetBarkUI(member);
			this.delayTime = GetDelayTime(member, delayTime);
		}

		public BarkRequest(string barkText, BarkGroupMember member, Transform listener, string sequence, float delayTime = -1f)
		{
			this.member = member;
			this.listener = listener;
			conversation = null;
			barkHistory = null;
			entryID = -1;
			this.barkText = barkText;
			this.sequence = sequence;
			barkUI = GetBarkUI(member);
			this.delayTime = GetDelayTime(member, delayTime);
		}

		private AbstractBarkUI GetBarkUI(BarkGroupMember member)
		{
			if (member == null)
			{
				return null;
			}
			DialogueActor componentInChildren = member.GetComponentInChildren<DialogueActor>();
			if (componentInChildren != null && componentInChildren.barkUISettings.barkUI != null)
			{
				return componentInChildren.barkUISettings.barkUI;
			}
			return member.GetComponentInChildren<AbstractBarkUI>();
		}

		private float GetDelayTime(BarkGroupMember member, float delayTime)
		{
			if (delayTime >= 0f)
			{
				return delayTime;
			}
			if (!(member == null))
			{
				return Random.Range(member.minDelayBetweenQueuedBarks, member.maxDelayBetweenQueuedBarks);
			}
			return 0f;
		}
	}

	public BarkGroupQueueLimitMode queueLimitMode;

	[Tooltip("Only used if mode is Stop At Limit or Drop Oldest At Limit")]
	public int queueLimit = 256;

	private static bool s_applicationIsQuitting;

	private static BarkGroupManager s_instance;

	public Dictionary<string, HashSet<BarkGroupMember>> groups = new Dictionary<string, HashSet<BarkGroupMember>>();

	private Dictionary<string, Queue<BarkRequest>> queues = new Dictionary<string, Queue<BarkRequest>>();

	public static BarkGroupManager instance
	{
		get
		{
			if (s_applicationIsQuitting)
			{
				return null;
			}
			if (s_instance == null)
			{
				s_instance = DialogueManager.instance.GetComponent<BarkGroupManager>();
				if (s_instance == null)
				{
					s_instance = DialogueManager.instance.gameObject.AddComponent<BarkGroupManager>();
				}
			}
			return s_instance;
		}
	}

	private void OnApplicationQuit()
	{
		s_applicationIsQuitting = true;
	}

	public void AddToGroup(string groupId, BarkGroupMember member)
	{
		if (!string.IsNullOrEmpty(groupId) && !(member == null))
		{
			if (!groups.ContainsKey(groupId))
			{
				groups.Add(groupId, new HashSet<BarkGroupMember>());
			}
			groups[groupId].Add(member);
		}
	}

	public void RemoveFromGroup(string groupId, BarkGroupMember member)
	{
		if (!string.IsNullOrEmpty(groupId) && !(member == null) && groups.ContainsKey(groupId) && groups[groupId].Contains(member))
		{
			groups[groupId].Remove(member);
			if (groups[groupId].Count == 0)
			{
				groups.Remove(groupId);
			}
		}
	}

	public void CancelAllBarks()
	{
		foreach (Queue<BarkRequest> value in queues.Values)
		{
			value.Clear();
		}
		foreach (HashSet<BarkGroupMember> value2 in groups.Values)
		{
			foreach (BarkGroupMember item in value2)
			{
				if (item != null)
				{
					item.CancelBark();
				}
			}
		}
	}

	public void MutexBark(string groupId, BarkGroupMember member)
	{
		if (string.IsNullOrEmpty(groupId) || !groups.ContainsKey(groupId))
		{
			return;
		}
		foreach (BarkGroupMember item in groups[groupId])
		{
			if (!(item == member))
			{
				item.CancelBark();
			}
		}
	}

	public void GroupBark(string conversation, BarkGroupMember member, Transform listener, BarkHistory barkHistory, float delayTime = 0f)
	{
		if (member == null || !member.queueBarks)
		{
			DialogueManager.Bark(conversation, (member != null) ? member.transform : null, listener, barkHistory);
		}
		else
		{
			Enqueue(new BarkRequest(conversation, member, listener, barkHistory, -1, delayTime));
		}
	}

	public void GroupBark(string conversation, BarkGroupMember member, Transform listener, int entryID, float delayTime = 0f)
	{
		if (member == null || !member.queueBarks)
		{
			DialogueManager.Bark(conversation, (member != null) ? member.transform : null, listener, entryID);
		}
		else
		{
			Enqueue(new BarkRequest(conversation, member, listener, null, entryID, delayTime));
		}
	}

	public void GroupBarkString(string barkText, BarkGroupMember member, Transform listener, string sequence, float delayTime = 0f)
	{
		if (member == null || !member.queueBarks)
		{
			DialogueManager.BarkString(barkText, (member != null) ? member.transform : null, listener, sequence);
		}
		else
		{
			Enqueue(new BarkRequest(barkText, member, listener, sequence, delayTime));
		}
	}

	private void Enqueue(BarkRequest barkRequest)
	{
		BarkGroupMember member = barkRequest.member;
		if (member.evaluateIdEveryBark)
		{
			member.UpdateMembership();
		}
		string currentIdValue = member.currentIdValue;
		if (!queues.ContainsKey(currentIdValue))
		{
			queues.Add(currentIdValue, new Queue<BarkRequest>());
		}
		Queue<BarkRequest> queue = queues[currentIdValue];
		if (queueLimitMode != BarkGroupQueueLimitMode.NoLimit && queue.Count > queueLimit)
		{
			switch (queueLimitMode)
			{
			case BarkGroupQueueLimitMode.StopAtLimit:
				return;
			case BarkGroupQueueLimitMode.DropOldestAtLimit:
				queue.Dequeue();
				break;
			}
		}
		queue.Enqueue(barkRequest);
		if (queue.Count == 1)
		{
			barkRequest.delayTime = 0f;
		}
	}

	private void Update()
	{
		Dictionary<string, Queue<BarkRequest>>.Enumerator enumerator = queues.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Queue<BarkRequest> value = enumerator.Current.Value;
			if (value.Count == 0)
			{
				continue;
			}
			BarkRequest barkRequest = value.Peek();
			if (!barkRequest.isPlaying)
			{
				barkRequest.delayTime -= Time.deltaTime;
				if (!(barkRequest.delayTime <= 0f))
				{
					continue;
				}
				if (barkRequest.member == null || barkRequest.barkUI == null || (string.IsNullOrEmpty(barkRequest.conversation) && string.IsNullOrEmpty(barkRequest.barkText)))
				{
					value.Dequeue();
				}
				else if (!string.IsNullOrEmpty(barkRequest.conversation))
				{
					if (barkRequest.entryID == -1)
					{
						DialogueManager.Bark(barkRequest.conversation, barkRequest.member.transform, barkRequest.listener, barkRequest.barkHistory);
					}
					else
					{
						DialogueManager.Bark(barkRequest.conversation, barkRequest.member.transform, barkRequest.listener, barkRequest.entryID);
					}
				}
				else
				{
					DialogueManager.BarkString(barkRequest.barkText, barkRequest.member.transform, barkRequest.listener, barkRequest.sequence);
				}
				barkRequest.isPlaying = true;
				barkRequest.delayTime = 0.5f;
			}
			else
			{
				barkRequest.delayTime -= Time.deltaTime;
				if (barkRequest.delayTime <= 0f && !barkRequest.barkUI.isPlaying)
				{
					value.Dequeue();
				}
			}
		}
	}
}
