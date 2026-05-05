using UnityEngine;

public class SlowmoToggler : MonoBehaviour
{
	public void Slowmo(bool slowmo)
	{
		Time.timeScale = (slowmo ? 0.25f : 1f);
	}
}
