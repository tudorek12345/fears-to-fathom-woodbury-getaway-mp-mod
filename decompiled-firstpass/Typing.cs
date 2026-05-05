using UnityEngine;

public class Typing : MonoBehaviour
{
	[TextArea(3, 10)]
	public string text;

	public float speed = 0.5f;

	public TextMesh tm1;

	public TextMesh tm2;

	private void Start()
	{
	}

	private void Update()
	{
		int num = (int)(Time.time * speed);
		if (num > this.text.Length)
		{
			num = this.text.Length;
		}
		string text = this.text.Substring(0, num);
		tm1.text = text;
		tm2.text = text;
	}
}
