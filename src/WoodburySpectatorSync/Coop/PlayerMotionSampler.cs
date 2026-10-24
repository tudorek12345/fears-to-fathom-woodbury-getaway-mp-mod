using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class PlayerAirborneSampler
    {
        private bool _airborne;
        public bool Sample(bool grounded, bool controlsMovement, float verticalSpeed, bool reset)
        {
            if (reset || grounded || !controlsMovement) _airborne = false;
            if (!grounded && controlsMovement && Mathf.Abs(verticalSpeed) > 0.15f) _airborne = true;
            return _airborne;
        }
    }

    internal sealed class PlayerMotionSampler
    {
        private Vector3 _position, _velocity;
        private float _time;
        private bool _hasSample;
        public Vector3 Sample(Vector3 position, float now, bool changedSource)
        {
            float elapsed = now - _time;
            var delta = position - _position;
            if (!_hasSample || changedSource || elapsed < 0f || elapsed > 0.5f || delta.sqrMagnitude > 4f)
            {
                _position = position; _time = now; _hasSample = true;
                return _velocity = Vector3.zero;
            }
            // Keep accumulating short samples; high frame rates must not erase movement.
            if (elapsed < 0.02f) return _velocity;
            _velocity = Vector3.ClampMagnitude(delta / elapsed, 20f);
            _position = position; _time = now;
            return _velocity;
        }
    }
}
