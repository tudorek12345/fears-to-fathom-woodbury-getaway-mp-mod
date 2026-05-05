using UnityEngine;

public class UsecaseSwitcher : MonoBehaviour
{
	private Transform currentSelected;

	private void Start()
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).gameObject.SetActive(i == 0);
		}
		currentSelected = base.transform.GetChild(0);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			int siblingIndex = currentSelected.GetSiblingIndex();
			base.transform.GetChild(siblingIndex).gameObject.SetActive(value: false);
			siblingIndex++;
			currentSelected = base.transform.GetChild(siblingIndex % base.transform.childCount);
			currentSelected.gameObject.SetActive(value: true);
		}
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			int siblingIndex2 = currentSelected.GetSiblingIndex();
			base.transform.GetChild(siblingIndex2).gameObject.SetActive(value: false);
			siblingIndex2--;
			if (siblingIndex2 < 0)
			{
				siblingIndex2 = base.transform.childCount - 1;
			}
			currentSelected = base.transform.GetChild(siblingIndex2);
			currentSelected.gameObject.SetActive(value: true);
		}
	}
}
