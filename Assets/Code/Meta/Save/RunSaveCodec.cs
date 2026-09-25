using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>Ссылки на ассеты превращаются в стабильные id и обратно.</summary>
    public sealed class RunSaveCodec
    {
        private readonly Dictionary<string, UnitConfig> _units = new();
        private readonly Dictionary<string, UpgradeConfig> _upgrades = new();
        private readonly Dictionary<string, FormationConfig> _formations = new();
        private readonly Dictionary<string, ContractConfig> _contracts = new();

        public RunSaveCodec(
            RunConfig config,
            RosterCatalog roster,
            UpgradeCatalog upgrades,
            FormationCatalog formations,
            ContractCatalog contracts)
        {
            AddSquad(config.StartingSquad);
            Add(_formations, config.DefaultFormation, config.DefaultFormation?.Id);

            foreach (RosterEntry entry in roster.Entries)
                Add(_units, entry?.Unit, entry?.Id);

            foreach (UpgradeConfig upgrade in upgrades.Upgrades)
            {
                if (upgrade == null)
                    continue;

                Add(_upgrades, upgrade, upgrade.Id);
                Add(_units, upgrade.UnitToAdd, upgrade.UnitToAdd?.Id);
            }

            foreach (FormationEntry entry in formations.Entries)
                Add(_formations, entry?.Formation, entry?.Id);

            foreach (ContractConfig contract in contracts.Contracts)
            {
                if (contract == null)
                    continue;

                Add(_contracts, contract, contract.Id);
                AddSquad(contract.Enemies);
            }
        }

        public RunSaveData Capture(RunState run)
        {
            if (!run.IsActive)
                return null;

            var data = new RunSaveData
            {
                // Прерванный бой начинается заново с его подготовки.
                // Золото за убийства из незавершённого боя не дублируется.
                Phase = run.Phase == BattleFlowState.Fighting
                    ? BattleFlowState.Preparation : run.Phase,
                WaveIndex = run.WaveIndex,
                Gold = run.Phase == BattleFlowState.Fighting
                    ? run.GoldBeforeBattle : run.Gold,
                FormationId = run.SelectedFormation?.Id,
                Squad = CaptureSquad(run.Squad),
                UpgradeIds = CaptureUpgrades(run.AcquiredUpgrades),
                RewardChoiceIds = CaptureUpgrades(run.RewardChoices),
                ActiveContract = CaptureContract(run.ActiveContract),
                ContractOffers = new SavedContractOffer[run.ContractOffers.Count]
            };

            for (var i = 0; i < data.ContractOffers.Length; i++)
                data.ContractOffers[i] = CaptureContract(run.ContractOffers[i]);

            return data;
        }

        public void Restore(RunState run, RunSaveData data)
        {
            if (data == null)
            {
                run.Reset();
                return;
            }

            try
            {
                if (data.Phase < BattleFlowState.SquadSelection ||
                    data.Phase > BattleFlowState.Reward)
                    throw new InvalidOperationException("Неизвестный этап забега.");

                List<SquadEntry> squad = RestoreSquad(data.Squad);
                if (squad.Count == 0 && data.Phase != BattleFlowState.SquadSelection)
                    throw new InvalidOperationException("В сохранённом отряде нет бойцов.");

                var offers = new List<ContractOffer>();
                foreach (SavedContractOffer offer in data.ContractOffers ?? Array.Empty<SavedContractOffer>())
                    offers.Add(RestoreContract(offer) ?? new ContractOffer());

                ContractOffer active = RestoreContract(data.ActiveContract);
                if ((data.Phase == BattleFlowState.Preparation ||
                     data.Phase == BattleFlowState.FormationSelection ||
                     data.Phase == BattleFlowState.Fighting) && active == null)
                    throw new InvalidOperationException("В сохранении отсутствует выбранный контракт.");

                run.RestoreProgress(
                    data, squad, RestoreUpgrades(data.UpgradeIds),
                    string.IsNullOrEmpty(data.FormationId)
                        ? null : Find(_formations, data.FormationId),
                    active, RestoreUpgrades(data.RewardChoiceIds), offers);
            }
            catch (InvalidOperationException exception)
            {
                // Изменённый/повреждённый забег не должен мешать загрузке школы.
                run.Reset();
                Debug.LogWarning($"[Save] Забег не восстановлен: {exception.Message}");
            }
        }

        private void AddSquad(IReadOnlyList<SquadEntry> squad)
        {
            if (squad == null)
                return;

            for (var i = 0; i < squad.Count; i++)
                Add(_units, squad[i]?.Config, squad[i]?.Config?.Id);
        }

        private static void Add<T>(Dictionary<string, T> catalog, T asset, string id)
            where T : UnityEngine.Object
        {
            if (asset != null && !string.IsNullOrEmpty(id))
                catalog[id] = asset;
        }

        private static T Find<T>(Dictionary<string, T> catalog, string id)
        {
            if (string.IsNullOrEmpty(id) || !catalog.TryGetValue(id, out T asset))
                throw new InvalidOperationException($"Не найден ассет с id '{id}'.");

            return asset;
        }

        private static SavedSquadEntry[] CaptureSquad(IReadOnlyList<SquadEntry> squad)
        {
            var result = new SavedSquadEntry[squad.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = new SavedSquadEntry { UnitId = squad[i].Config.Id, Count = squad[i].Count };
            return result;
        }

        private List<SquadEntry> RestoreSquad(SavedSquadEntry[] squad)
        {
            var result = new List<SquadEntry>();
            foreach (SavedSquadEntry entry in squad ?? Array.Empty<SavedSquadEntry>())
            {
                if (entry == null || entry.Count <= 0)
                    throw new InvalidOperationException("Некорректная запись отряда.");
                result.Add(new SquadEntry(Find(_units, entry.UnitId), entry.Count));
            }
            return result;
        }

        private static string[] CaptureUpgrades(IReadOnlyList<UpgradeConfig> upgrades)
        {
            var result = new string[upgrades.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = upgrades[i].Id;
            return result;
        }

        private List<UpgradeConfig> RestoreUpgrades(string[] ids)
        {
            var result = new List<UpgradeConfig>();
            foreach (string id in ids ?? Array.Empty<string>())
                result.Add(Find(_upgrades, id));
            return result;
        }

        private static SavedContractOffer CaptureContract(ContractOffer offer)
        {
            return offer.Source == null ? null : new SavedContractOffer
            {
                ContractId = offer.Source.Id,
                GoldReward = offer.GoldReward,
                Enemies = CaptureSquad(offer.Enemies)
            };
        }

        private ContractOffer RestoreContract(SavedContractOffer saved)
        {
            if (saved == null || string.IsNullOrEmpty(saved.ContractId))
                return null;

            var offer = new ContractOffer();
            offer.Set(Find(_contracts, saved.ContractId),
                Math.Max(0, saved.GoldReward), RestoreSquad(saved.Enemies));
            return offer;
        }
    }
}
