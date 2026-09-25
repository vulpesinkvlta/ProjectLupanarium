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
    /// Прогресс школы и забега записывается одним JSON-снимком.
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
    /// Сохранение и загрузка школы и незавершённого забега.
    /// </summary>
    public sealed class SaveService
    {
        private const string SaveKey = "lupanarium.save";

        private readonly ISaveStorage _storage;
        private readonly LupanariumState _lupanarium;
        private readonly RunState _run;
        private readonly RunSaveCodec _runCodec;
        private readonly GameSaveData _buffer = new();

        public SaveService(
            ISaveStorage storage,
            LupanariumState lupanarium,
            RunState run,
            RunSaveCodec runCodec)
        {
            _storage = storage ??
                throw new ArgumentNullException(nameof(storage));

            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _runCodec = runCodec ?? throw new ArgumentNullException(nameof(runCodec));
        }

        public void Save()
        {
            _lupanarium.CaptureTo(_buffer);
            _buffer.HasActiveRun = _run.IsActive;
            _buffer.ActiveRun = _runCodec.Capture(_run);

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

            _runCodec.Restore(_run, data.HasActiveRun ? data.ActiveRun : null);
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
        /// Миграции идут по цепочке, шаг за шагом: игрок мог пропустить
        /// несколько обновлений, и сейв версии 1 должен доехать до
        /// текущей через все промежуточные шаги, а не только с предыдущей.
        /// </summary>
        // internal, а не private: headless-тесты компилируют исходники
        // в свою сборку и проверяют цепочку миграций напрямую.
        internal static bool TryMigrate(GameSaveData data)
        {
            if (data.Version > GameSaveData.CurrentVersion)
            {
                Debug.LogWarning(
                    $"[Save] Сейв версии {data.Version} новее " +
                    $"поддерживаемой {GameSaveData.CurrentVersion}, " +
                    $"загрузка отменена.");

                return false;
            }

            if (data.Version < 1)
            {
                Debug.LogWarning(
                    $"[Save] Сейв версии {data.Version} не поддерживается, " +
                    $"прогресс начнётся заново.");

                return false;
            }

            int from = data.Version;

            if (data.Version == 1)
            {
                // 1 -> 2: добавились ростер и лучший раунд. Оба поля
                // необязательные, поэтому миграция сводится к тому,
                // чтобы признать сейв годным: постройки, денарии
                // и снаряжение из него читаются как есть.
                data.UnlockedUnitIds ??= Array.Empty<string>();
                data.Version = 2;
            }

            if (data.Version == 2)
            {
                // 2 -> 3: добавились открытые строи. Старый игрок
                // не открывал ни одного — пустой список ровно это
                // и означает, а первые забеги он и так проходил толпой.
                data.UnlockedFormationIds ??= Array.Empty<string>();
                data.Version = 3;
            }

            if (data.Version == 3)
            {
                // Раньше сохранялась только школа. Восстановить старый
                // забег невозможно, но весь постоянный прогресс остаётся.
                data.ActiveRun = null;
                data.HasActiveRun = false;
                data.Version = 4;
            }

            if (data.Version != GameSaveData.CurrentVersion)
            {
                Debug.LogWarning(
                    $"[Save] Для сейва версии {from} нет миграции " +
                    $"до версии {GameSaveData.CurrentVersion}, " +
                    $"прогресс начнётся заново.");

                return false;
            }

            if (from != GameSaveData.CurrentVersion)
            {
                Debug.Log(
                    $"[Save] Сейв версии {from} обновлён до версии " +
                    $"{GameSaveData.CurrentVersion}, прогресс школы сохранён.");
            }

            return true;
        }
    }
}
