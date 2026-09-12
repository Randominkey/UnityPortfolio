using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using MasterFramework.Core;

namespace MasterFramework.API
{
    public class MockAPIService : IAPIService
    {
        private const string LocalStorageKeyPrefix = "MockAPI_StudyData_";
        private const int NetworkSimulatedDelayMs = 250;

        private LessonData _cachedLessonData;
        private StudyJsonData _cachedStudyJsonData;

        public async UniTask<LessonData> GetLessonInfoAsync(LessonSessionContext context)
        {
            // Simulate network latency
            await UniTask.Delay(NetworkSimulatedDelayMs);

            string key = LocalStorageKeyPrefix + context.lessonId;
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                _cachedStudyJsonData = StudyJsonData.FromJson(json);
                Debug.Log($"[MockAPIService] Loaded cached local study state for lesson {context.lessonId}");
            }
            else
            {
                _cachedStudyJsonData = new StudyJsonData();
                Debug.Log($"[MockAPIService] No cached local study state found. Initialized fresh state.");
            }

            _cachedLessonData = new LessonData
            {
                contentUrl = $"/LCMSFiles/PackingFiles/WHY{context.levelCode}_Lesson{context.lessonId}.dat",
                studyStatus = 1,
                isComplete = false,
                courseLevelName = context.levelCode,
                weekSeq = 1
            };
            
            _cachedLessonData.comp.comId = context.comId.ToString();
            _cachedLessonData.comp.studyDataUrl = $"local://mock_storage/buildup/s/{context.userId}/{context.lessonId}/study_data_local.json";

            // Map cached activities to REST metadata
            _cachedLessonData.comp.actData = new List<ActData>();
            for (int i = 0; i < 5; i++)
            {
                _cachedLessonData.comp.actData.Add(new ActData
                {
                    actCode = $"ACT0{i + 1}",
                    completeDatetime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isComplete = i < _cachedStudyJsonData.study_step_idx,
                    starPoint = i < _cachedStudyJsonData.activities.Count ? _cachedStudyJsonData.activities[i].star_point : 0
                });
            }

            return _cachedLessonData;
        }

        public async UniTask<ResponseSasData> GetSasTokenAsync()
        {
            await UniTask.Delay(NetworkSimulatedDelayMs);
            return new ResponseSasData
            {
                result = true,
                cd = "200",
                message = "Success",
                data = new SasData
                {
                    azureUrl = "local://mock_azure",
                    containerName = "mock-container",
                    sasToken = "mock_sas_token_xyz"
                }
            };
        }

        public async UniTask SaveStudyDataAsync(LessonSessionContext context, StudyJsonData studyJsonData, List<ActData> actData)
        {
            await UniTask.Delay(NetworkSimulatedDelayMs);

            _cachedStudyJsonData = studyJsonData;
            string key = LocalStorageKeyPrefix + context.lessonId;
            PlayerPrefs.SetString(key, studyJsonData.ToJson());
            PlayerPrefs.Save();

            if (_cachedLessonData != null)
            {
                _cachedLessonData.comp.actData = actData;
                _cachedLessonData.comp.studyDataUrl = $"local://mock_storage/buildup/s/{context.userId}/{context.lessonId}/study_data_saved.json";
            }

            Debug.Log($"[MockAPIService] Successfully uploaded to local mock storage and updated local REST DB. URL: local://study_data_saved.json");
        }

        public async UniTask SaveIntermediateStateAsync(LessonSessionContext context, StudyJsonData studyJsonData, string blobFileName)
        {
            await UniTask.Delay(NetworkSimulatedDelayMs);

            _cachedStudyJsonData = studyJsonData;
            string key = LocalStorageKeyPrefix + context.lessonId;
            PlayerPrefs.SetString(key, studyJsonData.ToJson());
            PlayerPrefs.Save();

            Debug.Log($"[MockAPIService] Saved intermediate progress locally to {blobFileName}.");
        }

        public async UniTask CompleteLessonAsync(LessonSessionContext context, string studyDataUrl)
        {
            await UniTask.Delay(NetworkSimulatedDelayMs);

            if (_cachedLessonData != null)
            {
                _cachedLessonData.isComplete = true;
                _cachedLessonData.completeDatetime = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }

            Debug.Log($"[MockAPIService] Finalized lesson {context.lessonId} in local mock DB.");
        }
        
        /// <summary>
        /// Debug cheat helper: Clears cached local storage progress for this lesson.
        /// </summary>
        public void ClearLocalCache(string lessonId)
        {
            string key = LocalStorageKeyPrefix + lessonId;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                Debug.Log($"[MockAPIService] Cleared local cache for lesson {lessonId}");
            }
        }
    }
}
