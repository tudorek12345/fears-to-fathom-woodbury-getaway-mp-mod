using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class BitMaskAttribute : PropertyAttribute
{
	public Type propType;

	public BitMaskAttribute(Type propType)
	{
		this.propType = propType;
	}
}
