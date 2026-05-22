# Lead Engineer Task Plan (v1.0)
**날짜**: 2026년 05월 21일 07시 45분  
**작업명**: 카테고리/패밀리/유형 고정 및 코어 물리 파라미터(길이, 면적, 체적, 계통) 삭제

---

## 1) Problem Summary (핵심 문제 요약)
- Revit 3D 뷰 객체 추출 시 자동으로 가공 및 단위 환산 연산을 강제하던 물리 파라미터 4인방(길이, 면적, 체적, 계통)을 소스 코드 및 데이터 모델에서 완전히 걷어내어 성능을 경량화한다.
- DataGrid 표 상에서 객체 식별의 중심이 되는 **3대 기둥(카테고리, 패밀리, 유형)**은 체크와 무관하게 최좌측에 **상시 기본 고정 컬럼**으로 두고, '부재 ID' 및 '작업세트'는 일반 옵션으로 전환하여 체크 시에만 표출되도록 한다.

## 2) Design Summary (설계 요약)
- **목적**: 
  - 코어 물리량 연산 및 수집을 완전 제거하여 3D 뷰 추출 렉(Lag)을 최소화하고, 그리드 내 핵심 메타정보를 상시 표출하여 시각적 직관성을 향상시킨다.
- **입출력**:
  - 입력: Revit 3D 뷰 실물 객체 및 사용자 사이드바 체크 목록
  - 출력: 최좌측 3대 기둥 컬럼(카테고리, 패밀리, 유형) + 체크된 동적 커스텀 파라미터로 조합된 DataGrid
- **예외 처리**:
  - 사용자가 사이드바 목록에서 '카테고리', '패밀리', '유형'을 중복 체크하더라도, 중복 컬럼 생성을 완벽히 스킵하는 필터링을 탑재한다.
- **주요 모듈**:
  - `ExtractedElement` (데이터 모델)
  - `RevitElementExtractor` (추출 서비스)
  - `MainWindow` (DataGrid 바인딩 및 정렬/필터 뷰 모델)

## 3) Implementation Plan (구현 상세 계획)
- **1단계: 데이터 모델 클렌징**
  - [Models/ExtractedElement.cs](file:///d:/CostBim/Models/ExtractedElement.cs)에서 `Length`, `Area`, `Volume`, `SystemType`을 전면 삭제한다.
- **2단계: 추출 비즈니스 로직 경량화**
  - [Services/RevitElementExtractor.cs](file:///d:/CostBim/Services/RevitElementExtractor.cs)에서 4대 물리량 강제 수집 로직(243-277라인)을 제거하고, 기본 메타 데이터만 담은 `ExtractedElement` 객체와 커스텀 파라미터 룩업 로직만 남긴다.
- **3단계: DataGrid 렌더링 리팩토링**
  - [Views/MainWindow.xaml.cs](file:///d:/CostBim/Views/MainWindow.xaml.cs)의 `RebuildDynamicColumns()` 진입부에서 `Category`, `Family`, `Type`에 대응하는 `DataGridTextColumn`을 상시 추가하도록 변경한다.
  - 사이드바 체크 항목(`activeCustoms`) 순회 시, 중복 명칭('카테고리', '패밀리', '유형', 'category', 'family', 'type' 등)을 사전 검사하여 중복 렌더링을 스킵한다.
  - `ApplyFilter()`의 검색 필터 조건식에서 제거된 `x.SystemType` 항목을 삭제한다.
- **4단계: 컴파일 및 배포 검증**
  - `dotnet build`를 실행하여 컴파일 오류가 0개인지 보증하고, `install_addin.ps1`을 실행하여 빌드 및 배포 스크립트 실행이 성공하는지 검증한다.

## 4) Self Code Review (리스크 식별 및 대책)
- **[Assumption]**: 사용자는 카테고리, 패밀리, 유형이 무조건 항상 고정되어 있길 원하며, '부재 ID'는 고정에서 배제되길 원한다.
- **[Risk]**: 삭제된 `SystemType`이 `MainWindow.xaml.cs`의 검색 필터나 다른 로직에 잔재할 경우 컴파일 에러 또는 런타임 NullReferenceException이 발생할 수 있다.
- **[Fallback]**: 사전에 모든 파일에서 `SystemType`, `Length`, `Area`, `Volume` 멤버에 대한 전방위 의존성 분석을 진행하였으며, `MainWindow.xaml.cs` 371라인의 검색 필터링 조건만 유일하게 의존하고 있음을 확인하였다. 이 부분을 완벽히 거둬내어 빌드 무결성을 지킨다.
