using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Code.Gameplay
{
    /// <summary>
    /// Сценическая часть эффектов боя: цифры урона, звук, тряска камеры.
    ///
    /// Все ссылки на префабы и клипы живут здесь; презентер знает только
    /// эти четыре метода и ничего не знает про AudioSource и пулы.
    /// </summary>
    public sealed class BattleFeedbackView : MonoBehaviour
    {
        private const int InitialNumberCapacity = 64;
        private const int MaximumNumberCapacity = 400;

        [Header("Damage numbers")]
        [SerializeField] private DamageNumberView _damageNumberPrefab;
        [SerializeField] private Transform _numbersRoot;

        [Tooltip("Показывать цифру не чаще, чем раз в N ударов. " +
                 "На больших боях спасает от тысячи цифр в кадре.")]
        [SerializeField, Min(1)] private int _showEveryNthHit = 1;

        [Header("Camera")]
        [SerializeField] private CameraShaker _cameraShaker;
        [SerializeField, Min(0f)] private float _deathShake = 0.04f;
        [SerializeField, Min(0f)] private float _critShake = 0.02f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _critClips;
        [SerializeField] private AudioClip[] _deathClips;
        [SerializeField] private AudioClip[] _abilityClips;

        [Tooltip("Не больше стольких звуков за кадр: иначе на массовой " +
                 "рубке звук превращается в белый шум и клиппует.")]
        [SerializeField, Min(1)] private int _maximumSoundsPerFrame = 3;

        [SerializeField, Range(0f, 1f)] private float _hitVolume = 0.35f;

        private ObjectPool<DamageNumberView> _numberPool;

        private readonly List<DamageNumberView> _activeNumbers =
            new(InitialNumberCapacity);

        private int _hitCounter;
        private int _soundsThisFrame;

        public void BeginFrame()
        {
            _soundsThisFrame = 0;
        }

        public void ShowDamage(Vector2 position, float amount, bool isCrit)
        {
            if (_damageNumberPrefab == null)
                return;

            // Криты показываем всегда — это редкое и важное событие.
            if (!isCrit)
            {
                _hitCounter++;

                if (_hitCounter % _showEveryNthHit != 0)
                    return;
            }

            if (_activeNumbers.Count >= MaximumNumberCapacity)
                return;

            DamageNumberView number = _numberPool.Get();

            number.Show(position, amount, isCrit);
            _activeNumbers.Add(number);
        }

        public void PlayHit(bool isCrit)
        {
            PlayClip(isCrit ? _critClips : _hitClips);

            if (isCrit && _cameraShaker != null)
                _cameraShaker.Shake(_critShake);
        }

        public void PlayDeath()
        {
            PlayClip(_deathClips);

            if (_cameraShaker != null)
                _cameraShaker.Shake(_deathShake);
        }

        public void PlayAbility()
        {
            PlayClip(_abilityClips);
        }

        private void Awake()
        {
            _numberPool = new ObjectPool<DamageNumberView>(
                createFunc: CreateNumber,
                actionOnGet: null,
                actionOnRelease: OnReleaseNumber,
                actionOnDestroy: OnDestroyNumber,
                collectionCheck: true,
                defaultCapacity: InitialNumberCapacity,
                maxSize: MaximumNumberCapacity);
        }

        private void Update()
        {
            TickNumbers(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            _numberPool?.Clear();
        }

        private void TickNumbers(float deltaTime)
        {
            for (var i = _activeNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumberView number = _activeNumbers[i];

                if (number.Tick(deltaTime))
                    continue;

                _numberPool.Release(number);

                _activeNumbers[i] = _activeNumbers[^1];
                _activeNumbers.RemoveAt(_activeNumbers.Count - 1);
            }
        }

        private void PlayClip(AudioClip[] clips)
        {
            if (_audioSource == null || clips == null || clips.Length == 0)
                return;

            if (_soundsThisFrame >= _maximumSoundsPerFrame)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            if (clip == null)
                return;

            _audioSource.PlayOneShot(clip, _hitVolume);
            _soundsThisFrame++;
        }

        private DamageNumberView CreateNumber()
        {
            DamageNumberView number = Object.Instantiate(
                _damageNumberPrefab,
                _numbersRoot != null ? _numbersRoot : transform);

            number.Hide();
            return number;
        }

        private static void OnReleaseNumber(DamageNumberView number)
        {
            number.Hide();
        }

        private static void OnDestroyNumber(DamageNumberView number)
        {
            if (number != null)
                Object.Destroy(number.gameObject);
        }
    }
}
