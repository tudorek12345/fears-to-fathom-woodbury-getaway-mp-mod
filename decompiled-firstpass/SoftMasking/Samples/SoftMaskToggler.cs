using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples;

public class SoftMaskToggler : MonoBehaviour
{
	public GameObject mask;

	public bool doNotTouchImage;

	public void Toggle(bool enabled)
	{
		if ((bool)mask)
		{
			((Behaviour)(object)mask.GetComponent<SoftMask>()).enabled = enabled;
			((Behaviour)(object)mask.GetComponent<Mask>()).enabled = !enabled;
			if (!doNotTouchImage)
			{
				((Behaviour)(object)mask.GetComponent<Image>()).enabled = !enabled;
			}
		}
	}
}
