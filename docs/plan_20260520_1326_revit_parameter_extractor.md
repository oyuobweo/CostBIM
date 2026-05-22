# [Plan] Revit 2026 3D View Parameter Extractor Add-in 개발 계획서

이 문서는 글로벌 마스터룰(v2.1 Master Full)과 사용자의 탭 구분 피드백에 의거하여 수정된 **Revit 2026용 3D 뷰 객체 파라미터 선택적 추출 애드인** 개발 계획서입니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **목표**: Revit 2026(영문 버전)에서 활성 3D 뷰에 노출된 객체들의 속성을 사용자가 원하는 파라미터만 선택해 CSV 형태로 데이터화하는 가벼운 고성능 WPF 애드인을 구축합니다.
- **피드백 반영**: 매개변수를 더욱 편리하게 필터링할 수 있도록, **공유 파라미터(Shared Parameter)**와 **프로젝트 파라미터(Project Parameter)**를 WPF UI 상에서 **각각 개별 탭으로 구분**하여 선택 및 필터링할 수 있도록 구현합니다.
- **해결 방안**: `FilteredElementCollector(doc, activeView.Id)`로 3D 가시 객체를 수집한 후, `Parameter.IsShared` 속성 등을 활용하여 런타임에 프로젝트 매개변수와 공유 매개변수를 정밀하게 분류해 WPF `TabControl`에 바인딩합니다.

---

## 2) [Assumption / Risk / Fallback] (가정 / 위험 / 대체 방안)

### 📌 Assumption (가정)
- **가정 1**: 개발 언어는 C# 12 (.NET 8.0) 및 WPF를 활용합니다.
- **가정 2**: `Parameter.IsShared` 속성이 `true`인 경우를 **Shared Parameters** 탭으로 분리하고, 그 외의 사용자 정의 바인딩 매개변수(또는 Revit 기본 속성)를 **Project Parameters** 탭으로 구성합니다.

### ⚡ Risk (위험 요소)
- **위험 1**: 객체 중 일부는 동일한 이름의 파라미터를 다르게 바인딩하고 있거나, 요소별로 누락된 파라미터가 있어 탭 분류 시 중복 혹은 속도 지연이 발생할 수 있습니다.
- **위험 2**: .NET Core (.NET 8.0) 마이그레이션에 따라 WPF 컨트롤 렌더링 시 간헐적으로 이전 리소스 사전(`.xaml`) 경로 호환성 문제가 생길 수 있습니다.

### 🛡️ Fallback (대체 방안)
- **대응 1**: 파라미터 분류 처리를 비동기로 병렬 처리하여 캐싱(HashSet)하고, UI 바인딩 지연을 원천 차단합니다.
- **대응 2**: .NET 8.0 WPF에 완벽히 정합되는 표준 리소스 사전 구문을 적용하여 UI 충돌을 예방합니다.

---

## 3) Design Summary (설계 요약)

### ① 목적
사용자가 활성 3D 뷰의 시각 객체를 즉시 파악하고, 추출하고자 하는 파라미터 리스트를 **Shared Parameters**와 **Project Parameters** 탭에서 직관적으로 다중 선택하여 정돈된 CSV로 내보내도록 지원.

### ② UI 설계 레이아웃 (WPF TabControl)
- **Tab 1: Project Parameters**
  - Revit 내장 및 프로젝트 바인딩 매개변수 체크박스 리스트뷰.
- **Tab 2: Shared Parameters**
  - `IsShared == true`인 외부 공유 매개변수 체크박스 리스트뷰.
- **하단 공통 정렬 영역**: Save Path 선택 버튼, Export 실행 버튼, 진행 로그 표시창.

### ③ 주요 모듈 정의
- **`App` / `Command`**: Revit 외부 명령(IExternalCommand) 진입점.
- **`ExtractorEngine`**: 3D 뷰 가시적 객체 추출 및 `IsShared` 플래그 판별을 통한 파라미터 분류 알고리즘 탑재.
- **`ParameterViewModel`**: 탭별 매개변수 리스트(Project / Shared)를 분리 관리하는 MVVM 뷰모델.
- **`ExtractorView`**: 신뢰도 높은 정렬형 영문 UI (WPF TabControl 적용).

---

## 4) Implementation Plan (구현 태스크 분할)

### Phase 1: .NET 8.0 기반 프로젝트 환경 셋팅
- **[Task 1]**: Revit 2026 SDK 참조 설정을 포함한 `.csproj` 파일 구성 (`net8.0-windows` 타겟 지정).
- **[Task 2]**: 애드인 매니페스트 파일(`.addin`) 정의 및 출력 자동화 설정.

### Phase 2: 비즈니스 로직 및 에러 처리 모듈 개발
- **[Task 3]**: 마스터룰 표준 포맷(`[Time] [Level] [Module] [ErrorCode] Message`)을 충족하는 C# 로그 기록기 개발.
- **[Task 4]**: `IsShared` 플래그 및 매개변수 성격에 따라 탭 데이터 소스를 분류하는 `ExtractorEngine.cs` 핵심 로직 구현.

### Phase 3: WPF UI 및 뷰모델 개발 (영문 및 탭 구조 반영)
- **[Task 5]**: 두 개의 독립된 컬렉션(Project / Shared)을 추적하고 다중 선택을 지탱하는 MVVM 뷰모델 구현.
- **[Task 6]**: `TabControl`을 적용한 차분하고 정돈된 톤의 정렬된 영문 WPF UI 레이아웃 설계.

---

## 5) Testing & Verification Plan (테스트 및 검증 계획)

### 자동화 및 시뮬레이션
- `IsShared` 플래그 모의 상태를 포함하는 Mock Revit Element 인프라를 바탕으로, 탭 분류 알고리즘의 유닛 테스트를 우선 수행합니다.

### 수동 검증 (Revit 런타임)
1. Revit 2026 실행 및 공유 파라미터가 다수 포함된 3D 뷰 활성화.
2. 애드인 구동 후 **Project Parameters**와 **Shared Parameters** 탭에 해당하는 필드가 각각 중복 없이 엄격히 분류되어 표기되는지 렌더링 상태 확인.
3. 양쪽 탭에서 복수의 매개변수를 교차 선택한 후 CSV 출력 시 파일 내에 칼럼 형태로 정상 통합되어 출력되는지 정합성 검증.
