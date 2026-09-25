using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>Мирное представление бойца: без симуляции боя и боевых эффектов.</summary>
    public sealed class BaseSquadUnitView : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private TMP_Text _label;
        [Tooltip("Необязательно: будущий персонаж с настоящим idle-клипом.")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _idleState = "Idle";
        [SerializeField, Min(0)] private float _idleAmplitude = 0.045f;
        [SerializeField, Min(0)] private float _idleSpeed = 1.8f;

        private float _phase;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private bool _animated;
        private UnityEngine.Rendering.SortingGroup _sortingGroup;

        private void Awake()
        {
            _restPosition = _visual.localPosition;
            _restScale = _visual.localScale;
            _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
        }

        public void Bind(string unitId, string displayName, Color color, int index, bool showLabel)
        {
            gameObject.name = $"BaseUnit_{unitId}_{index}";
            _phase = index * 2.399963f;
            _body.color = color;
            if (_sortingGroup != null)
                _sortingGroup.sortingOrder = 20 + Mathf.RoundToInt(-transform.localPosition.y * 100);
            _label.text = displayName;
            _label.gameObject.SetActive(showLabel);
            int idleHash = Animator.StringToHash(_idleState);
            _animated = _animator != null && _animator.runtimeAnimatorController != null &&
                        _animator.HasState(0, idleHash);
            if (_animated)
                _animator.Play(idleHash, 0, Mathf.Repeat(index * 0.173f, 1f));
        }

        private void Update()
        {
            if (_animated)
                return;
            float breathe = Mathf.Sin(Time.unscaledTime * _idleSpeed + _phase);
            _visual.localPosition = _restPosition + Vector3.up * (breathe * _idleAmplitude);
            _visual.localScale = Vector3.Scale(_restScale,
                new Vector3(1f - breathe * 0.025f, 1f + breathe * 0.04f, 1f));
        }
    }
}
