using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class ArenaSandboxRoster : MonoBehaviour
    {
        [Header("Patterns are repeated when spawning large armies")]
        [SerializeField]
        private UnitConfig[] _playerPattern;

        [SerializeField]
        private UnitConfig[] _enemyPattern;

        public UnitConfig GetConfig(
            TeamId team,
            int unitIndex)
        {
            if (unitIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitIndex));
            }

            UnitConfig[] pattern = team switch
            {
                TeamId.Player => _playerPattern,
                TeamId.Enemy => _enemyPattern,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(team),
                    team,
                    "Unsupported team.")
            };

            if (pattern == null ||
                pattern.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No unit pattern configured for team {team}.");
            }

            UnitConfig config =
                pattern[unitIndex % pattern.Length];

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Pattern for team {team} contains " +
                    $"an empty config at index " +
                    $"{unitIndex % pattern.Length}.");
            }

            return config;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidatePattern(
                _playerPattern,
                nameof(_playerPattern));

            ValidatePattern(
                _enemyPattern,
                nameof(_enemyPattern));
        }

        private void ValidatePattern(
            UnitConfig[] pattern,
            string fieldName)
        {
            if (pattern == null || pattern.Length == 0)
            {
                Debug.LogWarning(
                    $"{fieldName} is empty.",
                    this);

                return;
            }

            for (var i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] != null)
                    continue;

                Debug.LogWarning(
                    $"{fieldName} contains an empty " +
                    $"element at index {i}.",
                    this);
            }
        }
#endif
    }
}
