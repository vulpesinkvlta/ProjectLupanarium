using System;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Куда физически ложится сейв. Вынесено за интерфейс, чтобы логику
    /// сохранения можно было проверить без файловой системы и без Unity.
    /// </summary>
    public interface ISaveStorage
    {
        bool TryRead(string key, out string payload);
        void Write(string key, string payload);
        void Delete(string key);
    }

    /// <summary>
    /// PlayerPrefs как хранилище.
    ///
    /// Выбран вместо файла в persistentDataPath потому, что одинаково
    /// работает на всех целевых платформах, включая WebGL, где файловая
    /// система живёт в IndexedDB и требует ручной синхронизации.
    /// Сейв здесь маленький — несколько сотен байт JSON.
    /// </summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        public bool TryRead(string key, out string payload)
        {
            payload = PlayerPrefs.GetString(key, string.Empty);
            return !string.IsNullOrEmpty(payload);
        }

        public void Write(string key, string payload)
        {
            PlayerPrefs.SetString(key, payload);

            // Без явного Save данные на WebGL и мобилках могут
            // не дожить до следующего запуска.
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Сохранение и загрузка постоянного прогресса.
    /// </summary>
    public sealed class SaveService
    {
        private const string SaveKey = "lupanarium.save";

        private readonly ISaveStorage _storage;
        private readonly LupanariumState _lupanarium;
        private readonly GameSaveData _buffer = new();

        public SaveService(
            ISaveStorage storage,
            LupanariumState lupanarium)
        {
            _storage = storage ??
                throw new ArgumentNullException(nameof(storage));

            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));
        }

        public void Save()
        {
            _lupanarium.CaptureTo(_buffer);

            _storage.Write(
                SaveKey,
                JsonUtility.ToJson(_buffer));
        }

        /// <summary>
        /// Возвращает false, если сейва нет или он не читается —
        /// тогда игра просто начинается с чистой школы.
        /// </summary>
        public bool TryLoad()
        {
            if (!_storage.TryRead(SaveKey, out string payload))
                return false;

            GameSaveData data;

            try
            {
                data = JsonUtility.FromJson<GameSaveData>(payload);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Save] Сейв повреждён и будет проигнорирован: " +
                    $"{exception.Message}");

                return false;
            }

            if (data == null)
                return false;

            if (!TryMigrate(data))
                return false;

            _lupanarium.RestoreFrom(data);
            return true;
        }

        public void Delete()
        {
            _storage.Delete(SaveKey);
        }

        /// <summary>
        /// Приводит сейв к текущей версии схемы.
        ///
        /// Пока версия одна, и метод только отсекает будущие сейвы:
        /// если игрок откатит билд, читать данные новее нашей схемы
        /// нельзя — поля могли изменить смысл.
        /// </summary>
        private static bool TryMigrate(GameSaveData data)
        {
            if (data.Version == GameSaveData.CurrentVersion)
                return true;

            if (data.Version > GameSaveData.CurrentVersion)
            {
                Debug.LogWarning(
                    $"[Save] Сейв версии {data.Version} новее " +
                    $"поддерживаемой {GameSaveData.CurrentVersion}, " +
                    $"загрузка отменена.");

                return false;
            }

            // Здесь появятся миграции со старых версий:
            // if (data.Version < 2) { ...; data.Version = 2; }

            Debug.LogWarning(
                $"[Save] Сейв версии {data.Version} не поддерживается, " +
                $"прогресс начнётся заново.");

            return false;
        }
    }
}
