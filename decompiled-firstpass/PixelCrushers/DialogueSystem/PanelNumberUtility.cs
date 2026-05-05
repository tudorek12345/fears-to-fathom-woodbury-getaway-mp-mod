namespace PixelCrushers.DialogueSystem;

public static class PanelNumberUtility
{
	public static SubtitlePanelNumber IntToSubtitlePanelNumber(int i)
	{
		if (0 > i || i > 31)
		{
			return SubtitlePanelNumber.Default;
		}
		return (SubtitlePanelNumber)(i + 3);
	}

	public static MenuPanelNumber IntToMenuPanelNumber(int i)
	{
		if (0 > i || i > 31)
		{
			return MenuPanelNumber.Default;
		}
		return (MenuPanelNumber)(i + 2);
	}

	public static int GetSubtitlePanelIndex(SubtitlePanelNumber subtitlePanelNumber)
	{
		return subtitlePanelNumber switch
		{
			SubtitlePanelNumber.Panel0 => 0, 
			SubtitlePanelNumber.Panel1 => 1, 
			SubtitlePanelNumber.Panel2 => 2, 
			SubtitlePanelNumber.Panel3 => 3, 
			SubtitlePanelNumber.Panel4 => 4, 
			SubtitlePanelNumber.Panel5 => 5, 
			SubtitlePanelNumber.Panel6 => 6, 
			SubtitlePanelNumber.Panel7 => 7, 
			SubtitlePanelNumber.Panel8 => 8, 
			SubtitlePanelNumber.Panel9 => 9, 
			SubtitlePanelNumber.Panel10 => 10, 
			SubtitlePanelNumber.Panel11 => 11, 
			SubtitlePanelNumber.Panel12 => 12, 
			SubtitlePanelNumber.Panel13 => 13, 
			SubtitlePanelNumber.Panel14 => 14, 
			SubtitlePanelNumber.Panel15 => 15, 
			SubtitlePanelNumber.Panel16 => 16, 
			SubtitlePanelNumber.Panel17 => 17, 
			SubtitlePanelNumber.Panel18 => 18, 
			SubtitlePanelNumber.Panel19 => 19, 
			SubtitlePanelNumber.Panel20 => 20, 
			SubtitlePanelNumber.Panel21 => 21, 
			SubtitlePanelNumber.Panel22 => 22, 
			SubtitlePanelNumber.Panel23 => 23, 
			SubtitlePanelNumber.Panel24 => 24, 
			SubtitlePanelNumber.Panel25 => 25, 
			SubtitlePanelNumber.Panel26 => 26, 
			SubtitlePanelNumber.Panel27 => 27, 
			SubtitlePanelNumber.Panel28 => 28, 
			SubtitlePanelNumber.Panel29 => 29, 
			SubtitlePanelNumber.Panel30 => 30, 
			SubtitlePanelNumber.Panel31 => 31, 
			_ => -1, 
		};
	}

	public static int GetMenuPanelIndex(MenuPanelNumber menuPanelNumber)
	{
		return menuPanelNumber switch
		{
			MenuPanelNumber.Panel0 => 0, 
			MenuPanelNumber.Panel1 => 1, 
			MenuPanelNumber.Panel2 => 2, 
			MenuPanelNumber.Panel3 => 3, 
			MenuPanelNumber.Panel4 => 4, 
			MenuPanelNumber.Panel5 => 5, 
			MenuPanelNumber.Panel6 => 6, 
			MenuPanelNumber.Panel7 => 7, 
			MenuPanelNumber.Panel8 => 8, 
			MenuPanelNumber.Panel9 => 9, 
			MenuPanelNumber.Panel10 => 10, 
			MenuPanelNumber.Panel11 => 11, 
			MenuPanelNumber.Panel12 => 12, 
			MenuPanelNumber.Panel13 => 13, 
			MenuPanelNumber.Panel14 => 14, 
			MenuPanelNumber.Panel15 => 15, 
			MenuPanelNumber.Panel16 => 16, 
			MenuPanelNumber.Panel17 => 17, 
			MenuPanelNumber.Panel18 => 18, 
			MenuPanelNumber.Panel19 => 19, 
			MenuPanelNumber.Panel20 => 20, 
			MenuPanelNumber.Panel21 => 21, 
			MenuPanelNumber.Panel22 => 22, 
			MenuPanelNumber.Panel23 => 23, 
			MenuPanelNumber.Panel24 => 24, 
			MenuPanelNumber.Panel25 => 25, 
			MenuPanelNumber.Panel26 => 26, 
			MenuPanelNumber.Panel27 => 27, 
			MenuPanelNumber.Panel28 => 28, 
			MenuPanelNumber.Panel29 => 29, 
			MenuPanelNumber.Panel30 => 30, 
			MenuPanelNumber.Panel31 => 31, 
			_ => -1, 
		};
	}
}
