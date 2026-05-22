using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace CostBIM.Loader
{
    public class App : IExternalApplication
    {
        private static ImageSource? _cachedIcon;

        public Result OnStartup(UIControlledApplication application)
        {
            if (application == null) return Result.Failed;

            try
            {
                // Create custom ribbon tab
                string tabName = "CostBIM";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Ignore if tab already exists
                }

                // Create ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "CostBIM");

                // Register CmdLoader
                string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
                
                var buttonData = new PushButtonData(
                    "CmdExtract3D",
                    "Parameter\nScan",
                    thisAssemblyPath,
                    "CostBIM.Loader.CmdLoader"
                )
                {
                    ToolTip = "현재 활성화된 3D 뷰에서 실물 객체들을 식별하고 치수 파라미터를 추출해 작업그리드로 가져옵니다. (핫로드 로더)",
                    LongDescription = "이 버튼은 실행될 때마다 빌드된 최신 CostBIM.dll의 바이너리 바이트를 실시간으로 메모리 로드합니다. 따라서 레빗을 재부팅할 필요 없이 코드 변경 사항을 즉시 테스트할 수 있습니다."
                };

                var pushButton = panel.AddItem(buttonData) as PushButton;
                if (pushButton != null)
                {
                    pushButton.LargeImage = GetOrCreateExtractIcon();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating Ribbon Loader: {ex.Message}");
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
                    // Revit 2026 스타일: 투명 배경에 세련되고 얇은 문서 테두리 펜 정의
                    var slateGreyBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 85, 104));
                    var outlinePen = new Pen(slateGreyBrush, 1.2);
                    outlinePen.StartLineCap = PenLineCap.Round;
                    outlinePen.EndLineCap = PenLineCap.Round;
                    outlinePen.LineJoin = PenLineJoin.Round;

                    // 1) 문서 시트(Sheet of Paper) 그리기 - 우측 상단 모서리가 접힌 정밀 디자인
                    var docSheet = new StreamGeometry();
                    using (var ctx = docSheet.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(6, 4), true, true);
                        ctx.LineTo(new System.Windows.Point(20, 4), true, false);  // 접히기 시작하는 점
                        ctx.LineTo(new System.Windows.Point(26, 10), true, false); // 접힌 우측 모서리 끝
                        ctx.LineTo(new System.Windows.Point(26, 28), true, false);
                        ctx.LineTo(new System.Windows.Point(6, 28), true, false);
                    }
                    docSheet.Freeze();
                    
                    // 은은한 유광 아크릴 느낌의 반투명 백그라운드 채우기 (Revit Glass Look)
                    drawingContext.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 240, 240, 245)), outlinePen, docSheet);

                    // 접힌 모서리(Folded Corner) 세부 입체 묘사
                    var foldedCorner = new StreamGeometry();
                    using (var ctx = foldedCorner.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(20, 4), true, true);
                        ctx.LineTo(new System.Windows.Point(20, 10), true, false);
                        ctx.LineTo(new System.Windows.Point(26, 10), true, false);
                    }
                    foldedCorner.Freeze();
                    var foldBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 180, 180, 195));
                    drawingContext.DrawGeometry(foldBrush, outlinePen, foldedCorner);

                    // 2) 문서 내부 텍스트 파라미터(Parameter Text Lines) 표현 - 세련된 다크 퍼플 실선들
                    var textLinePen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 94, 53, 177)), 0.8);
                    drawingContext.DrawLine(textLinePen, new System.Windows.Point(10, 9), new System.Windows.Point(17, 9));
                    drawingContext.DrawLine(textLinePen, new System.Windows.Point(10, 13), new System.Windows.Point(22, 13));
                    drawingContext.DrawLine(textLinePen, new System.Windows.Point(10, 17), new System.Windows.Point(20, 17));
                    drawingContext.DrawLine(textLinePen, new System.Windows.Point(10, 21), new System.Windows.Point(18, 21));
                    drawingContext.DrawLine(textLinePen, new System.Windows.Point(10, 25), new System.Windows.Point(22, 25));

                    // 3) 스캔빔 영역 그라데이션 (스캔 레이저선 하단으로 흐르는 반투명 빔 효과)
                    var scanBeam = new StreamGeometry();
                    using (var ctx = scanBeam.Open())
                    {
                        ctx.BeginFigure(new System.Windows.Point(6, 15), true, true);
                        ctx.LineTo(new System.Windows.Point(26, 15), true, false);
                        ctx.LineTo(new System.Windows.Point(26, 23), true, false);
                        ctx.LineTo(new System.Windows.Point(6, 23), true, false);
                    }
                    scanBeam.Freeze();

                    var purpleGradient = new LinearGradientBrush(
                        System.Windows.Media.Color.FromArgb(60, 186, 104, 200),  // 반투명 네온 퍼플
                        System.Windows.Media.Color.FromArgb(0, 186, 104, 200),   // 완전 투명
                        90.0
                    );
                    drawingContext.DrawGeometry(purpleGradient, null, scanBeam);

                    // 4) 문서를 가로질러 실시간 스캔 중인 선명한 네온 바이올렛 레이저빔(Neon Laser Scanline)
                    var laserGlowPen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 224, 64, 251)), 2.8);
                    drawingContext.DrawLine(laserGlowPen, new System.Windows.Point(3, 15), new System.Windows.Point(29, 15));

                    var laserCorePen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 64, 251)), 1.2);
                    drawingContext.DrawLine(laserCorePen, new System.Windows.Point(3, 15), new System.Windows.Point(29, 15));
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
                System.Diagnostics.Debug.WriteLine($"リ본 아이콘 렌더링 실패: {ex.Message}");
                _cachedIcon = null;
            }

            return _cachedIcon;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdLoader : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Find CostBIM.dll in the same directory
                string thisDllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string targetDllPath = Path.Combine(thisDllDir, "CostBIM.dll");

                if (!File.Exists(targetDllPath))
                {
                    TaskDialog.Show("CostBIM Loader Error", $"Target DLL not found at:\n{targetDllPath}\n\nPlease compile the project first!");
                    return Result.Failed;
                }

                // Read all bytes to prevent file locking on disk
                byte[] assemblyBytes = File.ReadAllBytes(targetDllPath);
                
                // Read .pdb if exists to support debugging and line numbers
                string targetPdbPath = Path.ChangeExtension(targetDllPath, ".pdb");
                Assembly assembly;
                if (File.Exists(targetPdbPath))
                {
                    byte[] pdbBytes = File.ReadAllBytes(targetPdbPath);
                    assembly = Assembly.Load(assemblyBytes, pdbBytes);
                }
                else
                {
                    assembly = Assembly.Load(assemblyBytes);
                }

                // Dynamically fetch and execute CmdExtract
                Type? type = assembly.GetType("CostBIM.CmdExtract");
                if (type == null)
                {
                    TaskDialog.Show("CostBIM Loader Error", "Could not find 'CostBIM.CmdExtract' type in the loaded assembly.");
                    return Result.Failed;
                }

                object? instance = Activator.CreateInstance(type);
                MethodInfo? method = type.GetMethod("Execute");
                if (method == null || instance == null)
                {
                    TaskDialog.Show("CostBIM Loader Error", "Could not instantiate CmdExtract or find Execute method.");
                    return Result.Failed;
                }

                // Call the command handler!
                return (Result)method.Invoke(instance, new object[] { commandData, message, elements })!;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("CostBIM Loader Exception", ex.ToString());
                return Result.Failed;
            }
        }
    }
}
