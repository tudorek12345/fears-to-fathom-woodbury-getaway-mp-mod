using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LuaOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class LuaAction : Action
	{
		[Multiline]
		public string luaCode = string.Empty;
	}

	public LuaAction[] onStart = new LuaAction[0];

	public LuaAction[] onEnd = new LuaAction[0];

	public bool debugLua;

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(LuaAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (LuaAction luaAction in actions)
		{
			if (luaAction != null && luaAction.condition != null && luaAction.condition.IsTrue(actor))
			{
				DoAction(luaAction, actor);
			}
		}
	}

	public void DoAction(LuaAction action, Transform actor)
	{
		if (action != null)
		{
			Lua.Run(action.luaCode, debugLua);
			DialogueManager.SendUpdateTracker();
		}
	}
}
