using System;
using UnityEngine;

namespace Code.Gameplay
{
    public class UnitView : MonoBehaviour
    {
        private const int SortingPrecision = 100;

        [Header("Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Sandbox Colors")]
        [SerializeField] private Color _playerColor = Color.cyan;
        [SerializeField] private Color _enemyColor = Color.red;

        [Header("Sorting")]
        [SerializeField] private int _baseSortingOrder;

        public UnitRuntime Runtime { get; private set; }

        public bool IsBound => Runtime != null;

        public void Bind(UnitRuntime runtime)
        {
            Runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));

            gameObject.name =
                $"UnitView_{runtime.Id}_{runtime.Team}";

            _spriteRenderer.color = runtime.Team switch
            {
                TeamId.Player => _playerColor,
                TeamId.Enemy => _enemyColor,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(runtime),
                    runtime.Team,
                    "Unsupported team.")
            };

            SetVisualPosition(runtime.Position);
        }

        public void Unbind()
        {
            Runtime = null;
            gameObject.name = nameof(UnitView);
        }

        public void SetVisualPosition(Vector2 position)
        {
            transform.position = new Vector3(
                position.x,
                position.y,
                0f);

            _spriteRenderer.sortingOrder =
                _baseSortingOrder -
                Mathf.RoundToInt(position.y * SortingPrecision);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }
#endif
    }
}
