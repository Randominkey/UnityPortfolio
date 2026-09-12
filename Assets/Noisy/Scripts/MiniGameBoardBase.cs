using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MiniGame
{
    public abstract class MiniGameBoardBase : MonoBehaviour
    {
        public bool IsCorrect { get; protected set; }
        public abstract void Ready();
        public abstract void Refresh();
    }
}