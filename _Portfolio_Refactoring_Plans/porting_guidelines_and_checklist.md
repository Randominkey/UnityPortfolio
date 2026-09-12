# Portfolio Porting Guidelines & Master Protocol

이 문서는 크레버스 레거시 프로젝트(Gen 1, 2, 3)를 마스터 프레임워크 기반의 테마파크 포트폴리오로 포팅할 때, 코드 무결성을 지키고 불필요한 인프라 결합을 제거하며 매 세션 일관된 방향을 유지하기 위한 **공식 개발 지침서(Living Document)** 입니다. 포팅 과정에서 조율되거나 개선되는 사항이 생기면 이 문서를 즉각 최신화 및 누적 기록해야 합니다.

---

## 1. 포팅 핵심 원칙 (Core Principles)

### 1) 싱글 플레이화 및 로컬화 (Local Standalone)
* 기존 실서비스에 결합되어 있던 Socket.IO 기반의 실시간 네트워크 매칭(`NetworkManager`, `InGameVersusViewer` 등)은 완전히 제거합니다.
* 사용자 vs 로컬 AI 또는 로컬 2인 턴제로 동작하도록 게임 로직을 단순화합니다.

### 2) 의존성 주입의 프레임워크 표준화 (DIP)
* 기존의 `SoundManager.Instance`, `PopupManager.Instance` 같은 전역 정적 싱글톤이나 하드코딩된 Zenject 주입을 포트폴리오 프레임워크의 인터페이스(`ISoundService`, `IPopupService`, `IAPIService`)로 치환합니다.

### 3) 프리젠터(Presenter)의 커스터마이징 식별 및 간소화
* **패턴 A (껍데기 프리젠터)**: 단순히 씬을 로드하고 상단 HUD 백 버튼만 제어하는 프리젠터는 과감히 삭제하고, 마스터 프레임워크의 `UniversalStageManager` 시스템으로 대체합니다.
* **패턴 B (커스텀 로직 프리젠터)**: 턴 제어, AI 조율 등 게임 규칙의 코어가 들어있는 프리젠터는 해당 로직을 `IStageOrganizer`를 상속하는 고유 템플릿 스테이지 클래스(예: `GonuStageOrganizer`)로 이전하고 프리젠터 자체는 삭제합니다.

### 4) 저작권 방어 및 비주얼 리모델링
* 기존 회사명(`CMS.WhyXX.WeekXX`) 네임스페이스를 포트폴리오 도메인(`Portfolio.WhyXX.WeekXX`)으로 리팩토링합니다.
* 오래된 실사 이미지나 상표권/회사 관련 리소스는 세련되고 모던한 범용 디자인 에셋으로 단계적으로 스왑합니다.

### 5) 물리적 포팅 및 어셈블리 격리 (Physical Porting & Isolation)
* **Unity Package (`.unitypackage`) 활용**: 레거시 프로젝트에서 타겟 씬(또는 루트 프리팹)을 `Export Package` (Include Dependencies 체크)로 추출하여 메타 GUID와 레퍼런스(텍스처/머티리얼) 유실을 원천 방지합니다. Import 시 불필요한 레거시 전역 매니저는 체크 해제합니다.
* **Assembly Definition (`asmdef`) 분리**: Import 완료 후, 해당 게임 폴더 최상단에 고유한 `asmdef`를 생성하여 타 게임 간의 코드 의존성을 물리적으로 차단하고 컴파일 속도를 극대화합니다.

### 6) 런타임 아키텍처 및 프리팹 스폰 로딩 (Runtime Loading Strategy)
* **MasterSandbox 단일 씬 유지**: 씬 전환으로 인한 라이팅 꼬임, EventSystem 중복, 로딩 지연을 막기 위해 `MasterSandbox` 단일 씬을 유지합니다.
* **Base Game Prefab + Addressables**: 각 게임의 뷰/로직 루트를 **Base Game Prefab**으로 만들고, Addressables(또는 Resources)를 통해 런타임에 동적으로 Instantiate(스폰)합니다.

### 7) Zenject SubContainer (GameObjectContext) 캡슐화
* 각 미니게임의 루트 프리팹에 `GameObjectContext`와 전용 `Installer`(예: `Month10HighInstaller`, `GonuInstaller`)를 부착합니다.
* **OCP 원칙 준수**: 메인 프로젝트의 `ProjectContext`나 `MasterSandbox`를 수정할 필요 없이, 미니게임 프리팹이 생성될 때 자체 SubContainer가 룰 검증기(`IRuleValidator`), AI 로직(`GonuAIService`) 등을 독자적으로 인스턴스화/주입하도록 캡슐화합니다.

---

## 2. 아케이드 오락기 구조 및 세대별(Gen 1~3) 포팅 아키텍처 규약

### 1) 메인 아케이드 룸 & 오락기 상호작용 개념
* **메인 아케이드 룸 (Lobby Scene)**: 플레이어가 캐릭터를 조종하여 아케이드 방 안을 자유롭게 탐색.
* **오락기 1대 (Arcade Machine Object)**: 아케이드 룸에 설치된 1대의 오락기는 **1개의 게임 묶음(1 Content UNIT / PortfolioGameOutline)**을 상징.
* **게임 진입 (Inside 1 Arcade Machine)**:
  * 1대의 오락기 상호작용 시 1개의 `UniversalStageManager` 세션으로 진입.
  * 상단 HUD / 내비게이션 바를 통하여 해당 오락기가 품고 있는 N개의 Stage들(초기 세팅/룰)을 자유롭게 전환하며 플레이.

```
[Main Arcade Room (Lobby)]
   ├── [Arcade Machine 1: Month 10 High] ──> 진입 ──> [Stage 1] [Stage 2] [Stage 3] [Stage 4]
   └── [Arcade Machine 2: 우물고누 AI]   ──> 진입 ──> [1단계: 유불리 1] [2단계: 유불리 2] [3단계: AI 실전 대전]
```

### 2) 3단계 하이라키 구조 명세 (3-Tier Hierarchy Specification)
각 미니게임의 1개 콘텐츠 묶음(**1 Content UNIT = 오락기 1대**)은 최대 **36개 PAGE**까지 상하 구조를 가질 수 있습니다.

```
[1 Content UNIT (Presenter / 오락기 1대)] (최상위 구동 껍데기 / 루트 뷰)
   ├── [Sub UNIT 1: STAGE 1] (1 ~ 6개)
   │     ├── PAGE 1 (1 ~ 6개)
   │     ├── PAGE 2
   │     └── ...
   ├── [Sub UNIT 2: STAGE 2]
   └── ... (최대 6 STAGEs × 6 PAGEs = 36 PAGEs)
```

### 3) 레거시 세대별(Gen 1~3) 모듈화 구현 패턴
레거시 콘텐츠를 마스터 프레임워크로 포팅할 때 원본의 구현 방식에 맞춰 아래 4가지 분류 규칙을 엄격히 적용합니다.

1. **Gen 1 (오래된 콘텐츠)**:
   * 특징: 별도의 STAGE/PAGE 분리 없이 최상위 **PRESENTER** 단일 스크립트에 모든 비즈니스 로직이 몰려있는 형태가 대부분이나, 일부 Gen 1 콘텐츠의 경우 **STAGE 단위**로 로직이 구현되어 있는 경우도 존재함.
   * 포팅 전략: PRESENTER 또는 STAGE의 로직을 `IStageOrganizer` 상속 스테이지 클래스로 모듈화 이전.
2. **Gen 2 Challenge Puzzle (Month01 ~ Month36)**:
   * 특징: PRESENTER는 구동 껍데기 역할만 수행. 4개의 STAGE 오브젝트가 **동일한 스크립트(=동일한 룰)**를 사용하며, 맵 데이터 세팅만 다른 4개 STAGE 오브젝트를 ON/OFF 전환하는 방식.
   * 포팅 전략: 데이터 기반 동적 생성 패턴(`PlayableLevelData` SO + `UniversalPlayableStageTemplate`)으로 전환하여 4개 맵을 1개의 템플릿 프리팹으로 구동. (Month10 High 방식)
3. **Gen 2 Math Puzzle (Why?? 형태)**:
   * **Pattern 3-1**: Gen 1과 동일 (Presenter 전용 로직).
   * **Pattern 3-2**: PRESENTER는 구동 껍데기이며, 각 STAGE별로 **개별 STAGE 스크립트/로직**을 보유 (예: Why02 Week 11 우물고누 - Stage 1/2 유불리 퀴즈 토글 판정, Stage 3 AI 실전 대전).
   * **Pattern 3-3**: PRESENTER 및 STAGE는 모두 구동 껍데기이며, 최하위 **PAGE**에서 개별 로직을 보유. STAGE 오브젝트 ON/OFF 전환 및 STAGE 하위 PAGE 오브젝트 ON/OFF 전환을 계층적으로 수행.
4. **Gen 3 (모던 프레임워크)**:
   * 특징: PRESENTER - STAGE - PAGE 규격화된 동적 데이터 템플릿 아키텍처 준수.

---

## 3. 포팅 5단계 실행 체크리스트 (5-Step Checklist)

* [ ] **Step 1: 패키지 추출 & 임포트 (`.unitypackage`)**
  * 타겟 프로젝트 씬/프리팹 추출 ➔ `Assets/MasterFramework/Playable/[ContentName]/` 임포트.
* [ ] **Step 2: 원본 YAML 직렬화 데이터 추출 및 무결성 분석 (Data & Graph Extraction)**
  * 원본 씬/프리팹 파일에서 각 컴포넌트의 모든 `SerializedField` 값(`initialState`, 인접 그래프 리스트, 좌표, 토글 상태)을 파싱하여 기준 진실(Ground Truth)로 추출 및 양방향 무결성 검증.
* [ ] **Step 3: 어셈블리 격리 (`.asmdef`) 및 코드 리팩토링**
  * `.asmdef` 작성, 레거시 네트워크/매니저 코드 삭제, `IRuleValidator` / `IStageOrganizer` 상속 조율자 구현, 자연수 정렬(`Natural Numeric Ordering`) 적용.
* [ ] **Step 4: SubContainer (GameObjectContext) 및 Installer 작성**
  * 미니게임 전용 `Installer` 작성 후 `GameObjectContext`에 바인딩, 지연 생성(Lazy Initialization) 방어 로직 적용.
* [ ] **Step 5: 전용 `LessonOutline` 및 `PortfolioGameOutline` SO 생성**
  * `LessonOutline_[ContentName].asset` 및 `GameOutline_[ContentName].asset` 생성 및 로비 연동, 실전 동작 검증.

---

## 4. 포팅 시 데이터/그래프/프리팹 무결성 보존 3대 필수 규약 (Data Integrity Protocols)

### 1) 원본 씬/프리팹 YAML 메타 역추적 파싱 필수화 (Ground Truth Extraction)
* 임의의 추정치로 데이터를 작성하지 않고, 원본 `.unity` 또는 `.prefab` 파일의 YAML 메타데이터를 스크립트로 직접 역추적 파싱하여 정확한 초기 세팅 배열(`initialState`), 인접 리스트(`originateButtonComponents`), 정답 조건을 추출합니다.

### 2) 그래프 무결성 자동 양방향화 (Bidirectional Graph Validation)
* 노드 기반 보드 게임의 경우, 에디터 수작업 직렬화 과정에서 단방향 누락이 발생하기 쉽습니다. (예: 노드 1은 0을 가리키는데, 노드 0은 1을 가리키지 않는 누락)
* 코드로 인접 그래프를 바인딩할 때 상호 연결을 100% 보장하는 **양방향 인접 리스트(Bidirectional Adjacency Graph)**를 반드시 구축하고, AI 및 플레이어는 해당 인접 리스트에 등록된 유효 경로로만 이동하도록 제한합니다.

### 3) 자연수 정렬(`Natural Numeric Ordering`) 강제
* 유니티 하이라키 및 `GetComponentsInChildren`은 `(10)`을 `(2)` 앞에 두는 문자열 사전순(Alphabetical) 정렬을 반환하여 인덱스가 밀리는 고질적 문제가 있습니다.
* 모든 번호 매겨진 오브젝트(`StateButton (0)` ~ `StateButton (10)`)는 반드시 정규식 기반 정수 파싱을 통한 **자연수 정렬(`OrderBy(ExtractButtonIndex)`)**을 거친 후 초기화해야 합니다.

---

## 5. Gen 2 Challenge Puzzle (72종 Minigame) 전용 표준 포팅 프로토콜 & 트러블슈팅 복기

### 1) 표준 포팅 워크플로우 (3단계 초고속 루틴)
1. **Base Prefab 추출 및 범용 어댑터 부착**:
   - 원본 씬의 `Stage 1` 오브젝트를 `Assets/Resources/Prefabs/[Month][Grade].prefab`으로 저장.
   - 루트에 `UniversalMinigameStage` 부착 (`[RequireComponent]`로 `GameObjectContext` 및 `UniversalMinigameInstaller` 자동 부착).
   - 인스펙터 우클릭 `Reset` 실행 시 보드 컴포넌트(`MiniGameBoardBase`), 버튼(`Check`, `Refresh`, `Rule`), 룰 팝업, 헤더가 100% 자동 탐색 바인딩됨.
2. **LevelData SO 스키마 정의 및 Ground Truth 데이터 추출**:
   - 해당 게임 보드의 인스펙터 직렬화 필드(그리드 크기, 맵 문자열 배열, 힌트 등)를 담는 `[Game]LevelData.cs` 작성.
   - 원본 씬 파일(.unity)을 파싱하여 Stage 1~4(또는 1~6)의 `[Game]LevelData_Stage1~N.asset` 생성.
   - `UniversalMinigameStage`의 `InjectLevelDataToBoard()`가 리플렉션을 통해 보드 필드에 값을 주입하고 `board.Ready()`를 호출함.
3. **LessonOutline / GameOutline 생성 및 로비 연동**:
   - `LessonOutline` 생성 시 `stageTemplatePrefab` / `stagePrefab`에 반드시 **프리팹의 루트 GameObject fileID**를 연결.
   - `PortfolioBuilderHelper` 및 `LobbyScene`의 `availableGames`에 `GameOutline` 등록.

### 2) Month 35 포팅 과정에서 해결된 장애물 및 영구 해결 요소 (재발 방지 항목)
* **[영구 해결] New Input System 호환성 및 `RepeatButton` 예외 (999+ 에러)**:
  - `RepeatButton.cs`를 `UnityEngine.InputSystem.Keyboard` 및 `IPointerDownHandler` 기반으로 전면 리팩토링 완료.
  - 마우스 누름 즉시 발동 + 키보드 누름 즉시 발동 + 홀드 연속 입력(지연 후 반복)이 프로젝트 전체에서 완벽히 동기화되어 향후 다른 72종 챌린지 퍼즐에서도 재작업 불필요.
* **[영구 해결] 레거시 누락 폰트(Missing Font) 투명화 방어**:
  - 프리팹 원본 파일의 폰트 GUID를 훼손하지 않으면서, `UniversalMinigameStage.cs`의 `EnsureValidFonts()`가 런타임에 폰트가 누락된 `Text`에 한해 인메모리로 `LegacyRuntime.ttf`를 안전하게 폴백 주입함.
* **[주의 및 체크리스트] LessonOutline 프리팹 루트 fileID 바인딩**:
  - `AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)`를 사용할 때는 항상 최상단 루트 GameObject가 로드되지만, YAML 수동 작성 시 자식 오브젝트의 fileID가 바인딩되지 않도록 주의(루트 fileID 검증 필수).
* **[마이그레이션 대기] Month 10 High 표준화**:
  - 초기 프로토타입 방식으로 작성된 Month 10 High는 타 Month들을 포팅하는 과정에서 5단계 표준 프로토콜로 자연스럽게 일괄 재포팅 예정.

### 3) 룰 표시(Rule Display) 및 저작권/공용 UI 설계 제한사항 (Rule & IP Protocol)
* **저작권(IP) 보호 원칙**: 레거시 디자이너가 제작한 일러스트 팝업 그래픽은 원저작권 이슈가 존재하므로, 포트폴리오 메인 화면에서는 `UniversalRuleModal`을 통한 깔끔한 텍스트/Markdown 기반 룰 제시를 기본 표준으로 채택함.
* **상세 룰 일러스트화 제한사항 및 백로그**:
  - 기존 레거시 룰 중 복잡한 다이어그램/예시 그림을 필요로 하는 콘텐츠의 경우, LLM이 이미지를 일일이 읽어 재구성하는 것은 토큰 소모가 과도하므로 현재는 핵심 텍스트 룰/조작 가이드 위주로 제시함.
  - 향후 여유 시점에 포트폴리오 전용 벡터 다이어그램이나 SVG/클린 일러스트를 별도 제작하여 `UniversalRuleModal`의 `ruleIllustrationImage` 슬롯에 선택적으로 주입하는 방안을 백로그로 관리함.

