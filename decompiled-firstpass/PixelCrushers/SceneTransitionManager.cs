using System.Collections;
using UnityEngine;

namespace PixelCrushers;

public abstract class SceneTransitionManager : MonoBehaviour
{
	public virtual IEnumerator LeaveScene()
	{
		yield break;
	}

	public virtual IEnumerator EnterScene()
	{
		yield break;
	}

	public virtual void OnLoading(float progress)
	{
	}
}
