using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Всплывающая цифра урона. Живёт заданное время, поднимаясь и тая.
    /// </summary>
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float _lifetime = 0.7f;
        [SerializeField] private float _riseSpeed = 1.2f;
        [SerializeField] private float _horizontalSpread = 0.35f;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _critColor = new(1f, 0.75f, 0.2f);
        [SerializeField, Min(1f)] private float _critScale = 1.4f;

        private Vector3 _velocity;
        private float _remaining;

        public bool IsAlive => _remaining > 0f;

        public void Show(Vector3 position, float amount, bool isCrit)
        {
            transform.position = position;

            _remaining = _lifetime;

            // Разлёт по горизонтали, иначе цифры от нескольких ударов
            // в одну цель встают ровно друг на друга и сливаются.
            _velocity = new Vector3(
                Random.Range(-_horizontalSpread, _horizontalSpread),
                _riseSpeed,
                0f);

            if (_label != null)
            {
                _label.text = Mathf.RoundToInt(amount).ToString();
                _label.color = isCrit ? _critColor : _normalColor;
            }

            transform.localScale =
                Vector3.one * (isCrit ? _critScale : 1f);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _remaining = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Возвращает false, когда цифра отжила своё и её пора вернуть
        /// в пул. Тикается извне — по той же причине, что и UnitView:
        /// Update на сотнях объектов дороже одного цикла.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (_remaining <= 0f)
                return false;

            _remaining -= deltaTime;

            transform.position += _velocity * deltaTime;

            if (_label != null)
            {
                Color color = _label.color;
                color.a = Mathf.Clamp01(_remaining / _lifetime);

                _label.color = color;
            }

            return _remaining > 0f;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_label == null)
                _label = GetComponentInChildren<TMP_Text>();
        }
#endif
    }
}
