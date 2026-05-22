# [Plan] Parameter Scan Processing & Full Loading Overlay Implementation
**작성일**: 2026년 5월 21일 13:10
**작성자**: Lead Engineer Agent

---

## 1) Problem Summary (문제 요약)
1. **로딩UX 고도화**: 애드인 구동 즉시 텅 빈 메인 화면이 깜빡이며 보이는 현상(Flicker)을 차단하고, 처음부터 전면 불투명 로딩 창("Parameter 스캔 중...")이 나타난 후, 스캔 완료 시 확인 버튼을 눌러 본 화면으로 인입되는 모던 목업 창 방식의 고유 UX를 완결 지어야 함.
2. **실유효 파라미터 정밀 검출**: 3D 뷰에 존재하는 물리 실체 요소이면서, 카테고리 블랙리스트를 필터링하고, 실제 매개변수 값이 명확하게 기록되어 있는(`param.HasValue == true`) 진짜 매개변수만 추출하여 좌측 사이드바(Built-in, Project, Shared 탭)에 노출되도록 필터링 로직을 결합해야 함.

---

## 2) Design Summary (설계 요약)
- **목적**: 기동 즉시 전체 화면 차단 스캔 및 실유효 파라미터의 정밀 가공 처리.
- **입력**: 
  - Revit Active Document.
  - Active 3D Viewport Geometry Element Stream.
- **출력 / 예외 처리**:
  - `ParameterSchema`: 3D 뷰의 물리 요소(Solid)이며 카테고리 필터를 통과하고, 실제로 값이 들어 있는 유효 매개변수 리스트.
  - 에러 처리: Revit Geometry 획득 또는 DB 순회 예외 발생 시 크래시 방지 및 Safe Fallback.
- **주요 모듈**:
  - `RevitElementExtractor.ScanAvailableParameters`: 물리 기하 Solid 볼륨 검증 및 카테고리 블랙리스트 필터 이식, `param.HasValue` 검사 추가.
  - `MainWindow.xaml`: `LoadingOverlay`를 최초 로드 시 불투명 `Visible` 상태로 변경하여 Flicker 원천 소멸.
  - `MainWindow.xaml.cs`: `ShowLoading(string title, string subTitle)` 다형성 메서드 오버로드 이식 및 `ShowLoading()` 래퍼 구현.

---

## 3) Implementation Plan (구현 계획 - SRP & Search-First 준수)
### 1단계: RevitElementExtractor.cs 실유효 스캔 고도화
- `ScanAvailableParameters` 메서드 수정:
  - 3D 뷰 가시 요소 수집 루프 내에 `HasValidPhysicalGeometry(elem)` 물리 지오메트리 검증 이식.
  - 카테고리 명칭 기반 정밀 블랙리스트 필터 추가.
  - `CategorizeAndAdd` 내에 `if (!param.HasValue) return;` 및 Revit Storage Type별 비어있는 값 검사 추가하여 유령 매개변수 완전 제외.

### 2단계: MainWindow.xaml.cs 및 XAML UI 차단 최적화
- `MainWindow.xaml`:
  - `LoadingOverlay`의 초기 `Visibility`를 `Visible`로 수정하여 창이 열리자마자 바로 로딩 차폐막이 동작하도록 설정.
- `MainWindow.xaml.cs`:
  - `ShowLoading(string title, string subTitle)` 및 `ShowLoading()` 구현.
  - 생성자에서 첫 로드 시 `ShowLoading("Parameter 스캔 중...", ...)`를 기동하도록 설정 유지 및 지연 바인딩(`BtnConfirmLoading_Click`) 안전 작동 확인.

### 3단계: 빌드 및 컴파일 무결성 검증
- `dotnet build`를 통해 빌드가 정상적으로 완결되는지 확인.
- 배포 스크립트를 통한 핫로드 파일 동기화 검증.

---

## 4) Verification Plan (검증 계획)
### 수동/자동 검증
1. **Revit Addin 빌드 무결성 검증**:
   - `dotnet build` 명령어를 수행하여 무오류 컴파일을 입증함.
2. **시각적 로딩 동작 및 필터 스캔 검증**:
   - 애드인 실행 시 로딩 오버레이가 처음부터 불투명하게 메인 창을 완전히 차단하는지 관측.
   - 스캔 완료 시 인디고 체크마크와 함께 '확인' 버튼이 온전히 표시되고, '확인'을 클릭하면 오버레이가 Collapsed 되면서 유효 데이터 표가 부드럽게 노출되는지 시각적 유효성 확인.
   - 사이드바 내 매개변수 목록에 실제로 존재하고 값을 가지고 있는 유효한 Built-in / Project / Shared 매개변수만 깔끔하게 노출되는지 체크.
