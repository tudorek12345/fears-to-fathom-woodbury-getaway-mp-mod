namespace PixelCrushers.DialogueSystem;

public static class ConditionPriorityUtility
{
	public static ConditionPriority StringToConditionPriority(string s)
	{
		if (string.Equals(s, "High"))
		{
			return ConditionPriority.High;
		}
		if (string.Equals(s, "AboveNormal"))
		{
			return ConditionPriority.AboveNormal;
		}
		if (string.Equals(s, "BelowNormal"))
		{
			return ConditionPriority.BelowNormal;
		}
		if (string.Equals(s, "Low"))
		{
			return ConditionPriority.Low;
		}
		return ConditionPriority.Normal;
	}
}
