# [Plan] 좌측 파라미터 리스트박스 선택 하이라이트 소거 계획서

---

## 1. Problem Summary (문제 요약)
- **현상**: 좌측 파라미터 설정 창의 항목(ListBoxItem)에서 체크박스가 아닌 텍스트나 여백 영역을 클릭하면 보라색 선택 하이라이트(배경 및 테두리선)가 켜진 채 유지됨. 다른 빈 곳을 클릭하거나 다른 창을 터치해도 리스트박스의 이 어색한 선택 잔상이 계속 잔존함.
- **원인**: 
  - 리스트박스 아이템은 마우스 클릭 시 `IsSelected="True"` 상태가 됨.
  - XAML에 정의된 `ListBoxItem` 공용 스타일 내의 `IsSelected` 트리거로 인해 보라색 배경(`#106366F1`)과 보라색 테두리(`#306366F1`)가 영구히 활성화됨.
  - 하지만 본 도면 추출대 UI에서 파라미터 선택 활성화 여부는 전적으로 **체크박스의 체크 여부(`IsChecked`)로만 시각화되어야 하며**, 리스트박스의 고유 "선택(Selection)" 하이라이트는 기능적으로 아무런 의미가 없고 시각적 노이즈만 유발할 뿐임.

## 2. Design Summary (설계 요약)
- **해결 방안 (선택 비주얼 투명화)**:
  - `MainWindow.xaml`의 `ListBoxItem` 스타일 리소스 내의 `IsSelected` 트리거 정의를 수정합니다.
  - 아이템이 선택(`IsSelected="True"`)되더라도 배경색(`Background`)과 테두리(`BorderBrush`)를 **투명(`Transparent`)**으로 변경하고 `BorderThickness`를 `0`으로 처리합니다.
  - 이렇게 조치하면 체크박스 바깥 영역을 클릭하더라도 리스트박스의 어색한 보라색 선택 사각형 잔상이 애초에 화면상에 렌더링되지 않으므로 시각적 버그가 완벽하게 차단됩니다.
  - 사용자는 오직 체크박스(`CheckBox`)의 V 표식만으로 직관적인 활성 유무를 판단하게 되며, 마우스 오버(`IsMouseOver`) 시의 고급스러운 미세 반응 효과는 그대로 온전히 유지되어 고품격 피드백을 제공합니다.

## 3. Implementation Plan (구현 계획)
- **대상 파일**: `d:\CostBim\Views\MainWindow.xaml`
- **세부 태스크**:
  1. `MainWindow.xaml` L216-L220 범위의 `ListBoxItem` 스타일 `IsSelected` 트리거 셋업을 투명화 사양으로 변경.
  2. 로컬 테스트 빌드 가동 (`dotnet build`)
  3. Revit 2026 애드인 배포 실행 (`install_addin.ps1`)

## 4. Implementation (구현 상세)
- **MainWindow.xaml 수정안**:
  ```xml
  <!-- 수정 전 -->
  <Trigger Property="IsSelected" Value="True">
      <Setter TargetName="Bd" Property="Background" Value="{DynamicResource ListBoxItemSelectedBackground}"/>
      <Setter TargetName="Bd" Property="BorderBrush" Value="{DynamicResource ListBoxItemSelectedBorder}"/>
      <Setter TargetName="Bd" Property="BorderThickness" Value="1"/>
  </Trigger>
  
  <!-- 수정 후 -->
  <Trigger Property="IsSelected" Value="True">
      <Setter TargetName="Bd" Property="Background" Value="Transparent"/>
      <Setter TargetName="Bd" Property="BorderBrush" Value="Transparent"/>
      <Setter TargetName="Bd" Property="BorderThickness" Value="0"/>
  </Trigger>
  ```

## 5. Testing (테스트 검증 계획)
- **빌드 테스트**: `dotnet build d:\CostBim\CostBIM.csproj -c Debug` 명령어 호출을 통한 오류 여부 검증.
- **시각적 완성도 검증**:
  - 좌측 파라미터 리스트의 텍스트나 여백 공간을 클릭한 후, 다른 빈 공간을 클릭하거나 다른 창을 만졌을 때 보라색 선택 사각형 박스가 전혀 잔존하지 않는지 확인.
  - 마우스를 올렸을 때는 여전히 은은한 호버 스킨 효과가 잘 나타나는지 체크.
  - 체크박스를 활성화/비활성화할 때 테이블 열 생성 본연의 기능이 조금의 오차 없이 정상 작동하는지 전면 체크.

## 6. Behavior Summary (동작 요약)
- 변경 전: 파라미터 텍스트 영역 터치 시 기능과 상관없는 어색한 보라색 선택 테두리가 계속 남아 사용성을 저해함.
- 변경 후: 무의미한 리스트박스 선택 박스를 투명하게 차단하고, 은은한 호버 피드백과 명확한 체크 표시로만 직관적으로 조작이 유도되어 극도의 단정함을 선사함.

## 7. Self Code Review (자체 코드 리뷰)
- **우수성**: 리스트박스의 백그라운드 코드를 굳이 조작하지 않고 XAML 스타일 트리거만의 미세 조율로 사이드 이펙트가 전혀 없으며 무결한 WPF 렌더링 안정성을 달성함.
