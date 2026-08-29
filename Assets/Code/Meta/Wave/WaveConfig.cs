using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Состав одной волны. Номер волны здесь намеренно не хранится —
    /// его задаёт позиция в WaveCatalog, иначе поле и порядок в каталоге
    /// рано или поздно разъедутся.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WaveConfig",
        menuName = "Gladiator Game/Waves/Wave")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Header("Enemies")]
        [SerializeField] private SquadEntry[] _enemies;

        [Header("Formation")]
        [Tooltip("Строй врага. Пусто — враги строятся сеткой по умолчанию.")]
        [SerializeField] private FormationConfig _formation;

        [Header("Reward")]
        [SerializeField, Min(0)] private int _goldReward = 10;

        public IReadOnlyList<SquadEntry> Enemies =>
            _enemies ?? System.Array.Empty<SquadEntry>();

        public int GoldReward => _goldReward;

        public FormationConfig Formation => _formation;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_enemies == null || _enemies.Length == 0)
            {
                Debug.LogWarning(
                    $"WaveConfig '{name}': состав врагов пуст, " +
                    $"волна будет засчитана как победа мгновенно.",
                    this);
            }
        }
#endif
    }
}
