# [설계 계획서] WPF GroupStyle 렌더링 누락 버그 긴급 조치 계획

---

## 1. Problem Summary (문제 요약)
- **현상**: 좌측 파라미터 탭(BuiltIn, Project, Shared) 리스트박스에 계층형 `GroupStyle` 및 `Expander` 템플릿을 도입한 이후, 탭 아래 영역에 파라미터 목록이 전혀 렌더링되지 않는(아예 안 보이는) 백화 현상이 보고됨.
- **원인 분석**:
  1. **WPF 가상화 오작동 (핵심 원인)**: WPF `ListBox`는 대용량 성능 최적화를 위해 기본적으로 가상화 패널(`VirtualizingStackPanel`)을 이용하는데, `GroupStyle`을 적용하면 각 그룹의 크기를 가상화 엔진이 정상 측정하지 못하여 높이를 `0`으로 판단하고 화면에 아무것도 그리지 못하는 고질적인 레이아웃 무력화 버그가 발생함.
  2. **부모 탐색 바인딩 예외**: `Expander` 헤더 내부의 `TextBlock`에서 `RelativeSource` 바인딩을 통해 상위 `ListBox`의 `Foreground`를 실시간 역탐색하는 과정에서, 시각적 트리 초기화 타이밍 문제로 인해 바인딩 에러 예외가 발생하여 레이아웃 갱신 패스가 차단될 가능성이 있음.

---

## 2. Design Summary (설계 요약)
- **해결 조치**:
  1. **가상화 명시적 해제 및 픽셀 스크롤링 활성화**: 3개 `ListBox` 컨트롤에 `VirtualizingPanel.IsVirtualizing="False"` 및 `ScrollViewer.CanContentScroll="False"` 설정을 부여하여 WPF 가상화 측정 실패 버그를 완전히 우회하고 픽셀 단위의 부드러운 프리미엄 스크롤을 구현함. (각 탭당 파라미터는 수십~수백 개 수준으로 가상화를 꺼도 속도 지연이 전혀 발생하지 않음.)
  2. **바인딩 실패 요인 차단**: `Expander` 헤더 텍스트의 `Foreground`를 복잡한 `RelativeSource` 탐색 바인딩 대신 안전하고 명확한 짙은 슬레이트 색상 `#2D3748`로 정적 지정하여 어떠한 예외 유발 요소도 원천 배제함. (이 색상은 라이트 테마 전환 시 C# 비하인드 로직 `ApplyLightTheme`에 의해 자동으로 알맞게 재조정되므로 테마 연동성도 깨지지 않음.)

---

## 3. Implementation Plan (구현 계획)
- **Step 1: XAML GroupStyle 템플릿 수정**
  - `MainWindow.xaml`의 `ParameterGroupItemStyle` 내부 `TextBlock`의 `Foreground` 속성을 `#2D3748`로 안전하게 변경.
- **Step 2: 3개 ListBox 컨트롤에 가상화 해제 속성 주입**
  - `LstBuiltInParams`, `LstProjectParams`, `LstSharedParams` 리스트박스 태그 내부에 `VirtualizingPanel.IsVirtualizing="False"` 및 `ScrollViewer.CanContentScroll="False"` 삽입.
- **Step 3: 빌드 및 핫로드 배포**
  - `dotnet build`를 가동하여 컴파일 무결성을 재검사하고 `install_addin.ps1`을 가동하여 핫로드 가동 완료.

---

## 4. Verification Plan (검증 계획)
- **UI 정상 렌더링 여부**: Revit 런타임 상에서 좌측 파라미터 탭 내부가 더 이상 백화 현상 없이 올바른 그룹 헤더(인디고 불릿 + 뱃지)와 파라미터 체크박스 목록으로 복원되는지 검증.
- **스크롤 및 조작성 확인**: 픽셀 스크롤 활성화로 상하 스크롤이 걸림 없이 매끄럽게 흐르는지 검사.
