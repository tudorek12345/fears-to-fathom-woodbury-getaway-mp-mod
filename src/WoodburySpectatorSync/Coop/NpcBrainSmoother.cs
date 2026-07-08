using System.Collections.Generic;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class NpcBrainSmoother
    {
        private const int MaxBufferedSamples = 6;
        private const float InterpolationDelaySeconds = 0.12f;
        private const float ExtrapolateGraceSeconds = 0.22f;
        private const float MaxPredictionSeconds = 0.16f;
        private const float MaxPredictionDistance = 0.45f;
        private const float MaxPredictionVelocity = 7.5f;
        private const float SnapDistanceMeters = 4.0f;
        private const float StaleSnapSeconds = 1.25f;
        private const float FollowResponse = 14f;

        private readonly Dictionary<string, Track> _tracks = new Dictionary<string, Track>(System.StringComparer.Ordinal);

        public void Clear()
        {
            _tracks.Clear();
        }

        public void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _tracks.Remove(key);
            }
        }

        public void Submit(string key, Transform target, NpcBrainState state, bool snapshot)
        {
            if (string.IsNullOrEmpty(key) || target == null)
            {
                return;
            }

            if (!state.Active)
            {
                Remove(key);
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (!_tracks.TryGetValue(key, out var track) || track == null)
            {
                track = new Track();
                _tracks[key] = track;
            }

            var targetChanged = track.Target != null && !ReferenceEquals(track.Target, target);
            var activeChanged = track.Initialized && track.Active != state.Active;
            var stale = track.LastPacketTime > 0f && now - track.LastPacketTime > StaleSnapSeconds;
            var distance = Vector3.Distance(target.position, state.Position);
            var shouldSnap = snapshot || !track.Initialized || targetChanged || activeChanged || stale || distance > SnapDistanceMeters;

            track.Target = target;
            track.Latest = state;
            track.Active = state.Active;
            track.LastPacketTime = now;
            track.TargetPosition = state.Position;
            track.TargetRotation = state.Rotation;

            if (shouldSnap)
            {
                track.Samples.Clear();
                target.position = state.Position;
                target.rotation = state.Rotation;
                track.Initialized = true;
                track.LastRenderTime = now;
            }

            track.Samples.Add(new Sample
            {
                Position = state.Position,
                Rotation = state.Rotation,
                Velocity = state.Velocity,
                ReceiveTime = now,
                State = state
            });

            while (track.Samples.Count > MaxBufferedSamples)
            {
                track.Samples.RemoveAt(0);
            }
        }

        public void Update()
        {
            if (_tracks.Count == 0)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            var keys = new List<string>(_tracks.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!_tracks.TryGetValue(key, out var track) || track == null)
                {
                    continue;
                }

                var target = track.Target;
                if (target == null || target.gameObject == null || !track.Active)
                {
                    _tracks.Remove(key);
                    continue;
                }

                if (!target.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(true);
                }

                var dt = track.LastRenderTime > 0f
                    ? Mathf.Clamp(now - track.LastRenderTime, 0.001f, 0.1f)
                    : Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f);
                track.LastRenderTime = now;

                if (!track.Initialized || Vector3.Distance(target.position, track.TargetPosition) > SnapDistanceMeters)
                {
                    target.position = track.TargetPosition;
                    target.rotation = track.TargetRotation;
                    track.Initialized = true;
                    continue;
                }

                var renderTime = now - InterpolationDelaySeconds;
                if (TryGetBufferedPose(track, renderTime, out var position, out var rotation))
                {
                    target.position = position;
                    target.rotation = rotation;
                    continue;
                }

                var alpha = 1f - Mathf.Exp(-FollowResponse * dt);
                target.position = Vector3.Lerp(target.position, track.TargetPosition, alpha);
                target.rotation = Quaternion.Slerp(target.rotation, track.TargetRotation, alpha);
            }
        }

        private static bool TryGetBufferedPose(Track track, float renderTime, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (track == null || track.Samples.Count == 0)
            {
                return false;
            }

            if (track.Samples.Count == 1)
            {
                var only = track.Samples[0];
                if (Time.realtimeSinceStartup - only.ReceiveTime > ExtrapolateGraceSeconds)
                {
                    return false;
                }

                position = PredictPosition(only, null, renderTime);
                rotation = only.Rotation;
                return true;
            }

            var first = track.Samples[0];
            if (renderTime <= first.ReceiveTime)
            {
                position = first.Position;
                rotation = first.Rotation;
                return true;
            }

            for (var i = 1; i < track.Samples.Count; i++)
            {
                var previous = track.Samples[i - 1];
                var next = track.Samples[i];
                if (renderTime > next.ReceiveTime)
                {
                    continue;
                }

                var span = Mathf.Max(0.001f, next.ReceiveTime - previous.ReceiveTime);
                var t = Mathf.Clamp01((renderTime - previous.ReceiveTime) / span);
                position = Vector3.Lerp(previous.Position, next.Position, t);
                rotation = Quaternion.Slerp(previous.Rotation, next.Rotation, t);
                return true;
            }

            var latest = track.Samples[track.Samples.Count - 1];
            if (Time.realtimeSinceStartup - latest.ReceiveTime > ExtrapolateGraceSeconds)
            {
                return false;
            }

            position = PredictPosition(latest, track.Samples[track.Samples.Count - 2], renderTime);
            rotation = latest.Rotation;
            return true;
        }

        private static Vector3 PredictPosition(Sample latest, Sample previous, float renderTime)
        {
            var velocity = latest.Velocity;
            if (velocity.sqrMagnitude < 0.0001f && previous != null)
            {
                var span = Mathf.Max(0.001f, latest.ReceiveTime - previous.ReceiveTime);
                velocity = (latest.Position - previous.Position) / span;
            }

            velocity.y = Mathf.Clamp(velocity.y, -0.35f, 0.35f);
            if (velocity.sqrMagnitude > MaxPredictionVelocity * MaxPredictionVelocity)
            {
                velocity = Vector3.ClampMagnitude(velocity, MaxPredictionVelocity);
            }

            var forwardTime = Mathf.Clamp(Mathf.Max(0f, renderTime - latest.ReceiveTime) + 0.04f, 0f, MaxPredictionSeconds);
            var offset = velocity * forwardTime;
            offset.y = Mathf.Clamp(offset.y, -0.08f, 0.08f);
            if (offset.sqrMagnitude > MaxPredictionDistance * MaxPredictionDistance)
            {
                offset = Vector3.ClampMagnitude(offset, MaxPredictionDistance);
            }

            return latest.Position + offset;
        }

        private sealed class Track
        {
            public Transform Target;
            public NpcBrainState Latest;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public bool Active;
            public bool Initialized;
            public float LastPacketTime;
            public float LastRenderTime;
            public readonly List<Sample> Samples = new List<Sample>(MaxBufferedSamples);
        }

        private sealed class Sample
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public float ReceiveTime;
            public NpcBrainState State;
        }
    }
}
