# [설계 계획서] Revit 2026 BuiltInParameterGroup 타입 로드 크래시 긴급 조치 계획

---

## 1. Problem Summary (문제 요약)
- **현상**: Revit 3D 뷰 실행 시 객체 스캔을 시도할 때 `Could not load type 'Autodesk.Revit.DB.BuiltInParameterGroup'` 예외가 발생하며 애드인이 즉각 크래시(Crash)되는 현상 발생.
- **원인 분석**:
  - Revit 2024부터 `BuiltInParameterGroup`은 Deprecated 되었으며, Revit 2025/2026의 `.NET 8.0` 어셈블리(`RevitAPI.dll`)에서는 이 타입이 **완전히 삭제(Removed)**되었습니다.
  - 비록 `GetParameterGroupName` 메서드 내부의 2023 이하 호환 레이어가 `try-catch` 구문 안에 존재하더라도, JIT(Just-In-Time) 컴파일러는 메서드를 기계어로 번역하는 빌드타임 시점에 코드상에 정적으로 표기된 `BuiltInParameterGroup` 타입의 메타데이터를 어셈블리에서 조회합니다.
  - 이때 타입 자체가 존재하지 않아 JIT 엔진 내부에서 `TypeLoadException`이 유발되어 메서드 진입 조차 차단되고 즉시 런타임 크래시로 연결된 것입니다.

---

## 2. Design Summary (설계 요약)
- **해결 조치**:
  - **정적 컴파일 참조 완전 제거**: 코드에서 `BuiltInParameterGroup`이라는 타입 키워드를 정적으로 선언하고 참조하는 부분을 100% 제거합니다.
  - **동적 리플렉션 호환 레이어 구축**:
    - `Autodesk.Revit.DB.BuiltInParameterGroup` 타입을 문자열 기반 리플렉션(`Assembly.GetType`)을 통해 런타임에 동적으로 꺼내옵니다.
    - 해당 타입이 존재하는 Revit 2023 이하 환경에서만 동적으로 `LabelUtils.GetLabelFor` 메서드를 추출하여 호출합니다.
    - 해당 타입이 삭제된 Revit 2025/2026에서는 로드 블록이 자연스럽게 스킵되어 JIT 컴파일 크래시가 완벽히 소멸되고 상위 그룹화(GroupId / ForgeTypeId) 로직만 안전하게 가동되도록 처리합니다.

---

## 3. Implementation Plan (구현 계획)
- **Step 1: RevitElementExtractor.cs 수정**
  - `GetParameterGroupName` 메서드에서 `BuiltInParameterGroup` 캐스팅이 포함된 호환 블록을 동적 리플렉션 룩업 코드로 전면 개편합니다.
- **Step 2: 프로젝트 빌드 검증**
  - `dotnet build`를 가동하여 오류 0개 무결성을 재검증합니다.
- **Step 3: 핫로드 배포 가동**
  - `install_addin.ps1`을 가동하여 핫로드를 재배포합니다.

---

## 4. Verification Plan (검증 계획)
- **런타임 동작 복구 검사**: Revit 2026 런타임에서 '실행' 버튼 클릭 시 크래시 경고창이 더 이상 발생하지 않고, 실물 객체 수집 및 파라미터 계층형 그룹화 기능이 즉각 안전하게 동작하는지 확인합니다.
