# [설계 계획서] Revit CostBIM 좌측 파라미터 계층형 그룹화 (Expander 접고 펼치기) 개편

---

## 1. Problem Summary (문제 요약)
- **현상**: Revit CostBIM 좌측 파라미터 리스트박스가 너무 길고 평평하게(Flat) 표시되어 사용자가 원하는 매개변수를 찾기 어려움.
- **요청 사항**: 파라미터들을 카테고리/그룹(예: "ID 데이터", "치수", "재료 및 마감" 등)별로 분류하고, 이를 접고 펼칠 수 있는 **Expander** 형태의 계층형 UI로 개편 요청.
- **제약 사항**: 기존의 0ms 바인딩 성능 최적화, 알파벳 키보드 검색, 정렬 연동, 윈도우 바탕 클릭 시 선택 해제 등 기존 프리미엄 UX 기능들이 그대로 유지되어야 함.

---

## 2. Design Summary (설계 요약)
- **UI 프레임워크**: WPF (.NET 8-windows, Revit 2026 Target)
- **구현 방식**:
  - WPF `ICollectionView`에 기설정된 `PropertyGroupDescription("GroupName")`을 화면에 렌더링하기 위해 `ListBox`에 `GroupStyle` 및 `GroupItem`의 `ControlTemplate` 적용.
  - `ControlTemplate` 내부에 WPF 기본 `Expander`를 적용하여, 그룹별 접고 펼치기 구현.
- **시각적 프리미엄화 (Premium Design Aesthetics)**:
  - **그룹 헤더 (Expander Header)**: 은은한 인디고 보라색 원 불릿(`Ellipse`), 굵은 카테고리 텍스트(한글), 그리고 해당 그룹에 속한 아이템 수 뱃지(`(N)`)를 표시하여 직관성 극대화.
  - **여백 및 레이아웃**: 하단에 은은한 1px 얇은 구분선 `#E2E8F0`을 탑재하여 그룹 간 경계를 시각적으로 완벽히 구분.
  - **호환성**: 라이트 테마 자동 동기화 시에도 글씨와 배경색이 선명하게 대조되도록 세련된 브러시 자원 할당.

---

## 3. Implementation Plan (구현 계획)
- **Step 1: XAML 공용 스타일 및 GroupStyle 리소스 정의**
  - `MainWindow.xaml`의 `Window.Resources`에 `ParameterGroupStyle` 리소스 추가.
  - `GroupItem`의 `ControlTemplate`을 정의하여 `Expander` 적용.
  - `Expander` 헤더 템플릿과 내부에 들어갈 `ItemsPresenter` 패딩을 세밀히 조정하여 계층 구조를 명확히 함.
- **Step 2: 3개 ListBox 컨트롤에 GroupStyle 바인딩**
  - `LstBuiltInParams`, `LstProjectParams`, `LstSharedParams` 리스트박스에 `<ListBox.GroupStyle>` 항목 주입하여 공용 `ParameterGroupStyle` 공유.
- **Step 3: 빌드 및 무결성 검증**
  - `dotnet build`를 통해 XAML 네임스페이스 오류나 마크업 오류가 없는지 사전 빌드 검증 수행.
- **Step 4: 배포 핫로드 가동 및 마감**
  - `install_addin.ps1`을 가동하여 Addins 폴더에 핫로드 적용 및 아티팩트 문서(`walkthrough.md`, `task.md`)를 최신화.

---

## 4. Verification Plan (검증 계획)
- **자동 빌드 테스트**: `dotnet build` 명령어를 활용하여 컴파일 정상 완료 여부 확인.
- **XAML 파싱 무결성**: WPF 런타임에서 그룹화 정보가 올바르게 바인딩되고 리스트박스 렌더링 시 UI 먹통 현상이나 예외(Crash)가 없는지 검증.
- **수동 UI 검증**:
  - 그룹별 Expander 접기/펼치기가 잘 동작하는지.
  - 체크박스 클릭 및 체크박스 해제(칼럼 드래그아웃 해제 포함) 시 딜레이 없이 0ms 수준으로 즉각 반응하는지.
  - 라이트 테마 연동 시 그룹 헤더 글씨와 배경색이 깨끗하고 눈부심 없이 보이는지.

---

## 5. Risk & Fallback (리스크 및 대체 방안)
- **Risk**: WPF `Expander` 내부의 `ItemsPresenter`를 감싸는 레이아웃에 의해 키보드 포커스 이동이나 알파벳 검색 시 가상화(Virtualization)가 깨져 렉이 발생할 우려가 있음.
- **Fallback**: WPF `VirtualizingStackPanel` 설정을 리스트박스에 명시하여 가상화를 지원하거나, 데이터가 많지 않은 편(각 탭당 수십~수백 개 수준)이므로 가상화 비활성화 상태에서도 성능 저하가 전혀 없는지 프로파일링하고 대처함.
