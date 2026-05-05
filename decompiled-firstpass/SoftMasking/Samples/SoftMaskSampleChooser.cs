using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoftMasking.Samples;

public class SoftMaskSampleChooser : MonoBehaviour
{
	public Dropdown dropdown;

	public Text fallbackLabel;

	public void Start()
	{
		string activeSceneName = SceneManager.GetActiveScene().name;
		dropdown.value = dropdown.options.FindIndex((OptionData x) => x.text == activeSceneName);
		((UnityEvent<int>)(object)dropdown.onValueChanged).AddListener((UnityAction<int>)Choose);
	}

	public void Choose(int sampleIndex)
	{
		SceneManager.LoadScene(dropdown.options[sampleIndex].text);
	}
}
