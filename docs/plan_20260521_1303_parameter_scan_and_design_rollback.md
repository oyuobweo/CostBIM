# [설계 계획서] 표 디자인 원복, 기동 시 자동 스캔 및 전면 로딩 오버레이 개편 아키텍처

---

## 1. Problem Summary (핵심 문제 요약)
1. **표 헤더 디자인 왜곡**: 엑셀 필터 기능 도입 과정에서 컬럼 구분을 통해 깔때기 단추를 배치함에 따라, 텍스트가 좌측으로 쏠리고 가로 정중앙 정렬 대칭이 깨지는 현상 발생. 
2. **필터 버튼의 조기 노출**: 스캔 전 데이터가 아직 표에 출력되지 않은 공백 상태임에도 불구하고, 모든 헤더에 깔때기(필터) 버튼이 노출되어 시각적 정보 과부하 및 어색함 초래.
3. **Properties 탭 명칭 영어 불일치**: 좌측 사이드바 패널의 첫 번째 탭 제목이 `🏢 Properties`로 고정되어 있어 사용자가 요구한 빌드인 영문 명칭(`🏢 Built-in`)과 불일치.
4. **유령 파라미터의 무분별한 스캔**: 현재 3D 뷰의 모든 요소에서 값(Value) 소유 여부와 무관하게 빈 파라미터명까지 전부 수집되어 사이드바 목록에 불필요한 유령 매개변수들이 난립하는 현상.
5. **기동 시의 텅 빈 화면 노출 (추가 요구사항)**: 애드인 아이콘을 누르자마자 텅 빈 표와 사이드바가 노출되어 완성도가 낮아 보임. 처음에 켜자마자 자동으로 백그라운드 분석을 돌려 로딩창(`Parameter 스캔 중...`)이 화면 전체를 덮게 하고, 스캔 완료 후 `확인` 버튼을 눌러야 완성된 본 화면이 우아하게 노출되는 고품격 시퀀스 요구.

---

## 2. Design Summary (설계 요약)
### 목적
- **명품 UX 시나리오 연출**: 애드인 기동 즉시 전체를 가리는 전면 불투명 로딩 스크린을 띄워 자동 스캔을 개시하고, 사용자가 `확인` 버튼을 누르면 정돈된 사이드바와 완성된 표가 동시에 샤라락 공개되는 연출.
- **디자인 복원 및 세련미 향상**: 텍스트의 완벽한 1:1 정중앙 배치 복구 및 깔때기 필터 버튼의 동적/우측 마그네틱 플로팅 배치.
- **물리/유효 데이터 중심 파라미터 가시화**: 실제 모델링된 물리 실체만 엄격하게 검증하여 가용 값이 보장되는 유효 파라미터만 엄선 바인딩.

### 입출력 & 예외 처리
- **입력**: Revit 3D 뷰의 물리 요소 컬렉션 및 매개변수 데이터.
- **출력**: `MainWindow` DataGrid의 복원된 명품 정중앙 헤더 스타일 및 `HasItems` 트리거에 의한 동적 필터 노출.
- **예외 처리**: `param.HasValue` 조건 외에도 `param.Definition` Null 여부를 2중 스캔하여 Revit 내부 크래시 방지.

### 주요 모듈 정의
- **`MainWindow.xaml`**: `PremiumColumnHeaderStyle`의 `Grid` 템플릿 개편 (Overlap Grid 구조), `HasItems` 트리거 설계, Properties 탭 이름 `Built-in` 변경, `LoadingOverlay`를 `Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"` 및 최상위 `Panel.ZIndex="99"`, 완전 불투명 화이트 배경으로 격상.
- **`MainWindow.xaml.cs`**: 창이 처음 로딩될 때(`Loaded` 이벤트) 즉시 자동 스캔을 트리거하는 루틴 탑재 및 완료 확인 버튼 클릭 시 표 데이터 갱신.
- **`RevitElementExtractor.cs`**: `ScanAvailableParameters` 내부에서 유의미한 물리 실체 필터링 로직 동기화 및 `param.HasValue` 검증 로직 탑재.

---

## 3. Implementation Plan (구현 세부 계획)
### Phase 1: WPF UI 및 탭 명칭 개편 (MainWindow.xaml)
- [x] **Properties 탭 이름 변경**: `🏢 Properties` -> `🏢 Built-in`
- [ ] **헤더 Overlap Grid 개편**: `Grid.ColumnDefinitions` 제거, `ContentPresenter`를 Grid 전체 영역에서 `Center` 정렬로 전면 원복, `Button (BtnFilter)`을 `HorizontalAlignment="Right"`로 배치.
- [ ] **HasItems 동적 노출 트리거 탑재**: 
  - `BtnFilter`에 `Visibility="Collapsed"` 기본값 부여.
  - `ControlTemplate.Triggers`에 `DataTrigger Binding="{Binding Path=HasItems, RelativeSource={RelativeSource AncestorType=DataGrid}}" Value="True"` 일 때 `BtnFilter`의 `Visibility`를 `Visible`로 변경하는 트리거 추가.
- [ ] **로딩 오버레이 전면 격상 및 배경 처리**:
  - `LoadingOverlay` 마크업을 `Grid.Column="1"` 내부에서 윈도우 본문 Grid인 `Grid Grid.Row="1"` 직하단으로 이동.
  - `Grid.Column="0" Grid.ColumnSpan="2" Panel.ZIndex="99"` 부여.
  - `Background="#FCFDFE"` 완전 불투명 화이트로 조절하여 백그라운드의 빈 껍데기 요소들을 완벽 차단.

### Phase 2: 기동 즉시 자동 스캔 및 비하인드 로직 개편 (MainWindow.xaml.cs)
- [ ] **Loaded 이벤트 자동 스캔 트리거**:
  - `MainWindow` 생성자 또는 `Loaded` 핸들러에서 `ShowLoading("Parameter 스캔 중...", "3D 뷰의 물리 요소와 가용 매개변수를 수집하고 있습니다.")`를 호출하고 백그라운드 이벤트 `_extractEvent.Raise()`를 즉시 가동.
- [ ] **ShowLoading 다형성 보강**:
  - `ShowLoading(string title, string subTitle)` 형태의 오버로드 구현을 통해 최초 기동 시와 수동 재스캔 시의 텍스트 메시지를 명확히 분기.
- [ ] **완료 확인 클릭 시 메인 오픈 연출**:
  - `BtnConfirmLoading_Click` 동작 시점에 `ApplyFilter()`를 적용하여 비로소 데이터를 표시하게 함과 동시에 전면 로딩창을 걷어내며 본 화면이 화려하게 노출되도록 제어.

### Phase 3: 실유효 파라미터 스캔 최적화 (RevitElementExtractor.cs)
- [ ] **스캔 대상 물리 필터 정밀화**: `ScanAvailableParameters`에 `ExtractVisibleElements`와 100% 동일한 비물리 블랙리스트 필터링 및 `HasValidPhysicalGeometry` 지오메트리 Solid 체적/면적 실체 검증 로직 이식.
- [ ] **HasValue 실보유 검증**: 매개변수를 추출하는 인스턴스 파라미터 루프와 타입 파라미터 루프에서 `param.HasValue`가 `true`이고, Definition이 Null이 아닌 객체만 `CategorizeAndAdd`에 전달하도록 필터 제어.

### Phase 4: 컴파일 및 배포 검증
- [ ] PowerShell을 통해 `dotnet build` 수행.
- [ ] `install_addin.ps1` 스크립트를 가동하여 Revit 2026 Addin 폴더에 DLL 핫로드 배포 수행 및 정상 무오류 빌드 확인.

---

## 4. Behavior Summary (입출력 및 동작 요약)
- **입력**: Revit 3D 뷰 뷰포트 물리 실물 객체 및 내장 파라미터 데이터베이스.
- **처리 흐름**: 애드인 기동 -> 전면 차단 로딩 화면 노출 -> 백그라운드 자동 스캔 -> 완료 시 인디고 체크표시 및 "확인" 점등 -> 확인 클릭 -> 전면 로딩판 해제 및 메인 화면(사이드바와 유효 데이터 테이블) 대공개.
- **출력**: 시각적으로 완벽히 1:1 대칭 정중앙 정렬된 DataGrid 헤더와 엑셀 교차 필터 깔때기, 그리고 실유효 파라미터로 무장한 Built-in 탭.
