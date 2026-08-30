using System;
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
            _saveService.TryLoad();

            // Подписка идёт после загрузки: RestoreFrom поднимает Changed,
            // и без этого порядка мы бы тут же перезаписали только что
            // прочитанный сейв самим собой.
            _lupanarium.Changed += OnLupanariumChanged;
        }

        public void Dispose()
        {
            _lupanarium.Changed -= OnLupanariumChanged;
        }

        private void OnLupanariumChanged()
        {
            _saveService.Save();
        }
    }
}
