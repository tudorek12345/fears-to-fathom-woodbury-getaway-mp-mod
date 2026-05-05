using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ThreeDimensional.Examples;

public class ExampleController : MonoBehaviour
{
	public Canvas Canvas;

	public List<GameObject> Examples = new List<GameObject>();

	public GameObject WordSpaceText;

	public List<UIObject3D> gridItems = new List<UIObject3D>();

	public void SelectExample(int number)
	{
		Examples.ForEach(delegate(GameObject e)
		{
			e.SetActive(value: false);
		});
		Examples[number].SetActive(value: true);
	}

	public void SetCanvasMode(int mode)
	{
		Canvas.renderMode = (RenderMode)mode;
		Canvas.transform.position = Vector3.zero;
		Canvas.transform.localScale = Vector3.one;
		Canvas.transform.rotation = Quaternion.identity;
		Camera.main.transform.position = new Vector3(0f, 0f, -500f);
		Camera.main.transform.rotation = Quaternion.identity;
		WordSpaceText.SetActive(Canvas.renderMode == RenderMode.WorldSpace);
	}

	private void EnsureGridItemsCollectionIsPopulated()
	{
		if (!gridItems.Any())
		{
			gridItems = Examples[0].GetComponentsInChildren<UIObject3D>().ToList();
		}
	}

	public void ToggleGridItemOutlines(bool toggle)
	{
		EnsureGridItemsCollectionIsPopulated();
		foreach (UIObject3D gridItem in gridItems)
		{
			((Behaviour)(object)(gridItem.GetComponent<Outline>() ?? gridItem.gameObject.AddComponent<Outline>())).enabled = toggle;
		}
	}

	public void ToggleGridItemRotation(bool toggle)
	{
		EnsureGridItemsCollectionIsPopulated();
		foreach (UIObject3D gridItem in gridItems)
		{
			(gridItem.GetComponent<RotateUIObject3D>() ?? gridItem.gameObject.AddComponent<RotateUIObject3D>()).enabled = toggle;
		}
	}

	public void ToggleImageColor(bool toggle)
	{
		EnsureGridItemsCollectionIsPopulated();
		foreach (UIObject3D gridItem in gridItems)
		{
			((Graphic)gridItem.GetComponent<Image>()).color = (toggle ? Color.green : Color.white);
		}
	}
}
