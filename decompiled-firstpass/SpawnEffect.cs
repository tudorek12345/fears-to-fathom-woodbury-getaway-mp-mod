using UnityEngine;

public class SpawnEffect : MonoBehaviour
{
	public float spawnEffectTime = 2f;

	public float pause = 1f;

	public AnimationCurve fadeIn;

	private ParticleSystem ps;

	private float timer;

	private Renderer _renderer;

	private int shaderProperty;

	private void Start()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		shaderProperty = Shader.PropertyToID("_cutoff");
		_renderer = GetComponent<Renderer>();
		ps = GetComponentInChildren<ParticleSystem>();
		MainModule main = ps.main;
		((MainModule)(ref main)).duration = spawnEffectTime;
		ps.Play();
	}

	private void Update()
	{
		if (timer < spawnEffectTime + pause)
		{
			timer += Time.deltaTime;
		}
		else
		{
			ps.Play();
			timer = 0f;
		}
		_renderer.material.SetFloat(shaderProperty, fadeIn.Evaluate(Mathf.InverseLerp(0f, spawnEffectTime, timer)));
	}
}
