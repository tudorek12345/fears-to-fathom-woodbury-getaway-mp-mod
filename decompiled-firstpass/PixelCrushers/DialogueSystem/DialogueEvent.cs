using System;

namespace PixelCrushers.DialogueSystem;

[Flags]
public enum DialogueEvent
{
	OnBark = 1,
	OnConversation = 2,
	OnSequence = 4
}
