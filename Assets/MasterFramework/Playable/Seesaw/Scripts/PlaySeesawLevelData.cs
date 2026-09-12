using UnityEngine;

namespace Portfolio.Playable.Seesaw
{
    [CreateAssetMenu(fileName = "PlaySeesawLevelData", menuName = "MasterFramework/Playable/Seesaw/PlaySeesawLevelData")]
    public class PlaySeesawLevelData : ScriptableObject
    {
        public int cellSideLength = 116;
        [Header("x: blank / 4: lever")]
        public string[] map;
        public string rowNumbers;
        public string columnNumbers;
        public string[] frameMap;
        [Header("Auto-Solve Solution Preset (AI / Ground Truth)")]
        public string[] solutionGrid;
    }
}
