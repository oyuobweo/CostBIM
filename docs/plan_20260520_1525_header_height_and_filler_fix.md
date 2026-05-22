# [Plan] 데이터 그리드 헤더 높이 고정 및 우측 여백 이질감 해결 계획서

---

## 1. Problem Summary (문제 요약)
- **현상 1 (헤더 높이 가변 문제)**: 좌측 사이드바에서 파라미터 선택 시, 글자 수가 길거나 컬럼이 늘어남에 따라 헤더 행의 높이가 수시로 변해 전체적인 UI의 일관성이 손상됨.
- **현상 2 (우측 헤더 빈 공간 이질감)**: 데이터 그리드 맨 우측의 남는 빈 헤더 영역(Filler Column Header)과 실제 동적 생성된 컬럼 헤더 간에 어색한 색상 차이와 경계 틈(Gap)이 보임.
- **원인**:
  - 데이터 그리드 헤더의 높이가 명시적으로 지정되지 않아 내부 텍스트 줄바꿈에 따라 높이가 유동적으로 변함.
  - 동적 컬럼들은 코드 비하인드에서 `PremiumColumnHeaderStyle` (배경 `#F1F5F9`, 테두리 `#E2E8F0`)을 지정받는 반면, 우측 남는 공간을 차지하는 Filler Header는 `MainWindow.xaml`의 `DataGrid.ColumnHeaderStyle` 인라인 스타일 (배경 `#ECEFF1`, 테두리 `#CFD8DC`)을 적용받아 두 영역 간의 스타일 비대칭으로 인해 어색한 단차가 발생함.

## 2. Design Summary (설계 요약)
- **헤더 높이 고정**:
  - 데이터 그리드 컨트롤에 명시적 높이 `ColumnHeaderHeight="32"`를 설정하여 어떤 조건에서도 일정한 32px 높이를 유지하도록 함.
  - 텍스트 자동 줄바꿈 대신 한 줄 표시 및 말줄임표 처리 (`TextWrapping="NoWrap"`, `TextTrimming="CharacterEllipsis"`)를 적용해 고정된 높이 내에서 텍스트가 정돈되게 유도함.
- **우측 빈 헤더 스타일 통일 (이질감 제거)**:
  - `MainWindow.xaml`의 `DataGrid.ColumnHeaderStyle`에 정의된 배경색, 글자색, 테두리 색을 `PremiumColumnHeaderStyle`과 완전히 동일하게 리디자인함.
  - 이를 통해 실제 데이터 컬럼 헤더와 맨 우측의 Filler Header가 물 흐르듯 자연스럽게 이어져 어색한 여백 틈이 완벽히 소거됨.

## 3. Implementation Plan (구현 계획)
- **대상 파일**:
  1. `d:\CostBim\Views\MainWindow.xaml`
  2. `d:\CostBim\Views\MainWindow.xaml.cs` (필요 시 수정)
- **세부 태스크**:
  1. `MainWindow.xaml` 내 `DataGrid` 속성에 `ColumnHeaderHeight="32"` 추가.
  2. `DataGrid.ColumnHeaderStyle` 내부 `TextBlock` 스타일 수정: `TextWrapping="Wrap"` -> `TextWrapping="NoWrap"`, `TextTrimming="CharacterEllipsis"` 추가.
  3. `DataGrid.ColumnHeaderStyle` 내부 스타일 색상(배경 `#F1F5F9`, 테두리 `#E2E8F0`, 전경 `#475569`, 폰트 두께 `SemiBold`)을 `PremiumColumnHeaderStyle`과 완벽히 일치하도록 수정.
  4. 로컬 테스트 빌드 가동 (`dotnet build`)
  5. Revit 2026 애드인 배포 실행 (`install_addin.ps1`)

## 4. Implementation (구현 상세)
- **MainWindow.xaml 수정안**:
  ```xml
  <!-- DataGrid 정의부 수정 -->
  <DataGrid Grid.Row="1" x:Name="GridElements" 
            AutoGenerateColumns="False" 
            CanUserReorderColumns="True"
            CanUserSortColumns="False"
            ColumnHeaderHeight="32" <!-- 헤더 높이 32px로 고정 -->
            ...
  ```
  ```xml
  <!-- DataGrid.ColumnHeaderStyle 수정 -->
  <DataGrid.ColumnHeaderStyle>
      <Style TargetType="DataGridColumnHeader">
          <Style.Resources>
              <Style TargetType="TextBlock">
                  <Setter Property="TextWrapping" Value="NoWrap"/> <!-- 줄바꿈 해제 -->
                  <Setter Property="TextTrimming" Value="CharacterEllipsis"/> <!-- 말줄임표 적용 -->
                  <Setter Property="TextAlignment" Value="Center"/>
              </Style>
          </Style.Resources>
          <Setter Property="Background" Value="#F1F5F9"/> <!-- 색상 통일 (#ECEFF1 -> #F1F5F9) -->
          <Setter Property="Foreground" Value="#475569"/> <!-- 글자색 통일 (#37474F -> #475569) -->
          <Setter Property="FontWeight" Value="SemiBold"/>
          <Setter Property="Padding" Value="10,0"/> <!-- 높이가 고정되었으므로 상하 패딩은 제거하고 좌우 패딩만 유지 -->
          <Setter Property="BorderBrush" Value="#E2E8F0"/> <!-- 테두리색 통일 (#CFD8DC -> #E2E8F0) -->
          <Setter Property="BorderThickness" Value="0,0,1,1"/>
          <Setter Property="HorizontalContentAlignment" Value="Center"/>
          ...
      </Style>
  </DataGrid.ColumnHeaderStyle>
  ```

## 5. Testing (테스트 검증 계획)
- **빌드 테스트**: `dotnet build d:\CostBim\CostBIM.csproj -c Debug` 명령어 가동을 통한 오류 무결성 검증.
- **디자인 검증**:
  - 파라미터를 많이 선택하거나 열 너비를 좁혀도 헤더 높이가 일정하게 `32px`로 완벽 유지되는지 확인.
  - 데이터 그리드 맨 우측 여백과 헤더 사이의 경계가 단차나 색상 불일치 없이 미려하게 이어져 있는지 육안 확인.

## 6. Behavior Summary (동작 요약)
- 변경 전: 파라미터 추가 시 헤더 높이가 멋대로 요동치고, 우측의 Filler Header와 본문 컬럼 헤더 간의 상이한 스타일로 인해 경계 부분이 끊어져 보임.
- 변경 후: 헤더 높이가 일정한 32px로 견고하게 단일화되고, 헤더 전체 영역이 물 흐르듯 유기적으로 통합되어 극도로 모던한 완성도를 보임.

## 7. Self Code Review (자체 코드 리뷰)
- **고민점**: 헤더 텍스트가 줄바꿈 없이 생략(`...`)되더라도 툴팁을 추가하거나 열 리사이징을 제공하므로 사용자 측면의 사용성은 온전히 유지됨.
- **우수성**: WPF DataGrid 특유의 Filler Column 디자인 이질감을 두 스타일 간의 일치로 완벽하게 해결함.
