using System;
using UnityEngine;

namespace MasterFramework.Playable.Month34High
{
    [CreateAssetMenu(fileName = "NumberIslandLevelData", menuName = "MasterFramework/Playable/Month34High/LevelData")]
    public class NumberIslandLevelData : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int rowCount = 8;
        [SerializeField] private int columnCount = 8;

        [Header("Hints (X: Row, Y: Column, Z: Target Land Area Number)")]
        [SerializeField] private Vector3Int[] hints;

        public int RowCount => rowCount;
        public int ColumnCount => columnCount;
        public Vector3Int[] Hints => hints;

        public void SetData(int rows, int cols, Vector3Int[] hintData)
        {
            rowCount = rows;
            columnCount = cols;
            hints = hintData;
        }
    }
}
