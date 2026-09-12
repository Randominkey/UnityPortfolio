# TODO List: 마스터 프레임워크 확장 및 Gen 1-3 포팅 태스크

- `[x]` **Task 1: Month 10 High (1호 콘텐츠 - Gen 2 Challenge) 데이터 기반 동적 생성 이식 및 표준 규격 마이그레이션**
    - `[x]` Stage 1 ~ 4의 격자 레이아웃 및 힌트 프레임 기획 데이터 추출 완료
    - `[x]` `PlayableLevelData` SO 에셋 4종 생성 (`PlayableLevelData_Stage1~4.asset`) 및 GUID 메타 매핑 완료
    - `[x]` `PlayableRectSquareCell` / `PlayableRectSquareFrame` `ILockableElement` 인터페이스 구현 완료
    - `[x]` Unity 에디터 내 에디터 빌더 스크립트([PortfolioBuilderHelper.cs](file:///c:/dev/Portfolio/Assets/Editor/PortfolioBuilderHelper.cs)) 작성 및 자동화 빌드 완료
    - `[x]` **[S044]** 하드코딩 switch-case 팩토리 제거 ➔ `[Inject] IRuleValidator` 전환
    - `[x]` **[S044]** SubContainer `GameObjectContext` 및 `Month10HighInstaller.cs` 바인딩 완료
    - `[x]` **[S044]** 어셈블리 독립 `MasterFramework.Playable.Month10High.asmdef` 구축
    - `[x]` **[S044]** 전용 아웃라인 `LessonOutline_Month10High.asset` (4개 Stage) 및 `GameOutline_Month10High.asset` SO 생성 완료

- `[x]` **Task 2: 로비 허브(Lobby Hub) 시스템 구축 & UX 통일화 & HUD 정비**
    - `[x]` 개별 게임 선택 카드 및 세션 로드를 지원할 [PortfolioGameOutline.cs](file:///c:/dev/Portfolio/Assets/MasterFramework/Navigation/PortfolioGameOutline.cs) SO 설계 완료
    - `[x]` 로비 씬 UI 인스턴스화 및 게임 세션 로드를 총괄하는 [PortfolioLobbyManager.cs](file:///c:/dev/Portfolio/Assets/MasterFramework/Navigation/PortfolioLobbyManager.cs) 구현 완료
    - `[x]` [UniversalStageManager.cs](file:///c:/dev/Portfolio/Assets/MasterFramework/Navigation/UniversalStageManager.cs)에 Lobby Return 버튼 및 씬 전환(`ReturnToLobby`) 수명주기/로컬 상태 저장 결합 완료
    - `[x]` **[S044]** 미니게임별 전용 `LessonOutline` SO 분리 구축 ➔ 로비 내 어느 미니게임을 열더라도 상단 Universal HUD 내비게이션 바를 통해 일관된 스테이지 전환 UX 제공 완료
    - `[x]` **[S044]** 불필요한 `PREV`, `NEXT` 버튼 및 관련 순차 이동 로직 완전 소거 완료

- `[x]` **Task 3: 공용 튜토리얼(강제 조작 잠금) 템플릿 뼈대 구현**
    - `[x]` `ILockableElement` 조작 제어 인터페이스 설계 완료
    - `[x]` `TutorialSequence`, `TutorialStep`, `TweenAnimationData` 데이터 선언부 구조 정의 완료
    - `[x]` `WorldToScreenPoint` 좌표 정렬 및 잠금 제어를 담당하는 `TutorialOverlayView` 및 `TutorialEngine` C# 핵심 클래스 구현 완료

- `[x]` **Task 4: 고누 AI (2호 콘텐츠 - Gen 2 Math Puzzle Pattern 3-2) 신규 표준 규격 마이그레이션 & UX 통일**
    - `[x]` 레거시 네트워크 매칭 모델 및 껍데기 프리젠터 완전 제거
    - `[x]` 레거시 `Exclude build` 경로 파일 이전 ➔ `Assets/MasterFramework/Playable/Why02_Gonu/`
    - `[x]` **[S044]** `GonuStageOrganizer.cs` (`BaseStageOrganizer`, `IStageOrganizer`, `IStateRestorable`) 리팩토링 완료
    - `[x]` **[S044]** `GonuRuleValidator.cs` (`IRuleValidator`) 및 `GonuAIService.cs` 독립 클래스 분리 완료
    - `[x]` **[S044]** SubContainer `GameObjectContext` 및 `GonuInstaller.cs` 바인딩 완료
    - `[x]` **[S044]** 어셈블리 독립 `Portfolio.Why02.Gonu.asmdef` 구축 완료
    - `[x]` **[S044]** 고누 전용 아웃라인 `LessonOutline_Gonu.asset` (3개 Rule Stage: 1주차-삼목, 2주차-우물, 3주차-세기) 및 `GameOutline_Gonu.asset` SO 생성 및 UX 통합 완료
