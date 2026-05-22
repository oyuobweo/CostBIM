# [Plan] 윈도우 빈 바탕 클릭 시 검색 필터 포커스 해제 계획서

---

## 1. Problem Summary (문제 요약)
- **현상**: 객체 검색 필터(`TxtSearch` TextBox)에 포커스가 잡혀 클릭된 상태(Focus Glow 및 커서 깜빡임 활성화)에서, 다른 빈 바탕(Border, Grid 등)이나 비포커스 영역을 클릭해도 포커스가 TextBox에 계속 잔존하여 클릭 상태가 해제되지 않고 유지됨.
- **원인**: WPF의 기본 포커스 시스템 상, 마우스 클릭 대상이 포커스를 가질 수 없는 요소(`Focusable="False"`인 Grid, Border, TextBlock 등)인 경우 키보드 포커스가 다른 곳으로 넘어가지 않고 기존에 활성화되어 있던 `TextBox`에 계속 묶여있게 됨.

## Design Summary (설계 요약)
- **해결 방안 (전역 마우스 프리뷰 이벤트를 통한 정밀 포커스 해제)**:
  - 윈도우 레벨에서 마우스 왼쪽 클릭을 감지하는 `PreviewMouseLeftButtonDown` 이벤트를 가로챕니다.
  - 마우스 클릭이 발생한 지점의 Visual Tree 상위 노드들을 탐색하여, 클릭 대상이 `TextBox` 내부 영역인지 판별합니다.
  - 클릭 대상이 `TextBox`가 아닐 경우, `Keyboard.ClearFocus()`를 호출하여 검색 필터의 포커스를 완전히 차단하고, 최상위 윈도우 콘텐츠 Border에 포커스를 주어 포커스 글로우 테두리와 커서를 확실하게 삭제합니다.
  - 이 방식을 사용하면 다른 입력 컨트롤의 고유 동작(버튼 클릭, 리스트박스 선택 등)은 방해하지 않으면서, 빈 바탕을 클릭할 때에만 자연스럽고 확실하게 검색 필터 포커스가 풀리게 됩니다.

## 3. Implementation Plan (구현 계획)
- **대상 파일**:
  1. `d:\CostBim\Views\MainWindow.xaml` (최상위 Window 태그에 PreviewMouseLeftButtonDown 이벤트 연결 및 Focusable="True" 지정)
  2. `d:\CostBim\Views\MainWindow.xaml.cs` (이벤트 핸들러 구현)
- **세부 태스크**:
  1. `MainWindow.xaml`에서 최상위 Window 속성에 `Focusable="True"` 지정.
  2. 최상위 `Border`에 `Focusable="True"` 설정.
  3. `MainWindow.xaml`의 Window 태그에 `PreviewMouseLeftButtonDown="Window_PreviewMouseLeftButtonDown"` 추가.
  4. `MainWindow.xaml.cs`에서 `Window_PreviewMouseLeftButtonDown` 핸들러 메서드 구현 (Visual Tree를 타고 올라가며 TextBox 여부를 엄격히 감별하여 TextBox가 아니면 `Keyboard.ClearFocus()` 및 최상위 요소 포커싱 수행).
  5. 로컬 테스트 빌드 가동 (`dotnet build`)
  6. Revit 2026 애드인 배포 실행 (`install_addin.ps1`)

## 4. Implementation (구현 상세)
- **MainWindow.xaml 수정안**:
  ```xml
  <Window x:Class="CostBIM.Views.MainWindow"
          ...
          Focusable="True"
          PreviewMouseLeftButtonDown="Window_PreviewMouseLeftButtonDown"
          ...>
  ```
  ```xml
  <Border BorderBrush="#CBD5E1" BorderThickness="1" ClipToBounds="True" Focusable="True">
  ```

- **MainWindow.xaml.cs 수정안**:
  ```csharp
  private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
      // 1) 클릭된 근원 요소(OriginalSource)가 TextBox 내부에 속해 있는지 Visual Tree 추적
      var dep = e.OriginalSource as DependencyObject;
      while (dep != null && !(dep is TextBox))
      {
          dep = VisualTreeHelper.GetParent(dep);
      }

      // 2) 클릭된 위치가 TextBox 내부가 아닌 경우
      if (dep == null)
      {
          // 현재 포커스를 보유하고 있는 요소가 TextBox인 경우 강제 해제
          if (Keyboard.FocusedElement is TextBox)
          {
              Keyboard.ClearFocus();
              // 최상위 윈도우 콘텐츠에 포커스를 주어 포커스를 확실히 TextBox 밖으로 추출
              (this.Content as UIElement)?.Focus();
          }
      }
  }
  ```

## 5. Testing (테스트 검증 계획)
- **빌드 테스트**: `dotnet build d:\CostBim\CostBIM.csproj -c Debug` 명령어를 가동하여 문법 및 참조 무결성 검증.
- **포커스 해제 테스트**:
  - 검색 필터(`TxtSearch`)를 클릭하여 커서를 활성화한 후, 윈도우 상단 타이틀바, 좌측 사이드바 여백, 우측 본문 표 여백, 윈도우 하단 여백 등을 클릭할 때 테두리 글로우 효과가 즉각 사라지고 포커스가 성공적으로 풀리는지 검증.
  - 검색 필터를 클릭한 상태에서 다른 버튼(실행 버튼 등)을 누르거나 리스트박스 아이템을 클릭할 때, 포커스가 풀림과 동시에 해당 버튼의 고유 기능이 정상 작동하는지 체크 (Preview 마우스 방해 최소화 검증).

## 6. Behavior Summary (동작 요약)
- 변경 전: 검색 필터를 클릭하여 검색을 마친 후에도 다른 바탕을 클릭했을 때 입력 활성화 상태(파란 테두리 및 커서)가 영구히 남아있어 거슬림.
- 변경 후: 검색 완료 후 창 내부의 임의의 빈 공간이나 다른 요소를 터치하면 활성화되었던 테두리가 부드럽게 지워지며 깔끔하게 취소됨.

## 7. Self Code Review (자체 코드 리뷰)
- **우수성**: `Keyboard.ClearFocus()` 뿐만 아니라 `(this.Content as UIElement)?.Focus()`를 연달아 호출함으로써, 포커스가 낙동강 오리알이 되어 겉도는 것을 막고 윈도우 루트로 포커스를 완벽히 정착시켜 WPF Focus State를 가장 안전하게 정리함.
- **안정성**: `TextBox`가 아닌 경우에만 가동하게 유도하여 TextBox 내부에서 텍스트를 선택하거나 커서를 조작하는 마우스 클릭 동작은 일절 간섭하지 않음.
