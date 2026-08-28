using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Порядок волн забега.
    ///
    /// Когда авторские волны кончаются, каталог продолжает выдавать
    /// последнюю, наращивая количество врагов и награду. Роглайк обязан
    /// быть бесконечным, иначе забег упирается в пустоту на последней волне.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WaveCatalog",
        menuName = "Gladiator Game/Waves/Wave Catalog")]
    public sealed class WaveCatalog : ScriptableObject
    {
        [SerializeField] private WaveConfig[] _waves;

        [Header("Endless scaling")]
        [Tooltip("Прирост количества врагов за каждую волну сверх каталога.")]
        [SerializeField, Min(0f)] private float _enemyGrowthPerWave = 0.25f;

        [Tooltip("Прирост награды за каждую волну сверх каталога.")]
        [SerializeField, Min(0f)] private float _goldGrowthPerWave = 0.15f;

        public int DefinedWaveCount =>
            _waves?.Length ?? 0;

        /// <summary>
        /// Заполняет destination составом врагов для волны waveIndex
        /// и возвращает награду за неё.
        ///
        /// Пишет в переданный список, а не возвращает новый: метод
        /// вызывается на старте каждой волны, и лишний мусор здесь не нужен.
        /// </summary>
        public int GetWave(int waveIndex, List<SquadEntry> destination)
        {
            if (waveIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(waveIndex));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();

            if (DefinedWaveCount == 0)
            {
                throw new InvalidOperationException(
                    $"WaveCatalog '{name}' не содержит ни одной волны.");
            }

            int lastIndex = _waves.Length - 1;
            int sourceIndex = Mathf.Min(waveIndex, lastIndex);

            WaveConfig wave = _waves[sourceIndex];

            if (wave == null)
            {
                throw new InvalidOperationException(
                    $"WaveCatalog '{name}': пустая ячейка {sourceIndex}.");
            }

            int extraWaves = Mathf.Max(0, waveIndex - lastIndex);

            float enemyMultiplier = 1f + _enemyGrowthPerWave * extraWaves;
            float goldMultiplier = 1f + _goldGrowthPerWave * extraWaves;

            IReadOnlyList<SquadEntry> enemies = wave.Enemies;

            for (var i = 0; i < enemies.Count; i++)
            {
                SquadEntry entry = enemies[i];

                if (entry == null || entry.Config == null)
                    continue;

                int count = Mathf.Max(
                    1,
                    Mathf.RoundToInt(entry.Count * enemyMultiplier));

                destination.Add(new SquadEntry(entry.Config, count));
            }

            return Mathf.RoundToInt(wave.GoldReward * goldMultiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_waves == null || _waves.Length == 0)
            {
                Debug.LogWarning(
                    $"WaveCatalog '{name}': не задано ни одной волны.",
                    this);
            }
        }
#endif
    }
}
