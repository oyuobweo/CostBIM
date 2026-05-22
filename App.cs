using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace CostBIM
{
    public class App : IExternalApplication
    {
        private static ImageSource? _cachedIcon;

        public Result OnStartup(UIControlledApplication application)
        {
            if (application == null) return Result.Failed;

            try
            {
                // 1) Create Custom Ribbon Tab
                string tabName = "QTO System";
                application.CreateRibbonTab(tabName);

                // 2) Create Ribbon Panel
                string panelName = "BIM Extraction";
                RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

                // 3) Create Push Button for 3D Extraction
                string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
                
                var buttonData = new PushButtonData(
                    "CmdExtract3D",
                    "파라미터\n스캔",
                    thisAssemblyPath,
                    "CostBIM.CmdExtract"
                )
                {
                    ToolTip = "현재 활성화된 3D 뷰에서 실물 객체들을 식별하고 치수 파라미터를 추출해 작업그리드로 가져옵니다.",
                    LongDescription = "이 플러그인은 실무자가 수량산출 규칙(M_Template)에 맞게 객체 코드를 직접 매핑하고 데이터를 클라우드로 전송할 수 있는 강력한 UI 환경을 로컬에서 띄워줍니다."
                };

                // Add button to panel
                var pushButton = panel.AddItem(buttonData) as PushButton;
                if (pushButton != null)
                {
                    pushButton.LargeImage = GetOrCreateExtractIcon();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // 리드 엔지니어 표준 로깅 포맷 적용: [Time] [Level] [Module] [ErrorCode] Message
                string logMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] [App] [ERR_RIBBON_SETUP_FAILED] 레빗 리본 생성 중 치명적 오류 발생: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(logMsg);
                return Result.Succeeded;
            }
        }

        private static ImageSource? GetOrCreateExtractIcon()
        {
            if (_cachedIcon != null) return _cachedIcon;

            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Revit 2026 스타일: 투명 배경에 세련되고 얇은 플랫 와이어프레임 펜 정의
                    var darkSlateBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 64));
                    var outlinePen = new Pen(darkSlateBrush, 1.2);
                    outlinePen.StartLineCap = PenLineCap.Round;
                    outlinePen.EndLineCap = PenLineCap.Round;
                    outlinePen.LineJoin = PenLineJoin.Round;

                    // 1) 3D 입체 큐브(Isometric Box) 그리기
                    // Top Face
                    var topFace = new StreamGeometry();
                    using (var ctx = topFace.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(16, 11), true, true);
                        ctx.LineTo(new System.Windows.Point(25, 15.5), true, false);
                        ctx.LineTo(new System.Windows.Point(16, 20), true, false);
                        ctx.LineTo(new System.Windows.Point(7, 15.5), true, false);
                    }
                    topFace.Freeze();
                    // 투명하고 은은한 연보라색/바이올렛 틴트 채우기 (Revit 2026 Glass Look)
                    drawingContext.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 126, 87, 194)), outlinePen, topFace);

                    // Left Face
                    var leftFace = new StreamGeometry();
                    using (var ctx = leftFace.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(7, 15.5), true, true);
                        ctx.LineTo(new System.Windows.Point(16, 20), true, false);
                        ctx.LineTo(new System.Windows.Point(16, 28), true, false);
                        ctx.LineTo(new System.Windows.Point(7, 23.5), true, false);
                    }
                    leftFace.Freeze();
                    drawingContext.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(12, 60, 60, 65)), outlinePen, leftFace);

                    // Right Face
                    var rightFace = new StreamGeometry();
                    using (var ctx = rightFace.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(16, 20), true, true);
                        ctx.LineTo(new System.Windows.Point(25, 15.5), true, false);
                        ctx.LineTo(new System.Windows.Point(25, 23.5), true, false);
                        ctx.LineTo(new System.Windows.Point(16, 28), true, false);
                    }
                    rightFace.Freeze();
                    drawingContext.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 60, 60, 65)), outlinePen, rightFace);

                    // 2) 큐브 내부/위로 솟아오르는 보라색/바이올렛 그라데이션 추출 화살표(Extraction Arrow)
                    var arrowGeo = new StreamGeometry();
                    using (var ctx = arrowGeo.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(16, 2), true, true);
                        ctx.LineTo(new System.Windows.Point(21, 8), true, false);
                        ctx.LineTo(new System.Windows.Point(18.5, 8), true, false);
                        ctx.LineTo(new System.Windows.Point(18.5, 15), true, false);
                        ctx.LineTo(new System.Windows.Point(13.5, 15), true, false);
                        ctx.LineTo(new System.Windows.Point(13.5, 8), true, false);
                        ctx.LineTo(new System.Windows.Point(11, 8), true, false);
                    }
                    arrowGeo.Freeze();
                    
                    var purpleGradient = new LinearGradientBrush(
                        System.Windows.Media.Color.FromRgb(126, 87, 194),  // Deep Violet
                        System.Windows.Media.Color.FromRgb(156, 39, 176),  // Neon Purple
                        90.0
                    );
                    var arrowPen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(94, 53, 177)), 0.8);
                    drawingContext.DrawGeometry(purpleGradient, arrowPen, arrowGeo);

                    // 3) 객체를 관통하는 화려한 바이올렛/네온핑크 레이저 스캔라인(Laser Scan Line)
                    // 은은한 네온 발광을 위한 백그라운드 두꺼운 빛 그리기 (Glow Effect)
                    var glowPen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 224, 64, 251)), 2.5);
                    drawingContext.DrawLine(glowPen, new System.Windows.Point(4, 18), new System.Windows.Point(28, 18));

                    // 중심의 날카로운 선명한 네온 바이올렛 라인
                    var laserPen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 64, 251)), 1.2);
                    drawingContext.DrawLine(laserPen, new System.Windows.Point(4, 18), new System.Windows.Point(28, 18));
                }

                // 모니터 DPI 인식 대응 (4K UHD 및 고배율 디스플레이 완벽 최적화)
                double dpiX = 96;
                double dpiY = 96;
                
                // 🌟 중요: Revit 로딩 시 Application.Current 및 MainWindow가 null일 수 있으므로 안전 검사 필수!
                if (Application.Current?.MainWindow != null)
                {
                    try
                    {
                        var presentationSource = PresentationSource.FromVisual(Application.Current.MainWindow);
                        if (presentationSource != null && presentationSource.CompositionTarget != null)
                        {
                            dpiX = 96 * presentationSource.CompositionTarget.TransformToDevice.M11;
                            dpiY = 96 * presentationSource.CompositionTarget.TransformToDevice.M22;
                        }
                    }
                    catch
                    {
                        // DPI 계산 실패 시 기본 96 DPI로 우회
                    }
                }

                var renderTargetBitmap = new RenderTargetBitmap(
                    32, 32, dpiX, dpiY, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);
                renderTargetBitmap.Freeze();
                
                _cachedIcon = renderTargetBitmap;
            }
            catch (Exception ex)
            {
                // 에러 발생 시 Fallback 및 리드 엔지니어 규격 로깅
                string logMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] [App] [ERR_ICON_RENDER_FAILED] 리본 아이콘 렌더링 실패: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(logMsg);
                _cachedIcon = null;
            }

            return _cachedIcon;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
