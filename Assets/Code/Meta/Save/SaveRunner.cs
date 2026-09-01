using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Загружает прогресс на старте приложения и пишет его при каждом
    /// изменении школы.
    ///
    /// Автосейв по событию, а не по таймеру: состояние школы меняется
    /// редко — покупка постройки, покупка предмета, начисление денариев
    /// после забега. Писать чаще незачем, а терять прогресс при вылете
    /// не хочется.
    /// </summary>
    public sealed class SaveRunner : IStartable, IDisposable
    {
        private readonly SaveService _saveService;
        private readonly LupanariumState _lupanarium;

        public SaveRunner(
            SaveService saveService,
            LupanariumState lupanarium)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));

            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));
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
