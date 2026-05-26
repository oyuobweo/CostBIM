using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace CostBIM.Views
{
    public class FamilyConfig
    {
        public string RemappedFamily { get; set; } = "";
        public bool IsLengthSumChecked { get; set; } = false;
        public bool IsAreaSumChecked { get; set; } = false;
        public bool IsCountSumChecked { get; set; } = true;
    }

    public class FamilyMappingItem : INotifyPropertyChanged
    {
        private string _category = "";
        private string _originalFamily = "";
        private string _remappedFamily = "";
        private bool _isLengthSumChecked = false;
        private bool _isAreaSumChecked = false;
        private bool _isCountSumChecked = true;

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string OriginalFamily
        {
            get => _originalFamily;
            set { _originalFamily = value; OnPropertyChanged(nameof(OriginalFamily)); }
        }

        public string RemappedFamily
        {
            get => _remappedFamily;
            set { _remappedFamily = value; OnPropertyChanged(nameof(RemappedFamily)); }
        }

        public bool IsLengthSumChecked
        {
            get => _isLengthSumChecked;
            set { _isLengthSumChecked = value; OnPropertyChanged(nameof(IsLengthSumChecked)); }
        }

        public bool IsAreaSumChecked
        {
            get => _isAreaSumChecked;
            set { _isAreaSumChecked = value; OnPropertyChanged(nameof(IsAreaSumChecked)); }
        }

        public bool IsCountSumChecked
        {
            get => _isCountSumChecked;
            set { _isCountSumChecked = value; OnPropertyChanged(nameof(IsCountSumChecked)); }
        }

        private Dictionary<string, string> _customParameterValues = new Dictionary<string, string>();
        public Dictionary<string, string> CustomParameterValues
        {
            get => _customParameterValues;
            set { _customParameterValues = value; OnPropertyChanged(nameof(CustomParameterValues)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class FamilyMappingWindow : Window
    {
        public ObservableCollection<FamilyMappingItem> MappingItems { get; } = new ObservableCollection<FamilyMappingItem>();
        private readonly string _defaultPresetPath;

        public FamilyMappingWindow(List<Tuple<string, string>> familyList)
        {
            InitializeComponent();

            // 기본 로컬 세이브 경로 설정 (%APPDATA%/CostBIM/family_mappings.json)
            string appData = Environment.GetFolderPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).GetType() == typeof(string) 
                ? Environment.SpecialFolder.ApplicationData 
                : Environment.SpecialFolder.ApplicationData);
            
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CostBIM");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _defaultPresetPath = Path.Combine(dir, "family_mappings.json");

            // 데이터 리스트 바인딩 및 기존 저장된 로컬 설정 자동 로드
            LoadInitialMappings(familyList);

            GridMapping.ItemsSource = MappingItems;
        }

        private void LoadInitialMappings(List<Tuple<string, string>> familyList)
        {
            // 1) 기본 로컬 캐시 로드 (%APPDATA%/CostBIM/family_mappings.json)
            var savedDict = LoadSavedMappingsFromFile(_defaultPresetPath);

            // 2) 고유 패밀리 리스트 병합
            foreach (var item in familyList)
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
                    // 🌟 [CFT코드 지능형 자동 맵핑 추천] 
                    // 로컬 캐시가 비어 있더라도 패밀리 이름에 'CFT'가 포함되어 있다면 실무 규격 한글 명칭으로 자동 추천
                    string upperFam = family.ToUpper();
                    if (upperFam.Contains("CFT"))
                    {
                        // 정규식을 사용하여 치수 규격 추출 (예: 300x300x12 등)
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
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            string key = prop.Name;
                            var valElement = prop.Value;
                            var config = new FamilyConfig();

                            if (valElement.ValueKind == JsonValueKind.String)
                            {
                                config.RemappedFamily = valElement.GetString() ?? "";
                            }
                            else if (valElement.ValueKind == JsonValueKind.Object)
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
                // 로드 실패 시 무시 및 빈 사전 리턴
            }
            return result;
        }

        // Title Bar Dragging
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Close Button
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
                    string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveDialog.FileName, json);
                    
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

        // 💾 적용 및 내보내기 (기본 로컬에 저장 및 DialogResult True 승인)
        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 기본 로컬 경로 자동 세이브 보장
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
                string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_defaultPresetPath, json);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일 저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
