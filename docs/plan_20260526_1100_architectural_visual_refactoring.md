# [설계 검증 계획서] 비주얼 고도화 및 아키텍처 리팩토링
- **작성일시**: 2026-05-26 11:00
- **작성자**: Lead Engineer Agent

---

## 1) Problem Summary (핵심 문제 요약)
1. **WPF 최대화 시 작업표시줄 침범**: WindowStyle="None" 설정 시 최대화가 작업표시줄 영역을 가리는 문제를 해결하여 OS 무결성을 유지해야 함.
2. **매개변수 설정 탭을 플로팅 카드화**: 거대한 파라미터 스캔 탭을 "수집 데이터" 탭 내부의 플로팅 팝업 카드 형식으로 은은하게 구현하고, 탭 구조를 2개 탭으로 슬림화하여 직관적 인터페이스 구축.
3. **슬림 큐브 사이드바 고도화**: 사이드바 너비를 110px로 슬림하게 좁히면서도, 아이콘 아래의 텍스트가 전혀 잘리지 않는 미려한 폰트와 마진 설계 구축.
4. **인라인 텍스트 셀 및 실시간 필터링**: 패밀리 맵핑 설정 탭의 "치환할 명칭" TextBox 보더를 완전히 제거하고 활성화 시 보라색 밑선 처리, 그리고 카테고리별 실시간 콤보박스 필터링 연동.

---

## 2) Design Summary (설계 요약)
- **창 최대화**: Win32 모듈 대신 `SystemParameters.WorkArea` 값을 감지하여 최대화 시의 `MaxHeight`, `MaxWidth`를 동적으로 할당하는 안전 장치 코드 추가.
- **슬림 큐브 사이드바**:
  - 너비 `110px` 강제 적용.
  - 리스트박스 아이템 내부 텍스트 `FontSize="10.5"` 및 마진 튜닝, `TextWrapping="NoWrap"`을 적용하여 글자가 절대 잘리지 않도록 안전성 설계.
- **매개변수 플로팅 카드**:
  - 3단 파라미터 목록을 겹치는 `Border` 오버레이 패널(`ParameterConfigPanel`)로 감쌈.
  - ZIndex="10"으로 상위 배치하고, 은은한 드롭섀도우와 `Opacity` 페이드 연동을 구현.
  - 메인 탭 컨트롤을 2단 구조로 병합 (`TabItem 1` 삭제).
- **투명 인라인 에디터**:
  - `GridMapping` 데이터그리드 안의 TextBox 템플릿의 BorderThickness를 `0`으로 처리하고, `Background="Transparent"`.
  - TextBox가 `GotFocus` 될 때 얇은 밑선 보라색 브러시(#6366F1) 테두리가 생성되도록 스타일 템플릿 연동.
- **실시간 그룹 필터링**:
  - `CboCategoryFilter` ComboBox를 맵핑 탭 상단 툴바에 신설.
  - `CollectionView`에 필터 필드를 적용하여 런타임에 카테고리 변경 시 데이터그리드 행을 실시간 정밀 여과.

---

## 3) Implementation Plan (SRP 준수 태스크 분할)
- **Task 1: WPF 작업표시줄 최대화 보완 및 윈도우 크기 고정**: `MainWindow.xaml.cs`의 `BtnMaximize_Click` 핸들러 리팩토링.
- **Task 2: XAML 탭 구조 2단 병합 및 플로팅 카드 구현**:
  - `MainWindow.xaml` 탭 3개를 2개로 축소.
  - 수집 데이터 영역에 `BtnToggleParamPanel` 기획 버튼 및 `ParameterConfigPanel` 플로팅 오버레이 보더 기입.
- **Task 3: 사이드바 110px 슬림화 및 텍스트 짤림 완전 방어**: StackPanel, Margin, TextBlock FontSize 미세 튜닝.
- **Task 4: 투명 인라인 편집 텍스트박스 및 표 그리드선 매치**:
  - `GridMapping`에 Horizontal Grid Line과 `#E2E8F0` 브러시 장착.
  - `RebuildDynamicColumns()` 내 TextBox 팩토리 스타일 튜닝 (BorderThickness=0, Focus 시 보라 밑선).
- **Task 5: 카테고리 필터 콤보박스 추가 및 실시간 매핑 필터 연동**: C# CollectionView Filter 핸들러 연동.
- **Task 6: 빌드 검증 및 Revit 애드인 재배포 테스트**: `dotnet build` 및 `install_addin.ps1` 구동.

---

## 4) Verification Plan (검증 계획)
- **자동 빌드**: `dotnet build CostBIM.csproj` 전체 컴파일 에러 발생 여부 확인.
- **수동 UI 검증**:
  - 최대화 시 작업표시줄 영역 보존 여부 검증.
  - 사이드바 110px 너비에서 "수집 데이터", "설정" 텍스트의 잘림 현상 여부 검증.
  - ⚙️ 버튼 클릭으로 매개변수 플로팅 카드가 자연스럽게 스택되는지 검증.
  - 맵핑 탭에서 카테고리 선택 시 실시간으로 매핑 행이 여과되는지 검증.
