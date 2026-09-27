using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    public partial class UnitView
    {
        [Header("Shared presentation — base and arena")]
        [SerializeField] private Transform _visual;
        [SerializeField] private UnitAnimationCatalog _animations;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private SpriteRenderer _shadow;
        [Header("Procedural motion (visual only)")]
        [SerializeField, Min(0)] private float _hopHeight = .16f;
        [SerializeField, Min(.1f)] private float _strideLength = .7f;
        [SerializeField, Range(0, 30)] private float _walkTilt = 12;
        [SerializeField, Min(0)] private float _idleAmplitude = .018f;
        [SerializeField, Min(.01f)] private float _strikeDuration = .2f;
        [SerializeField, Min(0)] private float _attackReach = .16f;

        private UnitAnimationCatalog.Entry _animation;
        private Sprite _placeholder;
        private Vector3 _visualRestPosition, _visualRestScale, _bodyRestPosition, _bodyRestScale;
        private Vector3 _lastGroundPosition;
        private float _phase, _clock, _motion, _attackRemaining;
        private int _windupVersion, _attackVersion;
        private bool _poseInitialized, _facingRight, _usesArt;
        public bool IsAttackAnimation => Runtime != null && (Runtime.IsWindingUp || _attackRemaining > 0);
        private Color BodyColor => _usesArt ? Color.white : _baseColor;

        private void EnsurePose()
        {
            if (_poseInitialized) return;
            // Authored prefab always uses a child. The fallback supports old test fixtures.
            if (_visual == null) _visual = _spriteRenderer.transform;
            _visualRestPosition = _visual.localPosition;
            _visualRestScale = _visual.localScale;
            _bodyRestPosition = _spriteRenderer.transform.localPosition;
            _bodyRestScale = _spriteRenderer.transform.localScale;
            _placeholder = _spriteRenderer.sprite;
            _poseInitialized = true;
        }

        private void ResetPose(UnitClassId classId, int index, bool facingRight)
        {
            EnsurePose();
            _animation = _animations != null ? _animations.Find(classId) : null;
            _phase = index * 2.399963f;
            _clock = index * .173f;
            _motion = _attackRemaining = 0;
            _windupVersion = Runtime?.WindupVersion ?? 0;
            _attackVersion = Runtime?.AttackVersion ?? 0;
            _facingRight = facingRight;
            _lastGroundPosition = transform.position;
            if (_visual != transform) _visual.localPosition = _visualRestPosition;
            _visual.localScale = _visualRestScale;
            _visual.localRotation = Quaternion.identity;
            _spriteRenderer.sprite = _placeholder;
            if (_spriteRenderer.transform != transform) _spriteRenderer.transform.localPosition = _bodyRestPosition;
            _spriteRenderer.transform.localScale = _bodyRestScale;
            _spriteRenderer.flipX = false;
            _usesArt = false;
            if (_deadEffect != null) _deadEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_shadow != null) _shadow.color = new Color(0, 0, 0, .2f);
            ApplyFrame(false, 0);
            _spriteRenderer.color = BodyColor;
        }

        public void BindDisplay(UnitClassId classId, string displayName, Color color, int index, bool showLabel)
        {
            Runtime = null;
            _baseColor = color;
            _isDying = false;
            _remainingFlash = _remainingDeath = 0;
            ResetPose(classId, index, true);
            gameObject.name = $"UnitView_{classId}_{index}";
            if (_nameLabel != null)
            {
                _nameLabel.text = displayName;
                _nameLabel.gameObject.SetActive(showLabel);
            }
            SetVisualPosition(transform.position, true);
        }

        private void TickPose(float dt)
        {
            EnsurePose();
            if (_isDying)
            {
                if (_shadow != null) _shadow.color = new Color(0, 0, 0, .2f * _remainingDeath / _deathDuration);
                return;
            }
            _clock += dt;
            Vector3 displacement = transform.position - _lastGroundPosition;
            _lastGroundPosition = transform.position;
            bool moving = Runtime != null && Runtime.State == UnitState.Moving && displacement.sqrMagnitude > .0000001f;
            _motion = Mathf.MoveTowards(_motion, moving ? 1 : 0, dt * 10);
            if (moving) _phase += displacement.magnitude / Mathf.Max(.1f, _strideLength) * Mathf.PI;
            float direction = Runtime?.Target != null ? Runtime.Target.Position.x - Runtime.Position.x : displacement.x;
            if (Mathf.Abs(direction) > .001f) _facingRight = direction > 0;

            _attackRemaining = Mathf.Max(0, _attackRemaining - dt);
            if (Runtime != null)
            {
                if (_windupVersion != Runtime.WindupVersion) { _windupVersion = Runtime.WindupVersion; _attackRemaining = 0; }
                if (_attackVersion != Runtime.AttackVersion) { _attackVersion = Runtime.AttackVersion; _attackRemaining = _strikeDuration; }
                if (Runtime.IsStunned) _attackRemaining = 0;
            }
            bool windup = Runtime != null && Runtime.IsWindingUp && !Runtime.IsStunned;
            bool attack = windup || _attackRemaining > 0;
            float progress = windup
                ? .65f * (1 - Runtime.RemainingWindup / Mathf.Max(.001f, Runtime.Stats.AttackWindup))
                : .65f + .35f * (1 - _attackRemaining / _strikeDuration);
            float sign = _facingRight ? 1 : -1;
            float breathe = Mathf.Sin(_clock * 2.5f + _phase);
            float hop = Mathf.Abs(Mathf.Sin(_phase)) * _hopHeight * _motion;
            float lean = Mathf.Sin(_phase) * _walkTilt * _motion + breathe * 2 * (1 - _motion);
            float lunge = 0;
            if (attack)
            {
                hop = 0;
                lunge = windup ? -_attackReach * .35f * (progress / .65f) : _attackReach * Mathf.Sin(Mathf.PI * _attackRemaining / _strikeDuration);
                lean = -sign * lunge * 75;
            }
            if (_visual != transform)
                _visual.localPosition = _visualRestPosition + new Vector3(lunge * sign, hop + breathe * _idleAmplitude * (1 - _motion), 0);
            _visual.localRotation = Quaternion.Euler(0, 0, lean);
            _visual.localScale = Vector3.Scale(_visualRestScale, new Vector3(1 - breathe * .012f, 1 + breathe * .018f, 1));
            ApplyFrame(attack, progress);
            if (_shadow != null) _shadow.sortingOrder = _spriteRenderer.sortingOrder - 1;
        }

        private void ApplyFrame(bool attack, float progress)
        {
            var frames = attack ? _animation?.Attack : _animation?.IdleWalk;
            if (frames == null || frames.Length == 0) frames = _animation?.IdleWalk;
            Sprite frame = null;
            if (frames != null && frames.Length > 0)
            {
                int index = attack && frames == _animation.Attack
                    ? Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length))
                    : Mathf.FloorToInt(_clock * Mathf.Max(1, _animation.FramesPerSecond)) % frames.Length;
                frame = frames[index];
            }
            _usesArt = frame != null;
            _spriteRenderer.sprite = frame != null ? frame : _placeholder;
            _spriteRenderer.flipX = _usesArt && (_facingRight != _animation.FacesRight);
            _spriteRenderer.transform.localScale = _usesArt ? Vector3.one * _animation.Scale : _bodyRestScale;
            if (_spriteRenderer.transform != transform)
                _spriteRenderer.transform.localPosition = _bodyRestPosition + (_usesArt ? (Vector3)_animation.Offset : Vector3.zero);
        }
    }
}
