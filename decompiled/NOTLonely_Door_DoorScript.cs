using System;
using UnityEngine;
using UnityEngine.UI;

namespace NOTLonely_Door;

public class DoorScript : MonoBehaviour
{
	public enum OpenStyle
	{
		BUTTON,
		AUTOMATIC
	}

	[Serializable]
	public class DoorControls
	{
		public float openingSpeed = 1f;

		public float closingSpeed = 1.3f;

		[Range(0f, 1f)]
		public float closeStartFrom = 0.6f;

		public OpenStyle openMethod;

		public KeyCode openButton = KeyCode.E;

		public bool autoClose;

		[Tooltip("0 - the door is closed, 1 - the door is opened. You can set it to something like 0.15 to get semi-opened door at start.")]
		[Range(0f, 1f)]
		public float OpenedAtStart;
	}

	[Serializable]
	public class AnimNames
	{
		public string OpeningAnim = "Door_open";

		public string LockedAnim = "Door_locked";
	}

	[Serializable]
	public class DoorSounds
	{
		public bool enabled = true;

		public AudioClip open;

		public AudioClip close;

		public AudioClip closed;

		[Range(0f, 1f)]
		public float volume = 1f;

		[Range(0f, 0.4f)]
		public float pitchRandom = 0.2f;
	}

	[Serializable]
	public class DoorTexts
	{
		public bool enabled;

		public string openingText = "Press [BUTTON] to open";

		public string closingText = "Press [BUTTON] to close";

		public string lockText = "You need a key!";

		public GameObject TextPrefab;
	}

	[Serializable]
	public class KeySystem
	{
		public bool enabled;

		[HideInInspector]
		public bool isUnlock;

		[Tooltip("If you have a padlock model, you can put the prefab here.")]
		public GameObject LockPrefab;
	}

	private Transform[] Childs;

	private Transform Joint01;

	private Transform Joint02;

	[Tooltip("player's head with collider in trigger mode. Type your tag here (usually it is MainCamera)")]
	public string PlayerHeadTag = "MainCamera";

	[Tooltip("Empty gameObject in the door knobs area. It needed to open the door if 'Open by button' type is selected. If you don't want to put this object in this slot manually, you can simply create the object with the name 'doorKnob' and put it in the door prefab.")]
	public Transform knob;

	public DoorControls controls = new DoorControls();

	public AnimNames AnimationNames = new AnimNames();

	public DoorSounds doorSounds = new DoorSounds();

	public DoorTexts doorTexts = new DoorTexts();

	public KeySystem keySystem = new KeySystem();

	private Transform player;

	private bool Opened;

	private bool inZone;

	private Canvas TextObj;

	private Text theText;

	private AudioSource SoundFX;

	private Animation doorAnimation;

	private Animation LockAnim;

	private void Start()
	{
		Childs = GetComponentsInChildren<Transform>();
		Transform[] childs = Childs;
		foreach (Transform transform in childs)
		{
			if (transform.name == "Joint01")
			{
				Joint01 = transform.transform;
			}
			else if (transform.name == "Joint02")
			{
				Joint02 = transform.transform;
			}
		}
		childs = Childs;
		foreach (Transform transform2 in childs)
		{
			if (transform2.name == "Door_bottom01")
			{
				transform2.parent = Joint01;
			}
			else if (transform2.name == "Door_bottom02")
			{
				transform2.parent = Joint02;
			}
		}
		if (controls.openMethod == OpenStyle.AUTOMATIC)
		{
			controls.autoClose = true;
		}
		if (PlayerHeadTag == "")
		{
			Debug.LogError("You need to set a tag!");
		}
		if (GameObject.FindWithTag(PlayerHeadTag) != null)
		{
			player = GameObject.FindWithTag(PlayerHeadTag).transform;
		}
		else
		{
			Debug.LogWarning(base.gameObject.name + ": You need to set your player's camera tag to '" + PlayerHeadTag + "'. The '" + base.gameObject.name + "' can't open/close if you don't set this tag");
		}
		AddText();
		AddLock();
		AddAudioSource();
		DetectDoorKnob();
		doorAnimation = GetComponent<Animation>();
		if (controls.OpenedAtStart > 0f)
		{
			doorAnimation[AnimationNames.OpeningAnim].normalizedTime = controls.OpenedAtStart;
			doorAnimation[AnimationNames.OpeningAnim].speed = 0f;
			doorAnimation.Play(AnimationNames.OpeningAnim);
		}
	}

	private void AddText()
	{
		if (doorTexts.enabled)
		{
			if (doorTexts.TextPrefab == null)
			{
				Debug.LogWarning(base.gameObject.name + ": Text prefab missing, if you want see the text, please, put the text prefab in Text Prefab slot");
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(doorTexts.TextPrefab, Vector3.zero, new Quaternion(0f, 0f, 0f, 0f));
			TextObj = gameObject.GetComponent<Canvas>();
			theText = TextObj.GetComponentInChildren<Text>();
		}
	}

	private void AddLock()
	{
		if (keySystem.enabled && !(keySystem.LockPrefab == null))
		{
			LockAnim = keySystem.LockPrefab.GetComponent<Animation>();
			keySystem.enabled = true;
		}
	}

	private void AddAudioSource()
	{
		GameObject gameObject = new GameObject("SoundFX");
		gameObject.transform.position = base.transform.position;
		gameObject.transform.rotation = base.transform.rotation;
		gameObject.transform.parent = base.transform;
		SoundFX = gameObject.AddComponent<AudioSource>();
		SoundFX.volume = doorSounds.volume;
		SoundFX.spatialBlend = 1f;
		SoundFX.playOnAwake = false;
		SoundFX.clip = doorSounds.open;
	}

	private void DetectDoorKnob()
	{
		if (!(knob == null))
		{
			return;
		}
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name == "doorKnob")
			{
				knob = transform;
			}
		}
	}

	private void Update()
	{
		if (!doorAnimation.isPlaying && SoundFX.isPlaying)
		{
			SoundFX.Stop();
		}
		if (!inZone)
		{
			HideHint();
			return;
		}
		if (controls.openMethod == OpenStyle.AUTOMATIC && !Opened)
		{
			OpenDoor();
		}
		if (PLayerIsLookingAtDoorKnob())
		{
			if (controls.openMethod == OpenStyle.BUTTON)
			{
				ShowHint();
			}
			if (!Input.GetKeyDown(controls.openButton) || controls.openMethod != OpenStyle.BUTTON)
			{
				return;
			}
			if (Opened)
			{
				if (!controls.autoClose)
				{
					CloseDoor();
				}
			}
			else if (keySystem.enabled)
			{
				if (keySystem.isUnlock)
				{
					OpenLockDoor();
				}
				else
				{
					PlayClosedFXs();
				}
			}
			else
			{
				OpenDoor();
			}
		}
		else
		{
			HideHint();
		}
	}

	private bool PLayerIsLookingAtDoorKnob()
	{
		Vector3 vector = player.TransformDirection(Vector3.back);
		Vector3 vector2 = knob.position - player.transform.position;
		float num = Vector3.Dot(vector.normalized, vector2.normalized);
		if (num < 0f)
		{
			return num < -0.9f;
		}
		return false;
	}

	private void OpenLockDoor()
	{
		if (keySystem.LockPrefab != null)
		{
			LockAnim.Play("Lock_open");
			Invoke("OpenDoor", 1f);
		}
		else
		{
			OpenDoor();
		}
	}

	public void Unlock()
	{
		keySystem.isUnlock = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(other.tag != PlayerHeadTag))
		{
			inZone = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!(other.tag != PlayerHeadTag))
		{
			if (Opened && controls.autoClose)
			{
				CloseDoor();
			}
			inZone = false;
		}
	}

	private void ShowHint()
	{
		if (Opened)
		{
			if (!controls.autoClose)
			{
				CloseText();
			}
		}
		else if (keySystem.enabled && !keySystem.isUnlock)
		{
			LockText();
		}
		else
		{
			OpenText();
		}
	}

	private void HideHint()
	{
		if (controls.openMethod == OpenStyle.BUTTON)
		{
			HideText();
		}
	}

	private void PlaySFX(AudioClip clip)
	{
		if (doorSounds.enabled)
		{
			SoundFX.pitch = UnityEngine.Random.Range(1f - doorSounds.pitchRandom, 1f + doorSounds.pitchRandom);
			SoundFX.clip = clip;
			SoundFX.Play();
		}
	}

	private void PlayClosedFXs()
	{
		if (doorSounds.closed != null)
		{
			SoundFX.clip = doorSounds.closed;
			SoundFX.Play();
			if (doorAnimation[AnimationNames.LockedAnim] != null)
			{
				doorAnimation.Play(AnimationNames.LockedAnim);
				doorAnimation[AnimationNames.LockedAnim].speed = 1f;
				doorAnimation[AnimationNames.LockedAnim].normalizedTime = 0f;
			}
		}
	}

	private void CloseSound()
	{
		if (doorAnimation[AnimationNames.OpeningAnim].speed < 0f && doorSounds.close != null)
		{
			PlaySFX(doorSounds.close);
		}
	}

	public void OpenText()
	{
		ShowText(doorTexts.openingText);
	}

	private void LockText()
	{
		ShowText(doorTexts.lockText);
	}

	private void CloseText()
	{
		ShowText(doorTexts.closingText);
	}

	private void ShowText(string txt)
	{
		if (doorTexts.enabled)
		{
			string text = txt;
			if (controls.openMethod == OpenStyle.BUTTON)
			{
				text = txt.Replace("[BUTTON]", "'" + controls.openButton.ToString() + "'");
			}
			TextObj.enabled = false;
			theText.text = text;
			TextObj.enabled = true;
		}
	}

	private void HideText()
	{
		if (doorTexts.enabled)
		{
			if (doorTexts.TextPrefab != null)
			{
				TextObj.enabled = false;
			}
			else
			{
				doorTexts.enabled = false;
			}
		}
	}

	private void OpenDoor()
	{
		doorAnimation[AnimationNames.OpeningAnim].speed = controls.openingSpeed;
		doorAnimation[AnimationNames.OpeningAnim].normalizedTime = doorAnimation[AnimationNames.OpeningAnim].normalizedTime;
		doorAnimation.Play(AnimationNames.OpeningAnim);
		if (doorSounds.open != null)
		{
			PlaySFX(doorSounds.open);
		}
		Opened = true;
		if (controls.openMethod == OpenStyle.BUTTON)
		{
			HideText();
		}
		keySystem.enabled = false;
	}

	private void CloseDoor()
	{
		if (doorAnimation[AnimationNames.OpeningAnim].normalizedTime < 0.98f && doorAnimation[AnimationNames.OpeningAnim].normalizedTime > 0f)
		{
			doorAnimation[AnimationNames.OpeningAnim].speed = 0f - controls.closingSpeed;
			doorAnimation[AnimationNames.OpeningAnim].normalizedTime = doorAnimation[AnimationNames.OpeningAnim].normalizedTime;
			doorAnimation.Play(AnimationNames.OpeningAnim);
		}
		else
		{
			doorAnimation[AnimationNames.OpeningAnim].speed = 0f - controls.closingSpeed;
			doorAnimation[AnimationNames.OpeningAnim].normalizedTime = controls.closeStartFrom;
			doorAnimation.Play(AnimationNames.OpeningAnim);
		}
		if (doorAnimation[AnimationNames.OpeningAnim].normalizedTime > controls.closeStartFrom)
		{
			doorAnimation[AnimationNames.OpeningAnim].speed = 0f - controls.closingSpeed;
			doorAnimation[AnimationNames.OpeningAnim].normalizedTime = controls.closeStartFrom;
			doorAnimation.Play(AnimationNames.OpeningAnim);
		}
		Opened = false;
		if (controls.openMethod == OpenStyle.BUTTON && !controls.autoClose)
		{
			HideText();
		}
	}
}
