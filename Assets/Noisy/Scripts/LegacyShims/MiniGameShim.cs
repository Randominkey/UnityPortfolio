using System;
using UnityEngine;

namespace MiniGame.MineSweeper
{
    public class MineSweeperBoardComponent : MonoBehaviour
    {
        public virtual void Ready() { }
        public virtual void Refresh() { }
        public virtual bool CheckTheAnswer() => true;
    }
}
