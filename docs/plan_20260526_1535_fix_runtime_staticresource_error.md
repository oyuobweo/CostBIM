# [계획서] CostBIM 런타임 StaticResource 예외 크래시 완치 및 최종 배포 계획서
- **작성일시**: 2026-05-26 15:35
- **담당자**: Lead Engineer Agent

---

## 1) Problem Summary (핵심 문제 요약)
1. **런타임 StaticResource 누락 크래시**: `MainWindow.xaml` L867-869에 배치된 프리셋 제어 버튼 3개(프리셋 가져오기, 프리셋 저장, 초기화)에서 사용하는 `PremiumBorderButtonStyle` 스타일 리소스가 `MainWindow.xaml` 내부 리소스 사전에 정의되어 있지 않아, 런타임에 WPF의 BAML 로더가 윈도우를 로드할 때 `Provide value on 'System.Windows.StaticResourceExtension' threw an exception` 예외와 함께 프로그램이 강제 크래시되는 치명적 결함 발생.
2. **컴파일 정적 분석의 한계**: 해당 오류는 빌드 타임 컴파일러(`dotnet build`) 레벨에서는 정상 컴파일되므로 누락 여부 감지가 어려워, Revit 실구동 테스트 단계에서 비로소 노출됨.
3. **자원 소실 경로 확인**: 이전 버전인 `FamilyMappingWindow.xaml` L73에 해당 명품 스타일 리소스(`PremiumBorderButtonStyle`)가 온전히 보존되어 있으나, 새로운 메인 인터페이스인 `MainWindow.xaml`로 기능들이 통합되는 과정에서 스타일 이식이 유실된 것으로 최종 확인됨.

---

## 2) Design Summary (디자인 개요 및 예외 처리)
* **목적**: `FamilyMappingWindow.xaml`에 잔존하는 `PremiumBorderButtonStyle` 스타일 정의를 정밀 추출하여 `MainWindow.xaml` 자원 사전으로 이식함으로써 런타임 바인딩 예외를 근본적으로 소거함.
* **입출력 정의**:
  - **Input**: `FamilyMappingWindow.xaml` L73-100에 구현된 `PremiumBorderButtonStyle` 스타일 코드 블록.
  - **Output**: `MainWindow.xaml` 내에 정상 탑재되어 런타임 시 오류 없이 렌더링되는 프리셋 3단 버튼 컨트롤.
* **예외 처리 & 방어 코드**:
  - 리소스 키 `PremiumBorderButtonStyle` 오타 검증.
  - 템플릿 내 트리거(`IsMouseOver`, `IsPressed`) 색상 스키마가 다크 차콜 심플 메인 테마와 조화로운 매끄러운 톤앤매너를 유지하도록 확인.
  - 이식 후 `dotnet build`를 통한 사전 정적 빌드 테스트 통과 보장.

---

## 3) Implementation Plan (구현 및 이식 상세 태스크)

### [Task 1] `FamilyMappingWindow.xaml`에서 스타일 소스 추출
- [Views/FamilyMappingWindow.xaml](file:///d:/CostBim/Views/FamilyMappingWindow.xaml#L73-L100)의 `PremiumBorderButtonStyle` 스타일 마크업 텍스트를 정확히 획득함.

### [Task 2] `MainWindow.xaml`에 스타일 이식
- [Views/MainWindow.xaml](file:///d:/CostBim/Views/MainWindow.xaml#L213-L214) 부근 `PremiumExcelButtonStyle` 스타일 정의가 닫히는 바로 아래 지점에 추출한 스타일을 안전하게 삽입함.

### [Task 3] 빌드 및 배포 무결성 검증
- `dotnet build CostBIM.csproj` 명령을 구동하여 정적 컴파일 성공 여부(경고 및 에러 0개)를 확인.
- `install_addin.ps1`을 가동하여 컴파일된 핫로드 DLL을 복사하고 Revit 애드인을 최종 배포 완료함.

---

## 4) Implementation (구현 상세)
- [Assumption / Risk / Fallback]
  - *Assumption*: `PremiumBorderButtonStyle`은 `MainWindow.xaml` L867, L868, L869의 버튼들에만 적용되어 있으므로 해당 스타일 복원 시 모든 예외가 즉시 해제될 것이다.
  - *Risk*: 타 파일에서도 `PremiumBorderButtonStyle`을 정적으로 참조할 가능성이 있으므로, 이식하는 자원 키의 고유성과 일관성을 엄격히 유지해야 한다.
  - *Fallback*: 이식 후에도 리소스 오류가 다른 곳에서 추가 발견될 경우, `DynamicResource`로 우회하거나 로컬 리소스로 인라인 바인딩하여 윈도우 인스턴스 생성을 강제 보장하는 방어 패턴을 전개한다.

---

## 5) Testing (검증 계획)
* **정적 빌드 테스트**: `dotnet build` 정상 여부 확인.
* **런타임 빌드 무결성**: Revit 로딩 및 실행 중 `InitializeComponent` 지점 예외 발생 여부 확인.

---

## 6) Behavior Summary (동작 요약)
- **정상동작**: Revit 내에서 `Parameter Scan` 애드인 명령 실행 시 크래시 없이 고해상도 메인 윈도우가 자연스럽게 기동되며, 프리셋 제어 버튼들이 세련된 라이트 슬레이트 테두리와 둥근 모서리(CornerRadius=6) 효과를 지니고 정상적으로 마우스 호버/클릭 상호작용을 처리함.

---

## 7) Self Code Review (개선안 제안)
- **진단**: WPF에서 공통으로 쓰는 스타일 리소스들이 파일별로 파편화되어 존재함에 따라, 신규 윈도우 생성 시 동일한 명품 스타일을 복제하는 비효율과 누락 위험이 잔존함.
- **개선안**: 추후 리소스 관리 효율화를 위해 `Resources/Styles.xaml`과 같은 통합 리소스 딕셔너리(ResourceDictionary)를 도입하여 `App.xaml` 단에서 병합(MergedDictionaries) 관리할 것을 제안함.
