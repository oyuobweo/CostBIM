# CostBIM 플랫 레이아웃 일체화 개편 계획서
---
> **작업 일시**: 2026-05-26 14:04
> **역할**: Lead Engineer Agent
> **태스크명**: `flat_layout_integration`

---

## 1. Problem Summary (핵심 문제 요약)
* **현황**: 사이드바 패널 영역(`SidebarPanel`)과 우측 본문 영역(메인 작업 공간)이 각각 개별적인 카드(배경 `#FFFFFF`, 둥근 보더 `CornerRadius="8"`, 회색 테두리 `BorderThickness="1"`, 그림자 `DropShadowEffect`)로 분리되어 붕 떠 있는 형태입니다.
* **사용자 요구**: 이와 같은 쪼개진 상자형 프레임 레이아웃을 완전히 제거하고, 제공된 WI 서비스 화면 느낌과 유사하게 **전체 창이 하나로 플랫하게 일체화된 레이아웃**으로 변경을 원하고 있습니다.
* **목표**: 개별 카드 프레임과 그림자 효과를 모두 걷어내고, 창 전체가 순백색 바탕(`Background="#FFFFFF"`)을 공유하며, 영역 사이에는 얇은 1px 경계 구분선 하나만 존재하는 세련된 SaaS 디자인을 완성합니다.

---

## 2. Design Summary (설계 요약)
* **창 전체 백그라운드 및 레이아웃 밀착**:
  * 창 내부 메인 컨텐츠 영역의 루트 Grid(`Grid.Row="1"`) 마진을 기존 `Margin="12"`에서 `Margin="0"`으로 대폭 수정하여 화면 가장자리까지 100% 밀착시킵니다.
  * 최상위 마스터 보더 `RootBorder`의 배경색을 `#FFFFFF`로 통일합니다.
* **사이드바 패널(`SidebarPanel` Border) 플랫화**:
  * `BorderThickness="1"`을 우측만 경계를 남기도록 **`BorderThickness="0,0,1,0"`**으로 변경합니다.
  * 그림자 효과(`<Border.Effect>...</Border.Effect>`)를 완전히 제거합니다.
  * 모서리 곡률 `CornerRadius="8"`을 `CornerRadius="0"`으로 제거합니다.
  * 여백 `Margin="0,0,12,0"`을 `Margin="0"`으로 바짝 밀착시킵니다.
* **우측 본문 영역(`Border Grid.Column="1"`) 플랫화**:
  * 여백 `Margin="12,0,0,0"`을 `Margin="0"`으로 변경하여 사이드바의 우측 세로 경계선과 1:1로 칼같이 결합합니다.
  * 테두리 `BorderThickness="1"`을 **`BorderThickness="0"`**으로 완전 무효화합니다.
  * 그림자 효과(`<Border.Effect>...</Border.Effect>`)를 삭제합니다.
  * 모서리 곡률 `CornerRadius="8"`을 `CornerRadius="0"`으로 제거합니다.
  * 좌우측 안쪽 여백(`Padding="14"`)을 양옆 여유 공간 확장을 위해 `Padding="16,14"`로 상향 조정합니다.
* **하단 버튼 바 및 Empty State 패널 조화**:
  * 우측 메인 영역 하단 버튼 바(`Grid.Row="1"`)의 `CornerRadius="0,0,8,8"`을 `CornerRadius="0"`으로 원복합니다.
  * 하단 버튼 바의 배경색 `Background="#F8FAFC"`를 `#FFFFFF`로 플랫 병합하여 완전한 비주얼 일체감을 조성합니다.
  * 데이터 수집 전 노출되는 텅 빈 상태 패널(`EmptyStatePanel`)의 배경색 `Background="#F8FAFC"` 역시 `#FFFFFF`로 통일하여 플랫 디자인의 통일성을 지킵니다.

---

## 3. Implementation Plan (구현 계획)
* **1단계**: [MODIFY] `d:\CostBim\Views\MainWindow.xaml` 내 `RootBorder` 배경 및 Grid 마진 수정.
* **2단계**: [MODIFY] `d:\CostBim\Views\MainWindow.xaml` 내 `SidebarPanel` Border 속성 교체 및 그림자 코드 제거.
* **3단계**: [MODIFY] `d:\CostBim\Views\MainWindow.xaml` 내 우측 본문 영역 Border 속성 교체, 그림자 코드 및 하단 버튼 바 Border 속성 정밀 수정.
* **4단계**: [MODIFY] `d:\CostBim\Views\MainWindow.xaml` 내 `EmptyStatePanel` 배경 변경.
* **5단계**: 빌드 검증 (`dotnet build`)을 통한 문법 및 구조적 정합성 무결함 검증.

---

## 4. Verification Plan (검증 방안)
* **자동화 빌드 테스트**: MSBuild/Dotnet 컴파일 도구를 사용해 XAML 구조가 올바르게 파싱되고 C# 비하인드 코드와 바인딩이 깨지지 않았는지 점검합니다.
* **레이아웃 확인**: 런타임에 사이드바 패널의 둥근 모서리가 제거되었는지, 그림자가 사라졌는지, 우측 본문 영역과의 경계선(1px 세로 회색선)이 완벽히 플랫하게 렌더링되는지 점검합니다.
