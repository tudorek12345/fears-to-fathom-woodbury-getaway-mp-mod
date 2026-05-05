using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class CommonLibraryLua : MonoBehaviour
{
	[Tooltip("Unregister functions when this component is disabled. Leave unticked if this script is on Dialogue Manager or other persistent GameObject.")]
	public bool unregisterOnDisable;

	private static bool s_registered;

	private bool didIRegister;

	private void OnEnable()
	{
		if (!s_registered)
		{
			Lua.RegisterFunction("SendMessageSystem", this, SymbolExtensions.GetMethodInfo(() => SendMessageSystem(string.Empty, string.Empty)));
			Lua.RegisterFunction("SendMessageSystemString", this, SymbolExtensions.GetMethodInfo(() => SendMessageSystemString(string.Empty, string.Empty, string.Empty)));
			Lua.RegisterFunction("SendMessageSystemInt", this, SymbolExtensions.GetMethodInfo(() => SendMessageSystemInt(string.Empty, string.Empty, 0.0)));
			s_registered = true;
			didIRegister = true;
		}
	}

	private void OnDisable()
	{
		if (unregisterOnDisable && s_registered && didIRegister)
		{
			Lua.UnregisterFunction("SendMessageSystem");
			Lua.UnregisterFunction("SendMessageSystemString");
			Lua.UnregisterFunction("SendMessageSystemInt");
			s_registered = false;
			didIRegister = false;
		}
	}

	public void SendMessageSystem(string message, string parameter)
	{
		MessageSystem.SendMessage(this, message, parameter);
	}

	public void SendMessageSystemString(string message, string parameter, string value)
	{
		MessageSystem.SendMessage(this, message, parameter, value);
	}

	public void SendMessageSystemInt(string message, string parameter, double value)
	{
		MessageSystem.SendMessage(this, message, parameter, (int)value);
	}
}
