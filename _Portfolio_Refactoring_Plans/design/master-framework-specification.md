# Master Framework Design Specification

이 문서는 3개 세대의 교육용 수학/도형 퍼즐 아키텍처를 통합하기 위한 **마스터 프레임워크(Master Framework)**의 핵심 컴포넌트 명세서이자 설계 규약서입니다.

---

## 1. 아키텍처 아웃라인 & UI 구조 (Gen 3 스타일 계승)

마스터 프레임워크는 **상단 HUD 내비게이션 프레임(Frame)**과 **개별 씬 콘텐츠 영역(Content Stage Area)**을 완벽히 분리한 **Gen 3 데코레이터 스타일의 분리형 UI 구조**를 표준으로 채택합니다.

```
┌────────────────────────────────────────────────────────┐
│               Universal HUD Frame UI                   │
│  [이전 버튼]  [ 1 ] [ 2 ] [ 3 ] [ 4 ] [ 5 ]  [다음 버튼] │
├────────────────────────────────────────────────────────┤
│                                                        │
│               Dynamic Content Area                     │
│               (Dynamically Loaded Prefab)              │
│                                                        │
│               [ Center - Bottom Stage ]                │
│                                                        │
└────────────────────────────────────────────────────────┘
```

*   **프레임워크 책임**: 웹/로컬 통신 데이터 싱크(`IAPIService`), 상단 HUD의 페이지 버튼 클릭 및 상태 갱신, 씬 전환 시의 비동기 전환 효과(Fade In/Out).
*   **콘텐츠 책임**: 스테이지별 퍼즐 퀴즈 연출, 사용자 입력 및 드래그, 퀴즈 정답 검증 로직 실행.

---

## 2. API & Mocking 설계 명세 (API & Mocking Layer)

실 서버 통신 및 로컬 오프라인 테스팅용 에뮬레이션을 통일된 인터페이스로 스왑하는 구조입니다.

### A. 핵심 인터페이스 및 데이터 모델
*   **[LessonSessionContext.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Core/LessonSessionContext.cs)**: 세션에 필요한 학기, 레벨, 사용자 ID 정보를 캡슐화하여 static 글로벌 참조를 제거합니다.
*   **[IAPIService.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/API/IAPIService.cs)**:
    ```csharp
    public interface IAPIService
    {
        UniTask<LessonData> GetLessonInfoAsync(LessonSessionContext context);
        UniTask<ResponseSasData> GetSasTokenAsync();
        UniTask SaveStudyDataAsync(LessonSessionContext context, StudyJsonData studyJsonData, List<ActData> actData);
        UniTask SaveIntermediateStateAsync(LessonSessionContext context, StudyJsonData studyJsonData, string blobFileName);
        UniTask CompleteLessonAsync(LessonSessionContext context, string studyDataUrl);
    }
    ```

### B. Zenject 주입 기법 (DI Binding Strategy)
빌드 구성에 따라 필요한 서비스 모델을 런타임에 바인딩하여 클라이언트 코드가 특정 구현체에 의존하지 않게 합니다.

```csharp
public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private bool useMockApi;

    public override void InstallBindings()
    {
        if (useMockApi)
        {
            // 로컬 PlayerPrefs 기반으로 캐싱하고 딜레이를 흉내 내는 에뮬레이터 주입
            Container.Bind<IAPIService>().To<MockAPIService>().AsSingle();
        }
        else
        {
            // UnityWebRequest 및 REST API, Azure 연동을 처리하는 실 배포용 서비스 주입
            Container.Bind<IAPIService>().To<RealAPIService>().AsSingle();
        }
    }
}
```

---

## 3. 라이프사이클 & 내비게이션 명세 (Lifecycle & Navigation Layer)

모든 퍼즐 콘텐츠는 데이터 기반의 초기화와 UniTask 비동기 라이프사이클에 따라 안전하게 로드됩니다.

*   **[Lifecycle.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Core/Lifecycle.cs)**: `IPresenter`, `IStageOrganizer`, `IPageOrganizer` 인터페이스를 제공하여 씬 초기화 및 정답 확인, 연출 진입/퇴출 시점을 동기화합니다.
*   **[LessonOutline.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Navigation/LessonOutline.cs)**: 하드코딩된 스테이지 매핑을 없애기 위해, 주차별 스테이지 프리팹 경로와 비디오 체크 포인트를 `ScriptableObject` 데이터 애셋화합니다.
*   **[UniversalStageManager.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Navigation/UniversalStageManager.cs)**: `LessonOutline` 데이터를 기반으로 단계 전환 시 이전 프리팹을 제거하고 신규 프리팹을 동적 생성하여 메모리를 최적화합니다.

---

## 4. 인터랙션 & 그리드 스냅 명세 (Interaction & Drag-Drop Layer)

기존 `PieceManager`의 거대한 Input/Physics 결합 로직을 단일 컨트롤러와 규격 인터페이스로 격리시켰습니다.

*   **[IInteractable.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Interaction/IInteractable.cs)**: 드래그가 가능한 게임 블록 등의 물리 대상이 가져야 할 인터페이스 정의.
*   **[IGridSystem.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Interaction/IGridSystem.cs)**: 사각형 격자, 육각형 격자 등 다양한 타일 판 위에서 스냅 좌표를 검증하고 점유 여부(`OccupyCells`)를 판별하는 규격 정의.
*   **[DragDropSystem.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Interaction/DragDropSystem.cs)**: 화면의 레이캐스트 감지, 드래그 위치 갱신, 드롭 시 격자 스냅 검증을 중개하는 전용 컨트롤러.

---

## 5. 정답 검증 및 기반 인프라 서비스 명세 (Rule Validation & Core Services)

실제 플레이어블 씬이 논리 규칙에 따라 판정되고 연출(사운드/팝업)되기 위한 핵심 인프라 명세입니다.

### A. 기하 및 수학 논리 검증 규격
*   **[Validation.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Core/Validation.cs)**:
    *   `ValidationResult`: 정답 판별 여부(`isCorrect`), 오류 피드백 메시지, 오류를 유발한 기하 피스들의 ID 목록(`offendingElementIds`)을 포함하는 구조체.
    *   `IRuleValidator`: 모든 기하 판단(대칭/회전/선분), 수학적 판단(영역 합계), 그래프 판단(인접 충돌/연결) 검증 로직이 공통적으로 상속해야 할 인터페이스.

### B. 시스템 공용 서비스 격리
*   **[Services.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Core/Services.cs)**:
    *   `ISoundService`: 콘텐츠 연출에 필수적인 배경음(BGM) 및 효과음(SFX)의 비디오/학습 상태별 재생 조율.
    *   `IPopupService`: 통신 지연 경고, 정답 오답 메시지 팝업, 하단 토스트 팝업 분배.

### C. 플레이어블 통합 베이스 템플릿 (Base Templates)
*   **[BasePageOrganizer.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Navigation/BasePageOrganizer.cs)**:
    *   개별 문제 씬의 핵심 조율자. 드래그 시스템(`DragDropSystem`), 타일 격자(`IGridSystem`), 카드 피스들(`IInteractable`)의 참조를 조율합니다.
    *   `serializedRules` 목록에 바인딩된 `IRuleValidator` 리스트들을 순회하며 `CheckAnswer()`를 실행해 정답 유효성을 검사합니다.
*   **[BaseStageOrganizer.cs](file:///c:/dev/Unity/Creverse/CreversePortfolio/CreversePortfolio/Assets/MasterFramework/Navigation/BaseStageOrganizer.cs)**:
    *   스테이지별 다단계 페이지 뷰 전환 관리. 점수 제출, 정답/오답 사운드(`ISoundService`) 및 토스트 알림(`IPopupService`) 연동을 담당합니다.

---

## 6. 스테이지 상태 직렬화 및 복원 명세 (State Serialization & Restoration Layer)

스테이지 프리팹을 동적으로 로드 및 삭제(Destroy)하여 메모리를 아끼는 구조적 특성상, 유저가 특정 스테이지에서 행한 중간 조작 상태(블록 배치, 선택지 등)가 소멸되는 한계가 있습니다. 이를 방어하고 사용자의 학습 이력을 보존하기 위해 메멘토 패턴(Memento Pattern)에 기반한 상태 복원 메커니즘을 설계합니다.

### A. 핵심 인터페이스
*   `IStateRestorable`: 개별 스테이지 혹은 페이지가 구현하며, 자신의 현재 플레이 상태를 문자열로 직렬화하여 반환하거나 외부 문자열 데이터를 인계받아 뷰 상태를 복구하는 계약입니다.
    ```csharp
    public interface IStateRestorable
    {
        string SerializeState();
        void DeserializeAndRestoreState(string stateData);
    }
    ```

### B. 상태 조율 흐름 (State Coordination)
1.  **상태 임시 캐싱 (UniversalStageManager)**:
    *   `UniversalStageManager`는 로컬 메모리에 각 스테이지 인덱스별 직렬화된 데이터 문자열을 보관하는 캐시(`Dictionary<int, string> _stageStateCache`)를 운영합니다.
    *   단계 전환으로 인해 현재 활성화된 스테이지를 파괴하기 직전, 스테이지가 `IStateRestorable` 구현체인 경우 `SerializeState()`를 호출하여 상태를 캐시에 백업합니다.
    *   새 스테이지 프리팹을 인스턴스화한 직후, 캐시에 저장된 데이터가 존재하면 `DeserializeAndRestoreState(cachedState)`를 구동하여 유저의 조작 화면을 복구한 뒤 뷰를 노출합니다.
2.  **영구 상태 연동 (Session Synchronization)**:
    *   중간 상태 저장(`SaveIntermediateStateAsync`) 또는 진행 제출(`SaveStudyDataAsync`) 시, 메모리 내 `_stageStateCache`의 전체 딕셔너리 데이터를 JSON 형태로 변환하여 `StudyJsonData` 내에 신규 필드(`public string serialized_layout_states`)로 병합해 클라우드(Azure) 또는 로컬 스토리지(PlayerPrefs)에 동기화합니다.
    *   앱 재진입 시 복원 단계에서 해당 JSON 문자열을 복원하여 `_stageStateCache`를 먼저 재구축하고 스테이지 로딩 시 전달합니다.

---

## 7. 포트폴리오 면접 방어 가치 (Portfolio Design Pillars)

1.  **SOLID 원칙의 입증**:
    *   **단일 책임 원칙(SRP)**: 드래그 물리 처리(`IInteractable`), 스냅 상태 처리(`IGridSystem`), 입력 판단(`DragDropSystem`)이 완전히 고유한 단일 책임을 소유합니다.
    *   **개방-폐쇄 원칙(OCP)**: 새로운 형태의 그리드(예: 삼각형 그리드)나 기하학 피스가 등장해도 기존의 `DragDropSystem` 코드를 수정할 필요 없이 인터페이스 구현체를 확장하여 적용 가능합니다.
    *   **의존 역전 원칙(DIP)**: 플랫폼 데이터 연동 시 구현 클래스(`RealAPIService`)가 아닌 통신 추상화 레이어(`IAPIService`)를 매개로 상호작용합니다.
2.  **데이터 기반 생산성(Data-Driven Productivity)**:
    *   주차별 콘텐츠가 변경되더라도 C# 코드 상에서 하드코딩 매핑을 다시 할 필요 없이, `ScriptableObject`를 생성하여 인스펙터 상에서 교체해 주는 기획 친화적인 에셋 파이프라인의 설계 능력을 증명합니다.
3.  **메모리 최적화와 사용자 경험 보존의 균형**:
    *   동적 에셋 로드/언로드 기법을 활용하면서도 메멘토 패턴을 가미한 직렬화 데이터 기반 복원 프로세스를 구현함으로써, 모바일/웹 모바일 저사양 기기 환경에서 렉 방지(메모리 절감)와 유저 데이터 무손실이라는 상충하는 두 마리 토끼를 영리하게 해결한 모바일 클라이언트 설계 최적화 능력을 증명합니다.
