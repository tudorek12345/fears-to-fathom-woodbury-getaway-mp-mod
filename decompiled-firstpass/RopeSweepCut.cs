using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiRope))]
public class RopeSweepCut : MonoBehaviour
{
	public Camera cam;

	private ObiRope rope;

	private LineRenderer lineRenderer;

	private Vector3 cutStartPosition;

	private void Awake()
	{
		rope = GetComponent<ObiRope>();
		AddMouseLine();
	}

	private void OnDestroy()
	{
		DeleteMouseLine();
	}

	private void AddMouseLine()
	{
		GameObject gameObject = new GameObject("Mouse Line");
		lineRenderer = gameObject.AddComponent<LineRenderer>();
		lineRenderer.startWidth = 0.005f;
		lineRenderer.endWidth = 0.005f;
		lineRenderer.numCapVertices = 2;
		lineRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
		lineRenderer.sharedMaterial.color = Color.cyan;
		lineRenderer.enabled = false;
	}

	private void DeleteMouseLine()
	{
		if (lineRenderer != null)
		{
			Object.Destroy(lineRenderer.gameObject);
		}
	}

	private void LateUpdate()
	{
		if (!(cam == null))
		{
			ProcessInput();
		}
	}

	private void ProcessInput()
	{
		if (Input.GetMouseButtonDown(0))
		{
			cutStartPosition = Input.mousePosition;
			lineRenderer.SetPosition(0, cam.ScreenToWorldPoint(new Vector3(cutStartPosition.x, cutStartPosition.y, 0.5f)));
			lineRenderer.enabled = true;
		}
		if (lineRenderer.enabled)
		{
			lineRenderer.SetPosition(1, cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f)));
		}
		if (Input.GetMouseButtonUp(0))
		{
			ScreenSpaceCut(cutStartPosition, Input.mousePosition);
			lineRenderer.enabled = false;
		}
	}

	private void ScreenSpaceCut(Vector2 lineStart, Vector2 lineEnd)
	{
		bool flag = false;
		for (int i = 0; i < ((ObiRopeBase)rope).elements.Count; i++)
		{
			Vector3 vector = cam.WorldToScreenPoint(((ObiNativeList<Vector4>)(object)((ObiActor)rope).solver.positions)[((ObiRopeBase)rope).elements[i].particle1]);
			Vector3 vector2 = cam.WorldToScreenPoint(((ObiNativeList<Vector4>)(object)((ObiActor)rope).solver.positions)[((ObiRopeBase)rope).elements[i].particle2]);
			if (SegmentSegmentIntersection(vector, vector2, lineStart, lineEnd, out var _, out var _))
			{
				flag = true;
				rope.Tear(((ObiRopeBase)rope).elements[i]);
			}
		}
		if (flag)
		{
			((ObiRopeBase)rope).RebuildConstraintsFromElements();
		}
	}

	private bool SegmentSegmentIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out float r, out float s)
	{
		float num = (B.x - A.x) * (D.y - C.y) - (B.y - A.y) * (D.x - C.x);
		float num2 = (A.y - C.y) * (D.x - C.x) - (A.x - C.x) * (D.y - C.y);
		float num3 = (A.y - C.y) * (B.x - A.x) - (A.x - C.x) * (B.y - A.y);
		if (Mathf.Approximately(num2, 0f) || Mathf.Approximately(num, 0f))
		{
			r = -1f;
			s = -1f;
			return false;
		}
		r = num2 / num;
		s = num3 / num;
		if (r >= 0f && r <= 1f && s >= 0f)
		{
			return s <= 1f;
		}
		return false;
	}
}
