using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Code.Gameplay
{
    // Thin bridge to the official HTML5 SDK. No third-party ad networks or Editor rewards.
    public sealed class YandexBridge : MonoBehaviour
    {
        public static bool Enabled
        {
            get
            {
#if UNITY_WEBGL && YANDEX_GAMES && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        public bool Ready { get; private set; }
        public bool Busy { get; private set; }
        public bool Paused => _platformPause || _focusPause || _adVisible || Busy;
        public string Status { get; private set; } = "Подключение рекламы…";
        public Action Tick;
        private Action _reward, _closed;
        private int _request;
        private bool _granted, _platformPause, _focusPause, _adVisible, _appliedPause, _gameReady, _gameplay;
        private float _previousScale;
        private bool _previousAudioPause;
        private UnityEngine.EventSystems.EventSystem _pausedEvents;

        [DllImport("__Internal")] private static extern void YG_Init(string receiver);
        [DllImport("__Internal")] private static extern void YG_Ready();
        [DllImport("__Internal")] private static extern void YG_Gameplay(int active);
        [DllImport("__Internal")] private static extern void YG_Ad(int rewarded, int request);
        [DllImport("__Internal")] private static extern void YG_Banner(int visible);

        private void Start() { if (Enabled) YG_Init(gameObject.name); }
        private void Update()
        {
            if (Enabled && Ready && !_gameReady && SceneManager.GetActiveScene().name != "0.Bootstrap")
            { _gameReady = true; YG_Ready(); }
            Tick?.Invoke();
        }
        public void Gameplay(bool active)
        {
            if (!Enabled || !Ready) return;
            active &= !Paused;
            if (_gameplay == active) return;
            _gameplay = active;
            if (Enabled && Ready) YG_Gameplay(active ? 1 : 0);
        }
        public void Banner(bool visible) { if (Enabled && Ready) YG_Banner(visible ? 1 : 0); }
        public bool Show(bool rewarded, Action reward, Action closed = null)
        {
            if (!Enabled || !Ready || Busy || Paused) return false;
            Busy = true; _granted = false; _reward = reward; _closed = closed; _request++;
            Status = "Открывается реклама…";
            ApplyPause(); Gameplay(false);
            YG_Ad(rewarded ? 1 : 0, _request);
            return true;
        }
        [Preserve] public void OnSdkReady(string unused) { Ready = true; Status = ""; }
        [Preserve] public void OnSdkError(string unused) { Status = "Реклама недоступна. Игра доступна без неё."; }
        [Preserve] public void OnPlatformPause(string value) { _platformPause = value == "1"; ApplyPause(); }
        [Preserve] public void OnAdVisibility(string value) { _adVisible = value == "1"; ApplyPause(); }
        [Preserve] public void OnPageFocus(string value) { _focusPause = value != "1"; ApplyPause(); }
        [Preserve] public void OnAdReward(string value)
        {
            if (!Busy || _granted || value != _request.ToString()) return;
            _granted = true;
            _reward?.Invoke(); // Only the SDK onRewarded callback reaches this method.
        }
        [Preserve] public void OnAdClosed(string value)
        {
            if (!Busy || value != _request.ToString()) return;
            Busy = false; _reward = null;
            Status = _granted ? "Награда получена" : "Реклама закрыта или недоступна";
            ApplyPause();
            var action = _closed; _closed = null; action?.Invoke();
        }
        private void OnApplicationFocus(bool focused)
        { if (Enabled) { _focusPause = !focused; ApplyPause(); } }
        private void ApplyPause()
        {
            if (Paused == _appliedPause) return;
            if (Paused)
            {
                _previousScale = Time.timeScale; _previousAudioPause = AudioListener.pause;
                _pausedEvents = UnityEngine.EventSystems.EventSystem.current;
                if (_pausedEvents != null) _pausedEvents.enabled = false;
                Time.timeScale = 0; AudioListener.pause = true;
            }
            else { Time.timeScale = _previousScale; AudioListener.pause = _previousAudioPause;
                if (_pausedEvents != null) _pausedEvents.enabled = true; }
            _appliedPause = Paused;
        }
        private void OnDestroy()
        { if (_appliedPause) { Time.timeScale = _previousScale; AudioListener.pause = _previousAudioPause;
                if (_pausedEvents != null) _pausedEvents.enabled = true; } }
    }
}
