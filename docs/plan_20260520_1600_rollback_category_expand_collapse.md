# plan_20260520_1600_rollback_category_expand_collapse.md

## 1) Problem Summary (문제 요약)
- 복잡한 카테고리 확대/축소(하위 서브 카테고리 트리 및 개별 필터) 기능의 도입이 사용자의 실무 요건(수량 무관 항목 일괄 체크 방지 및 단순 매개변수 필터)에 맞지 않아, 이전에 구현했던 관련 UI 및 비즈니스 로직을 완전히 걷어내고 단순 매개변수 체크박스 형태로 100% 롤백(원상 복구)해야 함.
- 현재 `MainWindow.xaml`은 롤백이 완료되었으나, `MainWindow.xaml.cs` 내부에 삭제된 `CategoryParam` 및 `SubItems`를 참조하는 찌꺼기 로직이 남아 컴파일 에러를 유발하는 상태임.

## 2) Design Summary (설계 요약)
- **목적**: `MainWindow.xaml.cs` 내 잔존하는 카테고리 트리 구조 및 가상 필터 관련 코드를 완벽히 제거하여 빌드 성공 상태(컴파일 에러 0개)를 확보하고, 순수 텍스트 검색어로만 데이터그리드를 필터링하도록 복원함.
- **입출력**:
  - 입력: Revit 3D 뷰에서 스캔된 순수 매개변수 정보 스키마(`ParameterSchema`), 검색 쿼리 텍스트(`TxtSearch`).
  - 출력: 선택된 순수 매개변수 컬럼만 표현된 DataGrid, 텍스트 검색 결과에 맞춰 O(1)에 가깝게 리스트 정렬 및 필터링된 데이터그리드 뷰.
- **예외 처리**: 데이터 롤백 과정에서 `CategoryParam`을 찾을 수 없는 널 참조 또는 컴파일 에러를 원천 배제.
- **주요 모듈 정의**:
  - `GetActiveCustomParameters()`: 기존 `CategoryParam` 체크 확인 조건문 완전 삭제.
  - `UpdateElementsList()`: `BuildSubCategoryFilters()` 호출부 삭제.
  - `BuildSubCategoryFilters()`: 해당 메서드 본체 완전 삭제.
  - `ApplyFilter()`: `CategoryParam.SubItems`를 돌며 제외 카테고리를 식별하던 필터링 단계를 삭제하고, 검색 텍스트 필터링으로만 단순 복원.
  - `GridElements_ColumnReordered()`: 드래그아웃된 컬럼명이 "카테고리"일 때 개별 변수를 해제하려던 분기 제거.

## 3) Implementation Plan (구현 계획)
- **Search-First**: `MainWindow.xaml.cs` 내의 타겟 패턴 및 라인 범위를 확인하여 `replace_file_content` 또는 `multi_replace_file_content`로 비파괴적이고 정밀하게 코드 수정.
- **SRP 준수**: 각 모듈의 불필요한 부가 기능을 걷어내고 단일 책임(예: 순수 컬럼 빌드, 순수 텍스트 필터링 등)에 충실하도록 정돈.
- **TDD / 빌드 검증**: `dotnet build` 명령어를 수행하여 빌드 무오류 상태 재확인.
- **배포**: `install_addin.ps1`을 실행하여 빌드된 DLL을 Revit Addins 디렉터리에 적용 및 반영.

## 4) Testing (검증 계획)
- `dotnet build`를 통한 컴파일 에러 무결성 입증.
- `install_addin.ps1` 실행 완료 여부 확인.

## 5) Self Code Review (자가 코드 리뷰)
- 잔존 코드 분석 시 혹시 누락된 `CategoryParam`이나 `SubCategoryItem` 참조가 없는지 정밀 체크.
