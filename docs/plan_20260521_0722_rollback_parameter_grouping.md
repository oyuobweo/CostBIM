# plan_20260521_0722_rollback_parameter_grouping.md

## 1) Problem Summary (문제 요약)
- **현상**: 좌측 매개변수 리스트(Properties, Project, Shared 탭)에서 그룹별 접고 펼치기를 제공하던 `Expander` 화살표 버튼(`^`/`v`)이 사용자의 실무 검토 요건에 적합하지 않아, 그룹화 도입 이전의 단순하고 직관적인 평평한(Flat) 리스트 상태로의 전면적인 롤백이 필요함.
- **목표**: WPF `MainWindow.xaml` 및 비하인드 코드 `MainWindow.xaml.cs`에서 `GroupStyle` 및 `Expander` 관련 리소스와 정렬/초기화 코드를 완전히 걷어내고 컴파일 에러 0개 상태로 배포 완료하는 것.

## 2) Design Summary (설계 요약)
- **UI (XAML)**:
  - `MainWindow.xaml`에서 `ParameterGroupItemStyle` 리소스 완전히 삭제.
  - 3개 매개변수 ListBox(`LstBuiltInParams`, `LstProjectParams`, `LstSharedParams`) 내부에 주입된 `<ListBox.GroupStyle>` 선언 제거.
  - 그룹화 렌더링 무력화 회피용으로 설정되었던 가상화 해제 속성(`VirtualizingPanel.IsVirtualizing="False"`, `ScrollViewer.CanContentScroll="False"`)을 걷어내어 대용량 스크롤 성능 가상화 원복.
- **비하인드 코드 (C#)**:
  - `LoadScannedParameters()` 내 `InitializeCollectionGrouping()` 호출 제거.
  - `InitializeCollectionGrouping()` 메서드 정의 자체를 완전히 소거하여 컴파일 가벼움 유지.
  - `ApplyParameterSorting()`에서 더 이상 불필요해진 `GroupName` 기준 정렬 구문(`SortDescription`)을 제외하고 `IsChecked` 및 `Name` 정렬만 유지.

## 3) Implementation Plan (구현 계획)
- **Step 1**: `MainWindow.xaml`에서 `ParameterGroupItemStyle` 스타일 태그 및 각 ListBox의 GroupStyle 자식 노드 제거, 가상화 우회 속성 제거.
- **Step 2**: `MainWindow.xaml.cs`에서 `InitializeCollectionGrouping` 관련 로직 및 `ApplyParameterSorting` 내의 `GroupName` 정렬 라인 삭제.
- **Step 3**: `dotnet build`를 실행하여 컴파일 무결성 검증 (오류 0개 목표).
- **Step 4**: `install_addin.ps1`을 가동하여 Revit 애드인 폴더로 어셈블리 핫로드 재배포.

## 4) Verification Plan (검증 계획)
- 컴파일 단계에서 XAML 마크업 오류나 C# 빌드 예외가 전혀 없이 성공(오류 0개)하는지 검사.
- 배포 스크립트 실행 후 DLL 복사 완료 여부 파악.
