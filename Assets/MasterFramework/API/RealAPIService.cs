using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MasterFramework.Core;

namespace MasterFramework.API
{
    public class RealAPIService : IAPIService
    {
        private readonly string _hostApiUrl;
        private readonly string _apiKey;
        private readonly string _authToken;
        private readonly string _audience;

        // Dependencies are injected via Zenject (or constructor) rather than querying GlobalManager.Instance
        public RealAPIService(string hostApiUrl, string apiKey, string authToken, string audience)
        {
            _hostApiUrl = hostApiUrl;
            _apiKey = apiKey;
            _authToken = authToken;
            _audience = audience;
        }

        public async UniTask<LessonData> GetLessonInfoAsync(LessonSessionContext context)
        {
            // GET /buildup/lesson/info?semId=...&topCorsId=...&lessonId=...&comId=...&levelCode=...
            string url = $"{_hostApiUrl}/buildup/lesson/info?semId={context.semId}&topCorsId={context.topCorsId}&lessonId={context.lessonId}&comId={context.comId}&levelCode={context.levelCode}";

            using (var www = UnityWebRequest.Get(url))
            {
                ApplyHeaders(www);
                await www.SendWebRequest().ToUniTask();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    Debug.Log($"[RealAPIService] GetLessonInfo Success: {json}");
                    
                    // In real production, wrap JSON response parsing
                    var response = JsonUtility.FromJson<ResponseLessonDataWrapper>(json);
                    return response?.data;
                }
                else
                {
                    throw new Exception($"[RealAPIService] GetLessonInfo Failed: {www.error} (Code: {www.responseCode})");
                }
            }
        }

        public async UniTask<ResponseSasData> GetSasTokenAsync()
        {
            // GET /buildup/azureConnectInfo
            string url = $"{_hostApiUrl}/buildup/azureConnectInfo";

            using (var www = UnityWebRequest.Get(url))
            {
                ApplyHeaders(www);
                await www.SendWebRequest().ToUniTask();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    return JsonUtility.FromJson<ResponseSasData>(json);
                }
                else
                {
                    throw new Exception($"[RealAPIService] GetSasToken Failed: {www.error}");
                }
            }
        }

        public async UniTask SaveStudyDataAsync(LessonSessionContext context, StudyJsonData studyJsonData, List<ActData> actData)
        {
            // 1. Get SAS token for Azure
            var sasResponse = await GetSasTokenAsync();
            if (sasResponse == null || !sasResponse.result)
            {
                throw new Exception("[RealAPIService] Failed to retrieve SAS token for saving data.");
            }

            // 2. Upload JSON to Azure Blob Storage
            string fileName = $"study_data_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string azureBlobUrl = GetAzureBlobUrl(sasResponse.data, context, fileName);
            
            await UploadTextToAzureBlobAsync(sasResponse.data, context, fileName, studyJsonData.ToJson());

            // 3. Post to /buildup/lesson/info (LESSON_SUBMIT)
            string url = $"{_hostApiUrl}/buildup/lesson/info";
            var submitRequest = new RequestLessonSubmit
            {
                comId = context.comId,
                semId = int.Parse(context.semId),
                topCorsId = int.Parse(context.topCorsId),
                levelCode = context.levelCode,
                lessonId = int.Parse(context.lessonId),
                studyDataUrl = azureBlobUrl,
                actData = actData
            };

            string requestJson = JsonUtility.ToJson(submitRequest);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestJson);

            using (var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyBytes);
                www.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(www);

                await www.SendWebRequest().ToUniTask();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"[RealAPIService] SaveStudyData Submit Failed: {www.error}");
                }
            }
        }

        public async UniTask SaveIntermediateStateAsync(LessonSessionContext context, StudyJsonData studyJsonData, string blobFileName)
        {
            // Upload to Azure without submitting to Creverse REST API DB (e.g. video progress updating)
            var sasResponse = await GetSasTokenAsync();
            if (sasResponse == null || !sasResponse.result)
            {
                throw new Exception("[RealAPIService] Failed to retrieve SAS token.");
            }

            await UploadTextToAzureBlobAsync(sasResponse.data, context, blobFileName, studyJsonData.ToJson());
        }

        public async UniTask CompleteLessonAsync(LessonSessionContext context, string studyDataUrl)
        {
            // POST /buildup/lesson/complete
            string url = $"{_hostApiUrl}/buildup/lesson/complete";
            var completeRequest = new RequestLessonComplete
            {
                comId = context.comId,
                semId = int.Parse(context.semId),
                topCorsId = int.Parse(context.topCorsId),
                levelCode = context.levelCode,
                lessonId = int.Parse(context.lessonId),
                isComplete = true, // complete
                completeDatetime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                studyDataUrl = studyDataUrl
            };

            string requestJson = JsonUtility.ToJson(completeRequest);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestJson);

            using (var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyBytes);
                www.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(www);

                await www.SendWebRequest().ToUniTask();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"[RealAPIService] CompleteLesson Failed: {www.error}");
                }
            }
        }

        #region Helper Methods
        private void ApplyHeaders(UnityWebRequest www)
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Audience", _audience);
            www.SetRequestHeader("X-ApiKey", _apiKey);
            www.SetRequestHeader("X-Auth", _authToken);
        }

        private string GetAzureBlobUrl(SasData sas, LessonSessionContext context, string fileName)
        {
            string azureBase = $"https://ilcdn.blob.core.windows.net/{sas.containerName}";
            if (context.IsTeacher)
            {
                return $"{azureBase}/buildup/t/{context.userId}/{context.semId}/{context.topCorsId}/{context.levelCode}/{context.lessonId}/{fileName}";
            }
            else
            {
                return $"{azureBase}/buildup/s/{context.userId}/{context.semId}/{context.levelCode}/{context.lessonId}/{fileName}";
            }
        }

        private async UniTask UploadTextToAzureBlobAsync(SasData sas, LessonSessionContext context, string fileName, string content)
        {
            string blobPath = context.IsTeacher
                ? $"buildup/t/{context.userId}/{context.semId}/{context.topCorsId}/{context.levelCode}/{context.lessonId}/{fileName}"
                : $"buildup/s/{context.userId}/{context.semId}/{context.levelCode}/{context.lessonId}/{fileName}";

            // Full URL containing SAS token
            string putUrl = $"https://ilcdn.blob.core.windows.net/{sas.containerName}/{blobPath}{sas.sasToken}";

            byte[] bodyBytes = Encoding.UTF8.GetBytes(content);
            using (var www = new UnityWebRequest(putUrl, UnityWebRequest.kHttpVerbPUT))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyBytes);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("x-ms-blob-type", "BlockBlob");
                www.SetRequestHeader("Content-Type", "application/json");

                await www.SendWebRequest().ToUniTask();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"[RealAPIService] Azure Upload Failed: {www.error} (Code: {www.responseCode})");
                }
            }
        }
        #endregion

        #region Inner Request Wrappers
        [Serializable]
        private class ResponseLessonDataWrapper
        {
            public LessonData data;
        }

        [Serializable]
        private class RequestLessonSubmit
        {
            public int comId;
            public int semId;
            public int topCorsId;
            public string levelCode = "";
            public int lessonId;
            public string studyDataUrl = "";
            public List<ActData> actData;
        }

        [Serializable]
        private class RequestLessonComplete
        {
            public int comId;
            public int semId;
            public int topCorsId;
            public string levelCode = "";
            public int lessonId;
            public bool isComplete;
            public string completeDatetime = "";
            public string studyDataUrl = "";
        }
        #endregion
    }
}
