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

        [Header("Hit flash")]
        [SerializeField] private Color _hitFlashColor = Color.white;
        [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.12f;

        [Header("Death")]
        [SerializeField, Min(0.01f)] private float _deathDuration = 0.45f;

        [Header("Sorting")]
        [SerializeField] private int _baseSortingOrder;

        private Color _baseColor;
        private float _remainingFlash;
        private float _remainingDeath;
        private bool _isDying;
        private int _lastSortingOrder = int.MinValue;

        public UnitRuntime Runtime { get; private set; }

        public bool IsBound => Runtime != null;

        /// <summary>Сколько секунд занимает анимация гибели.</summary>
        public float DeathDuration => _deathDuration;

        [SerializeField]
        private ParticleSystem _deadEffect;
        public void Bind(UnitRuntime runtime)
        {
            Runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));

            gameObject.name =
                $"UnitView_{runtime.Id}_{runtime.Team}";

            _baseColor = runtime.Team switch
            {
                TeamId.Player => _playerColor,
                TeamId.Enemy => _enemyColor,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(runtime),
                    runtime.Team,
                    "Unsupported team.")
            };

            _isDying = false;
            _remainingFlash = 0f;
            _remainingDeath = 0f;

            _spriteRenderer.color = _baseColor;

            transform.localScale = Vector3.one;

            _lastSortingOrder = int.MinValue;

            SetVisualPosition(runtime.Position, updateSorting: true);
        }

        public void Unbind()
        {
            Runtime = null;

            _isDying = false;
            _remainingFlash = 0f;
            _remainingDeath = 0f;

            transform.localScale = Vector3.one;

            gameObject.name = nameof(UnitView);
        }

        /// <summary>
        /// Ставит вью в позицию. Порядок отрисовки пересчитывается
        /// не каждый кадр: запись sortingOrder помечает рендерер грязным
        /// и заставляет пересортировать пакет, а на пятистах юнитах это
        /// заметная работа впустую — глубина за один кадр меняется
        /// меньше, чем на толщину спрайта.
        /// </summary>
        public void SetVisualPosition(Vector2 position, bool updateSorting)
        {
            transform.position = new Vector3(
                position.x,
                position.y,
                0f);

            if (!updateSorting)
                return;

            int order =
                _baseSortingOrder -
                Mathf.RoundToInt(position.y * SortingPrecision);

            if (order == _lastSortingOrder)
                return;

            _lastSortingOrder = order;
            _spriteRenderer.sortingOrder = order;
        }

        /// <summary>Короткая вспышка при получении удара.</summary>
        public void Flash()
        {
            if (_isDying)
                return;

            _remainingFlash = _hitFlashDuration;
        }

        /// <summary>
        /// Запускает угасание. Вью остаётся живым ещё DeathDuration секунд,
        /// после чего синхронизатор вернёт его в пул.
        /// </summary>
        public void PlayDeath()
        {
            // Партикл необязателен: на префабах без него Play() падал бы
            // с NullReference прямо посреди боя.
            if (_deadEffect != null)
                _deadEffect.Play();

            _isDying = true;
            _remainingDeath = _deathDuration;
        }

        /// <summary>
        /// Продвигает визуальные эффекты. Вызывается синхронизатором раз
        /// в кадр, а не в Update: пул держит сотни отключённых объектов,
        /// и Update на каждом обходился бы дороже одного цикла.
        /// </summary>
        public void TickVisuals(float deltaTime)
        {
            if (_isDying)
            {
                TickDeath(deltaTime);
                return;
            }

            if (_remainingFlash <= 0f)
                return;

            _remainingFlash = Mathf.Max(0f, _remainingFlash - deltaTime);

            float t = _hitFlashDuration <= 0f
                ? 0f
                : _remainingFlash / _hitFlashDuration;

            _spriteRenderer.color = Color.Lerp(
                _baseColor,
                _hitFlashColor,
                t);
        }

        private void TickDeath(float deltaTime)
        {
            _remainingDeath = Mathf.Max(0f, _remainingDeath - deltaTime);

            float t = _deathDuration <= 0f
                ? 0f
                : _remainingDeath / _deathDuration;

            Color color = _baseColor;
            color.a = t;

            _spriteRenderer.color = color;

            // Оседает и слегка расплющивается — читается как падение
            // даже без покадровой анимации.
            transform.localScale = new Vector3(
                1f + (1f - t) * 0.2f,
                Mathf.Lerp(0.4f, 1f, t),
                1f);
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
