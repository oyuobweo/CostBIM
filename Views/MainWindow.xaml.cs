using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CostBIM.Models;

namespace CostBIM.Views
{
    public class ColumnDefinitionItem
    {
        public string Key { get; set; } = "";
        public string Header { get; set; } = "";
        public string BindingPath { get; set; } = "";
        public bool IsChecked { get; set; } = false;
        public string? StringFormat { get; set; }
    }

    public class FilterValueItem : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isChecked = true;
        private string _value = "";

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class ParameterItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "";
        private bool _isChecked = false;
        private string _groupName = "기타";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set 
            { 
                if (_isChecked != value)
                {
                    _isChecked = value; 
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        public string GroupName
        {
            get => _groupName;
            set { _groupName = value; OnPropertyChanged(nameof(GroupName)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        // C# 표준 이벤트 노출로 Revit 의존성 완전 분리
        public event Action? OnScanRequested;
        public event Action<string>? OnElementSelectRequested;

        private List<ExtractedElement> _allElements = new List<ExtractedElement>();
        private readonly HashSet<DataGridColumn> _selectedColumns = new HashSet<DataGridColumn>();
        private DataGridColumn? _lastClickedColumn;
        private bool _isBatchSelecting;

        // 🌟 엑셀 스타일 다중 열 교차 필터링 핵심 저장소
        private readonly Dictionary<string, HashSet<string>> _activeFilters = new Dictionary<string, HashSet<string>>();
        private DataGridColumn? _currentFilteringColumn;
        private List<FilterValueItem> _currentFilterItems = new List<FilterValueItem>();

        // Whitelist UI Collections
        public ObservableCollection<ParameterItem> BuiltInParams { get; } = new ObservableCollection<ParameterItem>();
        public ObservableCollection<ParameterItem> ProjectParams { get; } = new ObservableCollection<ParameterItem>();
        public ObservableCollection<ParameterItem> SharedParams { get; } = new ObservableCollection<ParameterItem>();

        // 🌟 패밀리 치환 설정을 위한 데이터 수량 집계 모델 바인딩 컬렉션
        public ObservableCollection<FamilyMappingItem> MappingItems { get; } = new ObservableCollection<FamilyMappingItem>();
        private string _defaultPresetPath = "";

        // Return currently checked custom parameter names dynamically
        public List<string> CustomParameterNames => GetActiveCustomParameters();

        public MainWindow(ParameterSchema schema, bool isRevitDarkTheme = true)
        {
            InitializeComponent();

            LoadScannedParameters(schema);

            // Bind ListBoxes to Collections
            LstBuiltInParams.ItemsSource = BuiltInParams;
            LstProjectParams.ItemsSource = ProjectParams;
            LstSharedParams.ItemsSource = SharedParams;

            // 기본 로컬 세이브 경로 설정 및 패밀리 치환 컬렉션 바인딩
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CostBIM");
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            _defaultPresetPath = System.IO.Path.Combine(dir, "family_mappings.json");

            if (GridMapping != null)
            {
                GridMapping.ItemsSource = MappingItems;
            }

            // 창이 닫힐 때 패밀리 명칭 치환 자동 세이브 연동
            this.Closed += (s, ev) => SaveCurrentMappings();

            // Build dynamic columns
            RebuildDynamicColumns();

            // Register Column Reordering Event (for drag-out check off)
            GridElements.ColumnReordered += GridElements_ColumnReordered;

            // 🌟 [드래그 반응속도 극대화] 드래그 선택이 끝나는 최종 시점에만 비주얼 및 Revit API 동기화를 비동기로 원샷 처리
            GridElements.PreviewMouseLeftButtonUp += GridElements_PreviewMouseLeftButtonUp;

            // 레빗 테마 자동 감지 및 연동 적용
            if (!isRevitDarkTheme)
            {
                ApplyLightTheme();
            }

            // 🌟 [기동 즉시 자동 스캔 위임] 창이 완전히 로딩되는 시점에 첫 번째 자동 수집 개시
            this.Loaded += MainWindow_Loaded;

            // 🌟 [스크롤뷰어 우상단 코너 갭 수정] GridElements 로드 시 코너 갭 색상 강제 패치
            GridElements.Loaded += (s, ev) => ApplyScrollViewerCornerFix();

            // 🌟 [동반 팝업 폐쇄 안전장치] 메인 창이 종료될 때 필터 팝업도 즉각 닫아 팝업이 화면에 떠도는 Win32 버그 원천 해결
            this.Closed += (s, ev) => { if (HeaderFilterPopup != null) HeaderFilterPopup.IsOpen = false; };

            // 🌟 [최대화 작업표시줄 덮음 방지 안전장치] 창 메시지 루프 가로채기를 통해 최대화 시 작업 표시줄 보호
            this.SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // [대안 B] 기동 시 강제 질문 오버레이를 띄우지 않고, 빈 상태의 대시보드를 맑게 제공합니다.
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }

            // 창 로드 시점에도 스크롤뷰어 패치 보강 호출
            ApplyScrollViewerCornerFix();

            // 초기 Empty State 동기화 (수집 전이므로 데이터그리드가 비어 있어 빈 화면 가이드 노출)
            ApplyFilter();
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024) // WM_GETMINMAXINFO
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, ref monitorInfo);

                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                
                mmi.ptMinTrackSize.X = 800;
                mmi.ptMinTrackSize.Y = 600;
            }

            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }

        // 2) Load Parameters scanned from Active View (Starts completely unchecked / blank slate)
        private void LoadScannedParameters(ParameterSchema schema)
        {
            if (schema == null) return;

            foreach (var name in schema.BuiltIn)
            {
                string group = schema.GroupMap != null && schema.GroupMap.ContainsKey(name) ? schema.GroupMap[name] : "기타";
                BuiltInParams.Add(new ParameterItem { Name = name, IsChecked = false, GroupName = group });
            }

            foreach (var name in schema.Project)
            {
                string group = schema.GroupMap != null && schema.GroupMap.ContainsKey(name) ? schema.GroupMap[name] : "기타";
                ProjectParams.Add(new ParameterItem { Name = name, IsChecked = false, GroupName = group });
            }

            foreach (var name in schema.Shared)
            {
                string group = schema.GroupMap != null && schema.GroupMap.ContainsKey(name) ? schema.GroupMap[name] : "기타";
                SharedParams.Add(new ParameterItem { Name = name, IsChecked = false, GroupName = group });
            }

        }

        // 3) Helper: Get checked parameters
        private List<string> GetActiveCustomParameters()
        {
            var customs = new List<string>();
            customs.AddRange(BuiltInParams.Where(x => x.IsChecked).Select(x => x.Name));
            customs.AddRange(ProjectParams.Where(x => x.IsChecked).Select(x => x.Name));
            customs.AddRange(SharedParams.Where(x => x.IsChecked).Select(x => x.Name));
            return customs;
        }

        // 4) Rebuild all Columns Dynamically based on active toggles
        private void RebuildDynamicColumns()
        {
            if (GridElements == null) return;

            // 🌟 [성능 최적화] ItemsSource 임시 격리를 통해 바인딩 다중 재평가 및 셀 생성 렉(Lag) 완전 소거
            var tempItemsSource = GridElements.ItemsSource;
            GridElements.ItemsSource = null;

            try
            {
                GridElements.Columns.Clear();

                // Rebuild Active Columns (with premium styles)
                var headerStyle = (Style)FindResource("PremiumColumnHeaderStyle");
                var textStyle = (Style)FindResource("PremiumTextBlockStyle");

                // 🌟 [3대 핵심 컬럼 고정 배치] 카테고리, 패밀리, 유형 컬럼은 체크 여부와 상관없이 항상 최좌측에 상시 고정 배치
                GridElements.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "카테고리", 
                    Binding = new Binding("Category"), 
                    Width = DataGridLength.Auto, // 고정값 대신 Auto 맞춤 적용
                    HeaderStyle = headerStyle,
                    ElementStyle = textStyle
                });

                GridElements.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "패밀리", 
                    Binding = new Binding("Family"), 
                    Width = DataGridLength.Auto, // 고정값 대신 Auto 맞춤 적용
                    HeaderStyle = headerStyle,
                    ElementStyle = textStyle
                });

                GridElements.Columns.Add(new DataGridTextColumn 
                { 
                    Header = "유형", 
                    Binding = new Binding("Type"), 
                    Width = DataGridLength.Auto, // 고정값 대신 Auto 맞춤 적용
                    HeaderStyle = headerStyle,
                    ElementStyle = textStyle
                });

                var activeCustoms = GetActiveCustomParameters();

                foreach (string paramName in activeCustoms)
                {
                    string lower = paramName.ToLower();

                    // 🌟 중복 렌더링 스킵: 이미 고정 배치된 3대 식별 정보와 매칭되는 파라미터는 추가하지 않고 스킵
                    if (lower == "category" || lower == "카테고리" || 
                        lower == "family" || lower == "family name" || lower == "familyname" || lower == "패밀리" || lower == "패밀리명" || 
                        lower == "type" || lower == "type name" || lower == "typename" || lower == "유형" || lower == "유형명")
                    {
                        continue;
                    }

                    if (lower == "id" || lower == "element id" || lower == "elementid" || lower == "부재 id")
                    {
                        GridElements.Columns.Add(new DataGridTextColumn 
                        { 
                            Header = paramName, 
                            Binding = new Binding("Id"), 
                            Width = DataGridLength.Auto, // 자동 맞춤 적용
                            HeaderStyle = headerStyle,
                            ElementStyle = textStyle
                        });
                    }
                    else if (lower == "workset" || lower == "작업세트")
                    {
                        GridElements.Columns.Add(new DataGridTextColumn 
                        { 
                            Header = paramName, 
                            Binding = new Binding("Workset"), 
                            Width = DataGridLength.Auto, // 자동 맞춤 적용
                            HeaderStyle = headerStyle,
                            ElementStyle = textStyle
                        });
                    }
                    else
                    {
                        var gridCol = new DataGridTextColumn
                        {
                            Header = paramName,
                            Binding = new Binding($"CustomParameters[{paramName}]")
                            {
                                TargetNullValue = "-",
                                FallbackValue = "-"
                            }, // 🌟 수집되지 않았거나 값이 비어 있는 파라미터는 공란 대신 "-"로 통일 출력
                            Width = DataGridLength.Auto, // 자동 맞춤 적용
                            HeaderStyle = headerStyle,
                            ElementStyle = textStyle
                        };
                        GridElements.Columns.Add(gridCol);
                    }
                }
            }
            finally
            {
                // 컬럼 구성을 마친 후 단 1회의 바인딩 연결로 레이아웃 극강 속도 실현
                GridElements.ItemsSource = tempItemsSource;

                // 🌟 [필터 비주얼 복원] 컬럼이 비주얼 트리에 로드된 후 활성화되어 있는 필터 깔때기 아이콘의 Indigo 색상 복원
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var col in GridElements.Columns)
                    {
                        string headerName = col.Header?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(headerName) && _activeFilters.ContainsKey(headerName))
                        {
                            UpdateHeaderFilterVisual(col, true);
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        // 5) Event trigger: Checkbox clicked
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            RebuildDynamicColumns();
            if (ChkSortSelectedToTop.IsChecked == true)
            {
                ApplyParameterSorting();
            }
        }

        private void ChkSortSelectedToTop_Click(object sender, RoutedEventArgs e)
        {
            ApplyParameterSorting();
        }

        private void ApplyParameterSorting()
        {
            bool sortBySelected = ChkSortSelectedToTop.IsChecked == true;

            var collections = new System.Collections.IEnumerable[] { BuiltInParams, ProjectParams, SharedParams };

            foreach (var col in collections)
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(col);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    if (sortBySelected)
                    {
                        view.SortDescriptions.Add(new System.ComponentModel.SortDescription("IsChecked", System.ComponentModel.ListSortDirection.Descending));
                    }
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
                }
            }
        }

        // 6) Trigger Extract Event
        private void BtnExtract_Click(object sender, RoutedEventArgs e)
        {
            ShowLoading();
            OnScanRequested?.Invoke();
        }

        // 🌟 [대안 B] Empty State 내 번개 스캔 실행 버튼 클릭 이벤트 핸들러
        private void BtnEmptyStateScan_Click(object sender, RoutedEventArgs e)
        {
            // Empty State에서 스캔 실행 버튼을 누르면 스캔 중 로딩 상태로 즉시 돌입하고 백그라운드 스캔 트리거
            ShowLoading("Parameter 스캔 중...", "3D 뷰의 물리 요소와 가용 매개변수를 수집하고 있습니다.");
            OnScanRequested?.Invoke();
        }

        // 🌟 [Excel Export] 저작권 및 DLL 버전 충돌 리스크 0%인 엑셀 호환 CSV 정밀 내보내기 엔진 (UTF-8 BOM 지원으로 한글 안깨짐)
        // 🌟 [Excel Export] 저작권 및 DLL 버전 충돌 리스크 0%인 엑셀 호환 CSV 및 표 양식 보존 Excel 통합 문서(*.xls) 정밀 내보내기 엔진
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (GridElements.ItemsSource == null)
            {
                MessageBox.Show("내보낼 데이터가 없습니다. 먼저 실행을 눌러 데이터를 스캔해주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var itemsList = GridElements.ItemsSource as System.Collections.IList;
            if (itemsList == null || itemsList.Count == 0)
            {
                MessageBox.Show("표시된 데이터가 0개입니다. 필터 조건을 확인하거나 다시 스캔해주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel 통합 문서 (*.xls)|*.xls|Excel CSV 파일 (*.csv)|*.csv",
                Title = "엑셀 호환 데이터 내보내기",
                FileName = $"CostBIM_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xls"
            };

            if (saveDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();

                try
                {
                    if (extension == ".xls")
                    {
                        // 🌟 [표 양식 스타일 보존 엑셀 엔진] HTML 5 <table> 스키마와 인라인 CSS 결합
                        using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            var columns = GridElements.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();

                            writer.WriteLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
                            writer.WriteLine("<head>");
                            writer.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
                            writer.WriteLine("<!--[if gte mso 9]>");
                            writer.WriteLine("<xml>");
                            writer.WriteLine(" <x:ExcelWorkbook>");
                            writer.WriteLine("  <x:ExcelWorksheets>");
                            writer.WriteLine("   <x:ExcelWorksheet>");
                            writer.WriteLine("    <x:Name>CostBIM_Elements</x:Name>");
                            writer.WriteLine("    <x:WorksheetOptions>");
                            writer.WriteLine("     <x:DisplayGridlines/>"); // 엑셀 격자 그리드 라인 상시 표출 보장
                            writer.WriteLine("    </x:WorksheetOptions>");
                            writer.WriteLine("   </x:ExcelWorksheet>");
                            writer.WriteLine("  </x:ExcelWorksheets>");
                            writer.WriteLine(" </x:ExcelWorkbook>");
                            writer.WriteLine("</xml>");
                            writer.WriteLine("<![endif]-->");
                            writer.WriteLine("<style>");
                            writer.WriteLine("  table { border-collapse: collapse; font-family: 'Malgun Gothic', 'Segoe UI', sans-serif; font-size: 10pt; }");
                            // 애드인 고유 테마 스킨 기하학적 복제 (연회색 #F1F5F9 헤더, 회색 테두리 #E2E8F0)
                            writer.WriteLine("  th { background-color: #F1F5F9; color: #475569; font-weight: bold; border: 1px solid #CBD5E1; padding: 8px 12px; height: 26pt; text-align: center; }");
                            writer.WriteLine("  td { border: 1px solid #E2E8F0; padding: 6px 10px; height: 20pt; color: #1E293B; vertical-align: middle; }");
                            writer.WriteLine("  .no-col { background-color: #F1F5F9; color: #64748B; font-weight: bold; text-align: center; border: 1px solid #CBD5E1; }");
                            writer.WriteLine("  .num-cell { text-align: right; mso-number-format:\"\\@\"; }"); // 텍스트 형태 포맷팅 지원으로 실무 소수점/0 누락 원천 방지
                            writer.WriteLine("  .text-cell { text-align: left; }");
                            writer.WriteLine("  .center-cell { text-align: center; }");
                            writer.WriteLine("</style>");
                            writer.WriteLine("</head>");
                            writer.WriteLine("<body>");
                            writer.WriteLine("<table>");
                            
                            // 1) 헤더 행 작성
                            writer.WriteLine("  <tr>");
                            writer.WriteLine("    <th class=\"no-col\" style=\"width: 50px;\">NO.</th>");
                            foreach (var col in columns)
                            {
                                string h = col.Header?.ToString() ?? "";
                                double colWidth = col.ActualWidth > 0 ? col.ActualWidth : 100;
                                writer.WriteLine($"    <th style=\"width: {colWidth}px;\">{System.Net.WebUtility.HtmlEncode(h)}</th>");
                            }
                            writer.WriteLine("  </tr>");

                            // 2) 데이터 행 순회 작성
                            int rowIndex = 1;
                            foreach (var item in itemsList)
                            {
                                if (item is ExtractedElement elem)
                                {
                                    writer.WriteLine("  <tr>");
                                    // 1열 NO. 정가운데 정렬 및 스킨 이식
                                    writer.WriteLine($"    <td class=\"no-col\">{rowIndex++}</td>");
                                    
                                    foreach (var col in columns)
                                    {
                                        string headerName = col.Header?.ToString() ?? "";
                                        string cellVal = GetPropertyValueAsString(elem, headerName);
                                        
                                        // 실무 가독성 극대화를 위한 데이터 도메인별 텍스트 정렬 분기
                                        string alignClass = "text-cell";
                                        if (headerName.Contains("ID") || headerName.Contains("코드") || headerName.Contains("Workset") || headerName.Contains("카테고리"))
                                        {
                                            alignClass = "center-cell";
                                        }
                                        else if (IsNumericValue(cellVal))
                                        {
                                            alignClass = "num-cell";
                                        }

                                        writer.WriteLine($"    <td class=\"{alignClass}\">{System.Net.WebUtility.HtmlEncode(cellVal)}</td>");
                                    }
                                    writer.WriteLine("  </tr>");
                                }
                            }
                            
                            writer.WriteLine("</table>");
                            writer.WriteLine("</body>");
                            writer.WriteLine("</html>");
                        }

                        MessageBox.Show("애드인 표 양식이 그대로 보존된 Excel 통합 문서로 성공적으로 내보냈습니다!\n엑셀(Excel)에서 정교한 격자와 색상을 즉시 확인해보세요.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // 🌟 [기존 CSV 내보내기 엔진 백업 연동] UTF-8 BOM 지원으로 한글 호환성 무결
                        using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            var columns = GridElements.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
                            
                            // CSV용 헤더 작성 (NO. 컬럼 추가 매핑)
                            var headers = new List<string> { "NO." };
                            headers.AddRange(columns.Select(c => EscapeCsvValue(c.Header?.ToString() ?? "")));
                            writer.WriteLine(string.Join(",", headers));

                            int rowIndex = 1;
                            foreach (var item in itemsList)
                            {
                                if (item is ExtractedElement elem)
                                {
                                    var rowValues = new List<string> { rowIndex++.ToString() };
                                    foreach (var col in columns)
                                    {
                                        string headerName = col.Header?.ToString() ?? "";
                                        string cellVal = GetPropertyValueAsString(elem, headerName);
                                        rowValues.Add(EscapeCsvValue(cellVal));
                                    }
                                    writer.WriteLine(string.Join(",", rowValues));
                                }
                            }
                        }

                        MessageBox.Show("엑셀 호환 CSV 파일로 데이터를 성공적으로 내보냈습니다!\n엑셀(Excel)에서 즉시 확인하실 수 있습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 중 오류가 발생했습니다:\n{ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 🌟 [실무형 수치 데이터 가독성 판별기] 단위 및 이스케이프가 결합된 치수도 스마트하게 감지
        private bool IsNumericValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string trimVal = value.Trim();
            
            // 쉼표 및 실무 치수 단위 제거 후 순수 소수 형상인지 확인
            string clean = trimVal.Replace(",", "")
                                  .Replace("mm", "")
                                  .Replace("m³", "")
                                  .Replace("m2", "")
                                  .Replace("m3", "")
                                  .Replace("EA", "")
                                  .Trim();
            
            return double.TryParse(clean, out _);
        }

        // 🌟 [CSV RFC 4180 표준 쉼표 이스케이프 엔진] 데이터 내 쉼표, 쌍따옴표, 개행 발생 시 셀 손실 원천 차단
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        // Show loading overlay with confirmation button after extraction completes
        public void ShowComplete()
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                }
                if (SpinnerGrid != null)
                {
                    SpinnerGrid.Visibility = Visibility.Collapsed;
                }
                if (CompleteIcon != null)
                {
                    CompleteIcon.Visibility = Visibility.Visible;
                }
                if (TxtLoadingTitle != null)
                {
                    TxtLoadingTitle.Text = "스캔 완료";
                }
                if (TxtLoadingSubTitle != null)
                {
                    TxtLoadingSubTitle.Visibility = Visibility.Collapsed;
                }
                if (BtnConfirmLoading != null)
                {
                    BtnConfirmLoading.Visibility = Visibility.Visible;
                }
                // Keep extract button disabled until user confirms
                if (BtnExtract != null)
                {
                    BtnExtract.IsEnabled = false;
                }
            });
        }

        // Confirmation button click handler
        private void BtnConfirmLoading_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
                if (BtnConfirmLoading != null)
                {
                    BtnConfirmLoading.Visibility = Visibility.Collapsed;
                }
                if (BtnExtract != null)
                {
                    BtnExtract.IsEnabled = true;
                }
                
                // [지연 바인딩] 확인 버튼을 누르는 시점에 비로소 분석 완료된 표 데이터를 출력
                ApplyFilter();
            });
        }

        // Row header numbering
        private void GridElements_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row != null)
            {
                e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            }
        }

        // 7) Update Elements in UI from Event Thread
        public void UpdateElementsList(List<ExtractedElement> elements)
        {
            Dispatcher.Invoke(() =>
            {
                _allElements = elements;
                
                // 🌟 스캔된 실물 물리 객체 데이터 기반 패밀리 치환 매핑 리스트 구성
                LoadInitialMappings(elements);

                // [지연 바인딩] 이전처럼 즉시 필터링(ApplyFilter)하지 않고 스캔 완료 알림창만 먼저 표출
                ShowComplete();
                SetStatus("");
            });
        }

        // 8) 엑셀 필터링용 부재 문자열 안전 추출기
        private string GetPropertyValueAsString(ExtractedElement elem, string headerName)
        {
            if (elem == null || string.IsNullOrEmpty(headerName)) return "";
            
            string norm = headerName.Trim();
            string lower = norm.ToLower();
            
            if (norm == "카테고리") return elem.Category ?? "";
            if (norm == "패밀리") return elem.Family ?? "";
            if (norm == "유형") return elem.Type ?? "";
            if (lower == "id" || lower == "부재 id" || lower == "부재id") return elem.Id ?? "";
            if (lower == "workset" || lower == "작업세트") return elem.Workset ?? "";
            
            if (elem.CustomParameters != null && elem.CustomParameters.ContainsKey(norm))
            {
                return elem.CustomParameters[norm] ?? "";
            }
            return "";
        }

        public void ApplyFilter()
        {
            if (GridElements == null) return;

            List<ExtractedElement> sourceList = _allElements;

            // 1) 다중 열 엑셀 교집합(AND) 필터 실시간 쿼리
            if (_activeFilters.Count > 0)
            {
                sourceList = sourceList.Where(elem =>
                {
                    foreach (var filterPair in _activeFilters)
                    {
                        string headerName = filterPair.Key;
                        var allowedValues = filterPair.Value;
                        string val = GetPropertyValueAsString(elem, headerName);
                        
                        if (!allowedValues.Contains(val))
                        {
                            return false; // 하나라도 통과 리스트에 없으면 교집합 탈락
                        }
                    }
                    return true;
                }).ToList();
            }

            // 2) 기본 정렬: 카테고리 → 패밀리 → 유형 오름차순
            var sorted = sourceList
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Family)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.Id)
                .ToList();

            GridElements.ItemsSource = sorted;

            int currentVisibleCount = GridElements.ItemsSource is System.Collections.IList list ? list.Count : 0;

            // 🌟 [대안 B Empty State 동적 조율]
            // 데이터 건수가 0개이면 미니멀 웰컴 Empty State 패널을 노출하고, 0개보다 크면 숨깁니다.
            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = currentVisibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // 🌟 [이모티콘 초간결 카운팅] 텍스트 중복을 전면 제거하고 심플하게 이모티콘+숫자만 출력
            if (LblCount != null)
            {
                LblCount.Text = $"📦 {currentVisibleCount}";
            }

            // 🌟 [내보내기 활성화] 데이터가 정상 수집되어 화면에 로드되는 시점에 엑셀 내보내기 버튼을 실제 작동할 수 있게 활성화
            if (BtnExport != null)
            {
                BtnExport.IsEnabled = currentVisibleCount > 0;
            }
        }

        // 🌟 [Excel 필터 이벤트 핸들러] 깔때기 버튼 클릭 시 팝업에 고유값 공급 및 위치 매핑
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // 🌟 [핵심] 필터 버튼 클릭 이벤트가 부모(컬럼 선택)로 버블링되는 것을 완벽 차단하여 엑셀처럼 팝업만 열리게 함
            if (sender is not Button btn) return;

            // 1) 비주얼 트리를 탐색하여 DataGridColumnHeader 획득
            var header = FindVisualAncestor<System.Windows.Controls.Primitives.DataGridColumnHeader>(btn);
            if (header == null || header.Column == null) return;

            _currentFilteringColumn = header.Column;
            string headerName = header.Column.Header?.ToString() ?? "";
            if (string.IsNullOrEmpty(headerName)) return;

            // 2) 전체 엘리먼트 데이터에서 해당 열의 고유값 추출 (정렬 적용)
            var uniqueValues = _allElements
                .Select(elem => GetPropertyValueAsString(elem, headerName))
                .Distinct()
                .OrderBy(val => val)
                .ToList();

            // 3) 필터 아이템 컬렉션 빌드
            _currentFilterItems = new List<FilterValueItem>();
            bool hasActiveFilter = _activeFilters.TryGetValue(headerName, out var allowedSet);

            foreach (var val in uniqueValues)
            {
                // 이미 활성화된 필터가 있으면 선택된 것만 체크, 없으면 기본적으로 전체 체크
                bool isChecked = !hasActiveFilter || (allowedSet != null && allowedSet.Contains(val));
                _currentFilterItems.Add(new FilterValueItem { Value = val, IsChecked = isChecked });
            }

            // 4) UI 목록 바인딩 및 텍스트박스 초기화
            LstFilterItems.ItemsSource = _currentFilterItems;
            TxtFilterSearch.Text = "";

            // '모두 선택' 체크박스 상태 동기화
            ChkFilterSelectAll.IsChecked = _currentFilterItems.All(x => x.IsChecked);

            // 5) 팝업 열기
            HeaderFilterPopup.PlacementTarget = btn;
            HeaderFilterPopup.IsOpen = true;
        }

        // 🌟 [Excel 필터 이벤트 핸들러] 개별 체크박스 선택 시 '모두 선택' 상태 자동 동기화
        private void FilterItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFilterItems != null)
            {
                var view = CollectionViewSource.GetDefaultView(LstFilterItems.ItemsSource);
                if (view != null)
                {
                    var visibleItems = view.Cast<FilterValueItem>().ToList();
                    if (visibleItems.Count > 0)
                    {
                        ChkFilterSelectAll.IsChecked = visibleItems.All(x => x.IsChecked);
                    }
                }
                else
                {
                    ChkFilterSelectAll.IsChecked = _currentFilterItems.All(x => x.IsChecked);
                }
            }
        }

        // 🌟 [Excel 필터 이벤트 핸들러] '(모두 선택)' 체크박스 클릭 시 일괄 처리
        private void ChkFilterSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (ChkFilterSelectAll.IsChecked == null) return;
            bool isChecked = ChkFilterSelectAll.IsChecked.Value;

            var view = CollectionViewSource.GetDefaultView(LstFilterItems.ItemsSource);
            if (view != null)
            {
                foreach (var item in view.Cast<FilterValueItem>())
                {
                    item.IsChecked = isChecked;
                }
            }
            else
            {
                foreach (var item in _currentFilterItems)
                {
                    item.IsChecked = isChecked;
                }
            }
        }

        // 🌟 [Excel 필터 이벤트 핸들러] 팝업 내부 실시간 고속 검색
        private void TxtFilterSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(LstFilterItems.ItemsSource);
            if (view == null) return;

            string query = TxtFilterSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is FilterValueItem item)
                    {
                        return (item.Value ?? "").ToLower().Contains(query);
                    }
                    return false;
                };
            }

            // 검색 결과물에 따른 '모두 선택' 체크 상태 재평가
            var visibleItems = view.Cast<FilterValueItem>().ToList();
            if (visibleItems.Count > 0)
            {
                ChkFilterSelectAll.IsChecked = visibleItems.All(x => x.IsChecked);
            }
            else
            {
                ChkFilterSelectAll.IsChecked = false;
            }
        }

        // 🌟 [Excel 필터 이벤트 핸들러] 필터 조건 적용
        private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFilteringColumn == null)
            {
                HeaderFilterPopup.IsOpen = false;
                return;
            }

            string headerName = _currentFilteringColumn.Header?.ToString() ?? "";
            if (string.IsNullOrEmpty(headerName)) return;

            // 1) 체크된 고유값 수집
            var checkedValues = _currentFilterItems
                .Where(x => x.IsChecked)
                .Select(x => x.Value)
                .ToHashSet();

            int totalUniqueCount = _currentFilterItems.Count;
            if (checkedValues.Count == totalUniqueCount)
            {
                // 모두 체크된 경우 필터 제거
                if (_activeFilters.ContainsKey(headerName))
                {
                    _activeFilters.Remove(headerName);
                }
                UpdateHeaderFilterVisual(_currentFilteringColumn, false);
            }
            else
            {
                // 일부만 체크된 경우 필터 적용
                _activeFilters[headerName] = checkedValues;
                UpdateHeaderFilterVisual(_currentFilteringColumn, true);
            }

            // 2) 교차(AND) 필터 적용 및 팝업 닫기
            ApplyFilter();
            HeaderFilterPopup.IsOpen = false;
        }

        // 🌟 [Excel 필터 이벤트 핸들러] 필터 조건 취소
        private void BtnCancelFilter_Click(object sender, RoutedEventArgs e)
        {
            HeaderFilterPopup.IsOpen = false;
        }

        // 🌟 [Excel 필터 비주얼 헬퍼] 특정 열의 깔때기 아이콘 색상 변경
        private void UpdateHeaderFilterVisual(DataGridColumn column, bool isActive)
        {
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter>(GridElements);
            if (presenter == null) return;

            var headers = FindVisualChildren<System.Windows.Controls.Primitives.DataGridColumnHeader>(presenter);
            foreach (var header in headers)
            {
                if (header.Column == column)
                {
                    var btn = FindVisualChild<Button>(header);
                    if (btn != null)
                    {
                        var path = FindVisualChild<System.Windows.Shapes.Path>(btn);
                        if (path != null)
                        {
                            path.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? "#6366F1" : "#94A3B8"));
                        }
                    }
                    break;
                }
            }
        }

        // 🌟 [Excel 필터 헬퍼] 비주얼 트리 상향 탐색
        public static T? FindVisualAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void UpdateSelectionVisualsFromSelectedCells()
        {
            var currentColsWithSelectedCells = new HashSet<DataGridColumn>();
            foreach (var cellInfo in GridElements.SelectedCells)
            {
                if (cellInfo.Column != null)
                {
                    currentColsWithSelectedCells.Add(cellInfo.Column);
                }
            }

            _selectedColumns.Clear();
            foreach (var col in currentColsWithSelectedCells)
            {
                _selectedColumns.Add(col);
            }
            UpdateHeaderSelectionVisuals();
        }

        // 9) Highlight select row in Revit 3D Viewport
        private void GridElements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 배치 선택 진행 중에는 수천번 반복되는 내부 그리드 동기화와 UI 이벤트를 전부 스킵하여 렉(Lag)을 완전히 제거함!
            if (_isBatchSelecting) return;

            // 🌟 [드래그 렉 해결 핵심 보호막] 마우스 드래그 범위 선택 중(왼쪽 버튼 클릭 중)일 때는 동기적 헤더 비주얼 갱신과 Revit API 호출을 일시 건너뜀!
            // 이는 마우스를 놓는 최종 시점에 단 1회만 비동기로 실행되어 렉을 완전히 분쇄합니다.
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            UpdateSelectionVisualsFromSelectedCells();
            TriggerRevitSelection();
        }

        // 🌟 [드래그 완료 최종 일괄 처리] 드래그 선택을 완료하고 마우스를 뗄 때 비로소 렉 없이 1회만 동기화 전달
        private void GridElements_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isBatchSelecting) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSelectionVisualsFromSelectedCells();
                TriggerRevitSelection();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TriggerRevitSelection()
        {
            if (GridElements.SelectedCells.Count > 0)
            {
                var firstCell = GridElements.SelectedCells[0];
                if (firstCell.Item is ExtractedElement selected)
                {
                    OnElementSelectRequested?.Invoke(selected.Id);
                    
                    SetStatus($"🔍 선택됨: ID {selected.Id} | {selected.Category} | {selected.Family} - {selected.Type}");
                }
            }
        }

        private void GridElements_ColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            if (e.Column == null || e.Column.Header == null) return;

            // Get mouse position relative to GridElements
            var mousePos = Mouse.GetPosition(GridElements);

            // If dragged upwards out of the header panel (Y < 0), uncheck it!
            if (mousePos.Y < 0)
            {
                string columnName = e.Column.Header.ToString() ?? "";
                if (string.IsNullOrEmpty(columnName)) return;

                string normalizedColumnName = columnName switch
                {
                    "길이 (m)" => "길이",
                    "면적 (㎡)" => "면적",
                    "체적 (㎥)" => "체적",
                    _ => columnName
                };

                bool updated = false;

                // 1) Search in Custom Parameter groups
                var builtinParam = BuiltInParams.FirstOrDefault(x => x.Name == normalizedColumnName);
                if (builtinParam != null)
                {
                    builtinParam.IsChecked = false;
                    updated = true;
                }

                var projectParam = ProjectParams.FirstOrDefault(x => x.Name == normalizedColumnName);
                if (projectParam != null)
                {
                    projectParam.IsChecked = false;
                    updated = true;
                }

                var sharedParam = SharedParams.FirstOrDefault(x => x.Name == normalizedColumnName);
                if (sharedParam != null)
                {
                    sharedParam.IsChecked = false;
                    updated = true;
                }

                if (updated)
                {
                    RebuildDynamicColumns();

                    if (ChkSortSelectedToTop.IsChecked == true)
                    {
                        ApplyParameterSorting();
                    }

                    // Instantly refresh ListBox items to reflect unchecked state in Sidebar checkboxes!
                    LstBuiltInParams.Items.Refresh();
                    LstProjectParams.Items.Refresh();
                    LstSharedParams.Items.Refresh();

                    SetStatus($"🗑️ '{columnName}' 열이 위로 드래그되어 제외(체크 해제)되었습니다.");
                }
            }
        }

        public void SetStatus(string msg)
        {
            // 🌟 [UI 성능 극대화] 하단 상태 표시줄(LblStatus)이 제거됨에 따라, 상태 변경에 따른 백그라운드 스레드 마샬링 및 UI 업데이트 연산을 완전 차단(No-Op)합니다.
        }

        public void ShowLoading(string title, string subTitle)
        {
            Dispatcher.Invoke(() =>
            {
                // [모던 UX] 새로운 스캔이 돌기 시작하면 기존 표의 데이터를 깨끗이 소거하여 투명 상태 유도
                if (GridElements != null)
                {
                    GridElements.ItemsSource = null;
                }

                // 스캔 로딩 시작 시 Empty State 패널을 즉시 숨겨 로딩 스피너의 주목도를 극대화
                if (EmptyStatePanel != null)
                {
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                }

                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                }
                if (SpinnerGrid != null)
                {
                    SpinnerGrid.Visibility = Visibility.Visible;
                }
                if (CompleteIcon != null)
                {
                    CompleteIcon.Visibility = Visibility.Collapsed;
                }
                if (TxtLoadingTitle != null)
                {
                    TxtLoadingTitle.Text = title;
                }
                if (TxtLoadingSubTitle != null)
                {
                    TxtLoadingSubTitle.Text = subTitle;
                    TxtLoadingSubTitle.Visibility = Visibility.Visible;
                }
                if (BtnExtract != null)
                {
                    BtnExtract.IsEnabled = false;
                }
                // Ensure confirm button hidden while loading
                if (BtnConfirmLoading != null)
                {
                    BtnConfirmLoading.Visibility = Visibility.Collapsed;
                }
                if (AskScanGrid != null) AskScanGrid.Visibility = Visibility.Collapsed;
                if (AskButtonPanel != null) AskButtonPanel.Visibility = Visibility.Collapsed;
            });
        }

        public void ShowAskScan()
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;
                if (SpinnerGrid != null) SpinnerGrid.Visibility = Visibility.Collapsed;
                if (CompleteIcon != null) CompleteIcon.Visibility = Visibility.Collapsed;
                if (AskScanGrid != null) AskScanGrid.Visibility = Visibility.Visible;

                if (TxtLoadingTitle != null) TxtLoadingTitle.Text = "파라미터 스캔 대기";
                if (TxtLoadingSubTitle != null)
                {
                    TxtLoadingSubTitle.Text = "3D 뷰의 물리 객체 및 파라미터를 스캔하시겠습니까?\n(객체가 많을 경우 다소 시간이 소요될 수 있습니다.)";
                    TxtLoadingSubTitle.Visibility = Visibility.Visible;
                }

                if (AskButtonPanel != null) AskButtonPanel.Visibility = Visibility.Visible;
                if (BtnConfirmLoading != null) BtnConfirmLoading.Visibility = Visibility.Collapsed;
                if (BtnExtract != null) BtnExtract.IsEnabled = false;
            });
        }

        private void BtnStartScan_Click(object sender, RoutedEventArgs e)
        {
            ShowLoading("Parameter 스캔 중...", "3D 뷰의 물리 요소와 가용 매개변수를 수집하고 있습니다.");
            OnScanRequested?.Invoke();
        }

        private void BtnCancelScan_Click(object sender, RoutedEventArgs e)
        {
            HideLoading();
            if (BtnExtract != null) BtnExtract.IsEnabled = true;
        }

        public void ShowLoading()
        {
            ShowLoading("스캔 중...", "3D 뷰 실물 객체를 정밀 분석하고 있습니다");
        }

        public void HideLoading()
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
                if (BtnExtract != null)
                {
                    BtnExtract.IsEnabled = true;
                }
            });
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static bool IsGripper(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is System.Windows.Controls.Primitives.Thumb)
                    return true;
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return false;
        }

        // 🌟 [Excel 필터 우회 판별기] 클릭된 원본 요소가 필터 깔때기 버튼(BtnFilter) 계통인지 탐색
        private static bool IsFilterButton(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is Button btn && btn.Name == "BtnFilter")
                    return true;
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return false;
        }

        private void ColumnHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.DataGridColumnHeader header && header.Column != null)
            {
                // 클릭한 근원 요소(OriginalSource)가 컬럼 크기조절 구분선(Thumb)이나 그 하위 요소(Border 등)인 경우 드래그 크기조절을 허용하도록 리턴
                if (e.OriginalSource is DependencyObject depObj && IsGripper(depObj))
                {
                    return; 
                }

                // 🌟 [필터 버튼 예외 처리] 클릭한 곳이 깔때기 필터 버튼인 경우 열 선택 로직을 완전 차단하여 엑셀 스타일 드롭다운 팝업만 활성화되게 함
                if (e.OriginalSource is DependencyObject depObj2 && IsFilterButton(depObj2))
                {
                    return;
                }

                var clickedColumn = header.Column;

                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

                _isBatchSelecting = true; // 대량 셀 일괄 작업 시작 지시 (렉 차단)
                try
                {
                    // Shift 키 범위 선택 처리 (엑셀 명품 사양)
                    if (isShiftPressed && _lastClickedColumn != null && GridElements.Columns.Contains(_lastClickedColumn))
                    {
                        int index1 = GridElements.Columns.IndexOf(_lastClickedColumn);
                        int index2 = GridElements.Columns.IndexOf(clickedColumn);

                        int start = Math.Min(index1, index2);
                        int end = Math.Max(index1, index2);

                        // Ctrl이 섞여있지 않다면 기존 선택 범위를 완전히 비움
                        if (!isCtrlPressed)
                        {
                            GridElements.SelectedCells.Clear();
                            _selectedColumns.Clear();
                        }

                        // 범위 내부의 모든 컬럼 일괄 선택
                        for (int i = start; i <= end; i++)
                        {
                            var col = GridElements.Columns[i];
                            if (!_selectedColumns.Contains(col))
                            {
                                _selectedColumns.Add(col);
                                foreach (var item in GridElements.Items)
                                {
                                    GridElements.SelectedCells.Add(new DataGridCellInfo(item, col));
                                }
                            }
                        }
                    }
                    else
                    {
                        // 일반 단일 선택 또는 Ctrl 복합 누적 선택
                        if (!isCtrlPressed)
                        {
                            GridElements.SelectedCells.Clear();
                            _selectedColumns.Clear();
                        }

                        if (_selectedColumns.Contains(clickedColumn))
                        {
                            _selectedColumns.Remove(clickedColumn);
                            var toRemove = GridElements.SelectedCells.Where(c => c.Column == clickedColumn).ToList();
                            foreach (var cell in toRemove)
                            {
                                GridElements.SelectedCells.Remove(cell);
                            }
                        }
                        else
                        {
                            _selectedColumns.Add(clickedColumn);
                            foreach (var item in GridElements.Items)
                            {
                                GridElements.SelectedCells.Add(new DataGridCellInfo(item, clickedColumn));
                            }
                        }
                    }
                }
                finally
                {
                    _isBatchSelecting = false; // 일괄 작업 완료 및 락 해제
                }

                // 마지막으로 클릭한 컬럼을 추적 기록하여 다음 Shift-Click의 기준점으로 확보
                _lastClickedColumn = clickedColumn;

                UpdateSelectionVisualsFromSelectedCells();
                e.Handled = true; // 중요: 헤더 본연의 정렬(Sorting) 이벤트를 무력화하고 오직 컬럼 '선택'으로 기능하게 함
            }
        }

        private void ColumnHeader_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.DataGridColumnHeader header && header.Column != null)
            {
                // 오직 컬럼간 구분선(Gripper/Separator)인 Thumb 영역 또는 그 하위 영역을 더블 클릭했을 때만 리사이징 가동 (IsGripper 헬퍼 경유)
                if (e.OriginalSource is DependencyObject depObj && IsGripper(depObj))
                {
                    // 엑셀 사양: 여러 열이 다중 선택된 상태에서 어느 구분선을 더블클릭하든 선택된 모든 열들의 너비를 일괄 리사이징 피팅!
                    if (_selectedColumns.Count > 1)
                    {
                        foreach (var col in _selectedColumns)
                        {
                            col.Width = 0;
                            col.Width = DataGridLength.Auto;
                        }
                        e.Handled = true;
                    }
                    else
                    {
                        // 선택된 열이 없거나 1개인 경우 더블클릭된 해당 단일 컬럼을 자동 피팅
                        header.Column.Width = 0;
                        header.Column.Width = DataGridLength.Auto;
                        e.Handled = true;
                    }
                }
            }
        }

        private void UpdateHeaderSelectionVisuals()
        {
            // 🌟 [최극강 성능 튜닝] DataGrid 전체 자식 노드(수천 개)를 돌던 비효율을 깨부수고, 헤더들을 가둔 'DataGridColumnHeadersPresenter' 영역만 콕 집어 타겟팅함!
            // 이 수색 국소화 튜닝을 통해 드래그 범위 선택 및 셀 다중 선택 시 렉(Lag)을 0ms 수준으로 압축.
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter>(GridElements);
            if (presenter == null) return;

            var headers = FindVisualChildren<System.Windows.Controls.Primitives.DataGridColumnHeader>(presenter);
            foreach (var header in headers)
            {
                if (header.Column != null)
                {
                    // 🌟 [원래대로 복원] 셀 선택 시 헤더의 배경색을 연보라색으로 바꾸는 비주얼 피드백을 제거하고, 원래의 고유 회색 스타일로 상시 유지함
                    header.ClearValue(Control.BackgroundProperty);
                }
            }
        }

        // 🌟 [단일 타겟 비주얼 트리 수색 헬퍼] 최초 일치하는 특정 컨트롤 하나만 가볍게 찾아 반환
        public static T? FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var grandChild in FindVisualChildren<T>(child))
                {
                    yield return grandChild;
                }
            }
        }

        private void LstParams_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListBox listBox && e.Key >= Key.A && e.Key <= Key.Z)
            {
                // 입력받은 알파벳 문자 ('a' ~ 'z') 계산
                char pressedChar = (char)('a' + (e.Key - Key.A));
                
                var items = listBox.ItemsSource as System.Collections.IEnumerable;
                if (items != null)
                {
                    // 1) 매칭되는 파라미터 항목들과 그 당시의 절대 인덱스를 튜플 목록으로 수집
                    var matches = new List<(ParameterItem Item, int Index)>();
                    int index = 0;
                    foreach (var item in items)
                    {
                        if (item is ParameterItem paramItem && !string.IsNullOrEmpty(paramItem.Name))
                        {
                            string name = paramItem.Name.Trim().ToLower();
                            if (name.StartsWith(pressedChar.ToString()))
                            {
                                matches.Add((paramItem, index));
                            }
                        }
                        index++;
                    }

                    // 2) 매칭되는 항목이 있는 경우 뫼비우스의 띠 순환 법칙 처리
                    if (matches.Count > 0)
                    {
                        // 현재 선택된 아이템이 매칭 리스트 중 어디에 있는지 인덱스 조회
                        var currentItem = listBox.SelectedItem as ParameterItem;
                        int currentMatchIndex = -1;
                        if (currentItem != null)
                        {
                            currentMatchIndex = matches.FindIndex(m => m.Item == currentItem);
                        }

                        // 다음 이동 타겟의 인덱스 계산 (뫼비우스의 띠 공식)
                        int nextMatchIndex = 0;
                        if (currentMatchIndex != -1)
                        {
                            nextMatchIndex = (currentMatchIndex + 1) % matches.Count;
                        }

                        var target = matches[nextMatchIndex];
                        var targetItem = target.Item;
                        int targetIndex = target.Index;

                        // 3) 선택 변경 및 ScrollViewer 최상단 밀어올림
                        listBox.SelectedItem = targetItem;

                        var scrollViewer = FindVisualChildren<ScrollViewer>(listBox).FirstOrDefault();
                        if (scrollViewer != null)
                        {
                            scrollViewer.ScrollToVerticalOffset(targetIndex);
                        }
                        else
                        {
                            listBox.ScrollIntoView(targetItem);
                        }

                        // 4) 가상화 렉 방지 비동기 포커스 처리
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                        {
                            var container = listBox.ItemContainerGenerator.ContainerFromItem(targetItem) as ListBoxItem;
                            if (container != null)
                            {
                                container.Focus();
                            }
                        }));

                        e.Handled = true;
                    }
                }
            }
        }

        // 🌟 [탭 전환 자동 포커싱] 탭 클릭 시 내부의 리스트박스를 자동 탐색하여 키보드 포커스를 인계하여 즉시 단축키 작동이 가능하게 함
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    // 선택된 탭 아래의 ListBox를 시각적 탐색을 통해 획득
                    var listBox = FindVisualChildren<ListBox>(selectedTab).FirstOrDefault();
                    if (listBox != null)
                    {
                        // 탭 전환 완료 및 렌더링 안정화 이후 비동기 포커스 처리로 렉 예방
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                        {
                            listBox.Focus();
                            
                            // 선택된 아이템이 이미 있다면 해당 아이템 컨테이너로, 없다면 첫 번째 아이템 컨테이너로 포커스 시도
                            var item = listBox.SelectedItem ?? (listBox.Items.Count > 0 ? listBox.Items[0] : null);
                            if (item != null)
                            {
                                var container = listBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                                if (container != null)
                                {
                                    container.Focus();
                                }
                            }
                        }));
                    }
                }
            }
        }

        private void GridElements_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            
            // 사용자가 컬럼 헤더 클릭 시, 1순위 기본 정렬 방향을 '내림차순(Descending)'으로 강제 설정! (Excel/CAD 명품 실무 요건)
            if (column.SortDirection == null)
            {
                column.SortDirection = System.ComponentModel.ListSortDirection.Descending;
                
                var view = CollectionViewSource.GetDefaultView(GridElements.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription(column.SortMemberPath, System.ComponentModel.ListSortDirection.Descending));
                    view.Refresh();
                }
                e.Handled = true;
            }
            else if (column.SortDirection == System.ComponentModel.ListSortDirection.Descending)
            {
                // 내림차순 상태에서 2차 클릭 시 '오름차순(Ascending)'으로 토글
                column.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
                
                var view = CollectionViewSource.GetDefaultView(GridElements.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription(column.SortMemberPath, System.ComponentModel.ListSortDirection.Ascending));
                    view.Refresh();
                }
                e.Handled = true;
            }
            else
            {
                // 3차 클릭 시 정렬 해제 및 기본 내림차순(ID 기준) 상태로 복원
                column.SortDirection = null;
                var view = CollectionViewSource.GetDefaultView(GridElements.ItemsSource);
                if (view != null)
                {
                    view.SortDescriptions.Clear();
                    view.Refresh();
                }
                ApplyFilter(); // 기본 내림차순 데이터셋 상태로 복원
                e.Handled = true;
            }
        }
        // 🌟 레빗 오토데스크 라이트 테마 자동 동기화 적용
        private void ApplyLightTheme()
        {
            try
            {
                // 0) Dynamic Hover/Selection Brush Resources for Light Theme
                this.Resources["TabHoverBackground"] = new SolidColorBrush(Color.FromArgb(0x12, 0, 0, 0)); // 7% black transparent
                this.Resources["TabHoverForeground"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22)); // dark charcoal
                
                this.Resources["ListBoxItemHoverBackground"] = new SolidColorBrush(Color.FromArgb(0x0C, 0, 0, 0)); // 5% black transparent
                this.Resources["ListBoxItemHoverBorder"] = new SolidColorBrush(Color.FromArgb(0x15, 0, 0, 0));
                this.Resources["ListBoxItemSelectedBackground"] = new SolidColorBrush(Color.FromArgb(0x1A, 0, 0, 0)); // 10% black transparent
                this.Resources["ListBoxItemSelectedBorder"] = new SolidColorBrush(Color.FromArgb(0x28, 0, 0, 0));

                // 1) 윈도우 배경 및 전경색 변경
                this.Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF6));
                this.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));

                // 2) 마스터 테두리 및 투명감 브러시 갱신
                var masterBorder = this.Content as Border;
                if (masterBorder != null)
                {
                    masterBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xD0));
                    masterBorder.Background = new LinearGradientBrush(
                        Color.FromRgb(0xFA, 0xFA, 0xFC),
                        Color.FromRgb(0xF0, 0xF0, 0xF4),
                        45.0
                    );
                }

                // 3) 사이드바 및 메인 그리드 패널 백그라운드 색상 정밀 반전 조정
                if (masterBorder?.Child is Grid mainGrid)
                {
                    // Row 1: 메인 작업 영역 Grid
                    var workAreaGrid = mainGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 1);
                    if (workAreaGrid != null)
                    {
                        var borders = workAreaGrid.Children.OfType<Border>().ToList();
                        foreach (var border in borders)
                        {
                            border.Background = new SolidColorBrush(Color.FromArgb(0xF0, 0xFD, 0xFD, 0xFE));
                            border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDE, 0xDE, 0xE2));
                        }

                        var sidebar = workAreaGrid.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 0);
                        if (sidebar != null)
                        {
                            // 사이드바 내부의 모든 텍스트 및 체크박스 전경색을 다크톤(#1F1F22)으로 일괄 자동 조율! (눈부심 방지 가독성 극대화)
                            var textBlocks = FindVisualChildren<TextBlock>(sidebar);
                            foreach (var tb in textBlocks)
                            {
                                tb.Foreground = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1C));
                            }

                            var checkBoxes = FindVisualChildren<CheckBox>(sidebar);
                            foreach (var cb in checkBoxes)
                            {
                                cb.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));
                            }

                            var tabItems = FindVisualChildren<TabItem>(sidebar);
                            foreach (var tab in tabItems)
                            {
                                tab.Foreground = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x36));
                            }
                        }
                    }

                    // 4) 타이틀바 색상 밝은 톤으로 세련되게 반전
                    var titleBar = mainGrid.Children.OfType<Border>().FirstOrDefault(b => Grid.GetRow(b) == 0);
                    if (titleBar != null)
                    {
                        titleBar.Background = new SolidColorBrush(Color.FromArgb(0xE0, 0xF3, 0xF3, 0xF6));
                        titleBar.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xE8));
                        var tbText = FindVisualChildren<TextBlock>(titleBar).FirstOrDefault();
                        if (tbText != null)
                        {
                            tbText.Foreground = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x42));
                        }
                    }
                }

                // 5) 데이터그리드(GridElements) 라이트 스킨 및 격자 라인 최적화
                GridElements.Background = Brushes.Transparent;
                GridElements.RowBackground = new SolidColorBrush(Color.FromRgb(0xFD, 0xFD, 0xFE));
                GridElements.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xFD, 0xFD, 0xFE));
                GridElements.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));
                GridElements.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xE8));
                GridElements.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xEC));
                GridElements.VerticalGridLinesBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xEC));

                // 6) 데이터그리드(GridElements) 라이트 스킨 격자라인 최적화 (검색창이 소거됨에 따라 검색창 스타일 제외)

                // 7) 하단 개수 레이블 가독성 색상 연동
                LblCount.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x54));

                // 8) 체크박스 및 상태 라벨 색상 연동
                ChkSortSelectedToTop.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0x99));

                // 9) 리스트박스 포그라운드 명시적 변경
                LstBuiltInParams.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));
                LstProjectParams.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));
                LstSharedParams.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x22));
            }
            catch
            {
                // UI 스킨 리다이렉션 중 예외 발생 시 크래시 방지 안전 필터 적용
            }
        }

        // 🌟 윈도우 빈 바탕 클릭 시 검색 필터 포커스 및 좌측 파라미터 선택 자동 해제 핸들러
        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 1) 클릭된 근원 노드부터 상위로 타고 올라가며 TextBox 및 ListBox, CheckBox 존재 여부 판별
            var dep = e.OriginalSource as DependencyObject;
            bool isInsideParameterArea = false;
            var tempDep = dep;

            while (tempDep != null)
            {
                if (tempDep is System.Windows.Controls.TextBox)
                {
                    break;
                }
                if (tempDep is System.Windows.Controls.ListBox || tempDep is System.Windows.Controls.CheckBox)
                {
                    isInsideParameterArea = true;
                    break;
                }
                tempDep = VisualTreeHelper.GetParent(tempDep);
            }

            // 2) 클릭된 계통이 Parameter 영역(ListBox, CheckBox)이 아니면, 좌측 파라미터 리스트박스의 선택을 즉시 해제함
            if (!isInsideParameterArea)
            {
                if (LstBuiltInParams != null) LstBuiltInParams.SelectedIndex = -1;
                if (LstProjectParams != null) LstProjectParams.SelectedIndex = -1;
                if (LstSharedParams != null) LstSharedParams.SelectedIndex = -1;
            }

            // 3) 클릭된 근원 노드부터 상위로 타고 올라가며 TextBox 객체 존재 여부 판별
            var textBoxDep = dep;
            while (textBoxDep != null && !(textBoxDep is System.Windows.Controls.TextBox))
            {
                textBoxDep = VisualTreeHelper.GetParent(textBoxDep);
            }

            // 4) 클릭된 계통이 TextBox가 아닌 경우
            if (textBoxDep == null)
            {
                // 현재 포커스가 TextBox에 있는 경우 포커스를 제거하여 텍스트박스 활성화를 취소함
                if (Keyboard.FocusedElement is System.Windows.Controls.TextBox)
                {
                    Keyboard.ClearFocus();
                    (this.Content as UIElement)?.Focus(); // 최상단 Border로 포커스 정착
                }
            }
        }

        // 🌟 [스크롤뷰어 우상단 코너 갭 강제 패치]
        // Revit 자체 테마 엔진이 WPF implicit style을 강제로 덮어씌워버리는 문제를 극복하기 위해,
        // 런타임 Visual Tree 탐색을 통해 ScrollViewer 우상단 코너(Row 0, Column 2)의 Border를 강제로 감지하고 정밀 도색합니다.
        private void ApplyScrollViewerCornerFix()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(GridElements);
                    if (scrollViewer != null && VisualTreeHelper.GetChildrenCount(scrollViewer) > 0)
                    {
                        var grid = VisualTreeHelper.GetChild(scrollViewer, 0) as Grid;
                        if (grid != null)
                        {
                            // Row 0, Column 2에 해당하는 우상단 Corner Border 요소를 조회
                            var existing = grid.Children.Cast<UIElement>()
                                .FirstOrDefault(x => Grid.GetRow(x) == 0 && Grid.GetColumn(x) == 2);
                            
                            if (existing is Border border)
                            {
                                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                                border.BorderThickness = new Thickness(0, 0, 0, 1);
                            }
                            else if (existing == null)
                            {
                                var newBorder = new Border
                                {
                                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                                    BorderThickness = new Thickness(0, 0, 0, 1),
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    HorizontalAlignment = HorizontalAlignment.Stretch
                                };
                                Grid.SetRow(newBorder, 0);
                                Grid.SetColumn(newBorder, 2);
                                grid.Children.Add(newBorder);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScrollViewerCornerFix Error] {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // 🌟 [ESC 입력 무력화 및 예외 처리]
        // ESC 키 입력 시 메인 창이나 필터 팝업 창이 절대 닫히지 않도록 키 입력을 터널링 단에서 가로채 무력화합니다.
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        // 🌟 [패밀리 명칭 치환 비즈니스 로직 및 프리셋 핸들러]
        private void LoadInitialMappings(List<ExtractedElement> elements)
        {
            if (elements == null) return;

            MappingItems.Clear();

            // 1) 기본 로컬 캐시 로드 (%APPDATA%/CostBIM/family_mappings.json)
            var savedDict = LoadSavedMappingsFromFile(_defaultPresetPath);

            // 2) 고유 패밀리 리스트 추출 (정렬 적용)
            var uniqueFamilies = elements
                .Where(x => !string.IsNullOrEmpty(x.Family))
                .Select(x => Tuple.Create(x.Category, x.Family))
                .Distinct()
                .OrderBy(x => x.Item1)
                .ThenBy(x => x.Item2)
                .ToList();

            foreach (var item in uniqueFamilies)
            {
                string category = item.Item1;
                string family = item.Item2;

                string remapped = "";
                bool isLengthSum = false;
                bool isAreaSum = false;
                bool isCountSum = true;

                if (savedDict != null && savedDict.ContainsKey(family))
                {
                    var config = savedDict[family];
                    remapped = config.RemappedFamily ?? "";
                    isLengthSum = config.IsLengthSumChecked;
                    isAreaSum = config.IsAreaSumChecked;
                    isCountSum = config.IsCountSumChecked;
                }
                else
                {
                    // CFT코드 지능형 자동 맵핑 추천
                    string upperFam = family.ToUpper();
                    if (upperFam.Contains("CFT"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(family, @"\d+.*");
                        if (match.Success)
                        {
                            remapped = $"CFT기둥 ({match.Value.Trim()})";
                        }
                        else
                        {
                            remapped = "CFT기둥";
                        }
                    }
                }

                MappingItems.Add(new FamilyMappingItem
                {
                    Category = category,
                    OriginalFamily = family,
                    RemappedFamily = remapped,
                    IsLengthSumChecked = isLengthSum,
                    IsAreaSumChecked = isAreaSum,
                    IsCountSumChecked = isCountSum
                });
            }
        }

        private Dictionary<string, FamilyConfig> LoadSavedMappingsFromFile(string path)
        {
            var result = new Dictionary<string, FamilyConfig>();
            try
            {
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            string key = prop.Name;
                            var valElement = prop.Value;
                            var config = new FamilyConfig();

                            if (valElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                config.RemappedFamily = valElement.GetString() ?? "";
                            }
                            else if (valElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                if (valElement.TryGetProperty("RemappedFamily", out var rf))
                                    config.RemappedFamily = rf.GetString() ?? "";
                                if (valElement.TryGetProperty("IsLengthSumChecked", out var len))
                                    config.IsLengthSumChecked = len.GetBoolean();
                                if (valElement.TryGetProperty("IsAreaSumChecked", out var area))
                                    config.IsAreaSumChecked = area.GetBoolean();
                                if (valElement.TryGetProperty("IsCountSumChecked", out var cnt))
                                    config.IsCountSumChecked = cnt.GetBoolean();
                            }
                            result[key] = config;
                        }
                    }
                }
            }
            catch
            {
                // 무시
            }
            return result;
        }

        private void SaveCurrentMappings()
        {
            try
            {
                if (MappingItems == null || MappingItems.Count == 0) return;
                var dict = MappingItems.ToDictionary(
                    x => x.OriginalFamily, 
                    x => new FamilyConfig 
                    { 
                        RemappedFamily = x.RemappedFamily,
                        IsLengthSumChecked = x.IsLengthSumChecked,
                        IsAreaSumChecked = x.IsAreaSumChecked,
                        IsCountSumChecked = x.IsCountSumChecked
                    }
                );
                string json = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_defaultPresetPath, json);
            }
            catch
            {
                // 무시
            }
        }

        // 📥 [프리셋 가져오기] 사용자 프리셋 파일 로드 및 결합
        private void BtnImportPreset_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CostBIM 맵핑 프리셋 (*.json;*.cbmpreset)|*.json;*.cbmpreset|모든 파일 (*.*)|*.*",
                Title = "패밀리 맵핑 프리셋 가져오기"
            };

            if (openDialog.ShowDialog() == true)
            {
                var imported = LoadSavedMappingsFromFile(openDialog.FileName);
                if (imported == null || imported.Count == 0)
                {
                    MessageBox.Show("프리셋 파일에 유효한 매핑 데이터가 없거나 훼손되었습니다.", "가져오기 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int updateCount = 0;
                foreach (var item in MappingItems)
                {
                    if (imported.ContainsKey(item.OriginalFamily))
                    {
                        var config = imported[item.OriginalFamily];
                        item.RemappedFamily = config.RemappedFamily ?? "";
                        item.IsLengthSumChecked = config.IsLengthSumChecked;
                        item.IsAreaSumChecked = config.IsAreaSumChecked;
                        item.IsCountSumChecked = config.IsCountSumChecked;
                        updateCount++;
                    }
                }

                MessageBox.Show($"총 {updateCount}개의 패밀리 맵핑을 프리셋에서 성공적으로 연동해 가져왔습니다!", "가져오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 📤 [프리셋 저장] 현재 설정된 전체 맵핑 사전을 외부 공유 가능한 프리셋 파일로 보존
        private void BtnExportPreset_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CostBIM 맵핑 프리셋 (*.json)|*.json|CostBIM 프리셋 (*.cbmpreset)|*.cbmpreset",
                Title = "패밀리 맵핑 프리셋 외부로 저장",
                FileName = $"CostBIM_Preset_{DateTime.Now:yyyyMMdd}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var dict = MappingItems.ToDictionary(
                        x => x.OriginalFamily, 
                        x => new FamilyConfig 
                        { 
                            RemappedFamily = x.RemappedFamily,
                            IsLengthSumChecked = x.IsLengthSumChecked,
                            IsAreaSumChecked = x.IsAreaSumChecked,
                            IsCountSumChecked = x.IsCountSumChecked
                        }
                    );
                    string json = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(saveDialog.FileName, json);
                    
                    MessageBox.Show("맵핑 프리셋 파일이 성공적으로 저장되었습니다!\n다른 작업자에게 전달하여 맵핑 명칭을 통일해서 작업할 수 있습니다.", "프리셋 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프리셋 저장 중 오류가 발생했습니다:\n{ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 초기화
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("입력된 모든 치환 명칭 및 합산 설정을 초기화하시겠습니까?", "확인", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                foreach (var item in MappingItems)
                {
                    item.RemappedFamily = "";
                    item.IsLengthSumChecked = false;
                    item.IsAreaSumChecked = false;
                    item.IsCountSumChecked = true;
                }
            }
        }

        // ⚙️ Floating Parameter Configuration Panel 토글 및 선택 메서드들
        private void BtnToggleParamPanel_Click(object sender, RoutedEventArgs e)
        {
            if (ParameterConfigPanel != null)
            {
                ParameterConfigPanel.Visibility = ParameterConfigPanel.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
        }

        private void BtnSelectAllParams_Click(object sender, RoutedEventArgs e)
        {
            foreach (var param in BuiltInParams) param.IsChecked = true;
            foreach (var param in ProjectParams) param.IsChecked = true;
            foreach (var param in SharedParams) param.IsChecked = true;
            RebuildDynamicColumns();
        }

        private void BtnDeselectAllParams_Click(object sender, RoutedEventArgs e)
        {
            foreach (var param in BuiltInParams) param.IsChecked = false;
            foreach (var param in ProjectParams) param.IsChecked = false;
            foreach (var param in SharedParams) param.IsChecked = false;
            RebuildDynamicColumns();
        }

        // 🧱 [기초 수량 집계 내보내기 핵심 연산 헬퍼]
        private double GetPropertyValueAsDouble(ExtractedElement elem, string keyPattern)
        {
            if (elem == null || elem.CustomParameters == null) return 0;
            
            foreach (var pair in elem.CustomParameters)
            {
                if (pair.Key.Contains(keyPattern, StringComparison.OrdinalIgnoreCase))
                {
                    string val = pair.Value;
                    if (string.IsNullOrEmpty(val)) continue;
                    
                    string clean = val.Replace(",", "")
                                      .Replace("mm", "")
                                      .Replace("m³", "")
                                      .Replace("m2", "")
                                      .Replace("m3", "")
                                      .Replace("EA", "")
                                      .Trim();
                    if (double.TryParse(clean, out double d))
                    {
                        return d;
                    }
                }
            }
            return 0;
        }

        // 📊 일반 엑셀 내보내기 위임 핸들러
        private void MenuItemExportGeneral_Click(object sender, RoutedEventArgs e)
        {
            BtnExport_Click(sender, e);
        }

        // 🧱 기초 수량 내보내기 엔진
        private void MenuItemExportFoundation_Click(object sender, RoutedEventArgs e)
        {
            if (GridElements.ItemsSource == null || _allElements == null || _allElements.Count == 0)
            {
                MessageBox.Show("내보낼 데이터가 없습니다. 먼저 실행을 눌러 데이터를 스캔해주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel 통합 문서 (*.xls)|*.xls",
                Title = "기초 수량 집계 내보내기",
                FileName = $"CostBIM_Foundation_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xls"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var rules = MappingItems.ToDictionary(x => x.OriginalFamily, x => x);

                    var grouped = _allElements
                        .GroupBy(elem => {
                            string origFam = elem.Family ?? "";
                            if (rules.TryGetValue(origFam, out var rule) && !string.IsNullOrEmpty(rule.RemappedFamily))
                            {
                                return rule.RemappedFamily;
                            }
                            return origFam;
                        })
                        .Select(g => {
                            var firstElem = g.First();
                            string origFam = firstElem.Family ?? "";
                            
                            bool isLengthSum = false;
                            bool isAreaSum = false;
                            bool isCountSum = true;

                            if (rules.TryGetValue(origFam, out var rule))
                            {
                                isLengthSum = rule.IsLengthSumChecked;
                                isAreaSum = rule.IsAreaSumChecked;
                                isCountSum = rule.IsCountSumChecked;
                            }

                            double totalLength = 0;
                            double totalArea = 0;
                            int count = g.Count();

                            if (isLengthSum)
                            {
                                foreach (var elem in g)
                                {
                                    totalLength += GetPropertyValueAsDouble(elem, "길이") + GetPropertyValueAsDouble(elem, "Length");
                                }
                            }

                            if (isAreaSum)
                            {
                                foreach (var elem in g)
                                {
                                    totalArea += GetPropertyValueAsDouble(elem, "면적") + GetPropertyValueAsDouble(elem, "Area");
                                }
                            }

                            return new {
                                Category = firstElem.Category ?? "",
                                RemappedFamily = g.Key,
                                Length = isLengthSum ? $"{totalLength:F2} m" : "-",
                                Area = isAreaSum ? $"{totalArea:F2} ㎡" : "-",
                                Count = isCountSum ? $"{count} EA" : "-"
                            };
                        })
                        .OrderBy(x => x.Category)
                        .ThenBy(x => x.RemappedFamily)
                        .ToList();

                    using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
                        writer.WriteLine("<head>");
                        writer.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
                        writer.WriteLine("<!--[if gte mso 9]>");
                        writer.WriteLine("<xml>");
                        writer.WriteLine(" <x:ExcelWorkbook>");
                        writer.WriteLine("  <x:ExcelWorksheets>");
                        writer.WriteLine("   <x:ExcelWorksheet>");
                        writer.WriteLine("    <x:Name>CostBIM_Foundation</x:Name>");
                        writer.WriteLine("    <x:WorksheetOptions>");
                        writer.WriteLine("     <x:DisplayGridlines/>");
                        writer.WriteLine("    </x:WorksheetOptions>");
                        writer.WriteLine("   </x:ExcelWorksheet>");
                        writer.WriteLine("  </x:ExcelWorksheets>");
                        writer.WriteLine(" </x:ExcelWorkbook>");
                        writer.WriteLine("</xml>");
                        writer.WriteLine("<![endif]-->");
                        writer.WriteLine("<style>");
                        writer.WriteLine("  table { border-collapse: collapse; font-family: 'Malgun Gothic', 'Segoe UI', sans-serif; font-size: 10pt; }");
                        writer.WriteLine("  th { background-color: #6366F1; color: #FFFFFF; font-weight: bold; border: 1px solid #CBD5E1; padding: 10px 14px; height: 28pt; text-align: center; }");
                        writer.WriteLine("  td { border: 1px solid #E2E8F0; padding: 8px 12px; height: 22pt; color: #1E293B; vertical-align: middle; }");
                        writer.WriteLine("  .no-col { background-color: #F1F5F9; color: #64748B; font-weight: bold; text-align: center; border: 1px solid #CBD5E1; }");
                        writer.WriteLine("  .num-cell { text-align: right; }");
                        writer.WriteLine("  .text-cell { text-align: left; }");
                        writer.WriteLine("  .center-cell { text-align: center; }");
                        writer.WriteLine("</style>");
                        writer.WriteLine("</head>");
                        writer.WriteLine("<body>");
                        writer.WriteLine("<h2>🧱 패밀리 명칭 치환 기준 기초 수량 집계표</h2>");
                        writer.WriteLine("<table>");
                        
                        writer.WriteLine("  <tr>");
                        writer.WriteLine("    <th class=\"no-col\" style=\"width: 50px;\">NO.</th>");
                        writer.WriteLine("    <th style=\"width: 150px;\">카테고리</th>");
                        writer.WriteLine("    <th style=\"width: 300px;\">치환 패밀리 명칭</th>");
                        writer.WriteLine("    <th style=\"width: 120px;\">길이 합산</th>");
                        writer.WriteLine("    <th style=\"width: 120px;\">면적 합산</th>");
                        writer.WriteLine("    <th style=\"width: 100px;\">개수 합산</th>");
                        writer.WriteLine("  </tr>");

                        int index = 1;
                        foreach (var row in grouped)
                        {
                            writer.WriteLine("  <tr>");
                            writer.WriteLine($"    <td class=\"no-col\">{index++}</td>");
                            writer.WriteLine($"    <td class=\"center-cell\">{System.Net.WebUtility.HtmlEncode(row.Category)}</td>");
                            writer.WriteLine($"    <td class=\"text-cell\" style=\"font-weight: bold;\">{System.Net.WebUtility.HtmlEncode(row.RemappedFamily)}</td>");
                            writer.WriteLine($"    <td class=\"num-cell\">{row.Length}</td>");
                            writer.WriteLine($"    <td class=\"num-cell\">{row.Area}</td>");
                            writer.WriteLine($"    <td class=\"num-cell\">{row.Count}</td>");
                            writer.WriteLine("  </tr>");
                        }

                        writer.WriteLine("</table>");
                        writer.WriteLine("</body>");
                        writer.WriteLine("</html>");
                    }

                    MessageBox.Show("치환 명칭 기준으로 집계된 기초 수량 보고서가 Excel 파일로 성공적으로 생성되었습니다!", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"보고서 생성 중 오류가 발생했습니다:\n{ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
