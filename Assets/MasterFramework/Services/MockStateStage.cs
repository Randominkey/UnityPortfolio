using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using MasterFramework.Core;
using System;

namespace MasterFramework.Services
{
    public class MockStateStage : MonoBehaviour, IStageOrganizer, IStateRestorable
    {
        public int StageIndex { get; set; }
        
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;
        private readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);

        // Dummy operational state values to be serialized
        [Header("Mock Operational State")]
        [SerializeField] private int testNumberValue = 0;
        [SerializeField] private string testStringValue = "Default";

        public int TestNumberValue
        {
            get => testNumberValue;
            set => testNumberValue = value;
        }

        public string TestStringValue
        {
            get => testStringValue;
            set => testStringValue = value;
        }

        public virtual async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;
            Debug.Log($"[MockStateStage] Initialized stage {index}");
            await UniTask.CompletedTask;
        }

        public void SetStageData(ScriptableObject data)
        {
            // Dummy implementation
        }

        public virtual async UniTask EnterStageAsync()
        {
            Debug.Log($"[MockStateStage] Entered stage {StageIndex}. Current TestNumberValue: {TestNumberValue}, TestStringValue: {TestStringValue}");
            await UniTask.CompletedTask;
        }

        public virtual async UniTask ExitStageAsync()
        {
            Debug.Log($"[MockStateStage] Exiting stage {StageIndex}");
            await UniTask.CompletedTask;
        }

        public void RefreshStage()
        {
            TestNumberValue = 0;
            TestStringValue = "Default";
            _isStageCleared.Value = false;
            Debug.Log($"[MockStateStage] Refreshed stage {StageIndex}");
        }

        // Memento Pattern State Serialization
        public string SerializeState()
        {
            var state = new MockStateData { number = TestNumberValue, text = TestStringValue };
            string json = JsonUtility.ToJson(state);
            Debug.Log($"[MockStateStage] State Serialized for stage {StageIndex}: {json}");
            return json;
        }

        public void DeserializeAndRestoreState(string stateData)
        {
            if (string.IsNullOrEmpty(stateData)) return;

            try
            {
                var state = JsonUtility.FromJson<MockStateData>(stateData);
                TestNumberValue = state.number;
                TestStringValue = state.text;
                Debug.Log($"[MockStateStage] State Restored for stage {StageIndex}: number={TestNumberValue}, text={TestStringValue}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MockStateStage] Failed to deserialize state: {ex.Message}");
            }
        }

        [Serializable]
        private class MockStateData
        {
            public int number;
            public string text;
        }
    }
}
