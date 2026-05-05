using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers;

[AddComponentMenu("")]
public class ScenePortal : MonoBehaviour
{
	[Tooltip("Only objects with this tag can use the portal.")]
	[SerializeField]
	private string m_requiredTag = "Player";

	[Tooltip("Go to this scene.")]
	[SerializeField]
	private string m_destinationSceneName;

	[Tooltip("If not blank, move the player to the GameObject with this name.")]
	[SerializeField]
	private string m_spawnpointNameInDestinationScene;

	[SerializeField]
	private UnityEvent m_onUsePortal = new UnityEvent();

	private bool m_isLoadingScene;

	public string requiredTag
	{
		get
		{
			return m_requiredTag;
		}
		set
		{
			m_requiredTag = value;
		}
	}

	public string destinationSceneName
	{
		get
		{
			return m_destinationSceneName;
		}
		set
		{
			m_destinationSceneName = value;
		}
	}

	public string spawnpointNameInDestinationScene
	{
		get
		{
			return m_spawnpointNameInDestinationScene;
		}
		set
		{
			m_spawnpointNameInDestinationScene = value;
		}
	}

	public bool isLoadingScene
	{
		get
		{
			return m_isLoadingScene;
		}
		set
		{
			m_isLoadingScene = value;
		}
	}

	public UnityEvent onUsePortal => m_onUsePortal;

	public virtual void UsePortal()
	{
		if (!isLoadingScene)
		{
			isLoadingScene = true;
			onUsePortal.Invoke();
			LoadScene();
		}
	}

	protected void LoadScene()
	{
		SaveSystem.LoadScene(string.IsNullOrEmpty(spawnpointNameInDestinationScene) ? destinationSceneName : (destinationSceneName + "@" + spawnpointNameInDestinationScene));
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag(requiredTag))
		{
			UsePortal();
		}
	}
}
