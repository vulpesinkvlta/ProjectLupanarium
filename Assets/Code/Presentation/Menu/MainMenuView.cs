using System;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _playButton;
        [SerializeField] private TMP_Text _playLabel;
        [SerializeField] private UnityEngine.UI.Button _deleteButton;
        [SerializeField] private UnityEngine.UI.Button _settingsButton;
        [SerializeField] private UnityEngine.UI.Button _closeSettingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private UnityEngine.UI.Button _resetAllButton;
        [SerializeField] private UnityEngine.UI.Button _confirmResetButton;
        [SerializeField] private UnityEngine.UI.Button _cancelResetButton;
        [SerializeField] private GameObject _resetPanel;

        private bool _hasRun;
        private bool _busy;
        public event Action PlayRequested;
        public event Action DeleteRequested;
        public event Action ResetAllRequested;

        private void Awake()
        {
            _playButton.onClick.AddListener(Play);
            _deleteButton.onClick.AddListener(Delete);
            _settingsButton.onClick.AddListener(OpenSettings);
            _closeSettingsButton.onClick.AddListener(CloseSettings);
            CloseSettings();
            _resetAllButton.onClick.AddListener(OpenReset);
            _confirmResetButton.onClick.AddListener(ConfirmReset);
            _cancelResetButton.onClick.AddListener(CloseReset);
            CloseReset();
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(Play);
            _deleteButton.onClick.RemoveListener(Delete);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _closeSettingsButton.onClick.RemoveListener(CloseSettings);
            _resetAllButton.onClick.RemoveListener(OpenReset);
            _confirmResetButton.onClick.RemoveListener(ConfirmReset);
            _cancelResetButton.onClick.RemoveListener(CloseReset);
        }

        public void Refresh(bool hasRun, int round)
        {
            _hasRun = hasRun;
            _playLabel.text = hasRun ? "Продолжить забег" : "Начать новый забег";
            _statusLabel.text = hasRun ? $"Сохранённый забег · раунд {round}" : "Нет активного забега";
            SetBusy(false);
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            _playButton.interactable = !busy;
            _deleteButton.interactable = !busy && _hasRun;
            _settingsButton.interactable = !busy;
            _resetAllButton.interactable = !busy;
        }

        public void ShowError(string message) => _statusLabel.text = message;
        private void Play() { if (!_busy) PlayRequested?.Invoke(); }
        private void Delete() { if (!_busy && _hasRun) DeleteRequested?.Invoke(); }
        private void OpenSettings() => _settingsPanel.SetActive(true);
        private void CloseSettings() => _settingsPanel.SetActive(false);
        private void OpenReset() { if (!_busy) _resetPanel.SetActive(true); }
        private void CloseReset() => _resetPanel.SetActive(false);
        private void ConfirmReset()
        {
            if (_busy || !_resetPanel.activeSelf) return;
            CloseReset();
            ResetAllRequested?.Invoke();
        }
    }
}
