using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneController
{
	private const string musicAdditive = "AsyncAudio";

	public static void CompleteScene(LoadSceneMode loadMode = LoadSceneMode.Single)
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, loadMode);
	}

	public static void CompletedAudioAfterRoadTrip()
	{
		SceneManager.LoadScene(3, LoadSceneMode.Additive);
	}

	public static void UnloadScene()
	{
		SceneManager.UnloadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public static void RestartScene()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
	}

	public static void AdditiveMusicScene()
	{
	}

	public static IEnumerator LoadYourAsyncScene()
	{
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("AsyncAudio");
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
	}

	public static async Task PlayAsyncAudio(AudioClip clipToPlayAdditive, float duration, float targetVolume)
	{
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("AsyncAudio", LoadSceneMode.Additive);
		while (!asyncLoad.isDone)
		{
			await Task.Delay(1);
		}
		await AsyncAudio.GetInstance().PlayAudioAdditive(clipToPlayAdditive, duration, targetVolume);
	}

	public static void EndAsyncAudio()
	{
		SceneManager.UnloadSceneAsync("AsyncAudio");
	}
}
