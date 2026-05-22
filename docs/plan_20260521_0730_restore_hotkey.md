# [Plan] 파라미터 리스트 영문 단축키 이동 기능 복원 및 최상단 스크롤 정렬 UX 개선 계획

이 문서는 좌측 매개변수 리스트(Properties, Project, Shared)에서 알파벳 키(A~Z) 입력 시 해당 항목으로 포커스가 이동하는 '영문 단축키 이동 기능'을 복원하고, 사용자가 입력한 알파벳으로 시작하는 항목들이 **리스트의 최상단에 정렬되어 보이도록 강제 상단 스크롤 정렬 UX**를 추가로 구현하기 위한 계획서입니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **현상 및 요구사항**:
  - 매개변수 리스트 영역에서 단축키(A~Z) 입력 시 해당 항목으로 포커스가 이동하도록 복원하는 것에서 나아가, **해당 알파벳으로 시작하는 매개변수 항목이 화면의 맨 위(최상단)에 걸치도록 리스트 스크롤을 위로 정렬**해 줄 것을 추가 요청함.
- **분석 결과**:
  - WPF의 기본 `ListBox.ScrollIntoView(item)`은 대상 항목이 화면에 보이기만 하면 스크롤을 멈추거나 하단에 걸치게 할 뿐, 최상단으로 끌어올려 주지 못함.
  - 따라서 리스트박스의 내부 `ScrollViewer`를 탐색하여, 매칭된 아이템의 인덱스 위치로 `ScrollToVerticalOffset`을 호출하는 물리적 스크롤 제어가 필요함.

---

## 2) Design Summary (설계 요약)
- **목적**: 영문 단축키 복원 및 타겟 파라미터를 리스트 뷰포트 최상단으로 강제 스크롤하여 시인성 극대화.
- **입출력**:
  - **입력**: 좌측 매개변수 탭 영역에서 영문 키보드 입력(A~Z).
  - **출력**: 입력 문자로 시작하는 첫 번째 매개변수를 자동 선택 및 포커싱하고, 해당 항목이 **ListBox 최상단 첫 줄**에 오도록 강제 오프셋 스크롤 수행.
- **예외 처리**:
  - 항목 수가 뷰포트 크기보다 작아 더 이상 스크롤할 수 없는 경우, 가능한 최대 지점까지 스크롤하여 자연스러운 바운더리 피드백 제공.

---

## 3) Assumption / Risk / Fallback (가정 / 리스크 / 대비책)
- **Assumption (가정)**: 비주얼 트리를 통해 ListBox의 `ScrollViewer`를 확보할 수 있으며, 가상화(Virtualization)가 활성화되어 있어도 항목의 인덱스 번호를 기반으로 오프셋 스크롤을 안정적으로 제어할 수 있다.
- **Risk (리스크)**: 단순 `ScrollViewer.ScrollToVerticalOffset`만 수행할 경우 가상화 템플릿 로딩 시점과의 미세한 타이밍 차이로 인해 컨테이너 포커싱이 풀리거나 스크롤이 흔들릴 수 있다.
- **Fallback (대비책)**: 인덱스를 기반으로 우선 스크롤을 처리한 후, `Dispatcher` 비동기 큐를 통해 `ListBoxItem` 컨테이너의 포커스를 안전하게 바인딩하여 렉 없이 즉각 고정시킨다.

---

## 4) Proposed Changes (제안된 변경사항)

### [Component: Views]

#### [MODIFY] [MainWindow.xaml](file:///d:/CostBim/Views/MainWindow.xaml)
- **ListBoxItem 스타일 개선**:
  - `IsSelected="True"` 트리거 시 은은한 인디고 브랜드 테두리와 초은은한 배경으로 복원:
    - `Background` = `{DynamicResource ListBoxItemSelectedBackground}` (#106366F1, 불투명도 6%)
    - `BorderBrush` = `{DynamicResource ListBoxItemSelectedBorder}` (#306366F1)
    - `BorderThickness` = `1`
- **WPF TextSearch 비활성화**:
  - `LstBuiltInParams`, `LstProjectParams`, `LstSharedParams` 3개 ListBox에서 `IsTextSearchEnabled="False"`로 설정하여 충돌 원천 차단.
- **탭 전환 시 포커스 동기화 바인딩**:
  - `TabControl`에 `SelectionChanged="TabControl_SelectionChanged"` 이벤트를 바인딩하여 탭 선택 즉시 단축키 입력 가능 상태 부여.

#### [MODIFY] [MainWindow.xaml.cs](file:///d:/CostBim/Views/MainWindow.xaml.cs)
- **LstParams_PreviewKeyDown 단축키 최상단 정렬 로직 고도화**:
  - 입력 문자와 일치하는 첫 번째 `ParameterItem` 및 해당 인덱스를 구합니다.
  - 리스트박스 내부에서 `ScrollViewer`를 탐색하는 헬퍼 메서드를 통해 스크롤뷰어를 확보합니다.
  - `scrollViewer.ScrollToVerticalOffset(matchingIndex)`를 호출하여 타겟 항목을 **뷰포트의 맨 위(최상단)**로 완벽하게 스크롤 정렬합니다.
  - 선택과 포커스(`container?.Focus()`)를 주어 사용자에게 시각적으로 정돈된 완벽한 리스트 뷰를 제공합니다.
- **TabControl_SelectionChanged 이벤트 구현 [NEW]**:
  - 탭 클릭 즉시 내부의 `ListBox`를 찾아 포커스를 자동으로 부여하는 스마트 네비게이션 적용.

---

## 5) Testing & Verification Plan (테스트 및 검증 계획)

### Automated Tests
- `dotnet build`를 통한 컴파일 무결성 검증 (오류 0건).
- `install_addin.ps1` 스크립트를 통한 Revit 2026 애드인 배포 준비.

### Manual Verification
1. **최상단 상단 스크롤 정렬 검증**: 좌측 매개변수 리스트 영역에서 'C'를 눌렀을 때, 'Category' 매개변수가 리스트박스의 **맨 첫 번째 줄(최상단)**에 완벽하게 정렬되어 노출되는지 검증.
2. **시각적 포커스 검증**: 해당 최상단 정렬과 동시에 은은한 보라색 배경과 테두리 하이라이트가 명확히 입혀지는지 검증.
3. **탭 전환 후 즉시 동작 검증**: 탭 클릭 후 어떠한 부가 행동 없이 키보드 입력만으로 최상단 스크롤 기능이 매끄럽게 작동하는지 검증.
