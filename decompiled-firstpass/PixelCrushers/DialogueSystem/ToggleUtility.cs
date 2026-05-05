namespace PixelCrushers.DialogueSystem;

public static class ToggleUtility
{
	public static bool GetNewValue(bool oldValue, Toggle state)
	{
		return state switch
		{
			Toggle.True => true, 
			Toggle.False => false, 
			_ => !oldValue, 
		};
	}
}
