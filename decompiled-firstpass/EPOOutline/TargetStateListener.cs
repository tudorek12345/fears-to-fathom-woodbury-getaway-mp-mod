using System;
using System.Collections.Generic;
using UnityEngine;

namespace EPOOutline;

[ExecuteAlways]
public class TargetStateListener : MonoBehaviour
{
	public struct Callback(Outlinable target, Action action)
	{
		public readonly Outlinable Target = target;

		public readonly Action Action = action;
	}

	private List<Callback> callbacks = new List<Callback>();

	public void AddCallback(Outlinable outlinable, Action action)
	{
		callbacks.Add(new Callback(outlinable, action));
	}

	public void RemoveCallback(Outlinable outlinable, Action callback)
	{
		int num = callbacks.FindIndex((Callback x) => x.Target == outlinable && x.Action == callback);
		if (num != -1)
		{
			callbacks.RemoveAt(num);
		}
	}

	private void Awake()
	{
		base.hideFlags = HideFlags.HideInInspector;
	}

	public void ForceUpdate()
	{
		callbacks.RemoveAll((Callback x) => x.Target == null);
		foreach (Callback callback in callbacks)
		{
			callback.Action();
		}
	}

	private void OnBecameVisible()
	{
		ForceUpdate();
	}

	private void OnBecameInvisible()
	{
		ForceUpdate();
	}
}
