# [구현 계획서] 프리미엄 오버랩 크로스페이드 및 미니멀 Empty State UX (대안 B) 구현 계획

나는 설계 검증, 코드 품질, 운영 안정성을 책임지는 **Lead Engineer Agent**로서, Revit 2026 Add-in의 기동 시 프레임 단절을 극복하는 프리미엄 오버랩 크로스페이드 연출을 반영하고, 사용자가 선택한 **[대안 B: 미니멀 웰컴 Empty State UX]**를 무결하게 구현하기 위한 세부 계획서를 작성한다.

---

## 1. Problem Summary (핵심 문제 요약)
1. **스플래시-메인 시각 프레임 단절**:
   - 스플래시 화면(`SplashWindow`)이 완전히 종료된 후 메인 윈도우(`MainWindow`)가 뜨는 순차적 흐름으로 인해, 두 화면의 연결 지점에서 툭 끊어지는 시각적 부조화가 발생했습니다.
2. **초기 스캔 대기 모달의 불편함**:
   - 앱 구동과 동시에 화면 중앙에 무조건적인 스캔 시작 질문 팝업 카드가 뜨는 강제성 UX로 인해 사용자가 번거로움을 느꼈습니다.

---

## 2. Design Summary (설계 요약)
1. **시각 혁신: 프리미엄 오버랩 크로스페이드 (Overlap Cross-fade)**:
   - `SplashWindow`가 페이드아웃되는 350ms 시작 타이밍과 동시에 `MainWindow`를 활성화하여 자연스러운 오버랩 페이드아웃/슬라이드인 크로스페이드를 연출합니다.
2. **기능 혁신: 대안 B 미니멀 웰컴 Empty State UX**:
   - 기동 시 전면 팝업 오버레이를 과감히 제거하여 맑은 메인 대시보드를 노출합니다.
   - 텅 빈 데이터그리드 상에 예쁜 상자 일러스트와 번개 모양의 `"⚡ 3D 객체 파라미터 스캔 실행"` 가이드 버튼이 장착된 `EmptyStatePanel`을 노출하여 자연스럽게 스캔을 유도합니다.
   - 스캔 데이터가 바인딩되면 `EmptyStatePanel`을 숨기고 데이터그리드를 부드럽게 보여줍니다.

---

## 3. Implementation Plan (세부 구현 단계)

### 3-1) SplashWindow 시각 연출 제어
- **대상 파일**: [SplashWindow.xaml.cs](file:///d:/CostBim/Views/SplashWindow.xaml.cs)
- **변경 사항**: `StartFadeOutSequence()` 내에서 `ActionOnComplete?.Invoke()` 호출 시점을 350ms 페이드아웃 애니메이션 **가동 시작 직전**으로 전격 이동.

### 3-2) MainWindow XAML 레이아웃 튜닝
- **대상 파일**: [MainWindow.xaml](file:///d:/CostBim/Views/MainWindow.xaml)
- **변경 사항**:
  - `GridElements`와 동일한 `Grid.Row="1"` 공간에 미니멀 `EmptyStatePanel` (`Border`) 배치.
  - 내부에 세련된 3D 큐브 Path 아이콘, 안내 메시지, 서브 텍스트, 그리고 `"⚡ 3D 객체 파라미터 스캔 실행"` 유도 버튼(`BtnEmptyStateScan`) 탑재.

### 3-3) MainWindow 코드비하인드 비즈니스 로직 제어
- **대상 파일**: [MainWindow.xaml.cs](file:///d:/CostBim/Views/MainWindow.xaml.cs)
- **변경 사항**:
  - `MainWindow_Loaded`에서 강제 질문 팝업 `ShowAskScan()` 호출 차단, 로딩 오버레이 즉각 숨김 처리.
  - `BtnEmptyStateScan_Click` 이벤트 핸들러 구현: 클릭 시 `ShowLoading(...)` 및 백그라운드 스캔 작업(`_extractEvent.Raise()`) 전개.
  - `ShowLoading()` 내부에서 `EmptyStatePanel`을 즉각적으로 숨기도록 조율.
  - `ApplyFilter()` 내부에서 데이터 건수가 0개이면 `EmptyStatePanel` 노출, 0개보다 크면 숨기도록 실시간 동적 상태 스위칭 적용.

---

## 4. Verification Plan (검증 방안)
1. **컴파일 무결성**: `dotnet build`를 통해 빌드가 완벽히 성공하는지 확인합니다.
2. **런타임 시각 및 UX 흐름 수동 검증**:
   - 스플래시 종료 직전 메인 창이 겹쳐서 자연스럽게 20px 슬라이드인되며 크로스페이드되는 시각 경험 체크.
   - 첫 진입 시 모달 오버레이 없이 맑은 대시보드와 Empty State가 미려하게 노출되는지 체크.
   - 번개 버튼 클릭 시 스피너 로더가 돌고 스캔이 돌며, 스캔 완료 및 확인 완료 시 데이터그리드가 채워지고 Empty State가 깔끔히 제거되는지 연동 확인.
