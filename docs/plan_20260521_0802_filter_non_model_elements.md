# Lead Engineer Task Plan (v2.0)
**날짜**: 2026년 05월 21일 08시 02분  
**작업명**: 비물리적/참조용 요소 배제를 위한 하이브리드 필터링 엔진 구현

---

## 1) Problem Summary (핵심 문제 요약)
- Revit 3D 뷰 추출 시 단면상자, 센터라인(중심선), 참조 평면, 그리드, 레벨 등 적산 및 물량 산출에 전혀 관여하지 않는 비물리적/참조용 요소가 함께 집계되어 데이터 정합성을 해치는 문제를 방지한다.
- 1차원적인 카테고리 문자열 비교에 그치지 않고, 객체의 **3D 실체(Solid) 보유 여부 및 유효 체적 검증**을 수행하는 고성능 하이브리드 필터링 엔진을 구현한다.

## 2) Design Summary (설계 요약)
- **목적**: 
  - 산출 대상이 아닌 노이즈 데이터를 원천 차단하여 순수한 견적용 실물 모델만 DataGrid에 표출되도록 정화한다.
- **입출력**:
  - 입력: Revit Active 3D View의 가시적인 모든 객체
  - 출력: 하이브리드 필터를 거쳐 정제된 `List<ExtractedElement>`
- **예외 처리**:
  - 패밀리 인스턴스는 Geometry 정보가 직접 획득되지 않고 `GeometryInstance` 래퍼로 제공되므로, 래퍼 내부를 재귀 순회하여 실제 심볼 지오메트리까지 완벽히 분석한다.
  - 기하 분석 시 오류가 나는 극소수의 경우 데이터를 안전하게 지키기 위해 예외처리(Safe Fence)를 결합한다.

## 3) Implementation Plan (구현 상세 계획)
- **1단계: 지오메트리 검증 헬퍼 구현**
  - [Services/RevitElementExtractor.cs](file:///d:/CostBim/Services/RevitElementExtractor.cs) 클래스 하단에 `HasValidPhysicalGeometry(Element elem)` 및 `CheckGeometryForValidSolid(IEnumerable<GeometryObject> geomObjects)` 헬퍼 메서드를 추가하여 단일 책임 원칙(SRP)을 완성한다.
- **2단계: 추출 루프 내 2중 필터 결합**
  - [Services/RevitElementExtractor.cs](file:///d:/CostBim/Services/RevitElementExtractor.cs)의 `ExtractVisibleElements` 메서드 내부에서 아래 2중 필터를 차례로 실행한다:
    - 1차: 블랙리스트 키워드 필터 (뷰, 카메라, 단면, 참조, 그리드, 레벨, 센터라인, analytical 등)
    - 2차: `HasValidPhysicalGeometry`를 통한 3D 실체(Solid) 보유 여부 및 체적 정밀 검증
- **3단계: 컴파일 및 배포 검증**
  - `dotnet build`를 실행하여 0 에러를 달성하고, `install_addin.ps1`을 통해 로컬 배포를 완료한다.

## 4) Self Code Review (리스크 식별 및 대책)
- **[Assumption]**: 지오메트리가 복잡하여 분석 시간이 늘어날 수 있다.
- **[Risk]**: 매 요소마다 지오메트리를 계산하면 대규모 도면에서 3D 뷰 추출 성능이 저하될 수 있다.
- **[Fallback]**: `DetailLevel`을 `Medium`으로 제한하고 `ComputeReferences = false` 설정을 부여하여 계산 렉을 완전히 소거함으로써 실시간성을 유지한다. 또한 1차 텍스트 블랙리스트 필터를 선제 가동하여, 불필요한 객체는 아예 지오메트리 계산조체 거치지 않고 곧바로 얼리 엑시트(Early Exit)하도록 유도한다.
