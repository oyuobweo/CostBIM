using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Interop;

namespace CostBIM.Views
{
    /// <summary>
    /// CostBIM의 아이덴티티와 프리미엄 로딩 경험을 선사하는 웰컴 스플래시 윈도우
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private double _progress = 0; // 0.0 ~ 1.0
        private const double TotalDurationMs = 2200; // 로딩 총 소요시간 (초기화 속도감 극대화)
        private const double TimerIntervalMs = 20;   // 타이머 주기 (부드러운 50fps 실현)
        private const double WidthTrack = 370.0;    // ProgressBarIndicator 총 가용 너비

        /// <summary>
        /// 스플래시 로딩 및 페이드아웃이 무결하게 완료되었을 때 실행될 콜백 액션
        /// </summary>
        public Action? ActionOnComplete { get; set; }

        public SplashWindow()
        {
            InitializeComponent();
            
            // 50fps의 무결성 타이머 설정
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(TimerIntervalMs)
            };
            _timer.Tick += Timer_Tick;
            
            Loaded += SplashWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 🌟 [검은 박스 방지] Revit 등의 호스트 어플리케이션 환경에서 AllowsTransparency="True" 사용 시
            // WPF 하드웨어 가속 렌더링 충돌로 인해 투명 윈도우 배경이 검은 박스로 칠해지는 고질적 버그 원천 차단.
            // 소프트웨어 렌더링(SoftwareOnly) 모드를 강제하여 완벽하게 투명하고 깔끔한 라운드 엣지 웰컴 화면 구현.
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 부드러운 페이드인 애니메이션 (0.0 -> 1.0, 250ms)
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
            
            // 로딩 타이머 시동
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // 프레임당 증가분 누적
            _progress += TimerIntervalMs / TotalDurationMs;

            if (_progress >= 1.0)
            {
                _progress = 1.0;
                _timer.Stop();
                
                // 최종 UI 상태 정합
                UpdateUI(1.0);
                
                // 완료 및 부드러운 페이드아웃 소멸 시퀀스 시동
                StartFadeOutSequence();
            }
            else
            {
                UpdateUI(_progress);
            }
        }

        private void UpdateUI(double ratio)
        {
            double percent = ratio * 100;
            TxtPercent.Text = $"{(int)percent}%";
            ProgressBarIndicator.Width = ratio * WidthTrack;

            // 단계별 인체공학적 상태 문구 갱신
            if (percent < 30)
            {
                TxtStatus.Text = "CostBIM 엔진 초기화 중...";
            }
            else if (percent < 60)
            {
                TxtStatus.Text = "데이터 수집 모듈 검증 중...";
            }
            else if (percent < 85)
            {
                TxtStatus.Text = "3D 도면 파라미터 매핑 분석 중...";
            }
            else
            {
                TxtStatus.Text = "스캔 준비 완료!";
            }
        }

        private void StartFadeOutSequence()
        {
            // 350ms 동안 투명도를 0으로 수축 (부드러운 EaseInOut 곡선)
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            
            fadeOut.Completed += (s, e) =>
            {
                // 스플래시 창 안전 소멸
                Close();
            };
            
            // 🌟 [오버랩 크로스페이드 연출]
            // 페이드아웃이 시작되는 바로 그 순간, 메인 윈도우 기동 액션(ActionOnComplete)을 선제 트리거!
            // SplashWindow는 Topmost="True"이므로 메인 윈도우 위에 머무르며 부드럽게 사르륵 사라집니다.
            try
            {
                ActionOnComplete?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Splash completion callback failed: {ex.Message}");
            }
            
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
