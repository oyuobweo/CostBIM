## [1.5.0] - 2026-05-22

### Added
- **CostBIM.Standalone.csproj & StandaloneApp.xaml/xaml.cs**:
  - Revit API 의존성이 0% 배제되어 Revit 미설치 환경에서도 단독 가동되는 독립 실행형 데스크톱 어플리케이션 라인을 신규 구축하고 다중 타겟 빌드 체계를 정립하였습니다.
  - 프리미엄 웰컴 스플래시 화면(SplashWindow) 가동, 2.2초 대기 후 대시보드와 부드럽게 겹치며 녹아내리는 오버랩 크로스페이드 연출을 반영하였습니다.
  - 초기 화면 진입 시 사용자 질의 모달 팝업 대신 미니멀한 스캔 대기 가이드와 번개 버튼으로 구성된 **[대안 B: Empty State]** 뷰를 기본 제공합니다.
  - 독립형 '스캔 실행' 클릭 시 1.5초 프로그레스바 시뮬레이션 후 실무 수량 산출 데이터 분석을 재현하는 14개의 고품질 가상 BIM 수량 데이터셋(ExtractedElement)을 그리드에 자동 적재하는 모의 분석 엔진을 이식하였습니다.
  - 오프라인 상에서도 다중 열 엑셀 교차 필터링, 정렬, 엑셀 표 스타일 보존 내보내기(*.xls)가 100% 무결하게 동작하도록 기능을 보존 및 검증하였습니다.

### Fixed
- **MainWindow.xaml.cs & CmdExtract.cs & StandaloneApp.xaml.cs**:
  - `MainWindow` 내부에서 `using Autodesk.Revit.UI;` 등 Revit API 종속 어셈블리를 전면 제거하고 C# 표준 델리게이트 이벤트(`event Action`) 인터페이스로 아키텍처를 전면 격리하였습니다.
  - 기존 Revit 애드인 호스트(`CmdExtract.cs`)가 윈도우 인스턴스를 구독하도록 우아하게 매핑하여 기존 Revit API 배포판(`CostBIM.csproj`) 역시 100% 무결하게 빌드 및 동작을 유지하도록 방어하였습니다.
  - `StandaloneApp.xaml.cs` 및 `CmdExtract.cs` 빌드 단계에서 발생할 수 있는 `ParameterSchema` 네임스페이스 및 `using CostBIM.Services;`에 대한 런타임/컴파일러 종속을 완전 해결하고 청정 컴파일을 완수하였습니다.

## [1.4.0] - 2026-05-22

### Added
- **Views/MainWindow.xaml**:
  - `GridElements` DataGrid 내부에 커스텀 `ScrollViewer` 스타일을 선언하고 `ControlTemplate`을 재정의하여 세로 스크롤바 위의 우측 상단 코너 영역(Top Right Corner)에 헤더 배경색(`#F1F5F9`) 및 하단 구분선(`#E2E8F0`)을 매칭하는 Border 요소를 안착시켜, 세로 스크롤바 작동 시 발생하는 흰색 여백(Corner Header Gap)을 완벽히 밀봉 및 차단하였습니다.
  - 우하단 스크롤바 코너(Bottom Right Corner) 역시 그리드 배경색(`#F8FAFC`)과 동일하게 통일하였습니다.

### Fixed
- **Views/MainWindow.xaml & MainWindow.xaml.cs**:
  - 셀 선택 시 파란색 경계선이 주변 다른 셀이나 행 번호에 의해 가려지거나 잘리는 현상을 막기 위해 `DataGridCell` 스타일 내 `IsSelected` 트리거 시 `Panel.ZIndex="1"` 상승 로직과 `Margin="-1"` 밀착 오버랩 레이아웃을 완벽하게 조합 및 고정하였습니다.
  - 열 헤더의 깔때기 필터 버튼 클릭 시 클릭 이벤트가 부모 열 헤더로 전파되어 불필요하게 열이 정렬/선택되는 현상을 `e.Handled = true` 처리를 통해 완벽 차단, 순수하게 엑셀 스타일의 팝업만 구동되도록 해결하였습니다.
  - 생성된 컬럼 중 마지막 컬럼 폭을 가변(`1*`)으로 설정하여 여백을 채웠으며 빌드 캐시 문제 우회를 위해 청정 빌드를 강제하였습니다.

## [1.3.0] - 2026-05-21

### Added
- **Views/MainWindow.xaml & MainWindow.xaml.cs**:
  - DataGrid에 행 번호(Row Number) 표시를 지원하기 위해 `HeadersVisibility`를 `All`로 설정하고 `LoadingRow` 이벤트와 연동을 보완하였습니다.
  - 행 번호 헤더 영역에 스타일(`RowHeaderStyle`)을 새롭게 정의하여, `#F1F5F9` 플랫 배경과 `#64748B` 세미볼드 텍스트, 그리고 얇은 경계선을 적용해 수려한 UX를 확보하였습니다.
  - 데이터 필터링(`ApplyFilter`) 처리 완료 시 카테고리(ASC) → 패밀리(ASC) → 유형(ASC) 정렬 체계가 데이터 리스트에 즉시 반영되도록 로직을 다듬었습니다.

### Fixed
- **Views/MainWindow.xaml**:
  - DataGridCell의 포커스 및 IsSelected 상태에서 테두리 두께가 변경되어 셀들이 시각적으로 붕 뜨던 문제를 두께를 1px로 평평하게 강제 고정하여 해결하고, 아름다운 인디고 블루 BorderBrush만 적용되도록 연동하였습니다.

## [1.2.0] - 2026-05-20

### Added
- **Views/MainWindow.xaml & MainWindow.xaml.cs (v4.0)**: 사용자 손그림 스케치를 200% 완벽히 이식한 트리형 미니멀 카테고리 필터 시스템을 신설하였습니다.
  - **MiniToggleButtonStyle**: 기존의 크고 둔탁한 Expander 대신 체크박스 우측에 아주 미니멀한 텍스트 기반의 `＋` 및 `－` 기호 교차 토글 버튼 스타일을 장착하였습니다. 호버 시 1.1배 확대 및 밝은 인디고 컬러 트랜지션 마이크로 인터랙션을 반영하였습니다.
  - **ParameterItemTemplate**: 3개의 ListBox(`LstBuiltInParams`, `LstProjectParams`, `LstSharedParams`)가 공통으로 공유하여 중복 코드를 제거하고 UI SRP를 실현하는 고성능 데이터 템플릿을 완성하였습니다.
  - **ItemsControl 트리 뎁스**: 둔탁한 보더 테두리를 배제하고 오직 20px의 미니멀 들여쓰기(`Margin="20,2,2,4"`) 여백만으로 하위 세부 카테고리 체크박스 목록이 유려하게 열리고 닫히는 트리 뷰를 구축하였습니다.
  - **ParameterItem.IsExpanded**: 트리 노드 접기/펼치기 활성화를 위한 속성을 ViewModel에 안착하고, `"카테고리"` 가상 매개변수가 최초 스캔 시 기본으로 펼쳐진 상태(`true`)가 되어 사용자가 즉시 세부 필터를 제어할 수 있도록 편의성을 극대화하였습니다.

### Fixed
- **Views/MainWindow.xaml**: 355라인 `ParameterGroupItemStyle` 리소스 끝부분의 누락되었던 `</Setter>` 닫는 XML 태그를 정상 복원하여 WPF 빌드 실패 오류를 해결하였습니다.

## [1.1.0] - 2026-05-20

### Added
- **Views/MainWindow.xaml & MainWindow.xaml.cs (v3.8)**: 도면 스캔 결과 유무에 무관하게 항상 좌측 "Properties" 탭의 최상단 "기본 정보" 그룹에 `"카테고리"` 가상 매개변수를 상시 등록하여 사용자가 언제나 하위 세부 필터를 제어할 수 있도록 구조를 완성하였습니다.
- **Services/RevitElementExtractor.cs (v3.8)**: 기존의 과도했던 비물리적/비수량적 객체(그리드, 레벨, 선, 센터라인, 스코프박스 등)에 대한 하드코딩 추출 제외 필터를 완벽히 제거/완화하여 사용자가 도면에 가시적인 모든 참조 및 물리 요소들을 표에 정상적으로 적재하고 필터링 제어권을 확보할 수 있게 하였습니다.
- **Views/MainWindow.xaml.cs (v3.8)**: 동적 세부 카테고리 수집(`BuildSubCategoryFilters`) 시 사용자가 이전에 체크 해제한 개별 카테고리 선택 상태를 100% 영구 보존하도록 딕셔너리 연동을 완성하였으며, O(1) 초고속 해시셋 기반 실시간 데이터 필터링(`ApplyFilter`)을 최종 안착시켰습니다.

### Fixed
- **Services/RevitElementExtractor.cs**: `GetParameterGroupName` 메서드 내부에서 Revit 2026 어셈블리에 더 이상 존재하지 않는 `Autodesk.Revit.DB.BuiltInParameterGroup` 에 대한 모든 정적 형식 참조 및 강제 캐스팅을 제거하였습니다. 대신 문자열 리플렉션(`Assembly.GetType`)을 통해 런타임에 동적으로 타입을 검색하고 `LabelUtils.GetLabelFor`를 바인딩하여 호출하도록 호환성 레이어를 개편하였습니다. 이를 통해 Revit 2026 런타임의 JIT 컴파일 단계에서 발생하는 `TypeLoadException` 크래시를 전면 해결하였습니다.
- **Views/MainWindow.xaml**: 좌측 파라미터 3종 ListBox에 대한 WPF 가상화(`VirtualizingPanel.IsVirtualizing="False"`)를 전면 해제하여 계층형 Expander 그룹화 렌더링 시 높이가 0으로 잡혀 화면이 하얗게 차단되던 백화 현상을 해결하였습니다. 또한 `ScrollViewer.CanContentScroll="False"`를 적용해 유려한 픽셀 단위의 부드러운 스크롤 환경을 탑재하였습니다.
- **Views/MainWindow.xaml**: `Expander` 내부 헤더의 Foreground 브러시 바인딩을 리플렉션 역탐색 대신 짙은 슬레이트 색상(`#2D3748`)으로 정적 선언하여 렌더링 중단 예외를 완전히 차단하고, 테마 변경 시 C# 코드 비하인드에서 해당 톤을 일괄 재적용하도록 최적화하였습니다.
