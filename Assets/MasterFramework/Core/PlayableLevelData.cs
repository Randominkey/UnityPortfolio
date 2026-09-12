using UnityEngine;
using System.Collections.Generic;

namespace MasterFramework.Core
{
    public enum RuleType
    {
        RectSquareDivision, // 직사각형/정사각형 분할 규칙 (Month 10 High 등)
        LineSymmetry,       // 선대칭
        PointSymmetry,      // 점대칭
        SummationCheck,     // 수학 영역 합계
        GridConnectivity    // 연결성
    }

    [System.Serializable]
    public class PresetFrameStartEndIndices
    {
        public Vector2Int startIdices;
        public Vector2Int endIdices;
    }

    [CreateAssetMenu(fileName = "PlayableLevelData", menuName = "MasterFramework/PlayableLevelData")]
    public class PlayableLevelData : ScriptableObject
    {
        [Header("Grid Size")]
        public int rows;
        public int cols;

        [Header("Grid Layout Data")]
        [Tooltip("행 개수와 크기가 일치해야 합니다. 각 행은 열 개수의 기호 텍스트/숫자로 이루어집니다.")]
        public List<string> signMap;

        [Header("Preset Frame Setup")]
        public PresetFrameStartEndIndices[] presetFrames;

        [Header("Validation Setup")]
        public RuleType validationRule;

        [Header("UI Visual Customization")]
        public float cellSpacing = 5f;
    }
}
