using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Samples;

public class Item : MonoBehaviour
{
	public Image image;

	public Text title;

	public Text description;

	public RectTransform healthBar;

	public RectTransform damageBar;

	public void Set(string name, Sprite sprite, Color color, float health, float damage)
	{
		if ((bool)(Object)(object)image)
		{
			image.sprite = sprite;
			((Graphic)image).color = color;
		}
		if ((bool)(Object)(object)title)
		{
			title.text = name;
		}
		if ((bool)(Object)(object)description)
		{
			description.text = "The short description of " + name;
		}
		if ((bool)healthBar)
		{
			healthBar.anchorMax = new Vector2(health, 1f);
		}
		if ((bool)damageBar)
		{
			damageBar.anchorMax = new Vector2(damage, 1f);
		}
	}
}
