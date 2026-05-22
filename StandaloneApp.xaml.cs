using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CostBIM.Models;
using CostBIM.Views;

namespace CostBIM
{
    /// <summary>
    /// Revit 없이 독립 실행되는 CostBIM Standalone 데스크톱 프로그램의 진입점
    /// </summary>
    public partial class StandaloneApp : Application
    {
        private static MainWindow? _activeWindow;

        [STAThread]
        public static void Main()
        {
            var app = new StandaloneApp();
            app.InitializeComponent();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1) 독립 실행형 전용 가상 BIM 스키마(Mock Schema) 구축
                var schema = CreateMockParameterSchema();

                // 2) 메인 작업대 생성 (Revit API 의존성 0% 결합)
                // 독립 실행형에서는 항상 세련된 다크 테마(isRevitDarkTheme = true)를 기조로 적용합니다.
                _activeWindow = new MainWindow(schema, isRevitDarkTheme: true);

                // 3) 독점 실행형 웰컴 오프라인 이벤트 핸들러 바인딩
                _activeWindow.OnScanRequested += HandleOfflineScanRequested;
                _activeWindow.OnElementSelectRequested += HandleOfflineElementSelectRequested;

                // 정적 참조 소멸 가비지 컬렉션 유도
                _activeWindow.Closed += (s, ev) => _activeWindow = null;

                // 4) 프리미엄 로딩 웰컴 윈도우(SplashWindow) 가동
                var splash = new SplashWindow();
                
                // 스플래시 화면이 소멸하는 시점에 자연스럽게 메인 창 활성화 (크로스페이드 오버랩)
                splash.ActionOnComplete = () =>
                {
                    if (_activeWindow != null)
                    {
                        _activeWindow.Show();
                        _activeWindow.Activate();
                    }
                };

                splash.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"독립형 프로그램을 초기화하는 중 오류가 발생했습니다:\n{ex.Message}\n\n상세 정보:\n{ex.StackTrace}", 
                    "CostBIM Standalone Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ParameterSchema CreateMockParameterSchema()
        {
            var schema = new ParameterSchema();

            // BuiltIn Parameters
            schema.BuiltIn.Add("길이");
            schema.BuiltIn.Add("면적");
            schema.BuiltIn.Add("체적");
            schema.BuiltIn.Add("두께");
            
            // Project Parameters
            schema.Project.Add("부재코드");
            schema.Project.Add("수량산출코드");
            schema.Project.Add("AP_공정구분");

            // Shared Parameters
            schema.Shared.Add("작업세트");
            schema.Shared.Add("구조재질");

            // Group Map 정밀 구성
            schema.GroupMap["길이"] = "📏 치수";
            schema.GroupMap["면적"] = "📏 치수";
            schema.GroupMap["체적"] = "📏 치수";
            schema.GroupMap["두께"] = "📏 치수";
            
            schema.GroupMap["부재코드"] = "🏷️ 아이디";
            schema.GroupMap["수량산출코드"] = "🏷️ 아이디";
            
            schema.GroupMap["AP_공정구분"] = "⚙️ 공정";
            schema.GroupMap["작업세트"] = "⚙️ 공정";
            schema.GroupMap["구조재질"] = "💎 재료";

            return schema;
        }

        private async void HandleOfflineScanRequested()
        {
            if (_activeWindow == null) return;

            // 1.5초(1500ms) 동안 비동기 스캔 대기 타임 시뮬레이션 (WPF 스레드 블로킹 없음)
            await Task.Delay(1500);

            // 실무 QTO(수량산출) 검토의 극치인 14개의 고품질 가상 BIM 수량 데이터셋 생성
            var mockElements = new List<ExtractedElement>
            {
                new ExtractedElement 
                { 
                    Id = "310452", Guid = "wall-guid-001", Category = "벽 (Walls)", Family = "기본벽", Type = "철근콘크리트 200mm", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "4.250" }, { "면적", "12.750" }, { "체적", "2.550" }, { "두께", "200" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "W-RC-20" }, { "부재코드", "W01" }, { "구조재질", "Concrete_C24" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310453", Guid = "wall-guid-002", Category = "벽 (Walls)", Family = "기본벽", Type = "철근콘크리트 200mm", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "6.800" }, { "면적", "20.400" }, { "체적", "4.080" }, { "두께", "200" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "W-RC-20" }, { "부재코드", "W01" }, { "구조재질", "Concrete_C24" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310488", Guid = "wall-guid-003", Category = "벽 (Walls)", Family = "기본벽", Type = "조적벽 시멘트벽돌 0.5B", Workset = "02_마감 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "3.200" }, { "면적", "9.600" }, { "체적", "0.864" }, { "두께", "90" }, { "AP_공정구분", "조적공사" }, { "수량산출코드", "W-BK-05" }, { "부재코드", "W02" }, { "구조재질", "Cement Brick" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310501", Guid = "floor-guid-001", Category = "바닥 (Floors)", Family = "일반바닥", Type = "콘크리트 슬래브 150mm", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "48.500" }, { "체적", "7.275" }, { "두께", "150" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "F-RC-15" }, { "부재코드", "F01" }, { "구조재질", "Concrete_C24" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310502", Guid = "floor-guid-002", Category = "바닥 (Floors)", Family = "일반바닥", Type = "콘크리트 슬래브 150mm", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "32.400" }, { "체적", "4.860" }, { "두께", "150" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "F-RC-15" }, { "부재코드", "F01" }, { "구조재질", "Concrete_C24" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310611", Guid = "col-guid-001", Category = "구조기둥 (Structural Columns)", Family = "콘크리트-사각형 기둥", Type = "C1 500x500", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "3.000" }, { "면적", "6.000" }, { "체적", "0.750" }, { "두께", "-" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "C-RC-55" }, { "부재코드", "C01" }, { "구조재질", "Concrete_C30" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310612", Guid = "col-guid-002", Category = "구조기둥 (Structural Columns)", Family = "콘크리트-사각형 기둥", Type = "C1 500x500", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "3.000" }, { "면적", "6.000" }, { "체적", "0.750" }, { "두께", "-" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "C-RC-55" }, { "부재코드", "C01" }, { "구조재질", "Concrete_C30" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310620", Guid = "col-guid-003", Category = "구조기둥 (Structural Columns)", Family = "H형강 기둥", Type = "H-300x300x10x15", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "3.200" }, { "면적", "3.840" }, { "체적", "0.298" }, { "두께", "-" }, { "AP_공정구분", "철골공사" }, { "수량산출코드", "C-ST-33" }, { "부재코드", "SC01" }, { "구조재질", "SS275" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310705", Guid = "beam-guid-001", Category = "구조프레임 (Structural Framing)", Family = "콘크리트-직사각형 보", Type = "B1 400x700", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "6.200" }, { "면적", "13.640" }, { "체적", "1.736" }, { "두께", "-" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "B-RC-47" }, { "부재코드", "G01" }, { "구조재질", "Concrete_C27" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310706", Guid = "beam-guid-002", Category = "구조프레임 (Structural Framing)", Family = "콘크리트-직사각형 보", Type = "B1 400x700", Workset = "01_구조 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "5.800" }, { "면적", "12.760" }, { "체적", "1.624" }, { "두께", "-" }, { "AP_공정구분", "골조공사" }, { "수량산출코드", "B-RC-47" }, { "부재코드", "G01" }, { "구조재질", "Concrete_C27" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310810", Guid = "door-guid-001", Category = "문 (Doors)", Family = "목재 문", Type = "외여닫이문 900x2100", Workset = "02_마감 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "1.890" }, { "체적", "-" }, { "두께", "40" }, { "AP_공정구분", "창호공사" }, { "수량산출코드", "D-WD-09" }, { "부재코드", "WD01" }, { "구조재질", "Lauan Wood" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310811", Guid = "door-guid-002", Category = "문 (Doors)", Family = "철재 방화문", Type = "편개 방화문 1000x2100", Workset = "02_마감 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "2.100" }, { "체적", "-" }, { "두께", "50" }, { "AP_공정구분", "창호공사" }, { "수량산출코드", "D-ST-10" }, { "부재코드", "FD01" }, { "구조재질", "Galvanized Steel" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310901", Guid = "win-guid-001", Category = "창 (Windows)", Family = "알루미늄 이중창", Type = "AW 1200x1200", Workset = "02_마감 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "1.440" }, { "체적", "-" }, { "두께", "150" }, { "AP_공정구분", "창호공사" }, { "수량산출코드", "W-AL-12" }, { "부재코드", "AW01" }, { "구조재질", "Aluminum/Glass" } } 
                },
                new ExtractedElement 
                { 
                    Id = "310902", Guid = "win-guid-002", Category = "창 (Windows)", Family = "알루미늄 이중창", Type = "AW 1500x1200", Workset = "02_마감 작업세트", 
                    CustomParameters = new Dictionary<string, string> { { "길이", "-" }, { "면적", "1.800" }, { "체적", "-" }, { "두께", "150" }, { "AP_공정구분", "창호공사" }, { "수량산출코드", "W-AL-15" }, { "부재코드", "AW02" }, { "구조재질", "Aluminum/Glass" } } 
                }
            };

            // 작업대에 가상 스캔이 완료되었음을 알리고 데이터를 밀어넣어 격자 구성
            _activeWindow.UpdateElementsList(mockElements);
        }

        private void HandleOfflineElementSelectRequested(string elementIdStr)
        {
            // 독립형 뷰어에서는 Revit 뷰포트 하이라이트 제어가 물리적으로 불가능하므로,
            // 백그라운드 진단 로그를 세련되게 남기는 용도로 안전하고 우아하게 우회 처리합니다.
            System.Diagnostics.Debug.WriteLine($"[CostBIM Standalone] Element {elementIdStr} selected in DataGrid.");
        }
    }
}
