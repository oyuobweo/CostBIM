# [Plan] 영문 단축키 뫼비우스의 띠(순환 탐색) 네비게이션 기능 구현 계획

이 문서는 사용자가 동일한 영문 단축키(예: C)를 두 번, 세 번 연타할 때, 해당 알파벳으로 시작하는 매개변수 항목들 사이를 순차적으로 이동하고, 마지막 항목에 도달하면 다시 첫 번째 항목으로 되돌아오는 **뫼비우스의 띠식 순환 네비게이션(Circular Navigation) 기능**을 정밀 구현하기 위한 계획서입니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **현상 및 요구사항**:
  - 기존 단축키 기능은 문자를 입력했을 때 무조건 첫 번째 매칭 항목으로만 이동하여 고정됨.
  - 사용자는 동일한 이니셜(예: 'C')을 반복해서 타이핑할 경우, C로 시작하는 다음 파라미터들('Category' -> 'Comments' -> 'Cost' 등)로 한 칸씩 아래로 이동하다가 마지막 항목에서 다시 첫 항목으로 돌아오는 **순환(뫼비우스의 띠) 탐색**을 원함.
- **해결 방안**:
  - `LstParams_PreviewKeyDown` 로직에서 입력받은 알파벳에 해당하는 모든 매개변수 항목과 리스트 내의 절대 인덱스를 수집.
  - 현재 선택되어 있는 항목(`SelectedItem`)이 매칭 리스트에 존재하는지 체크하고, 존재할 경우 `(현재 인덱스 + 1) % 전체 매칭 개수` 공식을 적용하여 다음 항목을 도출(순환 법칙).
  - 도출된 항목으로 선택을 변경하고, 해당 인덱스로 최상단 스크롤 오프셋 정렬 적용 및 컨테이너 포커싱 수행.

---

## 2) Design Summary (설계 요약)
- **목적**: 동일 영문 단축키 반복 입력 시 매칭 항목 간 순환 탐색(뫼비우스의 띠) 실현.
- **입출력**:
  - **입력**: 좌측 매개변수 리스트 영역에서 영문 키보드 입력(A~Z).
  - **출력**: 입력 문자로 시작하는 파라미터가 2개 이상일 때, 누를 때마다 순서대로 포커스가 넘어가고 마지막에서 처음으로 회귀하며 매번 최상단 스크롤 정렬 유지.
- **수학적 모델**:
  - 매칭 리스트를 $M = \{ (Item_0, Index_0), (Item_1, Index_1), \dots, (Item_{k-1}, Index_{k-1}) \}$라 할 때,
  - 현재 `SelectedItem`이 $Item_p$에 위치하면, 다음 이동할 타겟 항목의 인덱스 $next$는 다음과 같음:
    $$next = (p + 1) \pmod{k}$$

---

## 3) Assumption / Risk / Fallback (가정 / 리스크 / 대비책)
- **Assumption (가정)**: 매개변수 목록에 동일 알파벳 이니셜로 시작하는 매개변수가 다수 존재할 때만 순환이 발화하며, 1개만 있을 때는 기존처럼 제자리에 고정 포커싱을 유지한다.
- **Risk (리스크)**: 사용자가 연속으로 키를 너무 빠르게 타이핑할 경우, 비동기 `Dispatcher` 포커싱 큐가 누적되어 스크롤 버벅임이 발생하거나 의도치 않은 항목으로 초점이 튈 수 있다.
- **Fallback (대비책)**: 스크롤 및 선택 변경 처리는 전적으로 동기식 동기화 스레드에서 즉시 완료하고, 오직 포커스 컨테이너 획득만 `DispatcherPriority.Input` 우선순위로 제어하여 렌더링 부하를 예방하고 부드러운 순환 감각을 확보한다.

---

## 4) Proposed Changes (제안된 변경사항)

### [Component: Views]

#### [MODIFY] [MainWindow.xaml.cs](file:///d:/CostBim/Views/MainWindow.xaml.cs)
- **`LstParams_PreviewKeyDown` 순환 알고리즘 전면 개편**:
  - 리스트박스 아이템 소스 전체를 1회 순회하며 입력된 이니셜로 시작하는 항목들과 이들의 절대 인덱스를 튜플 목록 `List<(ParameterItem Item, int Index)>`로 고속 추출.
  - 추출된 매칭 리스트가 비어있지 않은 경우:
    - 현재 선택된 아이템의 매칭 리스트 내 인덱스 위치 `currentMatchIndex` 확인.
    - `currentMatchIndex`가 존재할 경우: `nextMatchIndex = (currentMatchIndex + 1) % matches.Count` 적용.
    - 존재하지 않거나 null인 경우: `nextMatchIndex = 0` (첫 번째 항목) 지정.
    - 최종 선정된 타겟 아이템 및 인덱스로 스크롤뷰어 `ScrollToVerticalOffset(targetIndex)` 수행 및 리스트박스 `SelectedItem = targetItem` 동기화.
    - 비동기 `Dispatcher.BeginInvoke`를 통한 `container.Focus()` 안착.

---

## 5) Testing & Verification Plan (테스트 및 검증 계획)

### Automated Tests
- `dotnet build`를 통한 컴파일 무결성 및 순환 로직 구문 검증 (오류 0개).
- `install_addin.ps1`을 통한 에드인 배포.

### Manual Verification
1. **뫼비우스의 띠 순환 검증**: 
   - 좌측 매개변수 리스트 영역에 C로 시작하는 매개변수가 다수(예: Category, Comments 등) 있을 때, C를 1회 누르면 'Category'로 이동 및 최상단 정렬되는지 확인.
   - C를 2회 누르면 다음 항목인 'Comments'로 아래로 이동하며 해당 항목이 최상단으로 끌어올려 지는지 확인.
   - C를 계속 눌러 C 매칭군의 마지막 항목에 도달한 후, 한 번 더 C를 누르면 다시 처음의 'Category'로 순환 회귀(뫼비우스의 띠)하는지 검증.
2. **단일 항목 검증**:
   - 매칭되는 항목이 단 1개인 문자(예: P로 시작하는 'Project' 등)를 입력할 때는 연타를 하더라도 포커스가 움직이지 않고 온전히 제자리를 지키는지 검증.
