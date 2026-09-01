using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Ужимает панель до безопасной области экрана.
    ///
    /// Вешается на RectTransform, растянутый на весь Canvas. Без этого
    /// на телефонах с вырезом и жестовой полосой кнопки уезжают под них
    /// и перестают нажиматься.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Игнорировать вырез по горизонтали. Полезно в портрете, " +
                 "где боковые отступы только сужают экран.")]
        [SerializeField] private bool _ignoreHorizontal;

        [SerializeField] private bool _ignoreVertical;

        private RectTransform _rectTransform;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Пересчитываем только при реальной смене — Screen.safeArea
            // на некоторых устройствах меняется с задержкой после
            // поворота, поэтому одного вызова в Awake недостаточно.
            if (!HasChanged())
                return;

            Apply();
        }

        private bool HasChanged()
        {
            return Screen.safeArea != _lastSafeArea ||
                   Screen.width != _lastScreenSize.x ||
                   Screen.height != _lastScreenSize.y ||
                   Screen.orientation != _lastOrientation;
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;

            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            if (_ignoreHorizontal)
            {
                min.x = 0f;
                max.x = 1f;
            }

            if (_ignoreVertical)
            {
                min.y = 0f;
                max.y = 1f;
            }

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;

            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
