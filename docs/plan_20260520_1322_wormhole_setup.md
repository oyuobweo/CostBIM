# [Plan] Wormhole 개발 환경 구축 및 프로젝트 셋팅

이 문서는 마스터룰(Lead Engineer Master Rules v2.1)에 의거하여 작성된 **웜홀(Wormhole)** 개발 환경 구축 계획서입니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **현황**: 사용자가 "웜홀" 셋팅을 요청하였으나 현재 작업 디렉토리 `D:\CostBim`은 비어 있는 상태입니다.
- **불확실성**: "웜홀"이 의미하는 기술 스택(예: 블록체인 크로스체인 메시징 프로토콜 Wormhole, 파일 전송 도구 magic-wormhole, 혹은 포트 포워딩 툴 등)이 명시적으로 지정되지 않았습니다.
- **목표**: 마스터룰의 `Search-First` 분석에 기반하여, 가장 가능성이 높은 **Wormhole 크로스체인 TypeScript SDK 개발 환경**을 구축하고, 불확실성을 해소하기 위한 `[Assumption / Risk / Fallback]` 구조를 제시합니다.

---

## 2) [Assumption / Risk / Fallback] (가정 / 위험 / 대체 방안)

### 📌 Assumption (가정)
- **가정 1**: 사용자가 구축하려는 "웜홀"은 블록체인 간 자산/데이터 전송을 위한 **Wormhole 크로스체인 프로토콜 SDK** 프로젝트 환경입니다.
- **가정 2**: 향후 멀티체인(EVM 계열 및 Solana 등) 연동을 염두에 두고 있으며, Node.js + TypeScript 기반의 개발 인프라가 필요합니다.

### ⚡ Risk (위험 요소)
- **위험 1**: 사용자가 원했던 웜홀이 네트워크 터널링 도구(예: Ngrok 대용 웜홀)이거나 파일 전송 CLI(`magic-wormhole`)일 경우, 셋팅 방향성이 일치하지 않아 리소스 낭비가 발생할 수 있습니다.
- **위험 2**: 블록체인 Wormhole SDK는 Node.js 버전 및 의존성 간의 버전 호환성(EVM/Solana SDK 호환성 등)에 매우 민감하여 빌드 에러가 발생할 가능성이 높습니다.

### 🛡️ Fallback (대체 방안)
- **대응 1**: TypeScript 기반 Wormhole SDK 프로젝트의 초안을 셋팅하되, 사용자가 다른 도구(예: magic-wormhole 등)를 원한 것으로 밝혀질 경우, 셋팅된 노드 환경을 유지한 채 해당 CLI 도구 설치 및 셋팅으로 신속히 전환합니다.
- **대응 2**: Node.js 버전을 확인하고, 안정화된 최신 버전의 `@wormhole-foundation/sdk` 의존성을 명시하여 호환성 문제를 미연에 방지합니다.

---

## 3) Design Summary (설계 요약)
- **목적**: Node.js + TypeScript 기반의 크로스체인 Wormhole 애플리케이션 개발 환경 구현.
- **프로젝트 구성**:
  - `package.json`: 의존성 관리 및 빌드 스크립트 정의.
  - `tsconfig.json`: 엄격한 타입 체킹 및 ESM(ESNext) 모듈 설정.
  - `src/config.ts`: 마스터룰에 따른 에러 로깅 포맷 및 환경 설정 모듈.
  - `src/index.ts`: Wormhole SDK 초기화 및 단순 테스트 실행 엔트리포인트.
- **에러 핸들링 & 관측성**: 마스터룰 4번 표준 로그 포맷 `[Time] [Level] [Module] [ErrorCode] Message`를 적용한 Logger 유틸리티 포함.

---

## 4) Implementation Plan (구현 태스크 분할)
1. **[Task 1]** Node.js 프로젝트 초기화 및 TypeScript 설치 (`package.json`, `tsconfig.json`).
2. **[Task 2]** Wormhole SDK 및 필수 의존성 설치 (`@wormhole-foundation/sdk`, `@wormhole-foundation/sdk-evm`, `@wormhole-foundation/sdk-solana`).
3. **[Task 3]** 마스터룰 4번을 준수하는 Logger 모듈(`src/utils/logger.ts`) 구현.
4. **[Task 4]** Wormhole SDK 초기화 및 예시 연동 코드(`src/index.ts`) 작성.
5. **[Task 5]** 정상 빌드 및 실행 여부 검증 (TDD 검증 및 동작 요약).

---

## 5) Verification Plan (검증 계획)
- **컴파일 검증**: `npx tsc` 명령어를 통해 타입 에러 없이 성공적으로 컴파일되는지 확인.
- **동작 검증**: `ts-node` 혹은 빌드된 JavaScript 실행을 통해 Wormhole SDK 인스턴스가 정상적으로 생성되고 네트워크(Testnet) 연결이 성립되는지 테스트.
