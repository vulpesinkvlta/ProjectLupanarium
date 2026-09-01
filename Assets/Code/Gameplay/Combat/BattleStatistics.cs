using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>Итоги боя по одному классу гладиаторов.</summary>
    public struct ClassBattleStats
    {
        public int Deployed;
        public int Lost;
        public int Kills;

        public float DamageDealt;
        public float DamageTaken;
        public float DamagePrevented;

        public bool HasAnything =>
            Deployed > 0 ||
            Kills > 0 ||
            DamageDealt > 0f ||
            DamageTaken > 0f;
    }

    /// <summary>
    /// Копит статистику боя для экрана итогов.
    ///
    /// Обычный приёмник данных, как DamageBuffer: боевые системы просто
    /// пишут сюда, ничего не зная о том, что кто-то это покажет.
    /// Индексация по классу, а не по бойцу: игрок читает итоги строками
    /// «мирмиллоны», а не списком из двухсот безымянных гладиаторов.
    /// </summary>
    public sealed class BattleStatistics
    {
        private static readonly int ClassCount =
            GetClassCount();

        private readonly ClassBattleStats[] _playerStats =
            new ClassBattleStats[ClassCount];

        private readonly int[] _enemyKillsByClass =
            new int[ClassCount];

        public int GoldEarned { get; private set; }
        public int TotalEnemiesKilled { get; private set; }
        public int TotalUnitsLost { get; private set; }

        public ClassBattleStats GetPlayerStats(UnitClassId classId)
        {
            int index = (int)classId;

            return index >= 0 && index < _playerStats.Length
                ? _playerStats[index]
                : default;
        }

        public int GetEnemyKills(UnitClassId classId)
        {
            int index = (int)classId;

            return index >= 0 && index < _enemyKillsByClass.Length
                ? _enemyKillsByClass[index]
                : 0;
        }

        public void Reset()
        {
            Array.Clear(_playerStats, 0, _playerStats.Length);
            Array.Clear(_enemyKillsByClass, 0, _enemyKillsByClass.Length);

            GoldEarned = 0;
            TotalEnemiesKilled = 0;
            TotalUnitsLost = 0;
        }

        /// <summary>
        /// Запоминает, кто вышел на арену. Вызывается после спавна:
        /// иначе в итогах не с чем сравнивать потери.
        /// </summary>
        public void CaptureDeployed(IReadOnlyList<UnitRuntime> playerUnits)
        {
            if (playerUnits == null)
                throw new ArgumentNullException(nameof(playerUnits));

            for (var i = 0; i < playerUnits.Count; i++)
            {
                int index = (int)playerUnits[i].ClassId;

                if (index >= 0 && index < _playerStats.Length)
                    _playerStats[index].Deployed++;
            }
        }

        public void RegisterDamage(
            UnitRuntime source,
            UnitRuntime target,
            float applied,
            float prevented)
        {
            if (source != null && source.Team == TeamId.Player)
            {
                int index = (int)source.ClassId;

                if (index >= 0 && index < _playerStats.Length)
                {
                    _playerStats[index].DamageDealt += applied;

                    // Предотвращённый урон засчитываем не защитнику,
                    // а тому, чью броню он пробивал бы: в итогах игрока
                    // интересует, сколько его броня съела.
                }
            }

            if (target != null && target.Team == TeamId.Player)
            {
                int index = (int)target.ClassId;

                if (index >= 0 && index < _playerStats.Length)
                {
                    _playerStats[index].DamageTaken += applied;
                    _playerStats[index].DamagePrevented += prevented;
                }
            }
        }

        public void RegisterDeath(UnitRuntime dead)
        {
            if (dead == null)
                throw new ArgumentNullException(nameof(dead));

            if (dead.Team == TeamId.Player)
            {
                int index = (int)dead.ClassId;

                if (index >= 0 && index < _playerStats.Length)
                    _playerStats[index].Lost++;

                TotalUnitsLost++;
                return;
            }

            int enemyIndex = (int)dead.ClassId;

            if (enemyIndex >= 0 && enemyIndex < _enemyKillsByClass.Length)
                _enemyKillsByClass[enemyIndex]++;

            TotalEnemiesKilled++;

            // Убийство засчитываем тому, кто нанёс последний удар.
            if (!dead.HasDamageSource ||
                dead.LastDamageSourceTeam != TeamId.Player)
            {
                return;
            }

            int killerIndex = (int)dead.LastDamageSourceClass;

            if (killerIndex >= 0 && killerIndex < _playerStats.Length)
                _playerStats[killerIndex].Kills++;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            GoldEarned += amount;
        }

        private static int GetClassCount()
        {
            var values = (UnitClassId[])Enum.GetValues(typeof(UnitClassId));
            var highest = 0;

            for (var i = 0; i < values.Length; i++)
                highest = Math.Max(highest, (int)values[i]);

            return highest + 1;
        }
    }
}
