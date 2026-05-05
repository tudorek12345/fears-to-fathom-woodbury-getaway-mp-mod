using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIQuestGroupTemplate : MonoBehaviour
{
	[Header("Quest Group Heading")]
	[Tooltip("The quest group name")]
	public Text heading;

	public bool ArePropertiesAssigned => (Object)(object)heading != null;

	public void Initialize()
	{
	}
}
