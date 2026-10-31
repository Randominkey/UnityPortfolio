using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Lighthouse
{
    public class LighthouseBoard : MiniGameBoardBase
    {
        [Header("Grid References")]
        [SerializeField] private GridLayoutGroup cellParent;
        [SerializeField] private LighthouseCell cellPrefab;

        [Header("Level Configuration & Target Goal")]
        [SerializeField] private LighthouseLevelData levelData;
        [SerializeField] private int targetLighthouseCount = 6;
        [Tooltip("/: blank, 0~9: number cell")]
        [SerializeField] private string[] map;

        [Header("Dynamic Goal UI Counter")]
        [Tooltip("화면상에 현재 배치된 등대 수와 목표치를 표시하는 UI Text")]
        [SerializeField] private Text lighthouseCounterText;

        private LighthouseCell[,] cells;
        private int rowCount;
        private int columnCount;
        private readonly CompositeDisposable _cellDisposables = new CompositeDisposable();

        private static readonly Color32 ColorMatch = new Color32(78, 206, 129, 255);   // Correct green
        private static readonly Color32 ColorExceed = new Color32(255, 108, 134, 255); // Error red
        private static readonly Color32 ColorNormal = new Color32(226, 232, 240, 255); // White/Slate

        public int TargetLighthouseCount => targetLighthouseCount;
        public int CurrentLighthouseCount => GetCurrentLighthouseCount();

        public void SetLevelData(LighthouseLevelData data)
        {
            if (data == null) return;
            levelData = data;
            map = data.Map;
            targetLighthouseCount = data.TargetLighthouseCount;
        }

        public override void Ready()
        {
            _cellDisposables.Clear();

            // ScriptableObject 주입 데이터 우선 적용
            if (levelData != null)
            {
                map = levelData.Map;
                targetLighthouseCount = levelData.TargetLighthouseCount;
            }

            if (map == null || map.Length == 0)
            {
                Debug.LogError("[LighthouseBoard] Board map is null or empty!");
                return;
            }

            rowCount = map.Length;
            columnCount = map[0].Length;

            // Clear previous cells if any
            if (cellParent != null)
            {
                for (int i = cellParent.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(cellParent.transform.GetChild(i).gameObject);
                }
                cellParent.constraintCount = columnCount;
            }

            cells = new LighthouseCell[rowCount, columnCount];

            // 1. 셀 인스턴스화 및 데이터 초기화
            for (int i = 0; i < rowCount; i++)
            {
                int row = i;
                for (int j = 0; j < columnCount; j++)
                {
                    int col = j;
                    cells[row, col] = Instantiate(cellPrefab, cellParent != null ? cellParent.transform : transform);

                    string mapKey = map[row][col].ToString();
                    if (mapKey == "/")
                    {
                        cells[row, col].Ready(-1);
                    }
                    else
                    {
                        cells[row, col].Ready(int.Parse(mapKey));
                    }
                }
            }

            // 2. 숫자 셀의 감시 타겟(행/열 셀들) 연결
            for (int i = 0; i < rowCount; i++)
            {
                int row = i;
                for (int j = 0; j < columnCount; j++)
                {
                    int col = j;
                    if (cells[row, col].cellType.Value == CellType.Number)
                    {
                        List<LighthouseCell> observingTargets = new List<LighthouseCell>();
                        for (int i0 = 0; i0 < rowCount; i0++)
                        {
                            if (i0 != row)
                            {
                                observingTargets.Add(cells[i0, col]);
                            }
                        }
                        for (int j0 = 0; j0 < columnCount; j0++)
                        {
                            if (j0 != col)
                            {
                                observingTargets.Add(cells[row, j0]);
                            }
                        }
                        cells[row, col].SetObservingTargetCellForNumberCell(observingTargets);
                    }
                }
            }

            // 3. 셀 클릭 및 상태 변경 시 실시간 정답 판정 & 등대 카운터 갱신
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = cells[i, j];
                    cell.button.OnClickAsObservable()
                        .Subscribe(_ =>
                        {
                            EvaluateBoard();
                        })
                        .AddTo(_cellDisposables);
                }
            }

            // 최초 UI 및 정답 상태 평가
            EvaluateBoard();
        }

        private void EvaluateBoard()
        {
            Check();
            UpdateLighthouseCounterUI();
            NotifyBoardEvaluated();
        }

        private void Check()
        {
            int currentLighthouses = GetCurrentLighthouseCount();

            // 1. 숫자 셀 조건 만족 검사
            bool allNumberCellsCorrect = true;
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (cells[i, j].cellState.Value != CellState.Correct)
                    {
                        allNumberCellsCorrect = false;
                        break;
                    }
                }
                if (!allNumberCellsCorrect) break;
            }

            // 2. [핵심] 전체 설치된 등대 개수 조건 검사
            bool countMatches = (currentLighthouses == targetLighthouseCount);

            // 두 조건이 모두 일치해야만 최종 정답 인정
            IsCorrect = allNumberCellsCorrect && countMatches;
        }

        public int GetCurrentLighthouseCount()
        {
            if (cells == null) return 0;
            int count = 0;
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (cells[i, j] != null && cells[i, j].cellType.Value == CellType.O)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private void UpdateLighthouseCounterUI()
        {
            if (lighthouseCounterText == null) return;

            int current = GetCurrentLighthouseCount();
            lighthouseCounterText.text = $"등대: {current} / {targetLighthouseCount}개";

            if (current == targetLighthouseCount)
            {
                lighthouseCounterText.color = ColorMatch; // 목표 수량 정확히 일치: 밝은 초록색
            }
            else if (current > targetLighthouseCount)
            {
                lighthouseCounterText.color = ColorExceed; // 목표 수량 초과: 경고 빨간색
            }
            else
            {
                lighthouseCounterText.color = ColorNormal; // 미달: 기본 텍스트 색상
            }
        }

        public override void Refresh()
        {
            if (cells == null) return;

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (cells[i, j] != null)
                    {
                        cells[i, j].Refresh();
                    }
                }
            }

            EvaluateBoard();
        }

        private void OnDestroy()
        {
            _cellDisposables.Dispose();
        }
    }
}