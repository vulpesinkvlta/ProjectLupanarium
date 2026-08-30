using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Превращает события боя в эффекты: цифры урона, вспышки, звук,
    /// тряску камеры.
    ///
    /// Единственное место, где симуляция встречается с ощущением боя.
    /// Очередь наполняется боевыми системами, а вычитывается раз в кадр
    /// здесь — за счёт этого ни AttackSystem, ни DamageSystem не знают
    /// о существовании звука и партиклов.
    /// </summary>
    public sealed class BattleFeedbackPresenter : ITickable
    {
        private readonly BattleFeedbackQueue _queue;
        private readonly UnitViewRegistry _viewRegistry;
        private readonly BattleFeedbackView _view;

        public BattleFeedbackPresenter(
            BattleFeedbackQueue queue,
            UnitViewRegistry viewRegistry,
            BattleFeedbackView view)
        {
            _queue = queue ??
                throw new ArgumentNullException(nameof(queue));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _view = view ??
                throw new ArgumentNullException(nameof(view));
        }

        public void Tick()
        {
            if (_queue.Count == 0)
                return;

            _view.BeginFrame();

            IReadOnlyList<BattleFeedbackEvent> events = _queue.Events;

            for (var i = 0; i < events.Count; i++)
                Handle(events[i]);

            _queue.Clear();
        }

        private void Handle(BattleFeedbackEvent battleEvent)
        {
            switch (battleEvent.Kind)
            {
                case BattleFeedbackKind.Hit:
                    HandleHit(battleEvent, isCrit: false);
                    break;

                case BattleFeedbackKind.Crit:
                    HandleHit(battleEvent, isCrit: true);
                    break;

                case BattleFeedbackKind.Death:
                    _view.PlayDeath();
                    break;

                case BattleFeedbackKind.Ability:
                    _view.PlayAbility();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(battleEvent),
                        battleEvent.Kind,
                        "Неизвестный вид события боя.");
            }
        }

        private void HandleHit(BattleFeedbackEvent battleEvent, bool isCrit)
        {
            _view.ShowDamage(
                battleEvent.Position,
                battleEvent.Amount,
                isCrit);

            _view.PlayHit(isCrit);

            // Вью может уже не быть: юнит умер от этого же удара и его
            // вью ушло доигрывать гибель. Вспышка тогда не нужна.
            if (_viewRegistry.TryGet(battleEvent.UnitId, out UnitView view))
                view.Flash();
        }
    }
}
