using UnityEngine;
using UnityEngine.UI;

namespace CMS.Util.MoveNRotate
{
    public class SelectedPieceController
    {
        public GameObject SelectedPieceRootObject { get; set; }
        public GameObject arrowObject { get; set; }
        public Transform Parent { get; set; }
        public RectTransform ParentRectTransform { get; set; }
        
        public RectTransform ImageRect { get; set; }
        public Image ArrowImage { get; set; }
        public string Name { get; set; }
        public Button RotationSelectButton { get; set; }
        public RectTransform selectedRootRect { get; set; }

        public Rigidbody2D myRigidBody { get; set; }

    }
}