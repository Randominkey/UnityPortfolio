using System.Collections.Generic;
using MasterFramework.Core;
using MasterFramework.Interaction;
using UnityEngine;

namespace Portfolio.Playable.Gonu
{
    public class GonuRuleValidator : IRuleValidator
    {
        public string RuleName => "GonuRuleValidator";

        public enum GonuRuleType
        {
            RuleOne = 0,   // 1주차 3x3 삼목고누
            RuleTwo = 1,   // 2주차 우물고누
            RuleThree = 2, // 3주차 세기/도달고누
            RuleFour = 3   // 4주차 종합 대전고누
        }

        public ValidationResult Validate(IInteractable[] elements, IGridSystem grid)
        {
            // Default element validation (no specific offending element IDs)
            return new ValidationResult
            {
                isCorrect = true,
                feedbackMessage = string.Empty,
                offendingElementIds = new List<string>()
            };
        }

        public bool CheckGameEnd(GonuRuleType rule, List<GonuStateButtonComponent> buttons, int currentPlayerIndex)
        {
            if (buttons == null || buttons.Count == 0) return false;

            switch (rule)
            {
                case GonuRuleType.RuleOne:
                    return CheckRuleOneWin(buttons, currentPlayerIndex);

                case GonuRuleType.RuleTwo:
                    return CheckRuleTwoWin(buttons, currentPlayerIndex);

                case GonuRuleType.RuleThree:
                case GonuRuleType.RuleFour:
                    return CheckRuleThreeWin(buttons, currentPlayerIndex);

                default:
                    return false;
            }
        }

        private bool CheckRuleOneWin(List<GonuStateButtonComponent> buttons, int playerIndex)
        {
            if (buttons.Count < 9) return false;

            int[][] winLines = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 3, 4, 5 },
                new int[] { 6, 7, 8 },
                new int[] { 0, 3, 6 },
                new int[] { 1, 4, 7 },
                new int[] { 2, 5, 8 },
                new int[] { 0, 4, 8 },
                new int[] { 2, 4, 6 }
            };

            foreach (var line in winLines)
            {
                if (buttons[line[0]].state.Value == playerIndex &&
                    buttons[line[1]].state.Value == playerIndex &&
                    buttons[line[2]].state.Value == playerIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckRuleTwoWin(List<GonuStateButtonComponent> buttons, int playerIndex)
        {
            if (buttons == null) return false;
            int opponent = 1 - playerIndex;
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                if (btn.state.Value == -1) // empty slot
                {
                    if (btn.originateButtonComponents != null)
                    {
                        foreach (var neighbor in btn.originateButtonComponents)
                        {
                            if (neighbor != null && neighbor.state.Value == opponent)
                            {
                                return false; // Opponent still has a valid move available
                            }
                        }
                    }
                }
            }
            return true; // Opponent blocked
        }

        private bool CheckRuleThreeWin(List<GonuStateButtonComponent> buttons, int playerIndex)
        {
            int opponent = 1 - playerIndex;
            int enemyCount = 0;

            foreach (var btn in buttons)
            {
                if (btn.state.Value > -1 && btn.state.Value % 2 == opponent)
                {
                    enemyCount++;
                }
            }

            if (enemyCount == 0) return true;

            if (playerIndex == 0 && buttons.Count > 1)
            {
                if (buttons[1].state.Value > -1 && buttons[1].state.Value % 2 == 0)
                    return true;
            }
            else if (playerIndex == 1 && buttons.Count > 0)
            {
                if (buttons[0].state.Value > -1 && buttons[0].state.Value % 2 == 1)
                    return true;
            }

            return false;
        }
    }
}
