using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace RopeToolkit;

public class Rope : MonoBehaviour
{
	public struct Measurements
	{
		public float spawnCurveLength;

		public float realCurveLength;

		public int segmentCount;

		public int particleCount;

		public float particleSpacing;

		public int GetParticleIndexAt(float distance)
		{
			return math.clamp((int)(distance / particleSpacing + 0.5f), 0, particleCount - 1);
		}
	}

	public struct OnSplitParams
	{
		public int minParticleIndex;

		public int maxParticleIndex;

		public Measurements preSplitMeasurements;
	}

	public struct EditorColors
	{
		public Color ropeSegments;

		public Color simulationParticle;

		public Color collisionParticle;

		public Color spawnPointHandle;
	}

	[Serializable]
	public struct CustomMeshSettings
	{
		[Tooltip("If specified, this mesh is rendered instead of the default rope cylinder at each simulation particle. The z-axis of the mesh will align with the rope tangent and the mesh will be scaled so that z=0 is the current simulation particle and z=1 is the next simulation particle. The material specified for the rope must support instancing.")]
		public Mesh mesh;

		[Tooltip("When using a custom mesh, this property specifies how much to rotate the mesh around the z-axis for every link in the chain of simulation particles.")]
		[Range(0f, 360f)]
		public float rotation;

		[Tooltip("When using a custom mesh, this property can be used to tweak the scale")]
		public Vector3 scale;

		[Tooltip("When using a custom mesh, this property specifies whether or not the mesh should be stretched lengthwise along with the rope.")]
		public bool stretch;
	}

	[Serializable]
	public struct SimulationSettings
	{
		[Tooltip("Turns on or off the simulation independently of the rendering of the rope. A use case could be to programmatically disable ropes that are too far away from the camera or ropes that are not visible.")]
		public bool enabled;

		[Header("Base characteristics")]
		[Tooltip("The number of simulation particles per meter. A higher resolution results in a smoother looking rope but requires more compute.")]
		[DisableInPlayMode]
		public float resolution;

		[Tooltip("The mass per meter of the rope. This value is used when interacting with rigidbodies via RopeRigidbodyConnection components.")]
		[Delayed]
		public float massPerMeter;

		[Tooltip("A measure of the stiffness of the rope. Note that the actual stiffness is heavily dependent on the number of solver iterations and the size of the physics time step used, if you change one value you problably need to re-tweak the other(s). This particular value does not influence performance.")]
		[Range(0.01f, 1f)]
		public float stiffness;

		[Tooltip("The percentage of energy to remove from the simulation each fixed update. Useful to model air resistance. Does not influence performance.")]
		[Range(0f, 1f)]
		public float energyLoss;

		[Header("Modifiers")]
		[Tooltip("A value that dynamically shortens or lengthens the rope by a multiplicative factor. This can be used to create a retractable grappling hook for example.")]
		[Range(0f, 2f)]
		public float lengthMultiplier;

		[Tooltip("The percentage of the gravity force to apply to the rope. A low gravity multiplier might be useful to straighten out ropes that otherwise sack but should be considered a 'hack' as the rope will behave as if it is in space.")]
		[Range(0f, 1f)]
		public float gravityMultiplier;

		[Tooltip("Whether to use a custom gravity value from this component or the global physics gravity")]
		public bool useCustomGravity;

		[Tooltip("The gravity force to use for this particular rope when not using global gravity")]
		public float3 customGravity;

		[Header("Advanced (changing these will require tweaking base characteristics)")]
		[Range(1f, 10f)]
		[Tooltip("The number of substeps that each fixed update should be divided into. A high substep count results in stiffer simulations since small deflections due to gravity can be countered early. The exception is if the rope is fixed between 2 rigidbodies, then the fixed update rate of the project determines stiffness.")]
		public int substeps;

		[Tooltip("The number of solver iterations to run for this rope. High resolution ropes need more iterations to become stiff. More iterations requires more compute.")]
		[Range(1f, 32f)]
		public int solverIterations;
	}

	[Serializable]
	public struct CollisionSettings
	{
		[Tooltip("Enables collision handling for the rope so that it reacts to colliders other than the ones it is connected to via RopeConnection components. Performance intensive on the main thread.")]
		public bool enabled;

		[Tooltip("Whether or not the rope should influence rigidbodies when it collides with them.")]
		public bool influenceRigidbodies;

		[Tooltip("Check and respond to collisions on every n:th simulation particle. A value of one will make every simulated particle react to collisions, a value of two will make every other particle react to collisions and so on. As one sphere-overlap test is performed per particle, a low value is very performance intensive. Collision particles are visualized by yellow spheres when the rope is selected.")]
		[Range(1f, 20f)]
		public int stride;

		[Tooltip("The dynamic friction coefficient of the rope. Used to slow the rope down if it is dragged along the ground for example.")]
		[Range(0f, 20f)]
		public float friction;

		[Tooltip("An extra distance (added ontop of the rope radius) that prevents small radius ropes from falling through geometry easily")]
		[Range(0f, 1f)]
		public float collisionMargin;

		public LayerMask ignoreLayers;
	}

	protected struct CollisionPlane
	{
		public float3 point;

		public float3 normal;

		public float3 velocityChange;

		public float3 feedback;
	}

	protected struct ParticleTarget
	{
		public int particleIndex;

		public float3 position;

		public float stiffness;
	}

	protected struct RigidbodyConnection
	{
		public Rigidbody rigidbody;

		public float rigidbodyDamping;

		public ParticleTarget target;
	}

	[BurstCompile]
	private struct SimulateJob : IJob
	{
		[ReadOnly]
		public float deltaTime;

		[ReadOnly]
		public float3 externalAcceleration;

		[ReadOnly]
		public float energyKept;

		public NativeArray<float3> positions;

		public NativeArray<float3> prevPositions;

		[ReadOnly]
		public NativeArray<float> massMultipliers;

		[ReadOnly]
		public bool isLoop;

		[ReadOnly]
		public int substeps;

		[ReadOnly]
		public int solverIterations;

		[ReadOnly]
		public float stiffness;

		[ReadOnly]
		public float desiredSpacing;

		[ReadOnly]
		public bool collisionsEnabled;

		[ReadOnly]
		public float radius;

		[ReadOnly]
		public float friction;

		[ReadOnly]
		public int maxCollisionPlanesPerParticle;

		[ReadOnly]
		public NativeArray<int> collisionPlanesActive;

		public NativeArray<CollisionPlane> collisionPlanes;

		[ReadOnly]
		public NativeArray<ParticleTarget> particleTargets;

		public NativeArray<float3> particleTargetFeedbacks;

		public void Execute()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_024c: Unknown result type (might be due to invalid IL or missing references)
			//IL_025e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
			//IL_026f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0294: Unknown result type (might be due to invalid IL or missing references)
			//IL_0296: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < particleTargetFeedbacks.Length; i++)
			{
				particleTargetFeedbacks[i] = float3.zero;
			}
			float num = deltaTime / (float)substeps;
			float num2 = 1f / num;
			bool flag = true;
			for (int j = 0; j < substeps; j++)
			{
				for (int k = 0; k < positions.Length; k++)
				{
					if (massMultipliers[k] == 0f)
					{
						prevPositions[k] = positions[k];
						continue;
					}
					float3 val = positions[k];
					float3 val2 = prevPositions[k];
					float3 val3 = (val - val2) * num2;
					val3 += externalAcceleration * num;
					val3 *= energyKept;
					prevPositions[k] = val;
					ref NativeArray<float3> reference = ref positions;
					int index = k;
					reference[index] += val3 * num;
				}
				for (int l = 0; l < solverIterations; l++)
				{
					int num3 = (isLoop ? positions.Length : (positions.Length - 1));
					if (flag)
					{
						for (int m = 0; m < num3; m++)
						{
							ApplyStickConstraint(m, (m + 1) % positions.Length);
						}
					}
					else
					{
						for (int num4 = num3 - 1; num4 >= 0; num4--)
						{
							ApplyStickConstraint(num4, (num4 + 1) % positions.Length);
						}
					}
					flag = !flag;
					if (collisionsEnabled)
					{
						for (int n = 0; n < positions.Length; n++)
						{
							for (int num5 = 0; num5 < collisionPlanesActive[n]; num5++)
							{
								int index2 = n * maxCollisionPlanesPerParticle + num5;
								CollisionPlane plane = collisionPlanes[index2];
								ApplyCollisionConstraint(n, ref plane);
								collisionPlanes[index2] = plane;
							}
						}
					}
					for (int num6 = 0; num6 < particleTargets.Length; num6++)
					{
						ParticleTarget particleTarget = particleTargets[num6];
						if (particleTarget.particleIndex != -1)
						{
							float3 val4 = (particleTarget.position - positions[particleTarget.particleIndex]) * particleTarget.stiffness;
							ref NativeArray<float3> reference = ref positions;
							int index = particleTarget.particleIndex;
							reference[index] += val4;
							reference = ref particleTargetFeedbacks;
							index = num6;
							reference[index] -= val4 * massMultipliers[particleTarget.particleIndex];
						}
					}
				}
			}
		}

		private void ApplyStickConstraint(int idx0, int idx1)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			float3 val = positions[idx0] - positions[idx1];
			float num = math.length(val);
			val = ((!(num > 0f)) ? float3.op_Implicit(0f) : (val / num));
			float num2 = (num - desiredSpacing) * stiffness;
			float num3 = massMultipliers[idx0];
			if (num3 > 0f)
			{
				num3 = 1f / num3;
			}
			float num4 = massMultipliers[idx1];
			if (num4 > 0f)
			{
				num4 = 1f / num4;
			}
			float num5 = num3 + num4;
			if (num5 > 0f)
			{
				num5 = 1f / num5;
			}
			ref NativeArray<float3> reference = ref positions;
			int index = idx0;
			reference[index] -= val * (num2 * num3 * num5);
			reference = ref positions;
			index = idx1;
			reference[index] += val * (num2 * num4 * num5);
		}

		private void ApplyCollisionConstraint(int idx, ref CollisionPlane plane)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			float num = math.dot(positions[idx] - plane.point, plane.normal);
			if (num <= radius)
			{
				float num2 = radius - num;
				float3 val = plane.normal * num2;
				ref NativeArray<float3> reference = ref positions;
				int index = idx;
				reference[index] += val;
				ref float3 feedback = ref plane.feedback;
				feedback -= val * massMultipliers[idx];
				float3 val2 = positions[idx] - prevPositions[idx] - plane.velocityChange;
				float num3 = math.lengthsq(val2);
				if (num3 > 0f)
				{
					num3 = math.sqrt(num3);
					val2 /= num3;
				}
				reference = ref prevPositions;
				index = idx;
				reference[index] += val2 * math.min(num2 * friction, num3);
			}
		}
	}

	[BurstCompile]
	private struct InterpolatePositionsJob : IJob
	{
		[ReadOnly]
		public NativeArray<float3> positions;

		[ReadOnly]
		public NativeArray<float3> prevPositions;

		[ReadOnly]
		public float invDeltaTime;

		[ReadOnly]
		public float timeSinceFixedUpdate;

		[WriteOnly]
		public NativeArray<float3> interpolatedPositions;

		public void Execute()
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			float num = timeSinceFixedUpdate * invDeltaTime;
			for (int i = 0; i < interpolatedPositions.Length; i++)
			{
				interpolatedPositions[i] = math.lerp(prevPositions[i], positions[i], num);
			}
		}
	}

	[BurstCompile]
	private struct ExtrapolatePositionsJob : IJob
	{
		[ReadOnly]
		public NativeArray<float3> positions;

		[ReadOnly]
		public NativeArray<float3> prevPositions;

		[ReadOnly]
		public float invDeltaTime;

		[ReadOnly]
		public float timeSinceFixedUpdate;

		[WriteOnly]
		public NativeArray<float3> interpolatedPositions;

		public void Execute()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < interpolatedPositions.Length; i++)
			{
				float3 val = (positions[i] - prevPositions[i]) * invDeltaTime;
				interpolatedPositions[i] = positions[i] + val * timeSinceFixedUpdate;
			}
		}
	}

	[BurstCompile]
	private struct OutputVerticesJob : IJob
	{
		[ReadOnly]
		public NativeArray<float3> positions;

		public NativeArray<float3> bitangents;

		[ReadOnly]
		public bool isLoop;

		[ReadOnly]
		public int radialVertices;

		[ReadOnly]
		public float radius;

		[ReadOnly]
		public NativeArray<float3> cosLookup;

		[ReadOnly]
		public NativeArray<float3> sinLookup;

		[WriteOnly]
		public NativeArray<Vector3> vertices;

		[WriteOnly]
		public NativeArray<Vector3> normals;

		public void Execute()
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0172: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_017a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0235: Unknown result type (might be due to invalid IL or missing references)
			//IL_0242: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0224: Unknown result type (might be due to invalid IL or missing references)
			//IL_0256: Unknown result type (might be due to invalid IL or missing references)
			//IL_025b: Unknown result type (might be due to invalid IL or missing references)
			//IL_025d: Unknown result type (might be due to invalid IL or missing references)
			//IL_025f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_0268: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0270: Unknown result type (might be due to invalid IL or missing references)
			//IL_0275: Unknown result type (might be due to invalid IL or missing references)
			//IL_024c: Unknown result type (might be due to invalid IL or missing references)
			//IL_027f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_028e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0293: Unknown result type (might be due to invalid IL or missing references)
			//IL_029d: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02da: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
			int num = positions.Length - 1;
			NativeArray<float3> nativeArray = new NativeArray<float3>(bitangents.Length, Allocator.Temp);
			nativeArray[0] = bitangents[0] + bitangents[1];
			if (isLoop)
			{
				ref NativeArray<float3> reference = ref nativeArray;
				reference[0] = reference[0] + bitangents[num];
			}
			for (int i = 1; i < bitangents.Length - 1; i++)
			{
				nativeArray[i] = bitangents[i - 1] + bitangents[i] + bitangents[i + 1];
			}
			nativeArray[num] = bitangents[num - 1] + bitangents[num];
			if (isLoop)
			{
				ref NativeArray<float3> reference = ref nativeArray;
				int index = num;
				reference[index] += bitangents[0];
			}
			for (int j = 0; j < bitangents.Length; j++)
			{
				float3 val = positions[(j + 1) % positions.Length] - positions[j];
				float3 val2 = math.cross(val, nativeArray[j]);
				bitangents[j] = math.normalizesafe(math.cross(val2, val), default(float3));
			}
			if (!isLoop)
			{
				bitangents[num] = bitangents[num - 1];
			}
			for (int k = 0; k < positions.Length; k++)
			{
				float3 zero = float3.zero;
				zero = ((!isLoop) ? ((k < num) ? (positions[k + 1] - positions[k]) : (positions[k] - positions[k - 1])) : (positions[(k + 1) % positions.Length] - positions[k]));
				float3 val3 = bitangents[k];
				float3 val4 = math.normalizesafe(math.cross(zero, val3), default(float3));
				for (int l = 0; l < radialVertices; l++)
				{
					float3 val5 = val3 * cosLookup[l] + val4 * sinLookup[l];
					vertices[k * radialVertices + l] = float3.op_Implicit(positions[k] + val5 * radius);
					normals[k * radialVertices + l] = float3.op_Implicit(val5);
				}
			}
		}
	}

	protected const int MaxCollisionPlanesPerParticle = 3;

	protected const int InitialParticleTargets = 3;

	protected const int MaxRigidbodyConnections = 24;

	public static readonly EditorColors Colors = new EditorColors
	{
		ropeSegments = Color.black,
		simulationParticle = new Color(0.2f, 0.8f, 0.2f, 0.5f),
		collisionParticle = new Color(1f, 0.92f, 0.016f, 0.5f),
		spawnPointHandle = new Color(0.1f, 0.5f, 0.8f)
	};

	[Tooltip("The radius of the rope. This value is used both for constructing the visual mesh and handling collisions.")]
	[Range(0.0001f, 1f)]
	public float radius = 0.05f;

	[Tooltip("The number of vertices to use for each segment of the rope's visual mesh. More vertices results in a rounder looking rope but increases the overall vertex and triangle count of the visual mesh. This value does not influence the simulation of the rope at all.")]
	[DisableInPlayMode]
	[Range(3f, 32f)]
	public int radialVertices = 6;

	[Tooltip("Whether or not the rope is a circular loop. If enabled, the last spawn point of the rope will be connected to the first spawn point.")]
	[DisableInPlayMode]
	public bool isLoop;

	[Tooltip("The material used to render the rope. This can be any material that uses vertex positions and optionally normals.")]
	public Material material;

	[Tooltip("The shadow casting mode to use for the rope")]
	public ShadowCastingMode shadowMode = ShadowCastingMode.On;

	public CustomMeshSettings customMesh = new CustomMeshSettings
	{
		mesh = null,
		rotation = 90f,
		scale = Vector3.one,
		stretch = false
	};

	[Tooltip("The spawn points used to initially place the rope in the world. Currently, pairs of consequtive spawn points are considered linear line segments.")]
	[DisableInPlayMode]
	public List<float3> spawnPoints = new List<float3>();

	[Tooltip("The interpolation mode to use in between calls to FixedUpdate(). Only meaningful if the fixed update rate is low. See documentation for Rigidbody.interpolation for more information.")]
	[DisableInPlayMode]
	public RopeInterpolation interpolation;

	[Space]
	public SimulationSettings simulation = new SimulationSettings
	{
		enabled = true,
		resolution = 10f,
		massPerMeter = 0.2f,
		stiffness = 1f,
		lengthMultiplier = 1f,
		energyLoss = 0.0025f,
		gravityMultiplier = 1f,
		useCustomGravity = false,
		customGravity = float3.op_Implicit(Physics.gravity),
		substeps = 4,
		solverIterations = 2
	};

	[Space]
	public CollisionSettings collisions = new CollisionSettings
	{
		enabled = false,
		influenceRigidbodies = true,
		stride = 2,
		friction = 0.1f,
		collisionMargin = 0.025f,
		ignoreLayers = 0
	};

	protected bool initialized;

	protected bool computingSimulationFrame;

	protected bool simulationDisabledPrevFrame;

	protected bool wasSplit;

	protected float timeSinceFixedUpdate;

	protected JobHandle simulationFrameHandle;

	protected NativeArray<float3> positions;

	protected NativeArray<float3> prevPositions;

	protected NativeArray<float3> interpolatedPositions;

	protected NativeArray<float3> bitangents;

	protected NativeArray<float> massMultipliers;

	protected NativeArray<int> collisionPlanesActive;

	protected NativeArray<CollisionPlane> collisionPlanes;

	protected Rigidbody[] collisionRigidbodies;

	protected List<RigidbodyConnection> queuedRigidbodyConnections;

	protected List<RigidbodyConnection> liveRigidbodyConnections;

	protected NativeArray<ParticleTarget> particleTargets;

	protected NativeArray<float3> particleTargetFeedbacks;

	protected NativeArray<Vector3> vertices;

	protected NativeArray<Vector3> normals;

	protected NativeArray<float3> cosLookup;

	protected NativeArray<float3> sinLookup;

	protected Mesh mesh;

	protected Measurements _measurements;

	protected Collider[] collisionQueryBuffer = new Collider[3];

	protected static Matrix4x4[] customMeshFrames;

	public Measurements measurements
	{
		get
		{
			if (!Initialize())
			{
				return default(Measurements);
			}
			return _measurements;
		}
	}

	public Bounds currentBounds
	{
		get
		{
			if (!Initialize())
			{
				return default(Bounds);
			}
			return mesh.bounds;
		}
	}

	public void OnValidate()
	{
		simulation.resolution = Mathf.Max(0.01f, simulation.resolution);
		simulation.massPerMeter = Mathf.Max(0.01f, simulation.massPerMeter);
	}

	public void PushSpawnPoint()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (spawnPoints.Count == 0)
		{
			spawnPoints.Add(float3.op_Implicit(Vector3.right));
			return;
		}
		float3 val = ((spawnPoints.Count >= 2) ? spawnPoints[spawnPoints.Count - 2] : float3.zero);
		float3 val2 = spawnPoints[spawnPoints.Count - 1];
		spawnPoints.Add(val2 + math.normalizesafe(val2 - val, default(float3)));
	}

	public void PopSpawnPoint()
	{
		if (spawnPoints.Count > 2)
		{
			spawnPoints.RemoveAt(spawnPoints.Count - 1);
		}
	}

	public int GetParticleIndexAt(float distance)
	{
		if (!Initialize() || _measurements.particleSpacing == 0f)
		{
			return 0;
		}
		return _measurements.GetParticleIndexAt(distance);
	}

	public float GetScalarDistanceAt(int particleIndex)
	{
		if (!Initialize() || particleIndex < 0 || particleIndex >= positions.Length)
		{
			return 0f;
		}
		return math.clamp((float)particleIndex / (float)(measurements.particleCount - 1), 0f, 1f);
	}

	public float3 GetPositionAt(int particleIndex, bool respectInterpolation = false)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (!Initialize() || particleIndex < 0 || particleIndex >= positions.Length)
		{
			return float3.zero;
		}
		CompletePreviousSimulationFrame();
		if (respectInterpolation && interpolation != RopeInterpolation.None)
		{
			return interpolatedPositions[particleIndex];
		}
		return positions[particleIndex];
	}

	public void SetPositionAt(int particleIndex, float3 position)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (Initialize() && particleIndex >= 0 && particleIndex < positions.Length)
		{
			CompletePreviousSimulationFrame();
			positions[particleIndex] = position;
		}
	}

	public float3 GetVelocityAt(int particleIndex)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!Initialize() || particleIndex < 0 || particleIndex >= positions.Length)
		{
			return float3.zero;
		}
		CompletePreviousSimulationFrame();
		return (positions[particleIndex] - prevPositions[particleIndex]) / Time.fixedDeltaTime;
	}

	public void SetVelocityAt(int particleIndex, float3 velocity)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (Initialize() && particleIndex >= 0 && particleIndex < positions.Length)
		{
			CompletePreviousSimulationFrame();
			prevPositions[particleIndex] = positions[particleIndex] - velocity * Time.fixedDeltaTime;
		}
	}

	public float GetMassMultiplierAt(int particleIndex)
	{
		if (!Initialize() || particleIndex < 0 || particleIndex >= positions.Length)
		{
			return 0f;
		}
		CompletePreviousSimulationFrame();
		return massMultipliers[particleIndex];
	}

	public void SetMassMultiplierAt(int particleIndex, float value)
	{
		if (Initialize() && particleIndex >= 0 && particleIndex < positions.Length)
		{
			CompletePreviousSimulationFrame();
			massMultipliers[particleIndex] = value;
		}
	}

	public void GetClosestParticle(float3 point, out int particleIndex, out float distance)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (!Initialize())
		{
			particleIndex = -1;
			distance = 0f;
		}
		else
		{
			CompletePreviousSimulationFrame();
			positions.GetClosestPoint(point, out particleIndex, out distance);
		}
	}

	public void GetClosestParticle(Ray ray, out int particleIndex, out float distance, out float distanceAlongRay)
	{
		if (!Initialize())
		{
			particleIndex = -1;
			distance = 0f;
			distanceAlongRay = 0f;
		}
		else
		{
			CompletePreviousSimulationFrame();
			positions.GetClosestPoint(ray, out particleIndex, out distance, out distanceAlongRay);
		}
	}

	public void RegisterRigidbodyConnection(int particleIndex, Rigidbody rigidbody, float rigidbodyDamping, float3 pointOnBody, float stiffness)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (Initialize() && particleIndex >= 0 && particleIndex < positions.Length && base.enabled && simulation.enabled)
		{
			queuedRigidbodyConnections.Add(new RigidbodyConnection
			{
				rigidbody = rigidbody,
				rigidbodyDamping = rigidbodyDamping,
				target = new ParticleTarget
				{
					particleIndex = particleIndex,
					position = pointOnBody,
					stiffness = stiffness
				}
			});
		}
	}

	public void ResetToSpawnCurve()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (Initialize())
		{
			CompletePreviousSimulationFrame();
			float4x4 val = float4x4.op_Implicit(base.transform.localToWorldMatrix);
			spawnPoints.GetPointsAlongCurve(ref val, _measurements.particleSpacing, positions);
			positions.CopyTo(prevPositions);
		}
	}

	public float GetCurrentLength()
	{
		if (!Initialize())
		{
			return 0f;
		}
		CompletePreviousSimulationFrame();
		return positions.GetLengthOfCurve(isLoop);
	}

	protected Rope InstantiateSplitRope(int minIdx, int maxIdx, string identifier)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		int num = maxIdx - minIdx + 1;
		if (minIdx < 0 || maxIdx > positions.Length - 1 || num < 2)
		{
			return null;
		}
		float num2 = _measurements.realCurveLength * ((float)num / (float)_measurements.particleCount);
		float3 val = positions[minIdx];
		float3 val2 = positions[maxIdx];
		float3 val3 = val2 - val;
		float num3 = math.length(val3);
		val2 += math.normalizesafe(val3, default(float3)) * (num2 - num3);
		Rope component = UnityEngine.Object.Instantiate(base.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Rope>();
		component.name = identifier;
		component.isLoop = false;
		component.spawnPoints = new List<float3> { val, val2 };
		if (component.Initialize())
		{
			for (int i = 0; i < component.positions.Length; i++)
			{
				int num4 = minIdx + i;
				if (num4 >= positions.Length)
				{
					break;
				}
				component.positions[i] = positions[num4];
				component.prevPositions[i] = prevPositions[num4];
			}
			component._measurements.realCurveLength = component.GetCurrentLength();
			component._measurements.particleSpacing = _measurements.particleSpacing;
			OnSplitParams onSplitParams = new OnSplitParams
			{
				minParticleIndex = minIdx,
				maxParticleIndex = maxIdx,
				preSplitMeasurements = _measurements
			};
			component.SendMessage("OnRopeSplit", onSplitParams, SendMessageOptions.DontRequireReceiver);
		}
		return component;
	}

	public void SplitAt(int particleIndex, Rope[] outNewRopes = null)
	{
		if (Initialize() && (outNewRopes == null || outNewRopes.Length == 2) && !wasSplit)
		{
			wasSplit = true;
			Rope rope = InstantiateSplitRope(0, particleIndex, base.name + "_split0");
			Rope rope2 = InstantiateSplitRope(particleIndex + 1, positions.Length - 1, base.name + "_split1");
			UnityEngine.Object.Destroy(base.gameObject);
			if (outNewRopes != null)
			{
				outNewRopes[0] = rope;
				outNewRopes[1] = rope2;
			}
		}
	}

	protected void ComputeRealCurve(Allocator allocator, out Measurements measurements, out NativeArray<float3> points)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		float4x4 val = float4x4.op_Implicit(base.transform.localToWorldMatrix);
		float lengthOfCurve = spawnPoints.GetLengthOfCurve(ref val);
		int num = math.max(1, (int)(lengthOfCurve * simulation.resolution));
		int num2 = num + 1;
		float num3 = lengthOfCurve / (float)num;
		points = new NativeArray<float3>(num2, allocator);
		spawnPoints.GetPointsAlongCurve(ref val, num3, points);
		float lengthOfCurve2 = points.GetLengthOfCurve(ref val);
		measurements = new Measurements
		{
			spawnCurveLength = lengthOfCurve,
			realCurveLength = lengthOfCurve2,
			segmentCount = num,
			particleCount = num2,
			particleSpacing = num3
		};
	}

	public void OnEnable()
	{
		if (initialized)
		{
			CompletePreviousSimulationFrame();
		}
	}

	public void Start()
	{
		Initialize();
	}

	public void OnDisable()
	{
		if (initialized)
		{
			CompletePreviousSimulationFrame();
			simulationDisabledPrevFrame = true;
		}
	}

	public void OnDestroy()
	{
		if (initialized)
		{
			CompletePreviousSimulationFrame();
			positions.Dispose();
			prevPositions.Dispose();
			if (interpolatedPositions.IsCreated)
			{
				interpolatedPositions.Dispose();
			}
			bitangents.Dispose();
			massMultipliers.Dispose();
			collisionPlanesActive.Dispose();
			collisionPlanes.Dispose();
			collisionRigidbodies = null;
			particleTargets.Dispose();
			particleTargetFeedbacks.Dispose();
			vertices.Dispose();
			normals.Dispose();
			cosLookup.Dispose();
			sinLookup.Dispose();
			UnityEngine.Object.Destroy(mesh);
		}
	}

	protected bool Initialize()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		if (initialized)
		{
			return true;
		}
		if (!Application.isPlaying || spawnPoints.Count < 2)
		{
			return false;
		}
		ComputeRealCurve(Allocator.Persistent, out _measurements, out positions);
		prevPositions = new NativeArray<float3>(_measurements.particleCount, Allocator.Persistent);
		positions.CopyTo(prevPositions);
		if (interpolation != RopeInterpolation.None)
		{
			interpolatedPositions = new NativeArray<float3>(_measurements.particleCount, Allocator.Persistent);
			positions.CopyTo(interpolatedPositions);
		}
		bitangents = new NativeArray<float3>(_measurements.particleCount, Allocator.Persistent);
		float3 val = default(float3);
		((float3)(ref val))._002Ector(0f, 1f, 0f);
		for (int i = 0; i < bitangents.Length; i++)
		{
			float3 val2 = positions[(i + 1) % bitangents.Length] - positions[i];
			float3 val3 = math.normalizesafe(math.cross(val, val2), default(float3));
			if (math.all(val3 == float3.zero))
			{
				val3 = math.normalizesafe(math.cross(val + new float3(0f, 0f, -1f), val2), default(float3));
			}
			bitangents[i] = val3;
			val = math.cross(val2, val3);
		}
		if (!isLoop)
		{
			bitangents[bitangents.Length - 1] = bitangents[bitangents.Length - 2];
		}
		massMultipliers = new NativeArray<float>(_measurements.particleCount, Allocator.Persistent);
		for (int j = 0; j < massMultipliers.Length; j++)
		{
			massMultipliers[j] = 1f;
		}
		collisionPlanesActive = new NativeArray<int>(_measurements.particleCount, Allocator.Persistent);
		collisionPlanes = new NativeArray<CollisionPlane>(_measurements.particleCount * 3, Allocator.Persistent);
		collisionRigidbodies = new Rigidbody[collisionPlanes.Length];
		queuedRigidbodyConnections = new List<RigidbodyConnection>();
		liveRigidbodyConnections = new List<RigidbodyConnection>();
		particleTargets = new NativeArray<ParticleTarget>(3, Allocator.Persistent);
		particleTargetFeedbacks = new NativeArray<float3>(3, Allocator.Persistent);
		vertices = new NativeArray<Vector3>(_measurements.particleCount * radialVertices, Allocator.Persistent);
		normals = new NativeArray<Vector3>(vertices.Length, Allocator.Persistent);
		cosLookup = new NativeArray<float3>(radialVertices, Allocator.Persistent);
		sinLookup = new NativeArray<float3>(radialVertices, Allocator.Persistent);
		for (int k = 0; k < radialVertices; k++)
		{
			float f = (float)k / (float)(radialVertices - 1) * MathF.PI * 2f;
			cosLookup[k] = float3.op_Implicit(Mathf.Cos(f));
			sinLookup[k] = float3.op_Implicit(Mathf.Sin(f));
		}
		int num = (isLoop ? _measurements.particleCount : (_measurements.particleCount - 1));
		int num2 = num * (radialVertices - 1) * 2 * 3;
		int num3 = ((!isLoop) ? (2 * (radialVertices - 3) * 3) : 0);
		int[] array = new int[num2 + num3];
		int num4 = 0;
		for (int l = 0; l < num; l++)
		{
			int num5 = l * radialVertices;
			int num6 = (l + 1) % _measurements.particleCount * radialVertices;
			for (int m = 0; m < radialVertices - 1; m++)
			{
				int num7 = num5 + m;
				int num8 = num5 + m + 1;
				int num9 = num6 + m;
				int num10 = num6 + m + 1;
				array[num4++] = num7;
				array[num4++] = num8;
				array[num4++] = num9;
				array[num4++] = num9;
				array[num4++] = num8;
				array[num4++] = num10;
			}
		}
		if (!isLoop)
		{
			for (int n = 1; n < radialVertices - 2; n++)
			{
				array[num4++] = 0;
				array[num4++] = n + 1;
				array[num4++] = n;
			}
			int num11 = num * radialVertices;
			for (int num12 = 1; num12 < radialVertices - 2; num12++)
			{
				array[num4++] = num11;
				array[num4++] = num11 + num12;
				array[num4++] = num11 + num12 + 1;
			}
		}
		Vector2[] array2 = new Vector2[vertices.Length];
		for (int num13 = 0; num13 < _measurements.particleCount; num13++)
		{
			Vector2 vector = new Vector2
			{
				x = (float)num13 / (float)(_measurements.particleCount - 1) * _measurements.realCurveLength
			};
			for (int num14 = 0; num14 < radialVertices; num14++)
			{
				vector.y = (float)num14 / (float)(radialVertices - 1);
				array2[num13 * radialVertices + num14] = vector;
			}
		}
		mesh = new Mesh
		{
			name = base.gameObject.name + "_rope"
		};
		mesh.MarkDynamic();
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.uv = array2;
		mesh.triangles = array;
		initialized = true;
		computingSimulationFrame = false;
		return true;
	}

	public void UpdateCollisionPlanes()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		if (!collisions.enabled)
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		int layerMask = ~(int)collisions.ignoreLayers;
		float num = radius + collisions.collisionMargin;
		float num2 = num * num;
		float num3 = num * 1.5f;
		for (int i = 0; i < collisionPlanesActive.Length; i++)
		{
			if (i % collisions.stride != 0)
			{
				collisionPlanesActive[i] = 0;
				for (int j = 0; j < 3; j++)
				{
					collisionRigidbodies[i * 3 + j] = null;
				}
				continue;
			}
			int num4 = 0;
			float3 val = positions[i];
			float3 val2 = prevPositions[i];
			float3 val3 = val - val2;
			val2 = val;
			val += val3;
			int num5 = Physics.OverlapSphereNonAlloc(float3.op_Implicit(val), num3, collisionQueryBuffer, layerMask);
			for (int k = 0; k < num5; k++)
			{
				if (num4 >= 3)
				{
					break;
				}
				Collider collider = collisionQueryBuffer[k];
				MeshCollider meshCollider = collider as MeshCollider;
				if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || (meshCollider != null && meshCollider.convex))
				{
					float3 val4 = float3.op_Implicit(Physics.ClosestPoint(float3.op_Implicit(val), collider, collider.transform.position, collider.transform.rotation));
					float3 val5 = math.normalizesafe(val - val4, default(float3));
					if (!math.all(val5 == float3.zero))
					{
						collisionPlanes[i * 3 + num4] = new CollisionPlane
						{
							point = val4,
							normal = val5,
							velocityChange = ((collider.attachedRigidbody != null) ? (float3.op_Implicit(collider.attachedRigidbody.GetPointVelocity(float3.op_Implicit(val4))) * fixedDeltaTime) : float3.zero)
						};
						collisionRigidbodies[i * 3 + num4] = collider.attachedRigidbody;
						num4++;
					}
				}
			}
			if (num4 < 3 && math.lengthsq(val3) > num2 && Physics.Linecast(float3.op_Implicit(val2), float3.op_Implicit(val), out var hitInfo, layerMask))
			{
				collisionPlanes[i * 3 + num4] = new CollisionPlane
				{
					point = float3.op_Implicit(hitInfo.point),
					normal = float3.op_Implicit(hitInfo.normal),
					velocityChange = ((hitInfo.rigidbody != null) ? (float3.op_Implicit(hitInfo.rigidbody.GetPointVelocity(hitInfo.point)) * fixedDeltaTime) : float3.zero)
				};
				collisionRigidbodies[i * 3 + num4] = hitInfo.rigidbody;
				num4++;
			}
			collisionPlanesActive[i] = num4;
		}
	}

	protected void PrepareRigidbodyConnections()
	{
		liveRigidbodyConnections.AddRange(queuedRigidbodyConnections);
		queuedRigidbodyConnections.Clear();
		if (liveRigidbodyConnections.Count > particleTargets.Length)
		{
			if (liveRigidbodyConnections.Count > 24)
			{
				Debug.LogWarning($"Encountered too many live rigid body connections ({liveRigidbodyConnections.Count}) this frame. " + $"Limiting enforcement to the max value ({24}) to avoid a performance drop...");
			}
			else
			{
				int length = liveRigidbodyConnections.Count * 2;
				particleTargets.Dispose();
				particleTargets = new NativeArray<ParticleTarget>(length, Allocator.Persistent);
				particleTargetFeedbacks.Dispose();
				particleTargetFeedbacks = new NativeArray<float3>(length, Allocator.Persistent);
			}
		}
		for (int i = 0; i < particleTargets.Length; i++)
		{
			if (i < liveRigidbodyConnections.Count)
			{
				RigidbodyConnection rigidbodyConnection = liveRigidbodyConnections[i];
				if (!rigidbodyConnection.rigidbody)
				{
					rigidbodyConnection.target.stiffness = 0f;
				}
				particleTargets[i] = rigidbodyConnection.target;
				if ((bool)rigidbodyConnection.rigidbody && rigidbodyConnection.rigidbody.isKinematic)
				{
					massMultipliers[rigidbodyConnection.target.particleIndex] = 0f;
				}
			}
			else
			{
				particleTargets[i] = new ParticleTarget
				{
					particleIndex = -1
				};
			}
		}
	}

	protected void ApplyRigidbodyFeedback()
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		float num = simulation.massPerMeter * _measurements.realCurveLength / (float)_measurements.particleCount;
		float num2 = 1f / (Time.fixedDeltaTime * (float)simulation.substeps * (float)simulation.solverIterations);
		if (collisions.enabled && collisions.influenceRigidbodies)
		{
			for (int i = 0; i < collisionPlanesActive.Length; i++)
			{
				if (i % collisions.stride != 0)
				{
					continue;
				}
				int num3 = collisionPlanesActive[i];
				for (int j = 0; j < num3; j++)
				{
					Rigidbody rigidbody = collisionRigidbodies[i * 3 + j];
					if (rigidbody != null && !rigidbody.isKinematic)
					{
						CollisionPlane collisionPlane = collisionPlanes[i * 3 + j];
						float3 impulse = collisionPlane.feedback * (num * num2);
						rigidbody.ApplyImpulseNow(collisionPlane.point, impulse);
					}
				}
			}
		}
		if (liveRigidbodyConnections.Count <= 0)
		{
			return;
		}
		int num4 = math.min(liveRigidbodyConnections.Count, particleTargetFeedbacks.Length);
		for (int k = 0; k < num4; k++)
		{
			RigidbodyConnection rigidbodyConnection = liveRigidbodyConnections[k];
			if ((bool)rigidbodyConnection.rigidbody)
			{
				float3 val = particleTargetFeedbacks[k] * (num * num2);
				rigidbodyConnection.rigidbody.ApplyImpulseNow(rigidbodyConnection.target.position, val);
				if (rigidbodyConnection.rigidbodyDamping > 0f)
				{
					float3 normal = math.normalizesafe(val, default(float3));
					rigidbodyConnection.rigidbody.SetPointVelocityNow(rigidbodyConnection.target.position, normal, 0f, rigidbodyConnection.rigidbodyDamping);
				}
			}
			massMultipliers[rigidbodyConnection.target.particleIndex] = 1f;
		}
		liveRigidbodyConnections.Clear();
	}

	protected void ScheduleNextSimulationFrame()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		computingSimulationFrame = true;
		float3 val = (simulation.useCustomGravity ? simulation.customGravity : float3.op_Implicit(Physics.gravity));
		JobHandle dependsOn = new SimulateJob
		{
			deltaTime = Time.fixedDeltaTime,
			externalAcceleration = val * simulation.gravityMultiplier,
			energyKept = 1f - simulation.energyLoss,
			positions = positions,
			prevPositions = prevPositions,
			massMultipliers = massMultipliers,
			isLoop = isLoop,
			substeps = simulation.substeps,
			solverIterations = simulation.solverIterations,
			stiffness = simulation.stiffness,
			desiredSpacing = _measurements.particleSpacing * simulation.lengthMultiplier,
			collisionsEnabled = collisions.enabled,
			radius = radius + collisions.collisionMargin,
			friction = collisions.friction,
			maxCollisionPlanesPerParticle = 3,
			collisionPlanesActive = collisionPlanesActive,
			collisionPlanes = collisionPlanes,
			particleTargets = particleTargets,
			particleTargetFeedbacks = particleTargetFeedbacks
		}.Schedule();
		if (interpolation == RopeInterpolation.None)
		{
			simulationFrameHandle = new OutputVerticesJob
			{
				positions = positions,
				bitangents = bitangents,
				isLoop = isLoop,
				radialVertices = radialVertices,
				radius = radius,
				cosLookup = cosLookup,
				sinLookup = sinLookup,
				vertices = vertices,
				normals = normals
			}.Schedule(dependsOn);
		}
		else
		{
			simulationFrameHandle = dependsOn;
		}
		JobHandle.ScheduleBatchedJobs();
	}

	protected void ScheduleInterpolation()
	{
		if (interpolation != RopeInterpolation.None)
		{
			CompletePreviousSimulationFrame();
			computingSimulationFrame = true;
			float invDeltaTime = 1f / Time.fixedDeltaTime;
			JobHandle jobHandle = default(JobHandle);
			simulationFrameHandle = IJobExtensions.Schedule(dependsOn: (interpolation != RopeInterpolation.Interpolate) ? new ExtrapolatePositionsJob
			{
				positions = positions,
				prevPositions = prevPositions,
				invDeltaTime = invDeltaTime,
				timeSinceFixedUpdate = timeSinceFixedUpdate,
				interpolatedPositions = interpolatedPositions
			}.Schedule() : new InterpolatePositionsJob
			{
				positions = positions,
				prevPositions = prevPositions,
				invDeltaTime = invDeltaTime,
				timeSinceFixedUpdate = timeSinceFixedUpdate,
				interpolatedPositions = interpolatedPositions
			}.Schedule(), jobData: new OutputVerticesJob
			{
				positions = interpolatedPositions,
				bitangents = bitangents,
				isLoop = isLoop,
				radialVertices = radialVertices,
				radius = radius,
				cosLookup = cosLookup,
				sinLookup = sinLookup,
				vertices = vertices,
				normals = normals
			});
			JobHandle.ScheduleBatchedJobs();
		}
	}

	protected void CompletePreviousSimulationFrame()
	{
		if (computingSimulationFrame)
		{
			simulationFrameHandle.Complete();
			computingSimulationFrame = false;
		}
	}

	protected static void FillMeshFrames(ref NativeArray<float3> positions, ref NativeArray<float3> bitangents, Matrix4x4[] meshFrames, float spacing, bool isLoop, float rotationOffset, Vector3 scaleMultiplier, bool stretch)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		Vector3 vector = scaleMultiplier * 0.5f * spacing;
		if (stretch)
		{
			vector.z = scaleMultiplier.z * 0.5f;
		}
		float num = 0f;
		for (int i = 0; i < positions.Length; i++)
		{
			Vector3 zero = Vector3.zero;
			zero = ((!isLoop) ? float3.op_Implicit((i < positions.Length - 1) ? (positions[i + 1] - positions[i]) : (positions[i] - positions[i - 1])) : float3.op_Implicit(positions[(i + 1) % positions.Length] - positions[i]));
			Vector3 s = (stretch ? new Vector3(vector.x, vector.y, vector.z * zero.magnitude) : vector);
			zero.Normalize();
			Quaternion q = Quaternion.LookRotation(zero, float3.op_Implicit(bitangents[i])) * Quaternion.Euler(0f, 0f, num);
			num += rotationOffset;
			customMeshFrames[i] = Matrix4x4.TRS(float3.op_Implicit(positions[i]), q, s);
		}
	}

	protected void SubmitToRenderer()
	{
		if (material == null)
		{
			return;
		}
		if (customMesh.mesh == null)
		{
			if (simulation.enabled)
			{
				mesh.SetVertices(vertices);
				mesh.SetNormals(normals);
				mesh.RecalculateBounds();
			}
			Graphics.DrawMesh(mesh, Matrix4x4.identity, material, base.gameObject.layer, null, 0, null, shadowMode);
			return;
		}
		if (customMeshFrames == null || customMeshFrames.Length < positions.Length)
		{
			customMeshFrames = new Matrix4x4[positions.Length];
		}
		if (interpolation == RopeInterpolation.None)
		{
			FillMeshFrames(ref positions, ref bitangents, customMeshFrames, _measurements.particleSpacing, isLoop, customMesh.rotation, customMesh.scale, customMesh.stretch);
		}
		else
		{
			FillMeshFrames(ref interpolatedPositions, ref bitangents, customMeshFrames, _measurements.particleSpacing, isLoop, customMesh.rotation, customMesh.scale, customMesh.stretch);
		}
		Graphics.DrawMeshInstanced(customMesh.mesh, 0, material, customMeshFrames, positions.Length, null, shadowMode, receiveShadows: true, base.gameObject.layer);
	}

	public void FixedUpdate()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		timeSinceFixedUpdate = 0f;
		if (!initialized)
		{
			return;
		}
		if (!simulation.enabled)
		{
			simulationDisabledPrevFrame = true;
			return;
		}
		CompletePreviousSimulationFrame();
		if (simulationDisabledPrevFrame)
		{
			queuedRigidbodyConnections.Clear();
			liveRigidbodyConnections.Clear();
		}
		simulationDisabledPrevFrame = false;
		base.transform.position = float3.op_Implicit(positions[0]);
		ApplyRigidbodyFeedback();
		UpdateCollisionPlanes();
		PrepareRigidbodyConnections();
		ScheduleNextSimulationFrame();
	}

	public void LateUpdate()
	{
		timeSinceFixedUpdate += Time.deltaTime;
		if (initialized)
		{
			if (interpolation != RopeInterpolation.None)
			{
				ScheduleInterpolation();
			}
			CompletePreviousSimulationFrame();
			SubmitToRenderer();
		}
	}
}
