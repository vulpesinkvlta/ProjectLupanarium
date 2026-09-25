using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Gameplay
{
    /// <summary>Три раздела базы; одновременно открыт только один.</summary>
    public sealed class BasePanelNavigation : MonoBehaviour
    {
        [SerializeField] private GameObject _panelLayer;
        [SerializeField] private GameObject[] _panels;
        [SerializeField] private UnityEngine.UI.Button[] _tabs;
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        [SerializeField] private UnityEngine.UI.Button _backdropButton;
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Color _normalColor = new(0.19f, 0.17f, 0.15f);
        [SerializeField] private Color _selectedColor = new(0.48f, 0.20f, 0.17f);

        private static readonly string[] Titles = { "Гладиаторы", "Постройки", "Арсенал" };
        private static readonly string[] Descriptions =
        {
            "Открывайте бойцов для следующих забегов",
            "Улучшения школы действуют во всех забегах",
            "Покупайте и надевайте снаряжение для своих бойцов"
        };

        public int OpenPanelIndex { get; private set; } = -1;

        private void Awake()
        {
            _tabs[0].onClick.AddListener(ToggleRoster);
            _tabs[1].onClick.AddListener(ToggleBuildings);
            _tabs[2].onClick.AddListener(ToggleItems);
            _closeButton.onClick.AddListener(ClosePanels);
            _backdropButton.onClick.AddListener(ClosePanels);
            ClosePanels();
        }

        private void Update()
        {
            if (OpenPanelIndex >= 0 && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
                ClosePanels();
        }

        public void TogglePanel(int index)
        {
            if (index < 0 || index >= _panels.Length)
                return;
            if (OpenPanelIndex == index)
            {
                ClosePanels();
                return;
            }

            OpenPanelIndex = index;
            _panelLayer.SetActive(true);
            for (var i = 0; i < _panels.Length; i++)
                _panels[i].SetActive(i == index);
            _heading.text = Titles[index];
            _description.text = Descriptions[index];

            // Размеры динамических карточек должны быть известны прежде,
            // чем ScrollRect рассчитает границы после открытия панели.
            Canvas.ForceUpdateCanvases();
            var scroll = _panels[index].GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            RefreshTabs();
            _closeButton.Select();
        }

        public void ClosePanels()
        {
            int previous = OpenPanelIndex;
            OpenPanelIndex = -1;
            for (var i = 0; i < _panels.Length; i++)
                _panels[i].SetActive(false);
            _panelLayer.SetActive(false);
            RefreshTabs();
            if (previous >= 0)
                _tabs[previous].Select();
        }

        private void RefreshTabs()
        {
            for (var i = 0; i < _tabs.Length; i++)
            {
                var colors = _tabs[i].colors;
                colors.normalColor = i == OpenPanelIndex ? _selectedColor : _normalColor;
                colors.selectedColor = colors.normalColor;
                _tabs[i].colors = colors;
            }
        }

        private void ToggleRoster() => TogglePanel(0);
        private void ToggleBuildings() => TogglePanel(1);
        private void ToggleItems() => TogglePanel(2);

        private void OnDestroy()
        {
            _tabs[0].onClick.RemoveListener(ToggleRoster);
            _tabs[1].onClick.RemoveListener(ToggleBuildings);
            _tabs[2].onClick.RemoveListener(ToggleItems);
            _closeButton.onClick.RemoveListener(ClosePanels);
            _backdropButton.onClick.RemoveListener(ClosePanels);
        }
    }
}
