# 계획서: ##Sample 애드인 이식 및 CostBIM 통합 리팩토링 계획

---
> **Role**: 설계 검증, 코드 품질, 운영 안정성을 책임지는 **Lead Engineer Agent**로서 샘플 애드인의 무결성 이식 및 네임스페이스 일괄 전환(RevitQTO -> CostBIM)을 설계합니다.
---

## 1) Problem Summary (문제 요약)
1. 기존에 작성된 `CostBIM` 관련 소스 코드를 비우고, 실제 작동하는 프리미엄 샘플 애드인 소스 코드(`D:\##Sample\apps\desktop` 및 `install_addin.ps1`)를 `D:\CostBim`으로 안전하게 이식해야 합니다.
2. 이때 AI 설정 관련 파일(`.claude` 폴더 및 이전 계획서 등)은 보존해야 합니다.
3. 이식 완료 후 모든 프로젝트 파일명, 폴더명, 매니페스트 파일명 및 코드 내 모든 명칭(`RevitQTO` 계열)을 **`CostBIM`**으로 일괄 리팩토링(명칭 전환)해야 합니다.

## 2) Design Summary (설계 요약)
* **청소 범위**: 
  - `D:\CostBim` 내부의 `.claude`, `docs` 폴더(AI 설정 및 문서 보존)를 제외한 모든 소스 코드 및 빌드 결과물(`src`, `resources`, `bin`, `obj`, `CostBIM.csproj`, `CostBIM.addin`, `install_addin.ps1` 등) 삭제.
* **이식 및 매핑**:
  - `D:\##Sample\apps\desktop` ➔ `D:\CostBim` (WPF 및 Loader 모듈 포함)
  - `D:\##Sample\install_addin.ps1` ➔ `D:\CostBim\install_addin.ps1`
* **명칭 치환 규칙**:
  - 파일명 변경: `RevitQTO.Addin.csproj` ➔ `CostBIM.csproj`, `RevitQTO.Addin.addin` ➔ `CostBIM.addin`, `RevitQTO.Loader` ➔ `CostBIM.Loader`
  - 코드 텍스트 치환: `RevitQTO.Addin` ➔ `CostBIM`, `RevitQTO.Loader` ➔ `CostBIM.Loader`, `RevitQTO` ➔ `CostBIM`
* **자동화 배포 파이프라인**:
  - 스크립트 기반 복사 및 일괄 명칭 치환 수행으로 휴먼 에러 방지.

## 3) Implementation Plan (구현 계획)
* **단계 1**: `D:\CostBim` 백업 및 청소 스크립트 작성 및 가동.
* **단계 2**: `D:\##Sample` 에서 애드인 원본 소스 복사 및 폴더/파일명 교정 (`CostBIM.csproj`, `CostBIM.Loader` 구조화).
* **단계 3**: 폴더 내의 모든 `.cs`, `.xaml`, `.csproj`, `.addin`, `.ps1` 파일을 스캔하여 `RevitQTO` ➔ `CostBIM` 명칭 일괄 치환 자동화 스크립트 실행.
* **단계 4**: 리팩토링된 `CostBIM` 프로젝트 컴파일 및 설치 검증.

## 4) Testing & Verification Plan (검증 계획)
* **컴파일 빌드**:
  - `dotnet build d:\CostBim\CostBIM.csproj` 실행하여 에러 유무 검증.
* **설치 스크립트**:
  - `powershell -ExecutionPolicy Bypass -File d:\CostBim\install_addin.ps1` 실행하여 Revit 추가 기능 경로 정상 배포 검증.
