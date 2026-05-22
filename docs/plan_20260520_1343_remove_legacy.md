# 계획서: 레거시 ParameterExtractor 파일 소거 및 CostBIM 프로젝트 빌드/설치 정상화

---
> **Role**: 설계 검증, 코드 품질, 운영 안정성을 책임지는 **Lead Engineer Agent**로서 레거시 파일 정리 및 빌드 프로세스 수립을 진행합니다.
---

## 1) Problem Summary (문제 요약)
1. Revit Add-in의 명칭이 `ParameterExtractor`에서 `CostBIM`으로 마이그레이션 중입니다.
2. 하지만 기존 `ParameterExtractor` 관련 소스 코드 및 설정 파일들이 여전히 폴더에 남아있어 새 `CostBIM.csproj` 빌드 시 네임스페이스 및 참조 에러(CS0234 등)를 유발하고 있습니다.
3. 이를 해결하기 위해 불필요해진 옛 레거시 파일들을 안전하게 완전히 삭제하고, 새 `CostBIM` 프로젝트를 컴파일하여 Revit에 정상 설치되도록 배포 파이프라인을 복구해야 합니다.

## 2) Design Summary (설계 요약)
* **목적**: 
  - 중복되거나 충돌을 일으키는 구 버전 `ParameterExtractor` 관련 7개 파일/프로젝트 설정을 안전하게 소거.
  - 최신 리팩토링된 `CostBIM` 코드를 대상으로 무오류 빌드(`dotnet build`) 실현.
  - Revit 다중 버전(2019-2026)에 `CostBIM` 플러그인 자동 복사 및 배포 스크립트 실행 검증.
* **입출력 및 영향 범위**:
  - **입력**: `CostBIM` 아키텍처 소스 파일들
  - **출력**: `CostBIM.dll`, `CostBIM.addin` 바이너리 및 매니페스트 결과물
  - **예외 처리**: 파일 삭제 시 대상 파일이 존재하지 않는 경우의 예외 처리, 빌드 실패 시 상세 로그 분석 및 수정 절차 확보.
* **삭제 대상 레거시 파일 목록**:
  1. `D:\CostBim\ParameterExtractor.csproj` (구 프로젝트 설정)
  2. `D:\CostBim\ParameterExtractor.addin` (구 Revit 매니페스트)
  3. `D:\CostBim\src\Commands\ExtractCommand.cs` (구 진입 커맨드)
  4. `D:\CostBim\src\Engine\ExtractorEngine.cs` (구 분석 엔진)
  5. `D:\CostBim\src\UI\ExtractorView.xaml` (구 WPF UI XAML)
  6. `D:\CostBim\src\UI\ExtractorView.xaml.cs` (구 WPF UI 비하인드 코드)
  7. `D:\CostBim\src\UI\ParameterViewModel.cs` (구 뷰 모델)

## 3) Implementation Plan (구현 계획 - SRP 및 Search-First 준수)
* **단계 1**: 삭제 대상 레거시 파일 7개 확인 및 PowerShell `Remove-Item` 명령어를 활용한 안전한 일괄 삭제.
* **단계 2**: `CostBIM` 프로젝트 클린 빌드 및 복원 (`dotnet clean`, `dotnet build`).
* **단계 3**: 빌드 성공 후 `install_addin.ps1` 배포 스크립트를 가동하여 로컬 Revit 추가 기능 폴더에 `CostBIM` 바이너리 및 프리미엄 아이콘(`resources/costbim_icon.png`) 리소스를 완벽 전파.
* **단계 4**: 로그 관측성을 통해 설치 결과 수집 및 최적화 진행.

## 4) Implementation & Testing & Verification Plan (구현 및 검증 계획)
* **검증 명령어**: 
  - `dotnet build d:\CostBim\CostBIM.csproj`
  - `powershell -ExecutionPolicy Bypass -File d:\CostBim\install_addin.ps1`
* **엣지 케이스 검증**:
  - Revit 2019~2026 로컬 Add-ins 디렉토리 내의 구 매니페스트(`ParameterExtractor.addin`) 파일 및 이전 DLL의 존재 여부를 체크하여 오작동 방지.
  - WPF 윈도우 인스턴스화 시 프리미엄 아이콘이 파일로 존재하지 않는 경우를 대비한 `try-catch` 안전 장치 정상 작동 여부 검증.

## 5) Self Code Review (자체 코드 리뷰 및 리스크 대응)
* **[Assumption / Risk / Fallback]**
  - **가정 (Assumption)**: 사용자는 기존 `ParameterExtractor` 관련 파일들을 완전히 지우기를 희망하며 백업은 필요하지 않다고 판단함.
  - **리스크 (Risk)**: Revit 프로그램이 켜져 있거나 특정 파일이 락(Lock)에 걸려 있어 `Remove-Item` 또는 빌드가 차단될 가능성 있음.
  - **대비책 (Fallback)**: 파일 삭제 실패 시 사용자에게 Revit 종료를 권장하거나, 강제 삭제 옵션(`-Force`)을 활용하여 처리함.
