using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public enum BattleFeedbackKind : byte
    {
        Hit = 0,
        Crit = 1,
        Death = 2,
        Ability = 3
    }

    /// <summary>
    /// Одно событие для слоя представления: где, что и насколько сильно.
    /// </summary>
    public readonly struct BattleFeedbackEvent
    {
        public BattleFeedbackKind Kind { get; }
        public int UnitId { get; }
        public Vector2 Position { get; }
        public float Amount { get; }

        public BattleFeedbackEvent(
            BattleFeedbackKind kind,
            int unitId,
            Vector2 position,
            float amount)
        {
            Kind = kind;
            UnitId = unitId;
            Position = position;
            Amount = amount;
        }
    }

    /// <summary>
    /// Канал «симуляция — представление» для эффектов: цифры урона,
    /// вспышки, тряска камеры, звук.
    ///
    /// Тот же приём, что с DeadViewQueue: симуляция только пишет, слой
    /// представления раз в кадр вычитывает и очищает. Ни одна боевая
    /// система не знает, что где-то существуют партиклы и AudioSource.
    /// </summary>
    public sealed class BattleFeedbackQueue
    {
        private const int InitialCapacity = 256;

        private readonly List<BattleFeedbackEvent> _events =
            new(InitialCapacity);

        public IReadOnlyList<BattleFeedbackEvent> Events => _events;

        public int Count => _events.Count;

        public void Push(
            BattleFeedbackKind kind,
            int unitId,
            Vector2 position,
            float amount)
        {
            _events.Add(
                new BattleFeedbackEvent(kind, unitId, position, amount));
        }

        public void Clear()
        {
            _events.Clear();
        }
    }
}
