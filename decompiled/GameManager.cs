using System;
using System.Collections;
using UnityEngine;

public abstract class GameManager : MonoBehaviour
{
	[Header("Base Class")]
	[SerializeField]
	internal UIManager uiManager;

	[SerializeField]
	internal PlayerController playerController;

	[HideInInspector]
	public bool isPaused;

	public bool playerTalking;

	internal virtual void Start()
	{
		StartCoroutine(StartGame());
	}

	public abstract IEnumerator StartGame();

	public IEnumerator RequestFadeInAndFadeOut(float fadeOut, float fadeIn, float waitTime, Action fadeOutComplete, Action fadeInStart = null)
	{
		playerController.StartPlayerTransition();
		yield return StartCoroutine(uiManager.ShowFadeInAndOut(fadeOut, fadeIn, waitTime, delegate
		{
			fadeOutComplete();
		}, delegate
		{
			if (fadeInStart != null)
			{
				fadeInStart();
			}
		}));
		playerController.CompletedPlayerTransition();
	}

	public IEnumerator RequestFadeOut(float duration)
	{
		yield return StartCoroutine(uiManager.FadeOutBlackScreen(duration));
	}

	public void StartCharacterMovement()
	{
		playerController.StartPlayer();
	}
}
