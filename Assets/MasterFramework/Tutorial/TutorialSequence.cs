using System;
using System.Collections.Generic;
using UnityEngine;

namespace MasterFramework.Tutorial
{
    public enum TutorialActionType
    {
        ShowDialogue,       // 가이드 텍스트 & 말풍선 노출
        HighlightUI,        // 특정 UI/오브젝트 강조
        AnimateHandGuide,   // 드래그 가이드 손가락 애니메이션
        PlayTweenEffect,    // 씬 내 특정 트윈 연출 실행
        LockInteractions,   // 특정 타겟 제외 조작 비활성화
        WaitUserInteraction // 유저가 특정 동작(이벤트 발행)을 완료할 때까지 대기
    }

    public enum TweenPropertyType
    {
        Position,
        Scale,
        Rotation,
        Alpha,
        FillAmount
    }

    public enum EaseType
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic
    }

    [Serializable]
    public struct TweenAnimationData
    {
        public string targetId;                // 연출 대상 식별자
        public TweenPropertyType propertyType; // Position, Scale, Rotation, Alpha, FillAmount
        public Vector3 targetValue;            // 이동/크기 등 목적지 값
        public float targetFloat;              // 알파, Fill Amount 등 단일 수치
        public float duration;                 // 재생 시간
        public float delay;                    // 대기 시간
        public EaseType ease;                  // 이징 스타일
    }

    [CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "MasterFramework/Tutorial/Sequence")]
    public class TutorialSequence : ScriptableObject
    {
        public List<TutorialStep> steps;
    }

    [Serializable]
    public class TutorialStep
    {
        [Header("Step Info")]
        public string stepId;
        [TextArea(3, 5)]
        public string dialogueText;                  // 가이드 말풍선 텍스트
        
        [Header("Visual Effects & Locks")]
        public string targetHighlightId;             // 돋보이게 만들 포커스 오브젝트 ID
        public bool lockAllOtherInteractions;        // 지정된 타겟 외 조작 잠금 여부
        
        [Header("DOTween Actions to Play")]
        public List<TweenAnimationData> animations;  // 자동 재생될 애니메이션 리스트
        
        [Header("Completion Condition")]
        public TutorialActionType endTriggerType;    // 클릭 대기 vs 특정 이벤트 대기 분기
        public string expectedEventPieceId;          // 대기할 이벤트 피스 ID (조건부)
    }
}
