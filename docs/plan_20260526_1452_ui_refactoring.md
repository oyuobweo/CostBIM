# CostBIM 프리미엄 UI 및 레이아웃 정밀 튜닝 계획서

본 계획서는 마스터 룰(Lead Engineer Master Rules v2.1)에 의거하여, 사용자가 제시한 피드백을 반영하여 WPF UI의 비주얼 품질과 레이아웃 정교함을 극대화하고 프리미엄 SaaS 톤앤매너로 다듬기 위한 정밀 아키텍처 및 XAML 튜닝 설계도입니다.

---

## 1) Problem Summary (핵심 문제 요약)
1. **매개변수 설정 탭(TabControl) 테두리 노출 및 너비 불일치**: `Built-in`, `Project`, `Shared` 탭이 있는 탭 컨트롤에 3D 입체 테두리가 표시되어 이질감을 주고 있으며, 하단 긴 가로선에 딱 맞게 Uniform 너비 결합이 이루어지지 않음.
2. **스캔 버튼의 불필요한 이모지 및 한글 표기**: Empty State 중앙의 스캔 실행 버튼과 로딩 오버레이 내 스캔 버튼에 들어있는 `⚡` 이모지 및 `Path` 데코레이션이 디자인 완성도를 저해하고 있으므로, 이모지를 제거하고 세련된 영문 **`Parameter Scan`**으로 단독 명칭 개편 필요.
3. **탭별 메인 헤더 파편화 및 어긋남**: 데이터 탭(객체 정보 데이터)과 설정 탭(패밀리 명칭 치환 설정) 내부의 대제목 위치가 서로 달라 탭 전환 시 화면이 덜컹거리는 느낌을 줌. 최상단에 공용 메인 헤더를 고정 배치하여 일체화 필요.
4. **사이드바 첫 단추 "데이터" 탭의 상단 정렬 불일치**: 메인 헤더의 가로 중심(27px)과 사이드바 첫 단추의 중심이 수평축으로 완벽히 일치하지 않아 어색한 여백이 발생하며, 사이드바 항목들이 왼쪽으로 치우쳐 정렬되어 보임.

---

## 2) Design Summary (디자인 개요)
* **목적**: WPF Aero 기본 3D 보더 요소를 완벽히 제거하고, 기하학적 정밀 픽셀 칼정렬 및 SaaS 아키텍처 톤앤매너를 확보하여 고도로 정제된 사용자 경험(UX) 제공.
* **입력 및 트리거**:
  - `SidebarMenu.SelectedIndex` (0: 데이터 탭, 1: 설정 탭)
* **주요 UI 컴포넌트 설계**:
  - **공용 메인 헤더(Dynamic Headers)**: 메인 탭컨트롤 바깥 최상단 Row 0에 고정 배치하고, `DataTrigger` 바인딩을 통해 비하인드 코드 없이 0ms 동적으로 아이콘과 텍스트가 갱신되도록 연동.
  - **사이드바 27px 픽셀 칼정렬**: `SidebarPanel` 패딩을 `4,1,4,0`으로 미세 튜닝하고 `ListBoxItem` 높이와 마진을 가로 100% 매칭 및 수평 중심을 정확히 27px로 맞춰 메인 헤더 글자와 수평선 상에 정속 배치.
  - **Uniform 탭 결합**: `PremiumTabControlStyle` 및 `PremiumTabItemStyle` 스타일을 명시적으로 적용하고 3단 `UniformGrid` 구조를 유지하여 테두리 제거 및 하단 가로선에 정속 밀착.

---

## 3) Implementation Plan (세부 구현 태스크)

### [Component 1] 사이드바 픽셀 매칭 및 정중앙 정렬 교정
* `SidebarPanel`의 `Padding`을 `4,1,4,0`으로 보정하여 상단 타이틀바 하단선 기준으로 미세 여백 1px 확보.
* `SidebarMenu`에 `HorizontalAlignment="Stretch"`, `HorizontalContentAlignment="Center"`, `Margin="0"` 추가.
* `ListBoxItem`의 `ItemContainerStyle` 내 `ControlTemplate` 안의 `Border` 내부 `ContentPresenter`를 `HorizontalAlignment="Center"`, `VerticalAlignment="Center"`로 설정하여 치우침 방지.
* `ListBoxItem` 내부의 콘텐츠인 `StackPanel`과 `TextBlock`에 `HorizontalAlignment="Center"`를 명시하여 정중앙 수평 밀착.
* `ListBoxItem` Style의 `Margin`을 `0,0,0,8`, `Padding`을 `4,8`로 재정의하여 전체 가상 사각형의 높이를 정확히 52px로 제어. 이로써 `1px(상단 패딩) + 26px(높이 절반) = 27px` 세로 중심 매칭 성공.

### [Component 2] 우측 메인 영역 Grid 재정의 및 공용 메인 헤더 안착
* 우측 메인 Border의 `Padding`을 `16,0,16,14`로 설정하여 상단 여백을 Grid 내부에서 직접 통제.
* 메인 Grid의 `RowDefinitions`를 4단(`Height="54"`, `Height="1"`, `Height="*"`, `Height="Auto"`)으로 구성.
* **Row 0: 공용 메인 헤더**
  - 보라색 줄 대신 좌측 사이드패널 이모지(아이콘 Path)와 100% 동일한 Vector Path 배치.
  - 크기를 `Width="14"`, `Height="14"` 비율로 정밀 축소.
  - `SidebarMenu.SelectedIndex`에 바인딩된 `DataTrigger`를 추가하여 비하인드 코드 없이 동적으로 아이콘 Path 및 텍스트 갱신.
* **Row 1: 1px 가로 구분선**
  - `<Border Grid.Row="1" BorderBrush="#E2E8F0" BorderThickness="0,0,0,1" HorizontalAlignment="Stretch"/>` 관통 배치.
* **Row 2: 메인 작업 영역**
  - `MainTabControl`의 Grid Row를 `Row="2"`로 안착시키고, 구분선과의 조화를 위해 `Margin="0,12,0,0"` 부여.

### [Component 3] 버튼 개편 및 탭 테두리 차단
* **스캔 실행 버튼 이모지 제거**:
  - `BtnEmptyStateScan` 내부의 Path 및 StackPanel 데코레이션을 걷어내고, 콘텐츠를 오직 `Parameter Scan` 단독 텍스트로 단순화.
  - `LoadingOverlay` 내부 `BtnStartScan`의 텍스트 역시 `Parameter Scan`으로 변경하고, 너비를 `120`으로 넉넉히 확장.
* **매개변수 설정 탭 테두리 제거 및 3단 Uniform화**:
  - `ParameterConfigPanel` 내부 `TabControl`에 `Style="{StaticResource PremiumTabControlStyle}"`를 적용하여 3D 회색 보더 제거.
  - `Built-in`, `Project`, `Shared` 세 개의 `TabItem` 각각에 `Style="{StaticResource PremiumTabItemStyle}"` 직접 적용하여 하단 보라색 가로선에 딱 맞물려 늘어나도록 개선.
  - 탭 헤더의 텍스트에서 불필요한 이모지를 제거하고 심플하게 `Built-in`, `Project`, `Shared`로 리네이밍.

---

## 4) Verification Plan (검증 계획)
1. **컴파일 검증**: `dotnet build CostBIM.csproj`로 컴파일 에러 0개 무결성 패스 확인.
2. **배포 검증**: `powershell -ExecutionPolicy Bypass -File "./install_addin.ps1"` 실행으로 Revit 2026 핫로드 배포 성공 확인.
3. **비주얼 검증**:
   - 사이드바 첫 단추의 중심과 우측 메인 헤더 글자의 중심이 소수점 단위의 오차 없이 세로 27px 상에서 칼같이 수평 정렬되는지 육안 점검.
   - 탭 전환 시 대제목 영역이 덜컹거림 없이 상단 고정 위치를 유지하며 0ms 딜레이로 텍스트와 아이콘이 자동 전환되는지 확인.
   - 탭 컨트롤의 회색 테두리가 제거되고 하단 가로선에 딱 맞추어 3개의 탭이 꽉 들어차는지 확인.
   - 스캔 관련 버튼들이 이모지 없이 미니멀하고 세련된 영문 `Parameter Scan`으로 변경되었는지 확인.
