using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MasterFramework.Core
{
    /// <summary>
    /// 모든 퍼즐의 셀(Cell) 및 인터랙티브 타일 조작 시 왁뿌볼/뽁뽁이 터지는 ASMR 사운드 피드백을
    /// 기존 게임 코드 수정 없이 전역에서 100% 자동 후킹하여 재생하는 프레임워크 오디오 브릿지.
    /// </summary>
    public class CellInteractionAudioBridge : MonoBehaviour
    {
        private static CellInteractionAudioBridge _instance;

        private GameObject _lastHoveredObject;
        private float _lastHoverTime;
        private Vector2 _lastMousePos;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        private const float HoverCooldown = 0.035f; // 급격한 드래그 시 사운드 중첩 방지 (35ms)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("[CellInteractionAudioBridge]", typeof(CellInteractionAudioBridge));
                if (Application.isPlaying) DontDestroyOnLoad(go);
                _instance = go.GetComponent<CellInteractionAudioBridge>();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (EventSystem.current == null) return;

            Vector2 mousePos = GetCurrentPointerPosition();
            bool isMouseMoved = Vector2.Distance(mousePos, _lastMousePos) > 1.0f;
            bool isLeftMouseDown = WasLeftPointerPressed();

            // 1. 마우스 호버 감지 (마우스 이동 시에만 가벼운 UI 레이캐스트 수행)
            if (isMouseMoved)
            {
                _lastMousePos = mousePos;
                GameObject hoveredTarget = RaycastInteractiveTarget(mousePos);

                if (hoveredTarget != null && hoveredTarget != _lastHoveredObject)
                {
                    if (Time.unscaledTime - _lastHoverTime >= HoverCooldown)
                    {
                        PlayCellHoverSound();
                        _lastHoverTime = Time.unscaledTime;
                    }
                    _lastHoveredObject = hoveredTarget;
                }
                else if (hoveredTarget == null)
                {
                    _lastHoveredObject = null;
                }
            }

            // 2. 마우스 클릭(다운) 감지
            if (isLeftMouseDown)
            {
                GameObject clickedTarget = RaycastInteractiveTarget(mousePos);
                if (clickedTarget != null)
                {
                    PlayCellPopSound();
                }
            }
        }

        private Vector2 GetCurrentPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private bool WasLeftPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return Input.GetMouseButtonDown(0);
        }

        private GameObject RaycastInteractiveTarget(Vector2 screenPosition)
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            for (int i = 0; i < _raycastResults.Count; i++)
            {
                var hitGo = _raycastResults[i].gameObject;
                if (hitGo == null) continue;

                if (IsInteractiveCellTarget(hitGo, out GameObject resolvedTarget))
                {
                    return resolvedTarget;
                }
            }

            return null;
        }

        /// <summary>
        /// 해당 오브젝트가 퍼즐 셀(Cell) 또는 상호작용 인터랙티브 타일인지 판별
        /// </summary>
        private bool IsInteractiveCellTarget(GameObject go, out GameObject resolvedTarget)
        {
            resolvedTarget = null;
            if (go == null) return false;

            // 상단 헤더 HUD 버튼(네비게이션/종료/음소거)은 전역 기본 클릭음이 있으므로 제외
            string goName = go.name.ToLower();
            if (goName.Contains("back") || goName.Contains("mute") || goName.Contains("github") || goName.Contains("overview"))
            {
                return false;
            }

            Transform curr = go.transform;
            while (curr != null)
            {
                string currName = curr.name.ToLower();

                // 1. 이름 기반 판별 (cell, tile, button, slot, block, peg 등)
                if (currName.Contains("cell") || currName.Contains("tile") || currName.Contains("block") ||
                    currName.Contains("slot") || currName.Contains("peg") || currName.Contains("statebutton") ||
                    currName.Contains("keypad") || currName.Contains("toggle"))
                {
                    resolvedTarget = curr.gameObject;
                    return true;
                }

                // 2. Selectable (Button / Toggle) 컴포넌트 보유 확인
                if (curr.GetComponent<Selectable>() != null)
                {
                    resolvedTarget = curr.gameObject;
                    return true;
                }

                // 부모 탐색 깊이 제한 (최대 3단계 상위까지)
                if (curr.parent != null && curr.parent.name.ToLower().Contains("canvas")) break;
                curr = curr.parent;
            }

            return false;
        }

        private void PlayCellHoverSound()
        {
            if (AudioService.Instance == null) return;
            // 미세 피치 무작위성 (0.95f ~ 1.05f)으로 아날로그 왁뿌볼 스침 질감 연출
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            AudioService.Instance.PlaySfx(SfxType.CellHover, 0.75f, pitch);
        }

        private void PlayCellPopSound()
        {
            if (AudioService.Instance == null) return;
            // 쫀득한 왁뿌볼/에어캡 파열음 피치 변조 (0.93f ~ 1.07f)
            float pitch = UnityEngine.Random.Range(0.93f, 1.07f);
            AudioService.Instance.PlaySfx(SfxType.CellPop, 1.15f, pitch);
        }
    }
}
