# COLLABORATION PROTOCOL: The Unified Portfolio Project

이 문서는 사용자(Unity Developer)와 AI(Gemini CLI) 간의 효율적이고 체계적인 협업을 위해 설계되었습니다. 5년간의 프로젝트를 통합 리팩토링하는 거대한 여정 동안 컨텍스트를 유지하고, 전문적인 결과물을 도출하는 것이 목적입니다.

---

## 1. 요청 인코딩 절차 (AI-Driven Encoding)

사용자는 자연스러운 문장으로 요청을 전달합니다. Gemini CLI는 이를 분석하여 다음 정보를 스스로 분리/추론하고, **작업 시작 전 [ENCODED_PROMPT] 섹션을 통해 사용자에게 공유**합니다.

- **Target Module**: 분석/수정 대상 프로젝트 또는 모듈
- **Current Phase**: 리팩토링 로드맵상의 현재 단계
- **Technical Requirements**: 적용할 기술 스택 및 제약 사항
- **Operational Objective**: 해당 태스크의 구체적인 목표
- **Verification Plan**: 작업 성공 여부를 확인할 방법

AI는 이 인코딩된 프롬프트를 바탕으로 논리적 일관성을 유지하며 작업을 수행합니다.

## 2. AI의 응답 규약

Gemini CLI는 모든 작업에 대해 다음 형식을 준수합니다.

1.  **Context Analysis**: 요청된 작업이 전체 마스터 플랜에서 어느 위치에 있는지 확인.
2.  **Rationale**: 왜 이 설계를 제안하는지 기술적 근거(SOLID, Design Pattern 등) 설명.
3.  **Implementation**: 최소한의 수정으로 최대의 효과를 내는 코드 제안.
4.  **Work Log Update**: 작업 완료 후 `plans/WORK_LOG.md`에 자동으로 기록.

## 3. 작업 로그 (Work Log) 및 데이터베이스 관리

모든 기술적 결정과 리팩토링 과정은 `plans/WORK_LOG.md` 및 각 프로젝트별 상세 보고서에 기록됩니다. 이는 추후 포트폴리오의 **'기술 블로그'**나 **'프로젝트 상세 설명'**의 핵심 재료가 됩니다.

- **Deep Audit Report**: 각 레벨/주차별 구현 사항을 상세히 분석하여 MD로 기록합니다. 이 보고서는 **"코드 재열람 없이 보고서만으로 기능을 재구현할 수 있는 수준"**의 품질을 지향합니다.
- **Tool Usage Database**: 분석 과정에서 발견된 모든 Shared Tool(Manager, Component 등)의 사용 사례와 **사용 빈도**를 누적 기록합니다. 이는 마스터 프레임워크 설계 시 우선순위의 근거가 됩니다.
- **기록 항목**: 작업 날짜, 대상 모듈, 핵심 로직, 사용된 툴 목록 및 빈도, 리팩토링 포인트.

## 4. Phase 0 확장 운영 규약 (Deep Audit Phase)

Phase 1(설계) 진입 전, 다음 레벨(2, 7, 8, 9, C, E)에 대해 전수 조사를 실시합니다.

1. **분석**: 주차별 Presenter, Model, 핵심 Component를 분석.
2. **기록**: `analysis/gen1/reports/[Level]/week-[NN].md` 형식으로 보고서 작성.
3. **누적**: 사용된 Tool들을 `analysis/gen1/tools/` 하위에 누적 기록하고 사용 빈도 업데이트.
4. **검증**: 작성된 보고서가 기획 및 로직을 완벽히 설명하는지 확인.
5. **취합**: 모든 주차별 보고서 작성이 완료된 후, 발견된 공용 모듈(예: Keypad 시스템 등)을 취합하여 `analysis/gen1/tools/` 하위에 상세 기술 문서를 생성.

## 4. 피드백 루프

- **코드 리뷰 요청**: AI가 작성한 코드가 사용자님의 의도와 다르거나 기존 시스템과 충돌할 경우, 즉시 지적하여 설계를 수정합니다.
- **점진적 진행**: 한 번에 너무 많은 파일을 수정하기보다, 기능 단위(Feature)로 쪼개어 검증하며 진행합니다.
