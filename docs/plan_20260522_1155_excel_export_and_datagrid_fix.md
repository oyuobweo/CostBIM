# [Plan & Report] Revit Add-in CostBIM 엑셀 내보내기 & DataGrid 빈 행 제거 및 XAML 복구 계획서

- **작성일**: 2026년 05월 22일 11시 55분
- **작성자**: Lead Engineer Agent (Antigravity)
- **목적**: 손상된 `MainWindow.xaml` XAML 코드의 정교한 복구, DataGrid 가상 행 제거(`CanUserAddRows="False"`), 그리고 엑셀 호환 무오류 CSV 고속 내보내기 버튼의 완결 및 배포.

---

## 1) Problem Summary (핵심 문제 요약)
1. **XAML 파일 손상**: 이전 작업 중 서버 중단으로 인해 `MainWindow.xaml` 파일의 626라인 이하 마감 태그가 소실되고 중복 찌꺼기가 결합되어 컴파일 에러 발생.
2. **DataGrid 가상 빈 행 노출**: WPF DataGrid 하단에 불필요한 입력용 빈 가상 행이 상시 생성되어 1203개 물리 부재 외에 1204번째 가상 행이 지저분하게 노출됨.
3. **엑셀 내보내기 결합**: ClosedXML 등 Revit 환경의 버전 충돌이 없는 순수 C# 고속 CSV 내보내기 버튼 배치 및 데이터 유무에 따른 `IsEnabled` 동적 바인딩 보장.

---

## 2) Design Summary (설계 요약)
- **XAML 완전 복구**:
  - 이중 인코딩 문자(`\uFEFF`) 세척 및 `🌟` 이모지 문자를 컴파일 안전 기호인 `[Star]`로 전면 치환.
  - 626라인 이하 중복 찌꺼기를 완전히 지우고, `</DataGrid>`, `</Grid>`, `</Border>` 순의 정상적인 레이아웃 마감 태그를 정합성 있게 세팅.
- **DataGrid 빈 행 마감**:
  - `x:Name="GridElements"` DataGrid 속성에 `CanUserAddRows="False"`를 적용해 유령 가상 행 생성 원천 차단.
- **엑셀 고속 CSV 내보내기 엔진**:
  - `MainWindow.xaml.cs`에 내장된 `BtnExport_Click` 엔진이 Excel 100% 호환되도록 **UTF-8 BOM** 인코딩을 적용해 한글 깨짐 원천 해결.
  - 표에 데이터가 있을 때만 `BtnExport.IsEnabled = sorted.Count > 0;` 상태로 동적 컨트롤.

---

## 3) Implementation Plan (구현 및 복구 실행 계획)
- **태스크 1**: C# 임시 진단/보수 프로젝트를 구동하여 `MainWindow.xaml` 파일의 인코딩을 `UTF-8 with BOM`으로 복원하고 이중 BOM 헤더를 완전 소거.
- **태스크 2**: 626라인 이하의 엉킨 찌꺼기 XML 코드를 정밀 세척하고 올바른 닫는 태그(`</Grid>`, `</Border>`)를 정밀 이식.
- **태스크 3**: DataGrid에 `CanUserAddRows="False"` 속성을 주입.
- **태스크 4**: `dotnet build`를 실행하여 컴파일 에러 0개를 입증하고 `install_addin.ps1`을 가동해 핫로드 배포 완료.

---

## 4) Verification Plan (최종 검증 및 완료 보고)
- **자동 검증**:
  - `dotnet build`를 통한 무오류(Error 0) 컴파일 입증.
  - `install_addin.ps1`을 통해 Revit 2026 애드인 디렉토리에 핫로드 배포 성공.
- **수동 검증 시나리오**:
  - 애드인 실행 시 데이터그리드 하단에 가상 빈 행이 존재하지 않고 정확히 **1203개**로 마감되는지 확인.
  - 데이터가 로드되었을 때 세이지 그린 색상으로 켜지는 `[엑셀 내보내기]` 버튼을 클릭해 한글 깨짐 없는 엑셀 파일이 출력되는지 대조.

---

## 5) Self Code Review (자가 코드 리뷰)
- **식별된 리스크 & 보완**:
  - *이중 BOM 리스크*: `File.WriteAllText` 사용 시 발생할 수 있는 이중 BOM 삽입을 C# `TrimStart('\uFEFF')` 처리로 원천적으로 예방 완료.
  - *의존성 리스크*: 타사 Excel 어셈블리를 참조하지 않고 표준 StreamWriter 스트림 방식을 사용하여 Revit 서드파티 충돌 가능성을 완전히 배제함.
