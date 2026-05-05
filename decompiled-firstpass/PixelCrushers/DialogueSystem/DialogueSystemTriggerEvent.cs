using System;

namespace PixelCrushers.DialogueSystem;

[Flags]
public enum DialogueSystemTriggerEvent
{
	OnBarkEnd = 1,
	OnConversationEnd = 2,
	OnSequenceEnd = 4,
	OnTriggerEnter = 8,
	OnStart = 0x10,
	OnUse = 0x20,
	OnEnable = 0x40,
	OnTriggerExit = 0x80,
	OnDisable = 0x100,
	OnDestroy = 0x200,
	None = 0x400,
	OnCollisionEnter = 0x800,
	OnCollisionExit = 0x1000,
	OnBarkStart = 0x2000,
	OnConversationStart = 0x4000,
	OnSequenceStart = 0x8000,
	OnSaveDataApplied = 0x10000
}
