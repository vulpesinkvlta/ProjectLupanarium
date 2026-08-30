using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Тряска камеры.
    ///
    /// Сила накапливается и затухает экспоненциально, поэтому сотня
    /// смертей в одном тике не выбивает камеру за пределы арены —
    /// амплитуда упирается в потолок.
    /// </summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _maximumAmplitude = 0.35f;
        [SerializeField, Min(0.1f)] private float _decayPerSecond = 6f;

        private Vector3 _restPosition;
        private float _amplitude;

        public void Shake(float strength)
        {
            _amplitude = Mathf.Min(
                _maximumAmplitude,
                _amplitude + Mathf.Max(0f, strength));
        }

        private void Awake()
        {
            _restPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_amplitude <= 0.0001f)
            {
                transform.localPosition = _restPosition;
                return;
            }

            _amplitude = Mathf.Max(
                0f,
                _amplitude - _decayPerSecond * _amplitude * Time.unscaledDeltaTime);

            transform.localPosition = _restPosition + new Vector3(
                Random.Range(-_amplitude, _amplitude),
                Random.Range(-_amplitude, _amplitude),
                0f);
        }
    }
}
