# Lead Engineer 공식 설계 계획서 (v1.0)
---
> **작업명**: WPF UI 리스트 행 색상 단일화 및 모던 중앙 목업 로딩 애니메이션 구현
> **작성일시**: 2026-05-21 08:12
> **담당자**: Lead Engineer Agent (Antigravity)
---

## 1. Problem Summary (문제 정의)
- 사용자가 데이터그리드(GridElements)의 행 교차 색상(줄무늬)이 달라 시각적으로 번잡하다고 느낌. 행 색상을 단일 색상으로 통일 필요.
- 객체 추출이 완료되었을 때 하단 상태 표시줄에 뜨는 "3d뷰 객체 추출 완료!" 문구를 사용자가 불편해하여 제거 요구.
- 실물 객체 수량 추출 작업 시 시각적인 반응성이 부족하므로, 중앙에 콤팩트한 카드 형태(목업창)의 로딩 안내 UI 및 Indigo 회전 스피너 애니메이션을 띄우고, 로딩 완료 시 자동으로 리스트 데이터를 정갈하게 렌더링하는 UX 개선 필요.

## 2. Design Summary (설계 요약)
- **목적**: 데이터그리드 행 색상 통일 및 수집 로딩 시 부드럽고 역동적인 로딩 상태 표출과 자동 화면 전환.
- **입출력 및 예외 처리**:
  - **입력**: "실행" 버튼 클릭 이벤트 및 Revit 추출 완료 비동기 콜백.
  - **출력**: 중앙 오버레이 카드 UI 활성화 -> 추출 연산 -> 오버레이 비활성화 및 리스트 자동 렌더링.
  - **예외 처리**: 추출 작업 도중 Revit 엔진 오류가 발생하더라도, `finally` 또는 안전 Catch 블록을 통해 로딩 오버레이를 반드시 닫고(`HideLoading()`), 실행 버튼을 원상 복구하여 UI가 먹통이 되는 문제를 원천 차단함.
- **주요 모듈**:
  - `MainWindow.xaml`: `LoadingOverlay` 및 360도 회전 스토리보드 스피너 정의.
  - `MainWindow.xaml.cs`: `ShowLoading()`, `HideLoading()` 생명주기 제어 및 라이트 테마 교차 색상 통일.
  - `ExtractEvent.cs`: 완료 문구 제거 및 예외 상황 강제 로딩 해제 처리.

## 3. Implementation Plan (구현 세부 계획)
1. **[UI/UX]** `MainWindow.xaml`에서 `AlternatingRowBackground`를 `#FFFFFF`로 통일하고, 우측 메인 영역에 반투명 회색 배경(`D8FFFFFF`) 및 카드 보더, `RotateTransform`과 `DoubleAnimation` 기반 회전 스피너를 정의한 `LoadingOverlay` 배치.
2. **[Code-Behind]** `MainWindow.xaml.cs`에서 `ShowLoading`과 `HideLoading` 스레드-세이프 헬퍼 함수를 구현하고, 테마 변경 시에도 행 배경이 통일되도록 `GridElements.AlternatingRowBackground` 브러시 조정.
3. **[Event Link]** `BtnExtract_Click` 및 `UpdateElementsList`에 각각 로딩 시작/종료 기능을 주입하고, 기존 완료 메시지를 출력하던 `SetStatus` 호출 제거.
4. **[Safety Check]** `ExtractEvent.cs`의 `Execute` 내부 로직이 끝날 때와 예외 발생 시 `HideLoading`이 반드시 호출되도록 구성.

## 4. Expected Behavior (동작 시나리오)
- **로딩 전**: 표의 행 색상이 통일된 단일 색상으로 출력됨.
- **로딩 중**: 실행 클릭 시 메인 표 영역 위에 반투명 오버레이와 함께 중앙 카드 형태의 로딩 스피너가 회전함. 사용자는 중복 클릭이 차단됨.
- **로딩 후**: 별도 팝업이나 상단 완료 메시지 없이, 자동으로 로딩 오버레이가 사라지고 깨끗하게 갱신된 리스트가 표출됨.
