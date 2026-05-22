# [Plan] 엑셀 스타일 다중 열 교차 필터링 이벤트 핸들러 구현 및 빌드 무결성 검증

이 문서는 WPF DataGrid의 각 열 헤더에 제공되는 엑셀 스타일 깔때기 필터 기능의 이벤트 핸들러 누락 문제를 해결하고, 다중 열 교차(AND) 필터링 시스템을 완전하게 통합하기 위한 설계 및 구현 계획을 다룹니다.

---

## 1) Problem Summary (핵심 문제 요약)
- **증상**: `MainWindow.xaml`에 정의된 엑셀 필터 팝업 및 헤더 필터 버튼의 이벤트 핸들러(`BtnFilter_Click`, `TxtFilterSearch_TextChanged`, `ChkFilterSelectAll_Click`, `FilterItemCheckBox_Click`, `BtnApplyFilter_Click`, `BtnCancelFilter_Click`)가 C# 코드비하인드(`MainWindow.xaml.cs`)에 존재하지 않아 컴파일 오류(CS1061) 발생.
- **원인**: 이전 리팩토링 단계에서 UI 마크업(XAML)과 논리부(C#) 간의 이벤트 핸들러 바인딩 싱크가 맞지 않음.
- **목표**: 누락된 이벤트 핸들러를 SRP(단일 책임 원칙) 및 WPF 성능 최적화에 맞춰 완전 구현하여 빌드 에러를 해결하고, 렉 없는 엑셀 스타일 교차 필터링을 완성함.

---

## 2) Design Summary (설계 요약)
- **목적**: 렉 없는 대용량 데이터 필터링을 위해 단일 공유 팝업(`HeaderFilterPopup`)을 조작하는 논리부 완성.
- **입출력 데이터 흐름**:
  - **입력**: 각 컬럼 헤더의 깔때기 버튼 클릭, 팝업 내 검색 텍스트 입력, 체크박스 조작.
  - **출력**: `_activeFilters` 해시맵에 조건 누적 및 `ApplyFilter()`를 통한 DataGrid ItemSource 교차 LINQ 필터링 반영.
- **주요 구성 모듈**:
  - `BtnFilter_Click`: 클릭된 열에 해당하는 고유값 리스트를 실시간 수집하여 팝업 바인딩 및 배치.
  - `TxtFilterSearch_TextChanged`: WPF `CollectionView` 필터링 기능을 통한 팝업 내 고유값 목록의 실시간 고속 검색.
  - `ChkFilterSelectAll_Click`: '(모두 선택)' 체크박스에 맞춰 팝업 내 항목의 일괄 체크/해제.
  - `BtnApplyFilter_Click`: 선택된 고유값들을 `_activeFilters`에 갱신하고 깔때기 색상(Active Indigo 블루 / Normal 그레이) 동적 전환 후 필터 실행.
  - `BtnCancelFilter_Click`: 팝업 즉시 닫기.
  - `UpdateHeaderFilterVisual`: 비주얼 트리를 순회하여 특정 열의 깔때기 아이콘 색상을 조건 유무에 맞게 업데이트.

---

## 3) Implementation Plan (구현 계획)

### 3.1. 팝업 데이터 공급 및 필터 트리거 (`BtnFilter_Click`)
- 클릭된 버튼의 비주얼 조상 중 `DataGridColumnHeader`를 찾아 바인딩된 `DataGridColumn`을 `_currentFilteringColumn`으로 설정.
- 컬럼 헤더 텍스트(`headerName`)를 기준으로 전체 요소(`_allElements`)에서 중복되지 않는 고유값 목록을 추출.
- 기존에 설정된 필터 조건이 있으면 해당 값들만 `IsChecked = true`로, 없으면 모두 `IsChecked = true`로 설정된 `FilterValueItem` 목록 생성.
- `LstFilterItems.ItemsSource`에 리스트 공급 및 `HeaderFilterPopup.PlacementTarget = sender` 설정 후 열기.

### 3.2. 팝업 실시간 검색 및 일괄 선택 (`TxtFilterSearch_TextChanged`, `ChkFilterSelectAll_Click`)
- `CollectionViewSource.GetDefaultView`를 획득하여 실시간 검색어(대소문자 무시)가 포함된 항목만 보이도록 필터링 규칙 정의.
- `ChkFilterSelectAll_Click`에서는 검색어 유무에 상관없이 리스트박스 내부의 모든 아이템들의 `IsChecked`를 일괄 동기화.

### 3.3. 필터 적용 및 비주얼 피드백 (`BtnApplyFilter_Click`, `UpdateHeaderFilterVisual`)
- 적용 버튼 클릭 시 체크 해제된 항목이 있다면(일부만 선택된 상태) `_activeFilters[headerName]`에 활성 고유값 목록 저장 및 깔때기 아이콘 파란색(#6366F1) 활성화.
- 모든 항목이 체크되어 필터링할 필요가 없어진 경우 `_activeFilters.Remove(headerName)` 및 깔때기 색상 기본값(#94A3B8) 환원.
- 데이터그리드 갱신을 위해 `ApplyFilter()` 트리거.
- 비주얼 트리를 순회하여 `DataGridColumnHeader`를 찾고 템플릿 내부의 `BtnFilter` -> `FilterIcon` Path 브러시 색상을 제어하는 헬퍼 메서드 설계.

---

## 4) Verification Plan (검증 계획)
- **자동 빌드**: `dotnet build` 명령어를 활용하여 컴파일 오류가 완전히 해결되었는지 확인.
- **애드인 배포**: 빌드 성공 후 `install_addin.ps1` 스크립트를 파워셸로 실행하여 Revit 애드인 디렉토리에 설치 완료 확인.

---

## 5) Self Code Review (자체 코드 리뷰 사전 대비)
- **대용량 렉 제어**: 필터 아이템 생성 및 수집 시 LINQ 체인을 최소화하고, `CollectionView` 필터를 활용하여 UI 리렌더링 횟수를 원격 차단.
- **WPF 메모리 누수 방지**: 팝업이 닫힐 때 바인딩이나 이벤트 참조가 누수되지 않도록 설계.
