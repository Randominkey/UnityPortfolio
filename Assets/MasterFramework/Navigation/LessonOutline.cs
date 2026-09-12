using System.Collections.Generic;
using UnityEngine;
using MasterFramework.Core;

namespace MasterFramework.Navigation
{
    [CreateAssetMenu(fileName = "LessonOutline", menuName = "MasterFramework/LessonOutline")]
    public class LessonOutline : ScriptableObject
    {
        public string levelCode;
        public string lessonId;
        public List<PageStepInfo> steps;
    }

    [System.Serializable]
    public struct PageStepInfo
    {
        public int stepIndex;         // 0 to 9 (linear step progress index)
        public string actCode;        // ACT01 ~ ACT05
        public string stepName;       // E.g., "Dig Up 1", "Stack Up 1-2"

        [Header("Dynamic Generation Settings")]
        public bool isDynamicGeneration;             // true 이면 데이터를 기반으로 동적 생성
        public ScriptableObject levelData;            // 동적 생성용 게임별 전용 SO 데이터
        public GameObject stageTemplatePrefab;       // 동적 생성용 게임별 전용 스테이지 템플릿 프리팹 (지정 안 하면 프레임워크 기본값 사용)

        [Header("Legacy Manual Mode")]
        public GameObject stagePrefab;// Prefab containing the StageOrganizer/PageOrganizer
        
        [Header("Video Settings")]
        public bool requiresVideo;    // True if step contains a video checkpoint
        public int videoSlotIndex;    // 0 = First video, 1 = Second video, -1 = None
    }
}

