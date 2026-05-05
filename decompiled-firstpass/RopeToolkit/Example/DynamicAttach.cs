using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit.Example;

public class DynamicAttach : MonoBehaviour
{
	public Material ropeMaterial;

	public Vector3 attachPoint;

	public Transform target;

	public Vector3 targetAttachPoint;

	protected GameObject ropeObject;

	public void Detach()
	{
		if ((bool)ropeObject)
		{
			Object.Destroy(ropeObject);
		}
		ropeObject = null;
	}

	public void Attach()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		Detach();
		ropeObject = new GameObject();
		ropeObject.name = "Rope";
		Vector3 position = base.transform.TransformPoint(attachPoint);
		Vector3 position2 = target.TransformPoint(targetAttachPoint);
		Rope rope = ropeObject.AddComponent<Rope>();
		rope.material = ropeMaterial;
		rope.spawnPoints.Add(float3.op_Implicit(ropeObject.transform.InverseTransformPoint(position)));
		rope.spawnPoints.Add(float3.op_Implicit(ropeObject.transform.InverseTransformPoint(position2)));
		RopeConnection ropeConnection = ropeObject.AddComponent<RopeConnection>();
		ropeConnection.type = RopeConnectionType.PinRopeToTransform;
		ropeConnection.ropeLocation = 0f;
		ropeConnection.transformSettings.transform = base.transform;
		ropeConnection.localConnectionPoint = float3.op_Implicit(attachPoint);
		RopeConnection ropeConnection2 = ropeObject.AddComponent<RopeConnection>();
		ropeConnection2.type = RopeConnectionType.PinRopeToTransform;
		ropeConnection2.ropeLocation = 1f;
		ropeConnection2.transformSettings.transform = target;
		ropeConnection2.localConnectionPoint = float3.op_Implicit(targetAttachPoint);
	}

	public void OnGUI()
	{
		if (GUI.Button(new Rect(16f, 16f, 100f, 32f), "Attach"))
		{
			Attach();
		}
		if (GUI.Button(new Rect(16f, 64f, 100f, 32f), "Detach"))
		{
			Detach();
		}
	}
}
