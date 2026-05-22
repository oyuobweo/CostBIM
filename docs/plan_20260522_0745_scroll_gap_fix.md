# [Plan] Revit Add-in CostBIM UI 스크롤바 헤더 갭 및 마이너 튜닝 계획서

- **작성일**: 2026년 05월 22일 07시 45분
- **작성자**: Lead Engineer Agent (Antigravity)
- **목적**: WPF DataGrid의 우측 상단 스크롤바 영역 헤더 빈 여백(하얀색 갭) 제거 및 UI 완성도 극대화

---

## 1) Problem Summary (문제 요약)
제공해주신 최신 스크린샷(`media__1779403466808.png`)을 분석한 결과, 아래와 같은 시각적 아쉬움과 완벽함의 간극이 식별되었습니다:
1. **헤더 우측 끝 스크롤바 위 영역 흰색 갭(Gap)**: "유형" 컬럼 헤더 우측 끝(세로 스크롤바 바로 위 헤더 라인 구석)이 윈도우 배경색인 흰색(#FFFFFF)으로 뻥 뚫려 있어, 테이블 헤더 라인(#F1F5F9)의 시각적 연속성이 깨지고 어색하게 보입니다. 이는 WPF DataGrid ScrollViewer 내부의 `TopRight` 코너 디자인이 기본 투명/흰색으로 지정되어 발생하는 고질적인 프레임워크 한계입니다.
2. **선택 셀 테두리(Selection Border) & 필터 깔때기 아이콘**: 6행 1열의 선택 영역은 파란색 테두리와 ZIndex가 아주 깔끔하게 반영되었으며 필터의 동작성도 개선되었으나, 수직 스크롤바 코너 여백 문제로 인해 UI의 완성도가 미완성인 느낌을 줍니다.

---

## 2) Design Summary (설계 요약)
- **해결 방안**: DataGrid 내부에 사용되는 `ScrollViewer`의 `ControlTemplate`을 커스텀 정의하여, 스크롤바가 표시될 때 우측 상단 모서리 영역(Grid.Row="0", Grid.Column="2")에 헤더 배경색(#F1F5F9) 및 하단 구분선(#E2E8F0)을 매칭하는 Border 요소를 배치합니다.
- **적용 스킨**: 
  - `Top Right Corner (Headers Right)`: `#F1F5F9` 배경색 + 1px 하단 보더선 `#E2E8F0`
  - `Bottom Right Corner (Scroll Bar Bottom)`: `#F8FAFC` 배경색 (그리드 배경과 완벽 일치)
- **동적 바인딩**: `ComputedVerticalScrollBarVisibility`를 활용하여 세로 스크롤바가 나타날 때만 코너 채우기 영역이 부드럽게 노출되도록 제어합니다.

---

## 3) Implementation Plan (구현 계획)
- **태스크 1**: `d:\CostBim\Views\MainWindow.xaml` 내 `GridElements` DataGrid 리소스 섹션에 커스텀 `ScrollViewer` 스타일 정의 및 템플릿 적용.
- **태스크 2**: clean build 수행 (`dotnet build --force --no-incremental`) 및 배포 스크립트 실행하여 Revit 애드인 강제 업데이트.
- **태스크 3**: 배포된 DLL의 빌드 타임스탬프 및 정상 반영 여부를 엄격하게 재검증.

---

## 4) Verification Plan (검증 계획)
- **수동 검증**: 빌드 성공 확인 후, Revit 애드인 상에서 스크롤바 영역 우측 끝 헤더가 잘 메워져 단절감 없는 일체형 플랫 헤더를 구성하는지 확인 요청.
