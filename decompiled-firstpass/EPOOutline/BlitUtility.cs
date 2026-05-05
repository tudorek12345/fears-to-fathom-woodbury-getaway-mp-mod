using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EPOOutline;

public static class BlitUtility
{
	public struct MeshSetupResult(int vertexIndex, int triangleIndex)
	{
		public readonly int VertexIndex = vertexIndex;

		public readonly int TriangleIndex = triangleIndex;
	}

	public struct Vertex
	{
		public Vector4 Position;

		public Vector3 Normal;
	}

	private static readonly int MainTexHash = Shader.PropertyToID("_MainTex");

	private static Vector4[] normals = new Vector4[8]
	{
		new Vector4(-0.578f, -0.578f, -0.578f),
		new Vector4(0.578f, -0.578f, -0.578f),
		new Vector4(0.578f, 0.578f, -0.578f),
		new Vector4(-0.578f, 0.578f, -0.578f),
		new Vector4(-0.578f, 0.578f, 0.578f),
		new Vector4(0.578f, 0.578f, 0.578f),
		new Vector4(0.578f, -0.578f, 0.578f),
		new Vector4(-0.578f, -0.578f, 0.578f)
	};

	private static Vector4[] tempVertecies = new Vector4[8]
	{
		new Vector4(-0.5f, -0.5f, -0.5f, 1f),
		new Vector4(0.5f, -0.5f, -0.5f, 1f),
		new Vector4(0.5f, 0.5f, -0.5f, 1f),
		new Vector4(-0.5f, 0.5f, -0.5f, 1f),
		new Vector4(-0.5f, 0.5f, 0.5f, 1f),
		new Vector4(0.5f, 0.5f, 0.5f, 1f),
		new Vector4(0.5f, -0.5f, 0.5f, 1f),
		new Vector4(-0.5f, -0.5f, 0.5f, 1f)
	};

	private static VertexAttributeDescriptor[] vertexParams = new VertexAttributeDescriptor[2]
	{
		new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
		new VertexAttributeDescriptor(VertexAttribute.Normal)
	};

	private static ushort[] indecies = new ushort[20480];

	private static Vertex[] vertices = new Vertex[4096];

	private static Matrix4x4[] matrices = new Matrix4x4[4096];

	private static int itemsToDraw = 0;

	private static bool? supportsInstancing;

	private static bool SupportsInstancing
	{
		get
		{
			if (supportsInstancing.HasValue)
			{
				return supportsInstancing.Value;
			}
			supportsInstancing = SystemInfo.supportsInstancing;
			return supportsInstancing.Value;
		}
	}

	private static void UpdateBounds(Renderer renderer, OutlineTarget target)
	{
		if (target.renderer is MeshRenderer)
		{
			MeshFilter component = renderer.GetComponent<MeshFilter>();
			if (component.sharedMesh != null)
			{
				component.sharedMesh.RecalculateBounds();
			}
		}
		else if (target.renderer is SkinnedMeshRenderer)
		{
			SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
			if (skinnedMeshRenderer.sharedMesh != null)
			{
				skinnedMeshRenderer.sharedMesh.RecalculateBounds();
			}
		}
	}

	public static void PrepareForRendering(OutlineParameters parameters)
	{
		if (parameters.BlitMesh == null)
		{
			parameters.BlitMesh = parameters.MeshPool.AllocateMesh();
		}
		MeshSetupResult? meshSetupResult = (SupportsInstancing ? SetupForInstancing(parameters) : SetupForBruteForce(parameters));
		if (meshSetupResult.HasValue)
		{
			parameters.BlitMesh.SetVertexBufferParams(meshSetupResult.Value.VertexIndex, vertexParams);
			parameters.BlitMesh.SetVertexBufferData(vertices, 0, 0, meshSetupResult.Value.VertexIndex, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			parameters.BlitMesh.SetIndexBufferParams(meshSetupResult.Value.TriangleIndex, IndexFormat.UInt16);
			parameters.BlitMesh.SetIndexBufferData(indecies, 0, 0, meshSetupResult.Value.TriangleIndex, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			parameters.BlitMesh.subMeshCount = 1;
			parameters.BlitMesh.SetSubMesh(0, new SubMeshDescriptor(0, meshSetupResult.Value.TriangleIndex), MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
		}
	}

	private static MeshSetupResult? SetupForInstancing(OutlineParameters parameters)
	{
		if (vertices.Length < 8)
		{
			Array.Resize(ref vertices, 16);
			Array.Resize(ref indecies, vertices.Length * 5);
		}
		MeshSetupResult? meshSetupResult = null;
		int triangleIndex = 0;
		int num = 0;
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 2);
		indecies[triangleIndex++] = (ushort)(num + 1);
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 3);
		indecies[triangleIndex++] = (ushort)(num + 2);
		indecies[triangleIndex++] = (ushort)(num + 2);
		indecies[triangleIndex++] = (ushort)(num + 3);
		indecies[triangleIndex++] = (ushort)(num + 4);
		indecies[triangleIndex++] = (ushort)(num + 2);
		indecies[triangleIndex++] = (ushort)(num + 4);
		indecies[triangleIndex++] = (ushort)(num + 5);
		indecies[triangleIndex++] = (ushort)(num + 1);
		indecies[triangleIndex++] = (ushort)(num + 2);
		indecies[triangleIndex++] = (ushort)(num + 5);
		indecies[triangleIndex++] = (ushort)(num + 1);
		indecies[triangleIndex++] = (ushort)(num + 5);
		indecies[triangleIndex++] = (ushort)(num + 6);
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 7);
		indecies[triangleIndex++] = (ushort)(num + 4);
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 4);
		indecies[triangleIndex++] = (ushort)(num + 3);
		indecies[triangleIndex++] = (ushort)(num + 5);
		indecies[triangleIndex++] = (ushort)(num + 4);
		indecies[triangleIndex++] = (ushort)(num + 7);
		indecies[triangleIndex++] = (ushort)(num + 5);
		indecies[triangleIndex++] = (ushort)(num + 7);
		indecies[triangleIndex++] = (ushort)(num + 6);
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 6);
		indecies[triangleIndex++] = (ushort)(num + 7);
		indecies[triangleIndex++] = (ushort)num;
		indecies[triangleIndex++] = (ushort)(num + 1);
		indecies[triangleIndex++] = (ushort)(num + 6);
		for (int i = 0; i < 8; i++)
		{
			vertices[num++] = new Vertex
			{
				Position = tempVertecies[i],
				Normal = normals[i]
			};
		}
		meshSetupResult = new MeshSetupResult(num, triangleIndex);
		int num2 = 0;
		foreach (Outlinable item in parameters.OutlinablesToRender)
		{
			if (item.DrawingMode != OutlinableDrawingMode.Normal)
			{
				continue;
			}
			foreach (OutlineTarget outlineTarget in item.OutlineTargets)
			{
				Renderer renderer = outlineTarget.Renderer;
				if (!outlineTarget.IsVisible)
				{
					continue;
				}
				bool flag = false;
				Bounds bounds = default(Bounds);
				if (outlineTarget.BoundsMode == BoundsMode.Manual)
				{
					bounds = outlineTarget.Bounds;
					Vector3 size = bounds.size;
					Vector3 localScale = renderer.transform.localScale;
					size.x /= localScale.x;
					size.y /= localScale.y;
					size.z /= localScale.z;
					bounds.size = size;
				}
				else
				{
					if (outlineTarget.BoundsMode == BoundsMode.ForceRecalculate)
					{
						UpdateBounds(outlineTarget.Renderer, outlineTarget);
					}
					MeshRenderer meshRenderer = renderer as MeshRenderer;
					int num3 = ((!(meshRenderer == null)) ? meshRenderer.subMeshStartIndex : 0) + outlineTarget.SubmeshIndex;
					MeshFilter meshFilter = ((meshRenderer == null) ? null : meshRenderer.GetComponent<MeshFilter>());
					Mesh mesh = ((meshFilter == null) ? null : meshFilter.sharedMesh);
					if (mesh != null && mesh.subMeshCount > num3)
					{
						bounds = mesh.GetSubMesh(num3).bounds;
						flag = meshRenderer.isPartOfStaticBatch;
					}
					else
					{
						flag = true;
						bounds = renderer.bounds;
					}
				}
				if (flag)
				{
					matrices[num2++] = Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size);
					continue;
				}
				Transform transform = outlineTarget.renderer.transform;
				Vector3 size2 = bounds.size;
				matrices[num2++] = transform.localToWorldMatrix * Matrix4x4.Translate(bounds.center) * Matrix4x4.Scale(size2);
			}
		}
		itemsToDraw = num2;
		return meshSetupResult;
	}

	private static MeshSetupResult? SetupForBruteForce(OutlineParameters parameters)
	{
		int num = 0;
		int triangleIndex = 0;
		int num2 = 0;
		foreach (Outlinable item in parameters.OutlinablesToRender)
		{
			num2 += 8 * item.OutlineTargets.Count;
		}
		if (vertices.Length < num2)
		{
			Array.Resize(ref vertices, num2 * 2);
			Array.Resize(ref indecies, vertices.Length * 5);
		}
		foreach (Outlinable item2 in parameters.OutlinablesToRender)
		{
			if (item2.DrawingMode != OutlinableDrawingMode.Normal)
			{
				continue;
			}
			foreach (OutlineTarget outlineTarget in item2.OutlineTargets)
			{
				Renderer renderer = outlineTarget.Renderer;
				if (!outlineTarget.IsVisible)
				{
					continue;
				}
				bool flag = false;
				Bounds bounds = default(Bounds);
				if (outlineTarget.BoundsMode == BoundsMode.Manual)
				{
					bounds = outlineTarget.Bounds;
					Vector3 size = bounds.size;
					Vector3 localScale = renderer.transform.localScale;
					size.x /= localScale.x;
					size.y /= localScale.y;
					size.z /= localScale.z;
					bounds.size = size;
				}
				else
				{
					if (outlineTarget.BoundsMode == BoundsMode.ForceRecalculate)
					{
						UpdateBounds(outlineTarget.Renderer, outlineTarget);
					}
					MeshRenderer meshRenderer = renderer as MeshRenderer;
					int num3 = ((!(meshRenderer == null)) ? meshRenderer.subMeshStartIndex : 0) + outlineTarget.SubmeshIndex;
					MeshFilter meshFilter = ((meshRenderer == null) ? null : meshRenderer.GetComponent<MeshFilter>());
					Mesh mesh = ((meshFilter == null) ? null : meshFilter.sharedMesh);
					if (mesh != null && mesh.subMeshCount > num3)
					{
						bounds = mesh.GetSubMesh(num3).bounds;
					}
					else
					{
						flag = true;
						bounds = renderer.bounds;
					}
				}
				Vector4 vector = bounds.size;
				vector.w = 1f;
				Vector4 vector2 = bounds.center;
				Matrix4x4 matrix4x = Matrix4x4.identity;
				Matrix4x4 matrix4x2 = Matrix4x4.identity;
				if (!flag && (outlineTarget.BoundsMode == BoundsMode.Manual || !renderer.isPartOfStaticBatch))
				{
					matrix4x = outlineTarget.renderer.transform.localToWorldMatrix;
					matrix4x2 = Matrix4x4.Rotate(renderer.transform.rotation);
				}
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 2);
				indecies[triangleIndex++] = (ushort)(num + 1);
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 3);
				indecies[triangleIndex++] = (ushort)(num + 2);
				indecies[triangleIndex++] = (ushort)(num + 2);
				indecies[triangleIndex++] = (ushort)(num + 3);
				indecies[triangleIndex++] = (ushort)(num + 4);
				indecies[triangleIndex++] = (ushort)(num + 2);
				indecies[triangleIndex++] = (ushort)(num + 4);
				indecies[triangleIndex++] = (ushort)(num + 5);
				indecies[triangleIndex++] = (ushort)(num + 1);
				indecies[triangleIndex++] = (ushort)(num + 2);
				indecies[triangleIndex++] = (ushort)(num + 5);
				indecies[triangleIndex++] = (ushort)(num + 1);
				indecies[triangleIndex++] = (ushort)(num + 5);
				indecies[triangleIndex++] = (ushort)(num + 6);
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 7);
				indecies[triangleIndex++] = (ushort)(num + 4);
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 4);
				indecies[triangleIndex++] = (ushort)(num + 3);
				indecies[triangleIndex++] = (ushort)(num + 5);
				indecies[triangleIndex++] = (ushort)(num + 4);
				indecies[triangleIndex++] = (ushort)(num + 7);
				indecies[triangleIndex++] = (ushort)(num + 5);
				indecies[triangleIndex++] = (ushort)(num + 7);
				indecies[triangleIndex++] = (ushort)(num + 6);
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 6);
				indecies[triangleIndex++] = (ushort)(num + 7);
				indecies[triangleIndex++] = (ushort)num;
				indecies[triangleIndex++] = (ushort)(num + 1);
				indecies[triangleIndex++] = (ushort)(num + 6);
				for (int i = 0; i < 8; i++)
				{
					Vector4 vector3 = matrix4x2 * normals[i];
					Vector4 vector4 = tempVertecies[i];
					Vector4 vector5 = new Vector4(vector4.x * vector.x, vector4.y * vector.y, vector4.z * vector.z, 1f);
					Vertex vertex = new Vertex
					{
						Position = matrix4x * (vector2 + vector5),
						Normal = vector3
					};
					vertices[num++] = vertex;
				}
			}
		}
		return new MeshSetupResult(num, triangleIndex);
	}

	public static void Blit(OutlineParameters parameters, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier destinationDepth, Material material, CommandBuffer targetBuffer, int pass = -1, Rect? viewport = null)
	{
		CommandBuffer commandBuffer = ((targetBuffer == null) ? parameters.Buffer : targetBuffer);
		commandBuffer.SetRenderTarget(destination, destinationDepth);
		if (viewport.HasValue)
		{
			parameters.Buffer.SetViewport(viewport.Value);
		}
		commandBuffer.SetGlobalTexture(MainTexHash, source);
		if (SupportsInstancing)
		{
			commandBuffer.DrawMeshInstanced(parameters.BlitMesh, 0, material, pass, matrices, itemsToDraw);
		}
		else
		{
			commandBuffer.DrawMesh(parameters.BlitMesh, Matrix4x4.identity, material, 0, pass);
		}
	}

	public static void Draw(OutlineParameters parameters, RenderTargetIdentifier target, RenderTargetIdentifier depth, Material material, Rect? viewport = null)
	{
		parameters.Buffer.SetRenderTarget(target, depth);
		if (viewport.HasValue)
		{
			parameters.Buffer.SetViewport(viewport.Value);
		}
		if (SupportsInstancing)
		{
			parameters.Buffer.DrawMeshInstanced(parameters.BlitMesh, 0, material, -1, matrices, itemsToDraw);
		}
		else
		{
			parameters.Buffer.DrawMesh(parameters.BlitMesh, Matrix4x4.identity, material, 0, -1);
		}
	}
}
