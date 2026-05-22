# 카테고리 확대 축소(트리 세부 필터) 기능 제거 및 일반 매개변수 롤백 계획서

## 1) Problem Summary (핵심 문제 요약)
- 사용자가 카테고리의 "확대 축소(트리 구조, 세부 항목 필터)" 기능을 전면 배제하고, 기능 도입 이전의 단순 매개변수 체크박스 형태로 완전히 되돌릴 것을 요청함.
- 이에 따라 상단 전용 필터 카드와 하위 세부 카테고리 렌더링 요소를 제거하고, 카테고리를 Properties 탭 내의 일반 파라미터와 동등하게 복원함.

## 2) Design Summary (설계 요약)
- **목적**: `ParameterItem` 내의 `SubItems`, `IsExpanded` 등의 트리 계층 모델을 제거하고, XAML의 `＋`/`－` 미니 토글 버튼 및 들여쓰기 템플릿을 걷어내어 심플한 단일 체크박스 리스트로 복원.
- **입출력**:
  - 입력: Revit 스캔 매개변수 리스트.
  - 출력: Properties/Project/Shared 탭 내에 일반 체크박스로 배치된 "카테고리" 매개변수 및 일반 그리드 열 연동.
- **예외 처리**: 기존의 서브 카테고리 필터링(`BuildSubCategoryFilters`, `ApplyFilter` 내의 서브 카테고리 필터) 로직을 모두 걷어내어 텍스트 검색어로만 필터링하도록 롤백.

## 3) Implementation Plan (구현 계획)
- **Task 1: `MainWindow.xaml` UI 복원**
  - 사이드바 상단의 `<!-- 🌟 [NEW] 트리형 미니멀 카테고리 전용 필터 카드 -->` 영역 완전 삭제.
  - `ParameterItemTemplate`에서 토글 버튼(`ToggleButton`) 및 하위 `ItemsControl` 제거하여 순수 CheckBox 1개만 렌더링되도록 롤백.
  - `MiniToggleButtonStyle` 등 미니 토글 관련 스타일 리소스 삭제.
- **Task 2: `MainWindow.xaml.cs` 데이터 모델 및 비즈니스 로직 롤백**
  - `SubCategoryItem` 클래스 및 `ParameterItem` 내의 `SubItems`, `IsExpanded`, `HasSubItems` 속성 제거.
  - `CategoryParam` 단독 프로퍼티 및 `this.DataContext = this;` 설정 제거.
  - `LoadScannedParameters`에서 "카테고리" 가상 매개변수를 `BuiltInParams` 최상단에 강제 주입하던 코드를 복구하되, 세부 항목이 없는 심플한 일반 매개변수 객체로 복원하거나 스키마에 포함된 순수 객체로 롤백. (사용자가 "왜 기본정보로 따로 나와있어"라고 한 부분도 단순화되므로, Properties 내의 가상 매개변수 생성부를 완전히 걷어내고 일반 스키마 순회로 일원화).
  - `BuildSubCategoryFilters` 메서드 완전 제거.
  - `ApplyFilter`에서 `unselectedCategories`를 수집하여 솎아내던 서브 필터링 연산 완전 제거.
  - `GetActiveCustomParameters`에서 `CategoryParam` 참조부를 걷어내고, 순수하게 3개 파라미터 컬렉션의 선택 상태만 합산하도록 롤백.
  - `GridElements_ColumnReordered`에서 "카테고리"를 특별 처리하지 않고, 일반 컬럼들과 동일하게 3개 파라미터 컬렉션에서 검색하여 체크 해제하도록 롤백.
- **Task 3: 빌드 및 정상 작동 검증**
  - `dotnet build`를 통한 컴파일 안정성 검증.
  - `install_addin.ps1`을 가동하여 로컬 환경 배포 완료.
