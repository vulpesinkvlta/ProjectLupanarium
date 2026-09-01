using System;
using UnityEngine;

namespace Code.Gameplay
{
    public class ArenaSceneReference : MonoBehaviour
    {
        [Header("Units")]
        [SerializeField] private UnitView _unitViewPrefab;
        [SerializeField] private Transform _unitViewRoot;

        [Header("Arena bounds")]
        [Tooltip("Центр площадки боя в мировых координатах.")]
        [SerializeField] private Vector2 _arenaCenter = Vector2.zero;

        [Tooltip("Радиус площадки. За этот круг бойцы не выходят.")]
        [SerializeField, Min(1f)] private float _arenaRadius = 8f;

        [SerializeField] private Color _boundsGizmoColor =
            new(1f, 0.85f, 0.3f, 0.6f);

        public UnitView UnitViewPrefab =>
            _unitViewPrefab != null
                ? _unitViewPrefab
                : throw new InvalidOperationException(
                    "UnitView prefab is not assigned.");

        public Transform UnitViewRoot =>
            _unitViewRoot != null
                ? _unitViewRoot
                : transform;

        public Vector2 ArenaCenter => _arenaCenter;
        public float ArenaRadius => _arenaRadius;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_unitViewPrefab == null)
            {
                Debug.LogWarning(
                    "[ArenaSceneReference] " +
                    "UnitView prefab is not assigned.",
                    this);
            }
        }

        /// <summary>
        /// Рисует границу арены в сцене — иначе радиус подбирается
        /// вслепую, запуском за запуском.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = _boundsGizmoColor;

            const int segments = 64;

            Vector3 previous = GizmoPointOnCircle(0);

            for (var i = 1; i <= segments; i++)
            {
                Vector3 current = GizmoPointOnCircle(
                    i / (float)segments * 2f * Mathf.PI);

                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }

        private Vector3 GizmoPointOnCircle(float angle)
        {
            return new Vector3(
                _arenaCenter.x + Mathf.Cos(angle) * _arenaRadius,
                _arenaCenter.y + Mathf.Sin(angle) * _arenaRadius,
                0f);
        }
#endif
    }
}
