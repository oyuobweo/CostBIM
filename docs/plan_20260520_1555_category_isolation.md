# 카테고리 매개변수 물리적 분리 및 상단 독자 필터 카드 바인딩 수정 계획서

## 1) Problem Summary (핵심 문제 요약)
- "카테고리" 매개변수가 좌측 파라미터 Properties 탭의 일반 "기본 정보" 그룹에 포함되어 섞여 나옴으로써 사용자 혼선을 유발함.
- 사이드바 상단에 독립된 전용 카드로 배치된 "카테고리 필터"가 비하인드 코드(`MainWindow.xaml.cs`) 내 `CategoryParam` 프로퍼티의 누락 및 생성자 내 `this.DataContext = this;` 바인딩 부재로 인해 런타임에 제대로 동작하지 않음.

## 2) Design Summary (설계 요약)
- **목적**: "카테고리" 매개변수를 일반 탭 리스트에서 완전히 제거하고, 사이드바 최상단 전용 카드로 단독 격리하여 트리 구조(`＋`/`－` 토글 및 20px 들여쓰기)를 유지하고 O(1) 실시간 필터링을 활성화함.
- **입출력**:
  - 입력: Revit 3D 뷰에서 스캔된 매개변수 세트, 실물 객체 리스트 및 열 드래그아웃 이벤트.
  - 출력: 상단 카테고리 필터 카드 UI 실시간 동기화, `GridElements` 컬럼 구성 및 필터링 동작.
- **주요 모듈**:
  - `MainWindow.CategoryParam` (단독 프로퍼티)
  - `BuildSubCategoryFilters` (서브 카테고리 스캔 및 구성)
  - `ApplyFilter` (미선택된 서브 카테고리 필터링)
  - `GridElements_ColumnReordered` (컬럼 드래그아웃 실시간 연동)
- **예외 처리**: `CategoryParam.SubItems`가 비어있거나 Revit에서 반환된 데이터가 없을 경우 크래시가 나지 않도록 방어 코드 설계.

## 3) Implementation Plan (구현 계획)
- **Task 1: `MainWindow.xaml.cs` 데이터 바인딩 설정**
  - `MainWindow` 생성자 내부에서 `this.DataContext = this;`를 지정하여 XAML의 `{Binding CategoryParam.IsChecked}` 등이 윈도우 프로퍼티와 동기화되도록 조치.
- **Task 2: `CategoryParam` 단독 프로퍼티 선언**
  - `MainWindow.xaml.cs` 클래스 레벨에 `public ParameterItem CategoryParam { get; } = new ParameterItem { Name = "카테고리", IsChecked = false, IsExpanded = true, GroupName = "기본 정보" };` 정의.
- **Task 3: 일반 파라미터 리스트 내 가상 카테고리 등록 제거**
  - `LoadScannedParameters` 시작 지점에서 BuiltInParams에 가상 카테고리 객체를 추가하던 코드 전면 삭제하여 일반 그룹 리스트에서 완전 격리.
- **Task 4: 카테고리 빌드 및 필터 로직 교정**
  - `BuildSubCategoryFilters`가 BuiltInParams/ProjectParams/SharedParams 대신 `CategoryParam` 단일 개체를 표적으로 동작하게 교정.
  - `ApplyFilter`에서 비선택된 카테고리를 탐색할 때 `CategoryParam.SubItems`를 직접 조회하도록 소스 코드 변경.
  - `GetActiveCustomParameters`에서 `CategoryParam.IsChecked` 상태를 쿼리하여 True인 경우 결과 리스트에 포함하도록 변경.
- **Task 5: 열 드래그아웃 해제 로직 연동**
  - `GridElements_ColumnReordered`에서 드래그아웃된 컬럼명이 "카테고리"인 경우, `CategoryParam.IsChecked = false;` 처리 분기 적용 및 UI 리프레시.
- **Task 6: 컴파일 및 배포 검증**
  - `dotnet build`를 실행하여 컴파일 오류 0건을 달성하고, `install_addin.ps1`을 가동하여 최종 수정을 로컬 Revit 환경에 빌드 배포.
