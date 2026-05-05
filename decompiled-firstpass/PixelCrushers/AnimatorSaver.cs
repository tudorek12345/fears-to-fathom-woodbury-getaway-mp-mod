using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
[RequireComponent(typeof(Animator))]
public class AnimatorSaver : Saver
{
	[Serializable]
	public class LayerData
	{
		public int hash;

		public float time;
	}

	[Serializable]
	public class TriggerData
	{
		public string name;

		public bool isTriggered;
	}

	[Serializable]
	public class Data
	{
		public LayerData[] layers;

		public List<bool> bools = new List<bool>();

		public List<float> floats = new List<float>();

		public List<int> ints = new List<int>();

		public List<string> strings = new List<string>();

		public List<TriggerData> triggers = new List<TriggerData>();
	}

	private Data m_data = new Data();

	private Animator m_animator;

	private Animator animator
	{
		get
		{
			if (m_animator == null)
			{
				m_animator = GetComponent<Animator>();
			}
			return m_animator;
		}
	}

	private void CheckAnimator()
	{
		if (animator == null)
		{
			return;
		}
		if (m_data == null)
		{
			m_data = new Data();
		}
		if (m_data.layers == null || m_data.layers.Length != animator.layerCount)
		{
			m_data.layers = new LayerData[animator.layerCount];
			for (int i = 0; i < animator.layerCount; i++)
			{
				m_data.layers[i] = new LayerData();
			}
		}
	}

	public override string RecordData()
	{
		if (animator == null)
		{
			return string.Empty;
		}
		CheckAnimator();
		for (int i = 0; i < animator.layerCount; i++)
		{
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(i);
			m_data.layers[i].hash = currentAnimatorStateInfo.fullPathHash;
			m_data.layers[i].time = currentAnimatorStateInfo.normalizedTime;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < animator.parameterCount; j++)
		{
			AnimatorControllerParameter animatorControllerParameter = animator.parameters[j];
			switch (animatorControllerParameter.type)
			{
			case AnimatorControllerParameterType.Bool:
			{
				bool flag = animator.GetBool(animatorControllerParameter.name);
				if (num < m_data.bools.Count)
				{
					m_data.bools[num] = flag;
				}
				else
				{
					m_data.bools.Add(flag);
				}
				num++;
				break;
			}
			case AnimatorControllerParameterType.Float:
			{
				float num4 = animator.GetFloat(animatorControllerParameter.name);
				if (num2 < m_data.floats.Count)
				{
					m_data.floats[num2] = num4;
				}
				else
				{
					m_data.floats.Add(num4);
				}
				num2++;
				break;
			}
			case AnimatorControllerParameterType.Int:
			{
				int integer = animator.GetInteger(animatorControllerParameter.name);
				if (num3 < m_data.ints.Count)
				{
					m_data.ints[num3] = integer;
				}
				else
				{
					m_data.ints.Add(integer);
				}
				num3++;
				break;
			}
			case AnimatorControllerParameterType.Trigger:
			{
				bool isTriggered = animator.GetCurrentAnimatorStateInfo(0).IsName(animatorControllerParameter.name);
				m_data.triggers.Add(new TriggerData
				{
					isTriggered = isTriggered,
					name = animatorControllerParameter.name
				});
				break;
			}
			}
		}
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (string.IsNullOrEmpty(s) || animator == null)
		{
			return;
		}
		m_data = SaveSystem.Deserialize(s, m_data);
		if (m_data == null)
		{
			m_data = new Data();
		}
		else
		{
			if (m_data.layers == null)
			{
				return;
			}
			for (int i = 0; i < animator.layerCount; i++)
			{
				if (i < m_data.layers.Length)
				{
					animator.Play(m_data.layers[i].hash, i, m_data.layers[i].time);
				}
			}
			foreach (TriggerData trigger in m_data.triggers)
			{
				if (trigger.isTriggered)
				{
					animator.SetTrigger(trigger.name);
				}
				else
				{
					animator.ResetTrigger(trigger.name);
				}
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int j = 0; j < animator.parameterCount; j++)
			{
				AnimatorControllerParameter animatorControllerParameter = animator.parameters[j];
				switch (animatorControllerParameter.type)
				{
				case AnimatorControllerParameterType.Bool:
					if (num < m_data.bools.Count)
					{
						animator.SetBool(animatorControllerParameter.name, m_data.bools[num++]);
					}
					break;
				case AnimatorControllerParameterType.Float:
					if (num2 < m_data.floats.Count)
					{
						animator.SetFloat(animatorControllerParameter.name, m_data.floats[num2++]);
					}
					break;
				case AnimatorControllerParameterType.Int:
					if (num3 < m_data.ints.Count)
					{
						animator.SetInteger(animatorControllerParameter.name, m_data.ints[num3++]);
					}
					break;
				}
			}
		}
	}
}
