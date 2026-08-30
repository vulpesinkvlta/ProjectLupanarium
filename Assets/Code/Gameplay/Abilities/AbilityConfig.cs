using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Способность класса гладиаторов: сеть ретиария, удар щитом, рывок.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AbilityConfig",
        menuName = "Gladiator Game/Abilities/Ability")]
    public sealed class AbilityConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Effect")]
        [SerializeField]
        private AbilityEffect _effect = AbilityEffect.SlowTarget;

        [Tooltip("Секунды между применениями.")]
        [SerializeField, Min(0.1f)] private float _cooldown = 6f;

        [Tooltip("Дальность. Для рывка не используется.")]
        [SerializeField, Min(0f)] private float _range = 2f;

        [Tooltip("Сколько держится эффект.")]
        [SerializeField, Min(0.1f)] private float _duration = 2f;

        [Tooltip("Замедление: множитель скорости (0.5 — вдвое медленнее). " +
                 "Рывок: множитель скорости (2 — вдвое быстрее). " +
                 "Оглушение: не используется.")]
        [SerializeField, Min(0f)] private float _magnitude = 0.5f;

        public string Id => _id;
        public string Description => _description;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

        public AbilitySpec Spec =>
            new(_effect, _cooldown, _range, _duration, _magnitude);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"AbilityConfig '{name}' has an empty ID.",
                    this);
            }

            if (_effect == AbilityEffect.SlowTarget && _magnitude >= 1f)
            {
                Debug.LogWarning(
                    $"AbilityConfig '{name}': замедление с множителем " +
                    $"{_magnitude} ничего не замедлит, нужно меньше 1.",
                    this);
            }

            if (_effect == AbilityEffect.HasteSelf && _magnitude <= 1f)
            {
                Debug.LogWarning(
                    $"AbilityConfig '{name}': рывок с множителем " +
                    $"{_magnitude} ничего не ускорит, нужно больше 1.",
                    this);
            }
        }
#endif
    }
}
