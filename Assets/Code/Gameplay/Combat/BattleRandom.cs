using System;

namespace Code.Gameplay
{
    /// <summary>
    /// Источник случайности боя.
    ///
    /// Своя копия System.Random, а не UnityEngine.Random: у симуляции
    /// фиксированный тик, и с собственным зерном бой воспроизводим —
    /// это пригодится и для отладки, и для повторов. Глобальный
    /// UnityEngine.Random зависит от всего, что творится в кадре.
    /// </summary>
    public sealed class BattleRandom
    {
        private const int DefaultSeed = 20250817;

        private Random _random;

        public BattleRandom()
        {
            _random = new Random(DefaultSeed);
        }

        public void Reseed(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>Случайное число в диапазоне [0, 1).</summary>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>Прошла ли проверка с вероятностью chance.</summary>
        public bool Roll(float chance)
        {
            if (chance <= 0f)
                return false;

            if (chance >= 1f)
                return true;

            return NextFloat() < chance;
        }
    }
}
