using UnityEngine;

public class DoorHandle : MonoBehaviour
{
	public AudioSource hitImpact;

	public AudioSource handleOnFloor;

	public void PlayHitImpact()
	{
		hitImpact.Play();
	}

	public void PlayHandleOnFloor()
	{
		handleOnFloor.Play();
	}

	public void MuteDoorHandleAudio()
	{
		hitImpact.volume = 0f;
		handleOnFloor.volume = 0f;
	}
}
