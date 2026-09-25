using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Загружает прогресс на старте приложения и пишет его при каждом
    /// изменении школы и завершённом переходе между этапами забега.
    ///
    /// Автосейв по событию, а не по таймеру: состояние школы меняется
    /// редко — покупка постройки, покупка предмета, начисление денариев
    /// после боя. Забег сохраняется на границах этапов, а не на каждом
    /// убийстве или тике симуляции.
    /// </summary>
    public sealed class SaveRunner : IStartable, IDisposable
    {
        private readonly SaveService _saveService;
        private readonly LupanariumState _lupanarium;
        private readonly RunState _run;

        public SaveRunner(
            SaveService saveService,
            LupanariumState lupanarium,
            RunState run)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));

            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public void Start()
        {
            bool loaded = _saveService.TryLoad();

            Debug.Log(
                loaded
                    ? $"[Save] Прогресс загружен: " +
                      $"{_lupanarium.Denarii} денариев."
                    : "[Save] Сохранения нет, школа начинает с нуля.");

            // Подписка идёт после загрузки: RestoreFrom поднимает Changed,
            // и без этого порядка мы бы тут же перезаписали только что
            // прочитанный сейв самим собой.
            _lupanarium.Changed += OnLupanariumChanged;
            _run.ProgressChanged += _saveService.Save;

            // Событие Changed бывает не всегда: приложение могут закрыть
            // или свернуть между покупками. На мобилках свёрнутую игру
            // система вправе убить без предупреждения, поэтому пишем
            // и при потере фокуса.
            Application.quitting += OnQuitting;
            Application.focusChanged += OnFocusChanged;
        }

        public void Dispose()
        {
            _lupanarium.Changed -= OnLupanariumChanged;
            _run.ProgressChanged -= _saveService.Save;

            Application.quitting -= OnQuitting;
            Application.focusChanged -= OnFocusChanged;
        }

        private void OnQuitting()
        {
            _saveService.Save();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                _saveService.Save();
        }

        private void OnLupanariumChanged()
        {
            _saveService.Save();
        }
    }
}
