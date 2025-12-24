using System.Collections;
using UnityEngine;

public abstract class Holdable : MonoBehaviour
{
	public AudioClip[] sfxSoundsArray;

	internal AudioSource source;

	private const float delayInThrowSound = 0.1f;

	public Rigidbody rBody;

	internal PlayerController playerController;

	protected bool inUse;

	private bool isInteractible;

	public bool IsInUse()
	{
		return inUse;
	}

	internal virtual void Start()
	{
		source = GetComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = false;
		source.volume = 1f;
		source.spatialBlend = 1f;
		rBody = GetComponent<Rigidbody>();
		if (rBody != null)
		{
			rBody.isKinematic = true;
		}
		playerController = PlayerController.GetInstance();
	}

	internal IEnumerator ThrowSound()
	{
		source = GetComponent<AudioSource>();
		yield return new WaitForSeconds(0.1f);
		if (sfxSoundsArray.Length != 0)
		{
			source.PlayOneShot(sfxSoundsArray[Random.Range(0, sfxSoundsArray.Length)], 1f);
		}
	}

	public virtual void GoToPosition(Transform parentTransform)
	{
		if ((object)playerController == null)
		{
			playerController = PlayerController.GetInstance();
		}
		base.gameObject.SetActive(value: true);
		base.transform.parent = parentTransform;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.Euler(Vector3.zero);
		if ((object)rBody == null)
		{
			rBody = GetComponent<Rigidbody>();
		}
		if (rBody == null)
		{
			rBody = base.gameObject.AddComponent<Rigidbody>();
		}
		rBody.isKinematic = true;
	}

	public virtual void Throw(Transform throwDirection, float throwSpeed = 200f)
	{
		base.transform.parent = null;
		rBody.isKinematic = false;
		rBody.AddForce(throwDirection.forward * throwSpeed);
		StartCoroutine(ThrowSound());
	}

	public virtual void SetForUse()
	{
		inUse = true;
		playerController.RemoveHandObject();
	}

	public void SetInteractable(bool value)
	{
		isInteractible = value;
		base.gameObject.layer = (value ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Ignore Raycast"));
	}
}
