# [Plan] 글로벌 마스터룰 연동(웜홀) 설정 셋팅 계획서

이 문서는 마스터룰(Lead Engineer Master Rules v2.1)에 의거하여 작성된 **글로벌 마스터룰 로컬 연동(웜홀/디렉토리 정션)** 설정 계획서입니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **현황**: 사용자가 요청한 "웜홀 셋팅"은 블록체인 개발 환경이 아닌, 글로벌 마스터룰 설정 디렉토리(`C:\Users\LYH\.claude\rules`)를 현재의 로컬 작업 공간(`D:\CostBim`)과 연동하는 설정입니다.
- **목표**: 윈도우 환경에서 디렉토리 정션(Directory Junction)을 활용하여 글로벌 마스터룰 폴더를 로컬 `.claude/rules` 경로로 동기화(연동)하여, 로컬에서도 마스터룰을 실시간으로 참조 및 준수할 수 있도록 셋팅합니다.

---

## 2) [Assumption / Risk / Fallback] (가정 / 위험 / 대체 방안)

### 📌 Assumption (가정)
- **가정 1**: 로컬 프로젝트 디렉토리에 `.claude` 및 하위 `rules` 폴더를 생성하고, 이를 글로벌 규칙 경로(`C:\Users\LYH\.claude\rules`)와 연결하는 디렉토리 정션(Junction Link)을 설정하고자 합니다.
- **가정 2**: OS 환경이 Windows이므로, PowerShell 또는 CMD의 `mklink /J` 명령어를 사용하여 링크를 구성합니다.

### ⚡ Risk (위험 요소)
- **위험 1**: 대상 로컬 경로(`D:\CostBim\.claude\rules`)에 이미 폴더가 존재하거나 파일이 있는 상태에서 정션을 생성하려고 시도할 경우, 생성 에러가 발생하거나 기존 파일 유실의 위험이 있습니다. (현재는 `D:\CostBim`이 완전히 비어있으므로 안전합니다.)
- **위험 2**: 글로벌 폴더가 아닌 엉뚱한 경로를 지정하여 정션을 연결할 경우, 마스터룰 파일에 접근할 수 없게 됩니다.

### 🛡️ Fallback (대체 방안)
- **대응 1**: 정션 링크를 만들기 전에 로컬 `.claude` 하위 경로에 기존 폴더가 존재하는지 사전에 확인 및 백업 후 생성합니다.
- **대응 2**: 글로벌 `rules` 디렉토리(`C:\Users\LYH\.claude\rules`)가 안전하게 존재하는지 재검증하고, 마크다운 파일들의 접근성 여부를 링크 생성 직후 컴파일/조회 검증을 통해 확인합니다.

---

## 3) Design Summary (설계 요약)
- **목적**: `C:\Users\LYH\.claude\rules` 디렉토리를 `D:\CostBim\.claude\rules`로 디렉토리 정션 연결.
- **프로젝트 구성**:
  - `D:\CostBim\.claude` 폴더 구조화.
  - 디렉토리 정션 링크 생성 명령어 실행:
    `cmd /c mklink /J "D:\CostBim\.claude\rules" "C:\Users\LYH\.claude\rules"`
- **로깅 & 에러 핸들링**: 정션 생성 및 연결 시 발생하는 모든 로그를 마스터룰 표준 포맷(`[Time] [Level] [Module] [ErrorCode] Message`)에 맞춰 콘솔에 기록합니다.

---

## 4) Implementation Plan (구현 태스크 분할)
1. **[Task 1]** 로컬 프로젝트 루트에 `.claude` 폴더 존재 여부 확인 및 필요한 경우 상위 디렉토리 생성.
2. **[Task 2]** 윈도우 `mklink /J` 명령어를 호출하여 글로벌 `rules` 디렉토리와 로컬 `.claude/rules` 경로를 연결하는 디렉토리 정션 생성.
3. **[Task 3]** 연동 완료 후 로컬 디렉토리에서 글로벌 마스터룰 파일(`master.md`)이 정상적으로 읽히고 연동되었는지 파일 뷰어 및 검색으로 최종 검증.

---

## 5) Verification Plan (검증 계획)
- **정션 상태 검증**: 로컬 `D:\CostBim\.claude\rules\master.md` 파일에 접근하여 내용을 읽을 수 있는지 확인합니다.
- **로그 및 동작 검증**: 연동 셋팅 완료 보고를 한글로 요약하여 출력합니다.
