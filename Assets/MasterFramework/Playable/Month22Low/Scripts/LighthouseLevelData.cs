using System;
using UnityEngine;

namespace MiniGame.Lighthouse
{
    [CreateAssetMenu(fileName = "LighthouseLevelData", menuName = "MasterFramework/Playable/Month22Low/LevelData")]
    public class LighthouseLevelData : ScriptableObject
    {
        [Header("Level Information")]
        [SerializeField] private int levelIndex = 1;
        [SerializeField] private int rowCount = 5;
        [SerializeField] private int columnCount = 5;

        [Header("Target Lighthouse Count (화면 UI 및 정답 검증용 목표 수치)")]
        [Tooltip("플레이어가 격자판에 설치해야 하는 총 등대(O) 개수")]
        [SerializeField] private int targetLighthouseCount = 6;

        [Header("Board Map ('/': 빈칸, '0'~'9': 숫자 셀)")]
        [SerializeField] private string[] map;

        public int LevelIndex => levelIndex;
        public int RowCount => rowCount;
        public int ColumnCount => columnCount;
        public int TargetLighthouseCount => targetLighthouseCount;
        public string[] Map => map;

        public void SetData(int level, int rows, int cols, int targetCount, string[] boardMap)
        {
            levelIndex = level;
            rowCount = rows;
            columnCount = cols;
            targetLighthouseCount = targetCount;
            map = boardMap;
        }
    }
}
