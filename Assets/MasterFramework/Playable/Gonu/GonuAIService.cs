using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MasterFramework.Core;

namespace Portfolio.Playable.Gonu
{
    public class GonuAIService
    {
        private readonly ISoundService _soundService;

        public GonuAIService(ISoundService soundService = null)
        {
            _soundService = soundService;
        }

        public IEnumerator ExecuteAITurnCoroutine(
            List<GonuStateButtonComponent> buttons,
            System.Action onAIWin,
            System.Action onTurnHandover)
        {
            yield return new WaitForSeconds(0.8f);

            if (buttons == null || buttons.Count == 0)
            {
                onTurnHandover?.Invoke();
                yield break;
            }

            // 1. Collect all strictly legal moves for AI pieces (state == 1) to valid destination slots (state == -1)
            // In Gonu architecture, toBtn.originateButtonComponents defines all nodes that can legally move INTO toBtn!
            List<(int fromIdx, int toIdx)> validMoves = new List<(int fromIdx, int toIdx)>();

            for (int i = 0; i < buttons.Count; i++)
            {
                var toBtn = buttons[i];
                if (toBtn == null || toBtn.state.Value != -1) continue; // Target must be EMPTY

                if (toBtn.originateButtonComponents != null)
                {
                    foreach (var fromBtn in toBtn.originateButtonComponents)
                    {
                        if (fromBtn != null && fromBtn.state.Value == 1) // AI piece at origin
                        {
                            validMoves.Add((fromBtn.index, toBtn.index));
                        }
                    }
                }
            }

            // 2. If AI has 0 valid moves, AI is trapped (Player wins)
            if (validMoves.Count == 0)
            {
                Debug.Log("[GonuAIService] AI has no valid moves remaining (Blocked).");
                onAIWin?.Invoke();
                yield break;
            }

            // 3. Smart AI Heuristics:
            // Prioritize key strategic nodes (e.g. Node 5 = center, Node 7 = lower choke point, Node 3 = upper choke point)
            var strategicMoves = validMoves.Where(m => m.toIdx == 5 || m.toIdx == 7 || m.toIdx == 3).ToList();
            var chosenMove = (strategicMoves.Count > 0 && Random.value < 0.75f)
                ? strategicMoves[Random.Range(0, strategicMoves.Count)]
                : validMoves[Random.Range(0, validMoves.Count)];

            // 4. Visual Selection Highlight & Execution
            GonuStateButtonComponent fromComponent = buttons.Find(b => b.index == chosenMove.fromIdx) ?? buttons[chosenMove.fromIdx];
            GonuStateButtonComponent toComponent = buttons.Find(b => b.index == chosenMove.toIdx) ?? buttons[chosenMove.toIdx];

            if (fromComponent != null)
            {
                fromComponent.isSelected.Value = true;
                _soundService?.PlaySFX("Click");
            }

            yield return new WaitForSeconds(0.4f);

            if (fromComponent != null && toComponent != null)
            {
                toComponent.state.Value = 1;
                fromComponent.isSelected.Value = false;
                fromComponent.state.Value = -1;
                _soundService?.PlaySFX("Click");
            }

            onTurnHandover?.Invoke();
        }
    }
}
