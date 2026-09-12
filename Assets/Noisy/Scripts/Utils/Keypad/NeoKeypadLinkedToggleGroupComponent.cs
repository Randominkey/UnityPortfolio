using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace CMS.Template.UI.Keypad
{
    [RequireComponent(typeof(ToggleGroup))]
    public class NeoKeypadLinkedToggleGroupComponent : MonoBehaviour
    {
        [SerializeField] private List<NeoKeypadLinkedToggleComponent> neoKeypadLinkedToggleComponents = new List<NeoKeypadLinkedToggleComponent>();

        public void AddNeoKeypadLinkedToggle(NeoKeypadLinkedToggleComponent addTarget)
        {
            for (int i = neoKeypadLinkedToggleComponents.Count - 1; i > -1; i--)
            {
                if (neoKeypadLinkedToggleComponents[i] == null)
                {
                    neoKeypadLinkedToggleComponents.RemoveAt(i);
                }
            }

            if (!neoKeypadLinkedToggleComponents.Contains(addTarget))
            {
                neoKeypadLinkedToggleComponents.Add(addTarget);

                neoKeypadLinkedToggleComponents.Sort(delegate (NeoKeypadLinkedToggleComponent x, NeoKeypadLinkedToggleComponent y)
                {
                    if (x.name == null && y.name == null) return 0;
                    else if (x.name == null) return -1;
                    else if (y.name == null) return 1;
                    else return x.name.CompareTo(y.name);
                });
            }
        }

        public void PrevOrNext(KeypadButtonType keypadButtonType)
        {

            int isActiveCount = 0;

            foreach (var neoKeypadLinkedToggleComponent in neoKeypadLinkedToggleComponents)
            {
                if (neoKeypadLinkedToggleComponent.isActiveAndEnabled)
                {
                    isActiveCount++;
                }
            }

            if (isActiveCount > 1)
            {
                Toggle selectedToggle = GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault();

                if (selectedToggle)
                {
                    NeoKeypadLinkedToggleComponent selectedNKLT = selectedToggle.GetComponent<NeoKeypadLinkedToggleComponent>();

                    int index = neoKeypadLinkedToggleComponents.IndexOf(selectedNKLT);

                    int checkingIndex = index;

                    switch (keypadButtonType)
                    {
                        case KeypadButtonType.Previous:
                            while (checkingIndex > -1)
                            {
                                checkingIndex--;

                                if (checkingIndex < 0)
                                    checkingIndex = neoKeypadLinkedToggleComponents.Count;
                                else if (neoKeypadLinkedToggleComponents[checkingIndex].isActiveAndEnabled)
                                    break;
                            }
                            break;

                        case KeypadButtonType.Next:
                            while (checkingIndex < neoKeypadLinkedToggleComponents.Count)
                            {
                                checkingIndex++;

                                if (checkingIndex > neoKeypadLinkedToggleComponents.Count - 1)
                                    checkingIndex = -1;
                                else if (neoKeypadLinkedToggleComponents[checkingIndex].isActiveAndEnabled)
                                    break;

                            }
                            break;

                        default:
                            break;
                    }

                    neoKeypadLinkedToggleComponents[checkingIndex].Toggle.isOn = true;
                }
            }

        }
    }
}