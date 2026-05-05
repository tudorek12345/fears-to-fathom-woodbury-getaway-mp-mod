using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FPSDisplay : MonoBehaviour
{
	public float updateInterval = 0.5f;

	public bool showMedian;

	public float medianLearnrate = 0.05f;

	private float accum;

	private int frames;

	private float timeleft;

	private float currentFPS;

	private float median;

	private float average;

	private Text uguiText;

	public float CurrentFPS => currentFPS;

	public float FPSMedian => median;

	public float FPSAverage => average;

	private void Start()
	{
		uguiText = GetComponent<Text>();
		timeleft = updateInterval;
	}

	private void Update()
	{
		timeleft -= Time.deltaTime;
		accum += Time.timeScale / Time.deltaTime;
		frames++;
		if ((double)timeleft <= 0.0)
		{
			currentFPS = accum / (float)frames;
			average += (Mathf.Abs(currentFPS) - average) * 0.1f;
			median += Mathf.Sign(currentFPS - median) * Mathf.Min(average * medianLearnrate, Mathf.Abs(currentFPS - median));
			float num = (showMedian ? median : currentFPS);
			uguiText.text = $"{num:F2} FPS ({1000f / num:F1} ms)";
			timeleft = updateInterval;
			accum = 0f;
			frames = 0;
		}
	}

	public void ResetMedianAndAverage()
	{
		median = 0f;
		average = 0f;
	}
}
