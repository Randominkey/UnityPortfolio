using UnityEngine;

namespace MasterFramework.Navigation
{
    [CreateAssetMenu(fileName = "GameOutline_", menuName = "MasterFramework/Portfolio/GameOutline")]
    public class PortfolioGameOutline : ScriptableObject
    {
        public string gameId;
        public string gameTitle;
        public string subtitle; // e.g., "Play Seesaw"
        [TextArea(2, 4)]
        public string systemSummary; // e.g., "지레 평형 공식(거리 × 무게) 및 격자 제약 조건 검증"
        public string description { get => systemSummary; set => systemSummary = value; }

        public string[] featurePoints; // 2 bullet points of engineering/gameplay focus
        public string[] techTags { get => featurePoints; set => featurePoints = value; }

        public string generationTag;   // e.g., "Gen 2 Minigame", "Gen 3", "Why 02"
        public bool hasAutoSolve = true;
        public int totalStages = 4;
        public Sprite thumbnail;
        public LessonOutline lessonOutline; // 진입 시 로드할 레벨 아웃라인
    }
}
