# CostBIM 프리미엄 비주얼 정밀 튜닝 및 창 최대화 버그 해결 계획서 (v2.0)

본 계획서는 마스터 룰(Lead Engineer Master Rules v2.1)에 의거하여, 사용자가 제시한 추가 피드백(리스트-매개변수 간 실선 구분선 정상화, 탭의 좌측 정렬 정속 배치 및 이미지 매칭 가로 너비 확장, 창 최대화 시 작업 표시줄 덮음 방지)을 정교하게 해결하기 위한 XAML 및 C# 비하인드 코드 튜닝 설계도입니다.

---

## 1) Problem Summary (핵심 문제 요약)
1. **리스트-추출 매개변수 세로 구분선 증발 현상**: 세로 구분선이 있는 Column 1의 너비를 `1px`로 잡은 상태에서 내부 Border에 `Margin="12,0,12,0"`을 주어, 가로폭이 음수가 됨으로써 화면에서 세로 구분선이 완전히 사라진 현상 해결 필요.
2. **매개변수 설정 탭의 전체 강제 늘어남(Stretch)**: 탭이 화면을 3등분하여 강제로 채우고 있는데, 이미지와 같이 자연스럽게 좌측 정렬되고 탭 헤더당 가로 너비만 넉넉한 비율(패딩 확장)을 가지며 하단 구분선과 밑줄이 맞물리도록 변경 필요.
3. **창 최대화 시 윈도우 작업 표시줄 침범**: WPF `WindowStyle="None"` 커스텀 타이틀바 구현의 고질적인 한계로 인해 최대화 시 작업 표시줄을 가려버리는 현상을 Win32 API 후킹을 통해 완벽하게 방지 필요.

---

## 2) Design Summary (디자인 개요 및 주요 모듈)
* **세로 구분선 정상 노출**: Column 1의 너비를 `25`로 확보하고, Border를 `HorizontalAlignment="Center"`로 설정하여 1px 두께의 회색 세로 실선을 물리적으로 온전히 렌더링.
* **좌측 정렬 탭 및 가로 너비 튜닝**: 
  - `TabControl` 내부의 `UniformGrid` ItemsPanel 구성을 **완전히 제거**하여 Stretch 분할을 해제하고 기본 `TabPanel`로 좌측 정렬 정속 배치.
  - `PremiumTabItemStyle`의 `Padding`을 `22,8,22,8`로 대폭 확장하여 각 탭당 풍성하고 널찍한 가로 폭(너비) 확보.
  - 탭 하단 보라색 밑줄과 긴 가로선이 맞물리도록 마진(`Margin="0,0,12,-1"`)을 조정하여 탭 간 간격을 확보하고 이미지와 100% 일치하는 비주얼 완성.
* **작업 표시줄 덮음 방지 (Win32 Hook)**: 
  - `MainWindow.xaml.cs`의 `SourceInitialized` 시점에 창 메시지 루프에 후크를 등록.
  - `WM_GETMINMAXINFO` (0x0024) 메시지를 가로채어 현재 모니터의 `rcWork` (작업 표시줄 영역이 제외된 실제 작업 영역) 정보로 창의 최대 위치와 최대 크기를 동적으로 강제 통제.

---

## 3) Proposed Changes (제안된 변경 내용)

### [Views/MainWindow.xaml](file:///d:/CostBim/Views/MainWindow.xaml)
* **TabItem 스타일 가로 패딩 및 마진 조정**:
  - `PremiumTabItemStyle` (416 라인 부근)에서 `Padding`을 `22,8,22,8`로 변경하여 가로 너비를 이미지처럼 넉넉하게 확장.
  - `HorizontalContentAlignment`를 `Center`로 조정.
  - `Border Margin`을 `0,0,8,-1`로 설정하여 탭 간에 자연스러운 간격을 형성하면서 하단 보라색 바가 탭 가로폭에 맞게 안착하도록 조정.
* **세로 구분선 Column 너비 보정**:
  - 데이터 탭 내부 `Grid.ColumnDefinitions` (890 라인 부근)에서 Column 1의 너비 `Width="1"`을 **`Width="25"`**로 확장하여 구분선 렌더링을 위한 공간 확보.
  - 세로 구분선 Border (983 라인 부근)의 `Margin="12,0,12,0"`을 **제거**하고 **`HorizontalAlignment="Center"`**로 대체하여 1px 실선을 완벽히 복원.
* **매개변수 설정 탭 컨트롤 Stretch 제거**:
  - `ParameterConfigPanel` 내부 `TabControl` (1021 라인 부근) 하위의 `<TabControl.ItemsPanel>` 선언(UniformGrid 부분)을 **완전히 삭제**하여 기본 좌측 정렬 모드로 복구.

### [Views/MainWindow.xaml.cs](file:///d:/CostBim/Views/MainWindow.xaml.cs)
* **WM_GETMINMAXINFO 메시지 후킹 구현**:
  - 생성자에서 `SourceInitialized += MainWindow_SourceInitialized;` 이벤트 핸들러 등록.
  - `MainWindow_SourceInitialized` 내에서 `HwndSource`를 통해 후크 등록.
  - `MonitorFromWindow`, `GetMonitorInfo` Win32 API를 호출하여 다중 모니터 대응 및 작업 표시줄 제외 영역으로 `MINMAXINFO` 구조체 크기 보정 기능 이식.

---

## 4) Verification Plan (검증 계획)
1. **컴파일 빌드 검증**: `dotnet build CostBIM.csproj` 전체 컴파일을 구동하여 에러 0개 무결성 패스 확인.
2. **실물 작동 검증**:
   - 윈도우 최대화 시 작업 표시줄이 아래에 그대로 노출되며 가려지지 않고 정상 작동하는지 확인.
   - 데이터 탭 내의 Built-in, Project, Shared 탭이 꽉 차지 않고 이미지와 동일하게 널찍한 가로 패딩을 가지며 좌측에 나란히 안착하는지 확인.
   - 리스트와 우측 매개변수 패널 사이에 회색 1px 세로 실선이 깔끔하고 분명하게 노출되는지 확인.
