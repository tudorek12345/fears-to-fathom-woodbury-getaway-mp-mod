using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiRope))]
public class HosePump : MonoBehaviour
{
	[Header("Bulge controls")]
	public float pumpSpeed = 5f;

	public float bulgeFrequency = 3f;

	public float baseThickness = 0.04f;

	public float bulgeThickness = 0.06f;

	public Color bulgeColor = Color.cyan;

	[Header("Flow controls")]
	public ParticleSystem waterEmitter;

	public float flowSpeedMin = 0.5f;

	public float flowSpeedMax = 7f;

	public float minEmitRate = 100f;

	public float maxEmitRate = 1000f;

	private ObiRope rope;

	public ObiPathSmoother smoother;

	private float time;

	private void OnEnable()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		rope = GetComponent<ObiRope>();
		smoother = GetComponent<ObiPathSmoother>();
		((ObiActor)rope).OnBeginStep += new ActorStepCallback(Rope_OnBeginStep);
	}

	private void OnDisable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		((ObiActor)rope).OnBeginStep -= new ActorStepCallback(Rope_OnBeginStep);
	}

	private void Rope_OnBeginStep(ObiActor actor, float stepTime)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		time += stepTime * pumpSpeed;
		float num = 0f;
		float t = 0f;
		for (int i = 0; i < ((ObiActor)rope).solverIndices.Length; i++)
		{
			int num2 = ((ObiActor)rope).solverIndices[i];
			if (i > 0)
			{
				int num3 = ((ObiActor)rope).solverIndices[i - 1];
				num += Vector3.Distance(((ObiNativeList<Vector4>)(object)((ObiActor)rope).solver.positions)[num2], ((ObiNativeList<Vector4>)(object)((ObiActor)rope).solver.positions)[num3]);
			}
			t = Mathf.Max(0f, Mathf.Sin(num * bulgeFrequency - time));
			((ObiNativeList<Vector4>)(object)((ObiActor)rope).solver.principalRadii)[num2] = Vector3.one * Mathf.Lerp(baseThickness, bulgeThickness, t);
			((ObiNativeList<Color>)(object)((ObiActor)rope).solver.colors)[num2] = Color.Lerp(Color.white, bulgeColor, t);
		}
		if ((Object)(object)waterEmitter != null)
		{
			MainModule main = waterEmitter.main;
			((MainModule)(ref main)).startSpeed = MinMaxCurve.op_Implicit(Mathf.Lerp(flowSpeedMin, flowSpeedMax, t));
			EmissionModule emission = waterEmitter.emission;
			((EmissionModule)(ref emission)).rateOverTime = MinMaxCurve.op_Implicit(Mathf.Lerp(minEmitRate, maxEmitRate, t));
		}
	}

	public void LateUpdate()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)smoother != null && (Object)(object)waterEmitter != null)
		{
			ObiPathFrame sectionAt = smoother.GetSectionAt(1f);
			((Component)(object)waterEmitter).transform.position = base.transform.TransformPoint(sectionAt.position);
			((Component)(object)waterEmitter).transform.rotation = base.transform.rotation * Quaternion.LookRotation(sectionAt.tangent, sectionAt.binormal);
		}
	}
}
