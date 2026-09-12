using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MasterFramework.Services
{
    public interface IPortfolioSaveService
    {
        bool IsStageCleared(string gameId, int stageIndex);
        void SetStageCleared(string gameId, int stageIndex, bool cleared = true);
        int GetClearedStageCount(string gameId, int totalStages);
        bool IsGameCompleted(string gameId, int totalStages);
        string GetStageState(string gameId, int stageIndex);
        void SetStageState(string gameId, int stageIndex, string stateData);
        void Save();
        void Load();
        void ResetAllProgress();
    }

    [Serializable]
    public class PortfolioSaveData
    {
        public List<GameSaveEntry> games = new List<GameSaveEntry>();
    }

    [Serializable]
    public class GameSaveEntry
    {
        public string gameId;
        public List<int> clearedStages = new List<int>();
        public List<StageStateEntry> stageStates = new List<StageStateEntry>();
        public int lastPlayedStage = 0;
        public bool isCompleted = false;
    }

    [Serializable]
    public class StageStateEntry
    {
        public int stageIndex;
        public string stateJson;
    }

    public class PortfolioSaveService : IPortfolioSaveService
    {
        private static PortfolioSaveService _instance;
        public static PortfolioSaveService Instance => _instance ?? (_instance = new PortfolioSaveService());

        private PortfolioSaveData _data = new PortfolioSaveData();
        private const string PrefsKey = "Portfolio_Save_Data";
        private readonly string _filePath;

        public PortfolioSaveService()
        {
            _instance = this;
            _filePath = Path.Combine(Application.persistentDataPath, "portfolio_save.json");
            Load();
        }

        private GameSaveEntry GetOrCreateEntry(string gameId)
        {
            if (string.IsNullOrEmpty(gameId)) gameId = "Default_Game";
            var entry = _data.games.Find(g => g.gameId == gameId);
            if (entry == null)
            {
                entry = new GameSaveEntry { gameId = gameId };
                _data.games.Add(entry);
            }
            return entry;
        }

        public bool IsStageCleared(string gameId, int stageIndex)
        {
            var entry = GetOrCreateEntry(gameId);
            return entry.clearedStages.Contains(stageIndex);
        }

        public void SetStageCleared(string gameId, int stageIndex, bool cleared = true)
        {
            var entry = GetOrCreateEntry(gameId);
            if (cleared)
            {
                if (!entry.clearedStages.Contains(stageIndex))
                {
                    entry.clearedStages.Add(stageIndex);
                }
            }
            else
            {
                entry.clearedStages.Remove(stageIndex);
            }
            Save();
        }

        public int GetClearedStageCount(string gameId, int totalStages)
        {
            var entry = GetOrCreateEntry(gameId);
            return entry.clearedStages.Count;
        }

        public bool IsGameCompleted(string gameId, int totalStages)
        {
            if (totalStages <= 0) return false;
            var entry = GetOrCreateEntry(gameId);
            return entry.clearedStages.Count >= totalStages;
        }

        public string GetStageState(string gameId, int stageIndex)
        {
            var entry = GetOrCreateEntry(gameId);
            var state = entry.stageStates.Find(s => s.stageIndex == stageIndex);
            return state?.stateJson;
        }

        public void SetStageState(string gameId, int stageIndex, string stateData)
        {
            var entry = GetOrCreateEntry(gameId);
            var state = entry.stageStates.Find(s => s.stageIndex == stageIndex);
            if (state == null)
            {
                state = new StageStateEntry { stageIndex = stageIndex };
                entry.stageStates.Add(state);
            }
            state.stateJson = stateData;
            entry.lastPlayedStage = stageIndex;
            Save();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_data, true);
                PlayerPrefs.SetString(PrefsKey, json);
                PlayerPrefs.Save();

                if (!string.IsNullOrEmpty(_filePath))
                {
                    File.WriteAllText(_filePath, json);
                }
                Debug.Log($"[PortfolioSaveService] Saved game progress successfully to disk and PlayerPrefs.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PortfolioSaveService] Error saving progress: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                string json = null;
                if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                {
                    json = File.ReadAllText(_filePath);
                }

                if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey(PrefsKey))
                {
                    json = PlayerPrefs.GetString(PrefsKey);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    _data = JsonUtility.FromJson<PortfolioSaveData>(json) ?? new PortfolioSaveData();
                    Debug.Log($"[PortfolioSaveService] Loaded {_data.games.Count} saved game entries.");
                }
                else
                {
                    _data = new PortfolioSaveData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PortfolioSaveService] Failed to load save file, using fresh state: {ex.Message}");
                _data = new PortfolioSaveData();
            }
        }

        public void ResetAllProgress()
        {
            _data = new PortfolioSaveData();
            PlayerPrefs.DeleteKey(PrefsKey);
            if (File.Exists(_filePath)) File.Delete(_filePath);
            Debug.Log("[PortfolioSaveService] Reset all saved game progress.");
        }
    }
}
