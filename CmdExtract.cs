using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CostBIM.Services;
using CostBIM.Views;
using CostBIM.Models;

namespace CostBIM
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CmdExtract : IExternalCommand
    {
        // Keep active instance of Modeless Window to prevent multiple launches and garbage collection
        private static MainWindow? _activeWindow;

        private static bool IsRevitDarkTheme(UIApplication uiApp)
        {
            try
            {
                // UIThemeManager 타입 검색 (Revit 2024+)
                var themeManagerType = typeof(UIApplication).Assembly
                    .GetType("Autodesk.Revit.UI.UIThemeManager");
                
                if (themeManagerType != null)
                {
                    // UIThemeManager.ActiveTheme 속성 검색
                    var activeThemeProp = themeManagerType.GetProperty("ActiveTheme");
                    if (activeThemeProp != null)
                    {
                        var activeThemeVal = activeThemeProp.GetValue(null); // static property
                        if (activeThemeVal != null)
                        {
                            string themeStr = activeThemeVal.ToString();
                            // "Dark"가 포함되어 있으면 다크 테마
                            if (themeStr.Contains("Dark"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 리플렉션 실패 시 라이트 리턴 (Revit 2023 이하)
            }
            return false;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // 1) 메인 윈도우가 이미 로드되어 작동 중인 경우, 웰컴 화면 없이 바로 작업대 활성화 및 포커스 이동
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    _activeWindow.SetStatus("기존에 실행 중인 작업 콘솔로 포커스를 이동했습니다.");
                    return Result.Succeeded;
                }

                var uiApp = commandData.Application;
                bool isDark = IsRevitDarkTheme(uiApp);

                // 2) 모달리스 비동기 이벤트를 다룰 이벤트 핸들러 초기 정의 (표준 API 컨텍스트 내 안전 생성)
                var selectHandler = new SelectEvent();
                var selectEvent = ExternalEvent.Create(selectHandler);

                // 추출 관련 비동기 이벤트 런타임 제어기 정의 및 표준 API 컨텍스트 내 선제적 생성 (방어선 1)
                var extractHandler = new ExtractEvent();
                var extractEvent = ExternalEvent.Create(extractHandler);

                var doc = uiApp.ActiveUIDocument?.Document;
                
                // 3D 뷰의 물리 요소 및 사용 가능한 매개변수 가용 스키마 즉각 분석
                var schema = doc != null ? RevitElementExtractor.ScanAvailableParameters(doc) : new ParameterSchema();
                
                // 3) 메인 작업 콘솔 생성 (Revit API 의존성이 격리된 새로운 생성자 호출)
                _activeWindow = new MainWindow(schema, isDark);

                // C# 표준 이벤트를 수신하여 Revit Modeless 비동기 이벤트 핸들러와 결합
                _activeWindow.OnScanRequested += () =>
                {
                    extractEvent.Raise();
                };

                _activeWindow.OnElementSelectRequested += (elementIdStr) =>
                {
                    selectHandler.TargetElementIdStr = elementIdStr;
                    selectEvent.Raise();
                };

                // extractHandler에 윈도우 참조 안전 주입 (순환참조 우아한 해소)
                extractHandler.SetWindow(_activeWindow);

                // 윈도우가 소멸할 때 정적 참조 레퍼런스를 해제하여 메모리 가비지 컬렉션(GC) 유도
                _activeWindow.Closed += (s, e) => _activeWindow = null;

                // 메인 윈도우 역시 Revit 메인 창을 자식 형태로 고정하여 윈도우 레이어 배치 무결성 방어
                var mainHelper = new System.Windows.Interop.WindowInteropHelper(_activeWindow);
                mainHelper.Owner = uiApp.MainWindowHandle;

                // 4) 웰컴 스플래시 윈도우(SplashWindow) 신규 가동
                var splash = new SplashWindow();
                
                // 웰컴 창이 Revit 활성 창 뒤에 묻히지 않도록 Revit 메인 윈도우 핸들을 Owner로 명확히 마그네틱 결합
                var splashHelper = new System.Windows.Interop.WindowInteropHelper(splash);
                splashHelper.Owner = uiApp.MainWindowHandle;

                // 5) 2.2초 로딩 및 페이드아웃 종료 후 작동할 MainWindow 시각 노출 콜백 주입
                splash.ActionOnComplete = () =>
                {
                    try
                    {
                        if (_activeWindow != null)
                        {
                            // 웰컴 화면이 정상 종료된 시점에 메인 작업 콘솔을 정식으로 노출
                            _activeWindow.Show();
                            _activeWindow.Activate();
                            _activeWindow.SetStatus("파라미터 스캔 준비가 완료되었습니다.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        TaskDialog.Show("CostBIM Show Error", $"웰컴 화면 구동 후 메인 작업대를 표시하는 중 예외가 발생했습니다:\n{innerEx.Message}\n\n상세 정보:\n{innerEx.StackTrace}");
                    }
                };

                // 스플래시 화면 정식 노출
                splash.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("CostBIM Scan Error", $"작업대를 실행하는 중 예외가 발생했습니다.\n\n오류 내용:\n{ex.Message}\n\n상세 정보:\n{ex.StackTrace}");
                return Result.Failed;
            }
        }
    }
}
