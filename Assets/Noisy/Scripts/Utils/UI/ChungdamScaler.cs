using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CMS.Util.Extend;
using CMS.Util.ScreenInfo;
using UniRx;

namespace CMS.Util.UI
{
    public class ChungdamScaler : MonoBehaviour
    {
        [field: SerializeField] public bool IsChangedScale { get; private set; }
        [SerializeField] private int insideChungdamUICount = 0;
        [SerializeField] private Vector3 customScale = new Vector3(0.704f, 0.893f, 1);
        [SerializeField] public Camera sceneCam;

        public static Vector3 CurrentMousePosition 
        {
            get 
            {
                if (Display.displays.Length > 1)
                    return Display.RelativeMouseAt(Input.mousePosition);
                else
                    return Input.touchCount > 0 ? new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y) : Input.mousePosition;
            }
        }

        void Start()
        {
            // 포트폴리오용 단일 씬 구동을 위해 기본 스케일 변경 활성화
            IsChangedScale = true;
            UpdateScale();
        }

        private void Update()
        {
            ScreenUtils.IsOverlayChungdamUI = IsChangedScale;
            ScreenUtils.CurrentSceneCamera = sceneCam;
        }

        private void UpdateScale()
        {
            // 독립형 포트폴리오에서는 항상 전체 화면 비율 및 단일 디스플레이 0번을 타겟팅합니다.
            // 기존 레거시 플랫폼 클래스(InterfaceForNoisy) 의존성을 완전 격리하여 에디터 및 빌드 컴파일을 보장합니다.
            if (sceneCam != null)
            {
                sceneCam.rect = new Rect(0, 0, 1, 1);
                sceneCam.depth = 50;
                sceneCam.targetDisplay = 0;
            }
        }
    }
}