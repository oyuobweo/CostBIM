# [Plan] 파라미터 선택창 우측 테두리 선 누락 버그 해결 계획서

---

## 1. Problem Summary (문제 요약)
- **현상**: Revit 'CostBIM' UI의 파라미터 선택창(좌측 사이드바 `SidebarPanel`)의 우측 테두리(Border)가 화면상에서 잘려 보이지 않는 현상 발생.
- **원인**: 
  - `MainWindow.xaml`에서 사이드바가 위치한 `Grid.ColumnDefinition`의 `Width`가 `320`으로 고정되어 있음.
  - 동시에 내부 `SidebarPanel`인 `Border` 컨트롤 역시 `Width="320"`으로 고정된 상태에서 `Margin="0,0,12,0"`(우측 마진 12)이 지정되어 있음.
  - 이로 인해 필요한 총 레이아웃 너비는 `320 + 12 = 332`가 되지만 부모 컬럼 너비가 `320`으로 제한되어 우측 `12px` 영역이 잘리고 우측 테두리 선이 클리핑되어 보이지 않게 됨.

## 2. Design Summary (설계 요약)
- **해결 방안**: 
  - 사이드바 고유 너비인 `320px`와 우측 마진 `12px`가 충돌 없이 렌더링될 수 있도록 레이아웃 너비를 조정함.
  - `Grid.ColumnDefinitions`의 첫 번째 열 너비를 `320`에서 `332`로 변경함 (`<ColumnDefinition Width="332"/>`).
  - `SidebarPanel` (`Border`)의 고정 너비 속성 `Width="320"`을 제거하고 기본값인 `Stretch`로 동작하게 설정함. 
  - 결과적으로 실제 렌더링되는 사이드바의 너비는 `332(열 너비) - 12(우측 마진) = 320`으로 의도된 디자인을 정확히 충족하면서 우측 테두리 선과 내부 스크롤바가 완벽하게 노출됨.

## 3. Implementation Plan (구현 계획)
- **대상 파일**: `d:\CostBim\Views\MainWindow.xaml`
- **세부 태스크**:
  1. `ColumnDefinition` 수정: `Width="320"` -> `Width="332"`
  2. `SidebarPanel` `Border` 속성 수정: `Width="320"` 제거
  3. 로컬 테스트 빌드 가동 (`dotnet build`)
  4. Revit 2026 애드인 배포 실행 (`install_addin.ps1`)

## 4. Implementation (구현 상세)
- **MainWindow.xaml 수정안**:
  ```xml
  <!-- 수정 전 -->
  <Grid.ColumnDefinitions>
      <!-- Column 0: Parameter Settings Sidebar -->
      <ColumnDefinition Width="320"/>
      <!-- Column 1: Main Data Grid & Controls -->
      <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <!-- A) SIDEBAR PANEL: PARAMETER MANAGER & TAB SELECTOR -->
  <Border x:Name="SidebarPanel" Grid.Column="0" Width="320" Visibility="Visible" Margin="0,0,12,0" ...>
  
  <!-- 수정 후 -->
  <Grid.ColumnDefinitions>
      <!-- Column 0: Parameter Settings Sidebar (320px Sidebar Width + 12px Margin) -->
      <ColumnDefinition Width="332"/>
      <!-- Column 1: Main Data Grid & Controls -->
      <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <!-- A) SIDEBAR PANEL: PARAMETER MANAGER & TAB SELECTOR -->
  <Border x:Name="SidebarPanel" Grid.Column="0" Visibility="Visible" Margin="0,0,12,0" ...>
  ```

## 5. Testing (테스트 검증 계획)
- **빌드 테스트**: `dotnet build d:\CostBim\CostBIM.csproj -c Debug` 명령을 사용하여 컴파일 오류 유무 검증.
- **배포 및 확인**: Revit 2026의 UI에서 좌측 파라미터 선택창 우측 테두리(옅은 회색 선 `#E2E8F0`) 및 탭 영역이 정상적으로 표시되는지 확인.

## 6. Behavior Summary (동작 요약)
- 변경 전: 사이드바의 우측 여백 부족으로 테두리가 잘리고 우측 경계선이 묻힘.
- 변경 후: 사이드바의 렌더링 영역이 정상 보장되어 부드러운 현대식 테두리가 선명하게 나타남.

## 7. Self Code Review (자체 코드 리뷰)
- **리스크**: 사이드바의 너비가 320px로 완벽히 일치하여 화면 비율상의 변화가 없으며, 다른 컨트롤에 미치는 영향이 최소화됨.
- **개선점**: WPF 레이아웃 설계 시 고정 `Width` 설정과 `Margin` 간의 레이아웃 충돌(Clipping)을 원천 차단함.
