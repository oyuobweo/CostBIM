# [계획서] CostBIM 좌측 사이드바 복원, Empty State 극단적 단순화 및 탭 호버 정렬 보정 계획서
- **작성일시**: 2026-05-26 15:42
- **담당자**: Lead Engineer Agent

---

## 1) Problem Summary (핵심 문제 요약)
1. **좌측 사이드바 증발**: 이전 뷰 통합 빌드 패치 도중 메인 워크스페이스인 `workAreaGrid` 내부의 `ColumnDefinitions`와 좌측 네비게이터 `SidebarPanel`이 유실되어, 메뉴 탭 제어판이 노출되지 않는 심각한 인터페이스 결함 발생.
2. **Empty State 안내 노이즈**: 도면 스캔 전 텅 빈 데이터 뷰인 `EmptyStatePanel`에서 큐브 아이콘 및 다량의 설명 텍스트, 이모지가 포함된 안내 멘트들이 불필요한 시각적 노이즈를 유발하고 있어, 이를 걷어내고 정중앙에 깔끔한 **`Parameter Scan`** 단독 버튼만 제공되어야 함.
3. **탭 호버/선택 경계선 분리 현상**: 우측 매개변수 설정 탭 컨트롤에서 탭 아이템 하단 경계선(선택/호버 시 파란선)과 TabControl 하단 가로 구분선이 4px 갭으로 인해 서로 붕 뜬 채 별도로 노출되어 일체감이 훼손됨.

---

## 2) Design Summary (디자인 개요 및 예외 처리)
* **목적**: 
  - 유실되었던 좌측 110px SaaS 네비게이터 사이드바를 원형 복원하고, 정중앙 정렬 튜닝을 통해 비주얼 기하학을 일치시킴.
  - `EmptyStatePanel`을 단 하나의 명품 `Parameter Scan` 버튼으로 극단적 압축하여 미니멀리즘 실현.
  - 콘텐츠 테두리 마진 조율을 통해 탭 아이템 하단선이 전체 가로 구분선과 오차 없이 일체화되어 겹치도록 정속 밀착.
* **입출력 정의**:
  - **Input**: 사용자의 사이드바 정렬 불만, 이모지 소거 요청, 탭 호버 선 일치화 지적.
  - **Output**: 정밀 픽셀 보정이 적용된 명품 사이드바, Parameter Scan 단독 버튼, 탭 호버 1px 오버랩 무결성 확보.
* **주요 모듈**: `CostBIM.Views.MainWindow` (WPF XAML)

---

## 3) Implementation Plan (세부 구현 태스크)

### [Task 1] `MainWindow.xaml`에 `SidebarItemStyle` 추가
- `ListBoxItem`의 세련된 선택/호버 상태 제어를 위해 `SidebarItemStyle` 리소스를 정의합니다.
- 호버 시 `#F1F5F9` 배경 및 `#E2E8F0` 테두리, 선택 시 `#EEF2FF` 배경 및 `#C7D2FE` 테두리 브러시를 설정하고 정중앙 정렬을 보장합니다.

### [Task 2] 좌측 사이드바 `SidebarPanel` 복원 및 정렬 튜닝
- `workAreaGrid`에 `ColumnDefinition`을 복구(Column 0: 110px, Column 1: *)하고 `TabControl`을 Column 1로 배치합니다.
- `SelectedIndex="{Binding ElementName=SidebarMenu, Path=SelectedIndex, Mode=TwoWay}"` 바인딩을 적용하여 탭 전환을 연동합니다.
- `SidebarPanel`의 패딩을 `4,2`로 미세 튜닝하고 상단 여백을 극도로 압축합니다.
- `ListBoxItem` 내부 콘텐츠(`StackPanel`, `Path`, `TextBlock`)에 수평 중앙 정렬을 명시하여 왼쪽 치우침 문제를 완치합니다.
- `IsSelected` 상태 트리거에 따라 아이콘 Fill 브러시와 텍스트 Foreground가 인디고(`#4F46E5`)와 그레이(`#64748B`)로 0ms 딜레이 실시간 동적 전환되도록 설계합니다.

### [Task 3] `EmptyStatePanel` 극단적 미니멀리즘 개편
- `EmptyStatePanel` 내부의 아이콘 Path 및 설명 TextBlock 요소를 완전히 제거합니다.
- 정중앙에 높이 42px의 세련된 `Parameter Scan` 단독 버튼만 단독 표출되도록 레이아웃을 전면 개편합니다.

### [Task 4] 탭 호버링 파란선과 하단 가로선 정속 밀착
- `PremiumTabControlStyle`의 SelectedContent `Border` 요소에 지정되어 있던 불필요한 `Margin="0,4,0,0"`을 **`Margin="0,0,0,0"`**으로 제거합니다.
- `TabPanel` 영역에 `Panel.ZIndex="1"`을 추가하여 탭 헤더들이 아래 콘텐츠 보더 라인과 정확히 포개져 렌더링되게 설계합니다.

---

## 4) Testing (검증 계획)
* **정적 빌드 검증**: `dotnet build CostBIM.csproj` 전체 컴파일이 오류 0개로 완벽 통과하는지 확인.
* **핫로드 배포**: `install_addin.ps1`을 가동하여 Revit 2026 DLL 재배포 수행.
* **육안 검증**:
  - 좌측 사이드바가 정상 부활하였고, 첫 단추(데이터)의 시작 위치와 우측 헤더가 세로축에서 칼같이 정렬되는지 확인.
  - 메뉴 아이템들이 왼쪽 치우침 없이 정중앙에 깔끔하게 정렬되는지 확인.
  - 도면 스캔 전 화면 한가운데에 오직 `Parameter Scan` 버튼만 미니멀하게 노출되는지 확인.
  - 탭을 호버하거나 선택할 때 나오는 파란색 하단 경계선이 전체 TabControl을 관통하는 하단 회색 실선과 분리되지 않고 딱 달라붙어 일체형으로 작동하는지 확인.
