using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    public sealed class ArenaDebugPanel : MonoBehaviour
    {
        [Header("Spawn Buttons")]
        [SerializeField] private Button _spawn10Button;
        [SerializeField] private Button _spawn50Button;
        [SerializeField] private Button _spawn100Button;
        [SerializeField] private Button _spawn250Button;

        [Header("Other Buttons")]
        [SerializeField] private Button _clearButton;

        [Header("Information")]
        [SerializeField] private TMP_Text _statisticsLabel;

        public event Action<int> SpawnRequested;
        public event Action ClearRequested;


        public void SetStatistics(string statistics)
        {
            if (_statisticsLabel != null)
                _statisticsLabel.text = statistics;
        }

        private void Awake()
        {
            _spawn10Button.onClick.AddListener(
                OnSpawn10Clicked);

            _spawn50Button.onClick.AddListener(
                OnSpawn50Clicked);

            _spawn100Button.onClick.AddListener(
                OnSpawn100Clicked);

            _spawn250Button.onClick.AddListener(
                OnSpawn250Clicked);

            _clearButton.onClick.AddListener(
                OnClearClicked);
        }

        private void OnDestroy()
        {
            _spawn10Button.onClick.RemoveListener(
                OnSpawn10Clicked);

            _spawn50Button.onClick.RemoveListener(
                OnSpawn50Clicked);

            _spawn100Button.onClick.RemoveListener(
                OnSpawn100Clicked);

            _spawn250Button.onClick.RemoveListener(
                OnSpawn250Clicked);

            _clearButton.onClick.RemoveListener(
                OnClearClicked);
        }

        private void OnSpawn10Clicked()
        {
            SpawnRequested?.Invoke(10);
        }

        private void OnSpawn50Clicked()
        {
            SpawnRequested?.Invoke(50);
        }

        private void OnSpawn100Clicked()
        {
            SpawnRequested?.Invoke(100);
        }

        private void OnSpawn250Clicked()
        {
            SpawnRequested?.Invoke(250);
        }

        private void OnClearClicked()
        {
            ClearRequested?.Invoke();
        }
    }
}
