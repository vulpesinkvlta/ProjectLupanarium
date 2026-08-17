using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    public sealed class UnitHealthSegmentView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [Header("Health Colors")]
        [SerializeField]
        private Color _healthyColor =
            new(0.15f, 0.85f, 0.25f);

        [SerializeField]
        private Color _woundedColor =
            new(0.95f, 0.75f, 0.15f);

        [SerializeField]
        private Color _criticalColor =
            new(0.9f, 0.2f, 0.15f);

        [SerializeField]
        private Color _deadColor =
            new(0.25f, 0.25f, 0.25f);

        [Header("State")]
        [SerializeField, Range(0f, 1f)]
        private float _deadAlpha = 0.45f;

        private float _lastHealth = -1f;
        private bool _lastAlive;

        public int UnitId { get; private set; } = -1;

        public void Bind(int unitId)
        {
            UnitId = unitId;

            _lastHealth = -1f;
            _lastAlive = false;

            SetHealth(
                normalizedHealth: 1f,
                isAlive: true);
        }

        public void SetHealth(
            float normalizedHealth,
            bool isAlive)
        {
            float health =
                Mathf.Clamp01(normalizedHealth);

            if (Mathf.Approximately(
                    health,
                    _lastHealth) &&
                isAlive == _lastAlive)
            {
                return;
            }

            _lastHealth = health;
            _lastAlive = isAlive;

            _fillImage.fillAmount =
                isAlive
                    ? health
                    : 0f;

            _fillImage.color =
                GetHealthColor(
                    health,
                    isAlive);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    isAlive
                        ? 1f
                        : _deadAlpha;
            }
        }

        public void Unbind()
        {
            UnitId = -1;
            _lastHealth = -1f;
            _lastAlive = false;

            _fillImage.fillAmount = 0f;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        private Color GetHealthColor(
            float health,
            bool isAlive)
        {
            if (!isAlive)
                return _deadColor;

            if (health > 0.5f)
                return _healthyColor;

            if (health > 0.25f)
                return _woundedColor;

            return _criticalColor;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_fillImage == null)
            {
                _fillImage =
                    GetComponentInChildren<Image>();
            }

            ConfigureFillImage();
        }

        private void OnValidate()
        {
            ConfigureFillImage();
        }

        private void ConfigureFillImage()
        {
            if (_fillImage == null)
                return;

            _fillImage.type =
                Image.Type.Filled;

            _fillImage.fillMethod =
                Image.FillMethod.Vertical;

            _fillImage.fillOrigin =
                (int)Image.OriginVertical.Bottom;

            _fillImage.fillClockwise = true;
        }
#endif
    }
}
