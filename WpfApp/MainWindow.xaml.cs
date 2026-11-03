using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        private bool isDrawerOpen = false;
        private List<DrawingData> _allData = new List<DrawingData>();
        // 记录当前唯一打开的 PDF 窗口和它对应的路径
        private PdfWindow? _currentPdfWindow = null;
        private string? _currentPdfPath = null;

        public MainWindow()
        {
            InitializeComponent();
            LoadDataFromJson();
            ScanOrphanPdfs();
            LoadDefaultState();
        }

        private void LoadDataFromJson()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string jsonPath = Path.Combine(baseDir, "Assets", "Data.json");
            if (!File.Exists(jsonPath)) return;

            try
            {
                string jsonString = File.ReadAllText(jsonPath);
                var data = JsonSerializer.Deserialize<List<DrawingData>>(jsonString);
                if (data != null) _allData = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取 JSON 数据失败：" + ex.Message);
            }
        }

        private void ScanOrphanPdfs()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string pdfDir = Path.Combine(baseDir, "Assets", "PDFs");
            if (!Directory.Exists(pdfDir)) return;

            var pdfFiles = Directory.GetFiles(pdfDir, "*.pdf", SearchOption.AllDirectories);
            foreach (var file in pdfFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!_allData.Any(d => d.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    string relativePath = Path.GetRelativePath(baseDir, file).Replace("\\", "/");
                    _allData.Add(new DrawingData
                    {
                        Category = "仅有PDF文件",
                        Name = fileName,
                        // 改成字典格式
                        Details = new Dictionary<string, string> { { "提示", "暂无参数数据（仅提供图纸文件）" } },
                        PdfPath = relativePath
                    });
                }
            }
        }

        private void LoadDefaultState()
        {
            SearchResultList.ItemsSource = _allData;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = TxtSearch.Text.Trim();
            PerformSearch(SearchText);
        }

        private void PerformSearch(string keyword)
        {
            ListBoxItem? selectedItem = FlatDirectoryList.SelectedItem as ListBoxItem;
            string? selectedCategory = selectedItem?.Content?.ToString();

            if (string.IsNullOrEmpty(keyword))
            {
                if (selectedCategory != null)
                {
                    SearchResultList.ItemsSource = _allData.Where(d => d.Category.Contains(selectedCategory)).ToList();
                }
                else
                {
                    SearchResultList.ItemsSource = _allData;
                }
                return;
            }

            List<DrawingData> results;
            if (selectedCategory == null)
            {
                // 全局搜索：在 Name、Category 或者 Details 的所有 Value 中查找
                results = _allData.Where(d => d.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              d.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              d.Details.Values.Any(v => v.Contains(keyword, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            else
            {
                // 分类搜索
                results = _allData.Where(d => d.Category.Contains(selectedCategory) &&
                                             (d.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                              d.Details.Values.Any(v => v.Contains(keyword, StringComparison.OrdinalIgnoreCase)))).ToList();
            }

            SearchResultList.ItemsSource = results;
        }

        private void FlatDirectoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlatDirectoryList.SelectedItem is ListBoxItem selectedItem)
            {
                SearchScopeTag.Visibility = Visibility.Visible;
                string category = selectedItem.Content?.ToString() ?? "";
                SearchScopeText.Text = category;
                TxtSearch.Focus();
                TxtSearch.CaretIndex = 0;
            }
            else
            {
                SearchScopeTag.Visibility = Visibility.Collapsed;
            }
            PerformSearch(TxtSearch.Text.Trim());
        }

        private void OpenPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            // 这里的 Tag 现在是 DrawingData 对象
            if (sender is Button btn && btn.Tag is DrawingData data)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string targetPath = "";

                // 1. 优先按照 Name 去查找（推荐方式）
                string namePath = Path.Combine(baseDir, "Assets", "PDFs", $"{data.Name}.pdf");

                if (File.Exists(namePath))
                {
                    targetPath = namePath;
                }
                // 2. 如果按照 Name 找不到，且 JSON 里写了 PdfPath，则尝试用 PdfPath
                else if (!string.IsNullOrEmpty(data.PdfPath))
                {
                    string customPath = Path.Combine(baseDir, data.PdfPath.Replace('/', '\\'));
                    if (File.Exists(customPath))
                    {
                        targetPath = customPath;
                    }
                }

                // 3. 如果都没找到，报错提示
                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                {
                    MessageBox.Show($"找不到图纸文件：\n请检查 Assets/PDFs 目录下是否有名为【{data.Name}.pdf】的文件。",
                                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ================= 单例窗口管理逻辑 =================

                // 如果当前已经有打开的窗口
                if (_currentPdfWindow != null)
                {
                    // 如果点的是同一个图纸，说明用户想关闭它
                    if (_currentPdfPath == targetPath)
                    {
                        _currentPdfWindow.Close();
                        _currentPdfWindow = null;
                        _currentPdfPath = null;
                        return;
                    }
                    else
                    {
                        // 如果点的是新图纸，自动关闭旧窗口
                        _currentPdfWindow.Close();
                        _currentPdfWindow = null;
                        _currentPdfPath = null;
                    }
                }

                // 4. 创建并打开新窗口
                PdfWindow pdfWin = new PdfWindow(targetPath);
                PositionPdfWindow(pdfWin); // 智能贴边吸附

                // 清理内存
                pdfWin.Closed += (s, args) =>
                {
                    if (_currentPdfWindow == pdfWin)
                    {
                        _currentPdfWindow = null;
                        _currentPdfPath = null;
                    }
                };

                _currentPdfWindow = pdfWin;
                _currentPdfPath = targetPath;
                pdfWin.Show();
            }
        }

        // 1. 复制图纸名称 (Name)
        private void CopyName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is DrawingData data)
            {
                Clipboard.SetText(data.Name);
            }
        }

        // 2. 智能复制“代号”
        private void CopyPartNo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is DrawingData data)
            {
                // 查找字典中包含 "零件号" 或 "代号" 的项
                var partNo = data.Details.FirstOrDefault(kv => kv.Key.Contains("零件号") || kv.Key.Contains("代号")).Value;

                if (!string.IsNullOrEmpty(partNo))
                    Clipboard.SetText(partNo);
                else
                    MessageBox.Show("该图纸的参数中没有找到【代号】。", "提示");
            }
        }

        // 4. 复制全部参数
        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is DrawingData data)
            {
                if (data.Details == null || data.Details.Count == 0) return;

                var detailsStr = string.Join(" | ", data.Details.Select(kv => $"{kv.Key}: {kv.Value}"));
                Clipboard.SetText(detailsStr);
            }
        }
        private void PositionPdfWindow(PdfWindow pdfWin)
        {
            var mainWindowPos = this.PointToScreen(new Point(0, 0));
            var mainWindowRect = new Rect(mainWindowPos.X, mainWindowPos.Y, this.ActualWidth, this.ActualHeight);
            var workArea = SystemParameters.WorkArea;

            double rightSpace = workArea.Right - (mainWindowRect.Right + 20);
            double leftSpace = (mainWindowRect.Left - 20) - workArea.Left;

            if (rightSpace >= leftSpace)
                pdfWin.Left = mainWindowRect.Right + 20;
            else
                pdfWin.Left = Math.Max(workArea.Left, mainWindowRect.Left - 20 - pdfWin.Width);

            pdfWin.Top = mainWindowRect.Top;
            if (pdfWin.Top + pdfWin.Height > workArea.Bottom)
                pdfWin.Top = Math.Max(workArea.Top, workArea.Bottom - pdfWin.Height);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && SearchScopeTag.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(TxtSearch.Text) || TxtSearch.CaretIndex == 0)
                {
                    FlatDirectoryList.SelectedItem = null;
                    e.Handled = true;
                }
            }
        }

        private void SearchScopeTag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TxtSearch.Focus();
            TxtSearch.CaretIndex = 0;
        }

        private void HamburgerBtn_Click(object sender, RoutedEventArgs e) => SetDrawerState(!isDrawerOpen);

        private void OverlayLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (isDrawerOpen) SetDrawerState(false);
        }

        private void SetDrawerState(bool isOpen)
        {
            isDrawerOpen = isOpen;
            OverlayLayer.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;

            DoubleAnimation slideAnimation = new DoubleAnimation
            {
                To = isOpen ? 0 : -190,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DrawerTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }
    }

    public class DrawingData
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        // 把 string 改为 Dictionary<string, string>
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
        public string PdfPath { get; set; } = string.Empty;
        public string DisplayName => $"[{Category}] {Name}";
    }
}