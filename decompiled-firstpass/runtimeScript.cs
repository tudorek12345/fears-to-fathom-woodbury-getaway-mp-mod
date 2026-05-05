using EasyRoads3Dv3;
using UnityEngine;

public class runtimeScript : MonoBehaviour
{
	public ERRoadNetwork roadNetwork;

	public ERRoad road;

	public GameObject go;

	public int currentElement;

	public float distance;

	public float speed = 5f;

	private void Start()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		Debug.Log("Please read the comments at the top of the runtime script (/Assets/EasyRoads3D/Scripts/runtimeScript) before using the runtime API!");
		roadNetwork = new ERRoadNetwork();
		ERRoadType val = new ERRoadType();
		val.roadWidth = 6f;
		val.roadMaterial = Resources.Load("Materials/roads/road material") as Material;
		val.layer = 1;
		val.tag = "Untagged";
		Vector3[] array = new Vector3[4]
		{
			new Vector3(200f, 5f, 200f),
			new Vector3(250f, 5f, 200f),
			new Vector3(250f, 5f, 250f),
			new Vector3(300f, 5f, 250f)
		};
		road = roadNetwork.CreateRoad("road 1", val, array);
		road.AddMarker(new Vector3(300f, 5f, 300f), true);
		road.InsertMarker(new Vector3(275f, 5f, 235f), true);
		road.DeleteMarker(2, true);
		roadNetwork.BuildRoadNetwork();
		go = GameObject.CreatePrimitive(PrimitiveType.Cube);
	}

	private void Update()
	{
		if (roadNetwork != null)
		{
			float num = Time.deltaTime * speed;
			distance += num;
			Vector3 position = road.GetPosition(distance, ref currentElement);
			position.y += 1f;
			go.transform.position = position;
			go.transform.forward = road.GetLookatSmooth(distance, currentElement);
		}
	}

	private void OnDestroy()
	{
		if (roadNetwork != null && roadNetwork.isInBuildMode)
		{
			roadNetwork.RestoreRoadNetwork();
			Debug.Log("Restore Road Network");
		}
	}
}
