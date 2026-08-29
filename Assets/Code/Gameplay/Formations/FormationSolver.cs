using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Считает позиции слотов строя.
    ///
    /// Работает в локальном пространстве формации: +X — вперёд, на врага,
    /// +Y — вбок. Все слоты имеют x меньше либо равный нулю, то есть стоят
    /// позади якоря; якорь — это остриё строя. В мир слот переводится
    /// умножением x на знак направления команды, поэтому поворот сводится
    /// к смене знака и тригонометрия не нужна.
    /// </summary>
    public sealed class FormationSolver
    {
        public void BuildSlots(
            FormationShape shape,
            int count,
            List<Vector2> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            destination.Clear();

            if (count == 0)
                return;

            switch (shape.Layout)
            {
                case FormationLayout.Line:
                    BuildRanks(
                        count,
                        Mathf.CeilToInt(count / (float)Mathf.Max(1, shape.Rows)),
                        shape,
                        destination);
                    break;

                case FormationLayout.Column:
                    BuildRanks(
                        count,
                        Mathf.Max(1, shape.Columns),
                        shape,
                        destination);
                    break;

                case FormationLayout.Box:
                    BuildRanks(
                        count,
                        Mathf.CeilToInt(Mathf.Sqrt(count)),
                        shape,
                        destination);
                    break;

                case FormationLayout.Wedge:
                    BuildWedge(count, shape, destination);
                    break;

                case FormationLayout.Circle:
                    BuildCircle(count, shape, destination);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(shape),
                        shape.Layout,
                        "Неизвестный макет строя.");
            }
        }

        /// <summary>
        /// Прямоугольная расстановка: перед строя ровный, ряды уходят назад.
        /// Обслуживает Line, Column и Box — они отличаются только тем,
        /// сколько бойцов встаёт в один ряд.
        /// </summary>
        private static void BuildRanks(
            int count,
            int perRank,
            FormationShape shape,
            List<Vector2> destination)
        {
            perRank = Mathf.Clamp(perRank, 1, count);

            float halfWidth = (perRank - 1) * 0.5f;

            for (var i = 0; i < count; i++)
            {
                int rank = i / perRank;
                int file = i % perRank;

                // Последний ряд обычно неполный — центрируем его отдельно,
                // иначе строй получается визуально кривым с одного края.
                int unitsInThisRank = Mathf.Min(
                    perRank,
                    count - rank * perRank);

                float rankHalfWidth = unitsInThisRank == perRank
                    ? halfWidth
                    : (unitsInThisRank - 1) * 0.5f;

                destination.Add(new Vector2(
                    -rank * shape.RankSpacing,
                    (file - rankHalfWidth) * shape.FileSpacing));
            }
        }

        private static void BuildWedge(
            int count,
            FormationShape shape,
            List<Vector2> destination)
        {
            var placed = 0;
            var rank = 0;

            while (placed < count)
            {
                // Ряд номер r вмещает r+1 бойца: получается треугольник
                // с одним бойцом на острие.
                int capacity = rank + 1;

                int unitsInRank = Mathf.Min(capacity, count - placed);
                float halfWidth = (unitsInRank - 1) * 0.5f;

                for (var file = 0; file < unitsInRank; file++)
                {
                    destination.Add(new Vector2(
                        -rank * shape.RankSpacing,
                        (file - halfWidth) * shape.FileSpacing));
                }

                placed += unitsInRank;
                rank++;
            }
        }

        private static void BuildCircle(
            int count,
            FormationShape shape,
            List<Vector2> destination)
        {
            var placed = 0;
            var ring = 0;
            var maximumRadius = 0f;

            // Кольца строим концентрическими вокруг нуля. Точки разных
            // колец лежат на разном расстоянии от центра и поэтому никогда
            // не совпадают, а внутри кольца углы различны — дублей нет.
            while (placed < count)
            {
                if (ring == 0)
                {
                    destination.Add(Vector2.zero);
                    placed++;
                    ring++;

                    continue;
                }

                float radius = ring * shape.FileSpacing;

                // Сколько бойцов помещается на кольцо, чтобы они не
                // налезали друг на друга.
                int capacity = Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        2f * Mathf.PI * radius / shape.FileSpacing));

                int unitsInRing = Mathf.Min(capacity, count - placed);

                for (var i = 0; i < unitsInRing; i++)
                {
                    float angle = 2f * Mathf.PI * i / unitsInRing;

                    destination.Add(new Vector2(
                        radius * Mathf.Cos(angle),
                        radius * Mathf.Sin(angle)));
                }

                maximumRadius = radius;

                placed += unitsInRing;
                ring++;
            }

            // И только теперь сдвигаем круг целиком назад, чтобы передний
            // край оказался на якоре — как у всех остальных макетов.
            for (var i = 0; i < destination.Count; i++)
            {
                destination[i] = new Vector2(
                    destination[i].x - maximumRadius,
                    destination[i].y);
            }
        }
    }
}
