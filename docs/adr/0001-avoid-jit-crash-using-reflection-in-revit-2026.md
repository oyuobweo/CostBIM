# ADR 0001: Revit 2026 하위 호환성 레이어의 JIT 컴파일 크래시(TypeLoadException) 방지를 위한 동적 리플렉션 적용

## 1. Context (배경)
Revit 2026(.NET 8.0) 및 Revit 2025 환경에서는 기존에 Deprecated 되었던 `Autodesk.Revit.DB.BuiltInParameterGroup` enum 타입이 `RevitAPI.dll` 어셈블리에서 완전히 삭제되었습니다.
기존의 `RevitElementExtractor.cs` 내 `GetParameterGroupName` 메서드는 Revit 2023 이하 하위 버전을 지원하기 위해 `try-catch` 구문 내부에 `LabelUtils.GetLabelFor((BuiltInParameterGroup)groupObj)`와 같은 하위 호환성 처리 코드를 정적 타입 형태로 포함하고 있었습니다.

그러나 .NET CLR JIT(Just-In-Time) 컴파일러는 메서드가 처음 실행되어 기계어로 컴파일되는 시점에 메서드 본문 내부에 존재하는 모든 정적 타입과 어셈블리 의존성을 검증합니다. 비록 예외 처리 구문(`try-catch`)이 감싸고 있다 하더라도, JIT 컴파일 단계에서는 타입 로드가 실패하기 때문에 런타임에 즉각적으로 `TypeLoadException` 예외가 발생하여 메서드 진입 자체가 차단되고 애드인이 강제 크래시되는 현상이 나타났습니다.

## 2. Decision (결정)
Revit 2026을 포함한 상위 버전에서 JIT 컴파일 시점의 `TypeLoadException` 크래시를 전면 회피하기 위해 다음의 설계를 도입하기로 결정했습니다.

1. **정적 타입 참조의 완전 제거**: 소스코드 전체에서 `BuiltInParameterGroup` 타입 이름을 사용하는 정적 선언 및 캐스팅 식별자를 100% 제거합니다.
2. **동적 리플렉션을 이용한 하위 호환성 룩업**:
   - `typeof(Definition).Assembly.GetType("Autodesk.Revit.DB.BuiltInParameterGroup")` 문자열 기반 타입을 동적으로 로드합니다.
   - 해당 타입이 정상적으로 로드되는 하위 버전(Revit 2023 이하) 환경에서만 `LabelUtils.GetLabelFor` 메서드를 리플렉션으로 바인딩하여 안전하게 실행하도록 구성합니다.
   - 해당 타입이 존재하지 않는 상위 버전(Revit 2025/2026)에서는 타입 룩업 블록이 안전하게 `null`을 반환하고 아무런 예외 없이 넘어가도록(Skip) 하여 JIT 컴파일러가 아무런 방해 없이 메서드를 기계어로 번역할 수 있게 유도합니다.

## 3. Status (상태)
- **Approved** 및 구현 완료.
- `dotnet build`를 통한 빌드 무결성 확인 완료.
- `install_addin.ps1`을 통한 배포 및 Revit 2026 애드인 적용 완료.

## 4. Consequences (결과)
- **장점**:
  - Revit 2026 등 최신 버전에서 실행 시 컴파일러 검증 단계에서의 `TypeLoadException` 런타임 크래시가 완벽하게 해소되었습니다.
  - 리플렉션을 사용하는 블록은 하위 버전 호환 블록이며, Revit 2026/2025 환경에서는 최신 ForgeTypeId/GroupId API를 호출하므로 어떠한 리플렉션 성능 저하 없이 0ms 수준의 최상급 성능이 유지됩니다.
- **단점/주의사항**:
  - 런타임 문자열 룩업을 동반하므로, 만약 추후 어셈블리 내 네임스페이스나 클래스 이름 변경이 있을 경우에 대비해 `try-catch` 안전 장치를 철저히 유지하여 어플리케이션 안정성을 보장해야 합니다.
