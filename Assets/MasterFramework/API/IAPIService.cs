using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using MasterFramework.Core;

namespace MasterFramework.API
{
    public interface IAPIService
    {
        /// <summary>
        /// Retrieves the lesson progress status, star scores, and Azure storage URL.
        /// </summary>
        UniTask<LessonData> GetLessonInfoAsync(LessonSessionContext context);

        /// <summary>
        /// Retrieves the Azure SAS Token connection information for uploading detailed study states.
        /// </summary>
        UniTask<ResponseSasData> GetSasTokenAsync();

        /// <summary>
        /// Uploads the student's study JSON state to Azure Blob Storage,
        /// then posts the resulting URL and weekly component milestones back to the Creverse REST DB.
        /// </summary>
        UniTask SaveStudyDataAsync(LessonSessionContext context, StudyJsonData studyJsonData, List<ActData> actData);

        /// <summary>
        /// Saves intermediate Azure state without notifying/posting lesson progress to REST DB.
        /// (Equivalent to STUDY_DATA_SAVE and VIDEO_TIME_SAVE).
        /// </summary>
        UniTask SaveIntermediateStateAsync(LessonSessionContext context, StudyJsonData studyJsonData, string blobFileName);

        /// <summary>
        /// Finalizes the lesson by submitting completeDatetime and locking modifications in the REST DB.
        /// </summary>
        UniTask CompleteLessonAsync(LessonSessionContext context, string studyDataUrl);
    }
}
