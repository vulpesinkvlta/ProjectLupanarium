using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Круглая площадка боя. За её край юниты не выходят.
    ///
    /// Обычные данные, скопированные со сцены при создании: симуляция
    /// не должна дёргать MonoBehaviour каждый тик, да и тестировать
    /// такую границу можно без Unity.
    /// </summary>
    public sealed class ArenaBounds
    {
        private const float MinimumRadius = 1f;

        public Vector2 Center { get; }
        public float Radius { get; }

        public ArenaBounds(Vector2 center, float radius)
        {
            Center = center;
            Radius = Mathf.Max(MinimumRadius, radius);
        }

        /// <summary>
        /// Возвращает позицию, вжатую внутрь круга.
        ///
        /// bodyRadius вычитается из радиуса площадки, чтобы боец упирался
        /// краем тела, а не центром — иначе половина спрайта торчала бы
        /// за песком арены.
        /// </summary>
        public Vector2 Clamp(Vector2 position, float bodyRadius)
        {
            float limit = Mathf.Max(0f, Radius - bodyRadius);

            Vector2 offset = position - Center;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance <= limit * limit)
                return position;

            // Внутри круга — оставляем как есть; снаружи — сажаем
            // ровно на границу, сохраняя направление от центра.
            if (sqrDistance <= Mathf.Epsilon)
                return Center;

            float distance = Mathf.Sqrt(sqrDistance);

            return Center + offset / distance * limit;
        }

        public bool Contains(Vector2 position, float bodyRadius)
        {
            float limit = Mathf.Max(0f, Radius - bodyRadius);

            return (position - Center).sqrMagnitude <= limit * limit;
        }
    }
}
