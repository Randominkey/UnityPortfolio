# LOG ARCHIVING PROTOCOL

이 문서는 매 세션의 작업 내역이 누락 없이 기록되고 관리되도록 보장하는 규약입니다.

---

## 1. 아카이빙 구조
- 모든 작업 로그는 `_Portfolio_Refactoring_Plans/logs/` 폴더 내에 개별 세션 파일로 저장됩니다.
- 파일명 규칙: `log-sNNN.md` (예: `log-s001.md`)
- `work-log.md`는 전체 세션의 흐름을 한눈에 보는 **Index(목차)** 역할만 수행하며, 상세 내용은 개별 세션 로그에 위임합니다.

## 2. 하이브리드 로그 전략 (Hybrid Logging)
- **개별 세션 로그 (`logs/log-sNNN.md`)**: 작업마다 **신규 파일**로 생성. [ENCODED_PROMPT], 구체적 작업 내역, 기술적 결정, 다음 단계를 상세히 기록.
- **메인 워크로그 (`work-log.md`)**: 세션 번호를 우선 명시하여 한 줄 요약 업데이트. `replace` 도구를 사용하여 최상단에 추가.

## 3. 로그 템플릿 (Session Log Template)

매 세션 로그는 다음 항목을 포함해야 합니다:

```markdown
# SESSION LOG: [세션 번호]
- **Date**: [YYYY-MM-DD]
- **Time**: [HH:MM]
```
## 1. [ENCODED_PROMPT]
- **Target Module**: 
- **Current Phase**: 
- **Technical Requirements**: 
- **Operational Objective**: 
- **Verification Plan**:

## 2. 작업 내역 (Actions Taken)
- 구체적인 수정 파일 및 코드 변경 내용 설명.

## 3. 기술적 결정 및 근거 (Rationale)
- 특정 패턴이나 설계를 선택한 이유.

## 4. 해결된 이슈 및 한계점
- 작업 중 발생한 에러와 해결 방법, 혹은 미처 해결하지 못한 부분.

## 5. 다음 단계 (Next Steps)
- 이어서 진행해야 할 구체적인 태스크.
```

## 4. 메인 워크로그 업데이트 규칙
- 형식: `- **S[NNN]** ([YYYY-MM-DD]): [핵심 요약 한 줄]`
- 세션 번호를 기준으로 정렬하여 작업의 연속성을 보장.

## 5. 누락 방지 규칙
- 작업 종료 전 반드시 신규 로그 파일을 작성합니다.
- 이전 세션 로그를 참조해야 할 경우 `read_file`을 사용하여 명시적으로 컨텍스트를 복원합니다.
