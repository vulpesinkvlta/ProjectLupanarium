using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Id юнитов, чьи вью нужно вернуть в пул.
    ///
    /// Заполняется симуляцией (UnitCleanupSystem), вычитывается слоем
    /// представления раз в кадр. Нужна отдельно от UnitDeathBuffer, чтобы
    /// рендер не дренил буфер симуляции и не отбирал события смерти
    /// у остальных подписчиков.
    /// </summary>
    public sealed class DeadViewQueue
    {
        private const int InitialCapacity = 256;

        private readonly List<int> _unitIds =
            new(InitialCapacity);

        public IReadOnlyList<int> UnitIds => _unitIds;

        public int Count => _unitIds.Count;

        public void Enqueue(int unitId)
        {
            _unitIds.Add(unitId);
        }

        public void Clear()
        {
            _unitIds.Clear();
        }
    }
}
