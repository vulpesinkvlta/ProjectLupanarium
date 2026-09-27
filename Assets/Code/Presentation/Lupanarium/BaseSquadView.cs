using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class BaseSquadView : MonoBehaviour
    {
        [SerializeField] private UnitView _unitPrefab;
        [SerializeField] private Transform _unitsRoot;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private GameObject _emptyState;
        [SerializeField] private Vector2 _areaSize = new(10f, 4f);
        private readonly List<UnitView> _units = new();

        private void Update()
        {
            for (int i = 0; i < DisplayedUnitCount; i++)
                _units[i].TickVisuals(Time.deltaTime);
        }

        public int DisplayedUnitCount { get; private set; }

        public void Refresh(IReadOnlyList<SquadEntry> squad, UnitClassHudCatalog catalog)
        {
            var total = 0;
            for (var i = 0; i < squad.Count; i++)
                if (squad[i]?.Config != null)
                    total += squad[i].Count;

            DisplayedUnitCount = total;
            _countLabel.text = total > 0 ? $"ОТРЯД В ЗАБЕГЕ  /  {total}" : "ОТРЯД ЕЩЁ НЕ СОБРАН";
            _emptyState.SetActive(total == 0);

            while (_units.Count < total)
                _units.Add(Instantiate(_unitPrefab, _unitsRoot));
            for (int i = total; i < _units.Count; i++)
                _units[i].gameObject.SetActive(false);

            var index = 0;
            float scale = Mathf.Min(1f, Mathf.Sqrt(24f / Mathf.Max(1, total)));
            foreach (SquadEntry entry in squad)
            {
                if (entry?.Config == null)
                    continue;
                string name = entry.Config.ClassId.ToString();
                Color color = new(0.15f, 0.65f, 0.78f);
                if (catalog != null && catalog.TryGet(entry.Config.ClassId, out var hud))
                {
                    name = hud.DisplayName;
                    // В каталоге допускается белый акцент; на песке вместо
                    // него используем цвет союзников из текущего прототипа.
                    if (hud.AccentColor != Color.white && hud.AccentColor.a > 0)
                        color = hud.AccentColor;
                }

                for (var j = 0; j < entry.Count; j++, index++)
                {
                    UnitView unit = _units[index];
                    float radius = total <= 1 ? 0f : Mathf.Sqrt((index + 0.5f) / total);
                    float angle = index * 2.399963f;
                    unit.transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * radius * _areaSize.x * 0.5f,
                        Mathf.Sin(angle) * radius * _areaSize.y * 0.5f, 0);
                    unit.transform.localScale = Vector3.one * scale;
                    unit.gameObject.SetActive(true);
                    unit.BindDisplay(entry.Config.ClassId, name, color, index, total <= 12);
                }
            }
        }
    }
}
