# NEXT SESSION GUIDE (S047)

이 문서는 다음 세션을 시작하는 AI 어시스턴트와 개발자를 위한 지침서입니다. 다음 세션 시작 시 가장 먼저 이 문서를 검토하여 기존 작업 내역을 일목요연하게 복기하고 후속 작업을 이어가야 합니다.

---

## 🏁 세션 시작 시 최우선 행동 지침
다음 세션에 진입하는 AI 어시스턴트는 대화 시작과 동시에 아래의 핵심 문서 경로를 인용하며, 사용자에게 **전체 아키텍처 및 포팅 작업 히스토리**를 일목요연하게 복기해 주어야 합니다.

1. **[공식 포팅 지침서 (porting_guidelines_and_checklist.md)](file:///c:/dev/Portfolio/_Portfolio_Refactoring_Plans/porting_guidelines_and_checklist.md)**:
   * **3단계 하이라키 (UNIT ➔ STAGE ➔ PAGE)** 명세 및 Gen 2 Challenge 역순 포팅 규격 (Month 36부터 하향 진행), Zenject SubContainer(GameObjectContext) 룰 바인딩 원칙이 정리되어 있습니다.
   * `PREV`, `NEXT` 버튼이 UX상 삭제되었음을 숙지해야 합니다.
2. **[S046 개별 세션 로그 (log-s046.md)](file:///c:/dev/Portfolio/_Portfolio_Refactoring_Plans/logs/log-s046.md)**:
   * Month 36 HighGrade(회로 만들기 퍼즐) 포팅 완결, SO 레벨 데이터 4종 구축, Zenject SubContainer 프리팹 빌드, `PortfolioBuilderHelper`를 통한 로비 씬(3종 게임) 연동 및 PlayMode 검증 내역이 기록되어 있습니다.
3. **[작업 인덱스 (work-log.md)](file:///c:/dev/Portfolio/_Portfolio_Refactoring_Plans/work-log.md)**:
   * S001부터 S046까지의 전체 개발 히스토리가 인덱싱되어 있습니다.

---

## 🎮 차기 세션 (S047) 주요 마일스톤 및 실행 과제

### 1. Gen 2 Challenge Puzzle 역순 포팅 2탄 진행
* **타겟 옵션 1: Month 36 LowGrade (`TheSameOrDifferent` / 같거나 다르거나 스도쿠 논리 퍼즐)**
  - `Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/LowGrade/` 원본 분석 및 Stage 1~4 SO 레벨 데이터 추출.
  - 마스터 프레임워크 기반 `TheSameOrDifferentRuleValidator` 및 Stage/Cell 컴포넌트 이식, Zenject SubContainer 바인딩.
* **타겟 옵션 2: Month 35 HighGrade / LowGrade**

### 2. 포팅된 신규 게임의 로비 씬(`LobbyScene.unity`) 자동 등록
* `PortfolioBuilderHelper.cs`에 신규 게임 빌더 추가 (`BuildAll()` 파이프라인 연계).
* 로비 씬 게임 카드 목록 확장 및 인게임 전환 검증.
