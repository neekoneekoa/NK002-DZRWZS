using DiaryApp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DiaryApp
{
    public partial class DiaryEditWindow : Window
    {
        private readonly bool _isNewEntry;
        private readonly string _originalTitle = "输入日记标题...";
        private readonly string _originalContent = "";
        private readonly string _originalTagsPlaceholder = "输入标签后按回车添加";
        private readonly string _originalParamNamePlaceholder = "参数名";
        private readonly string _originalParamValuePlaceholder = "值";
        private readonly string _originalParamUnitPlaceholder = "单位";
        private readonly List<string> _photoPaths = new();
        private readonly List<string> _tags = new();
        private readonly List<DiaryParam> _parameters = new();
        private SolidColorBrush _currentTextColor = Brushes.Black;
        private SolidColorBrush _currentBackgroundColor = Brushes.Transparent;
        private double _currentFontSize = 16;
        private bool _isUnderline = false;
        private readonly PersonalInfo _personalInfo;
        private readonly AppData _appData; // 添加AppData成员变量
        private readonly HashSet<string> _boundParamNames = new HashSet<string> { "金钱", "savings", "Savings" }; // 绑定参数名集合
        private readonly Dictionary<string, decimal> _originalParamValues = new Dictionary<string, decimal>(); // 存储原始参数值，用于计算差值

        public DiaryEntry? ResultEntry { get; private set; }
        public bool IsSaved { get; private set; }

        public DiaryEditWindow(PersonalInfo personalInfo, AppData appData, bool isNewEntry = true)
        {
            InitializeComponent();
            _isNewEntry = isNewEntry;
            _personalInfo = personalInfo;
            _appData = appData;
            this.Title = isNewEntry ? "新增日记" : "编辑日记";
            
            if (isNewEntry)
            {
                DatePicker.SelectedDate = DateTime.Today;
                // 显示初始星期信息
                UpdateWeekDayDisplay();
            }
            
            InitializeRichTextBox();
            
            // 绘制饼状图
            DrawPieChart();
        }

        public DiaryEditWindow(PersonalInfo personalInfo, AppData appData, DiaryEntry entry)
        {
            InitializeComponent();
            _isNewEntry = false;
            _personalInfo = personalInfo;
            _appData = appData;
            _originalTitle = entry.Title;
            _originalContent = entry.Content;
            this.Title = "编辑日记";
            InitializeRichTextBox();
            LoadEntry(entry);
            
            // 绘制饼状图
            DrawPieChart();
        }

        private void InitializeRichTextBox()
        {
            if (DiaryContentRichTextBox == null) return;
            
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };
            var flowDoc = new FlowDocument(paragraph);
            DiaryContentRichTextBox.Document = flowDoc;
            
            UpdateFontSizeComboBox();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateWeekDayDisplay();
            // 重新绘制饼状图
            DrawPieChart();
        }

        private void UpdateWeekDayDisplay()
        {
            if (DatePicker.SelectedDate.HasValue && WeekDayTextBlock != null)
            {
                var selectedDate = DatePicker.SelectedDate.Value;
                string weekDay = GetChineseWeekDay(selectedDate.DayOfWeek);
                WeekDayTextBlock.Text = $"({weekDay})";
            }
        }

        private string GetChineseWeekDay(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "星期日",
                DayOfWeek.Monday => "星期一",
                DayOfWeek.Tuesday => "星期二",
                DayOfWeek.Wednesday => "星期三",
                DayOfWeek.Thursday => "星期四",
                DayOfWeek.Friday => "星期五",
                DayOfWeek.Saturday => "星期六",
                _ => ""
            };
        }

        private void UpdateFontSizeComboBox()
        {
            if (FontSizeComboBox == null) return;
            
            string currentSize = _currentFontSize.ToString();
            foreach (ComboBoxItem item in FontSizeComboBox.Items)
            {
                if (item.Content.ToString() == currentSize)
                {
                    FontSizeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LoadEntry(DiaryEntry entry)
        {
            DiaryTitleTextBox.Text = entry.Title;
            DatePicker.SelectedDate = entry.CreatedAt.Date;
            
            // 设置周期类型
            PeriodTypeComboBox.SelectedIndex = (int)entry.PeriodType;
            
            foreach (var tag in entry.Tags)
            {
                _tags.Add(tag);
            }
            RefreshTagsPanel();
            
            foreach (var photo in entry.Photos)
            {
                _photoPaths.Add(photo);
            }
            RefreshPhotosPanel();
            
            // 加载参数
            foreach (var param in entry.Parameters)
            {
                _parameters.Add(param);
                CreateParamRowUI(param);
                
                // 记录原始参数�?
                if (IsBoundParameter(param.Name) && decimal.TryParse(param.Value, out decimal originalValue))
                {
                    _originalParamValues[param.Id] = originalValue;
                }
            }
            
            LoadRichTextContent(entry.Content);
            
            // 显示星期信息
            UpdateWeekDayDisplay();
        }

        private void LoadRichTextContent(string content)
        {
            if (DiaryContentRichTextBox == null || string.IsNullOrWhiteSpace(content))
            {
                InitializeRichTextBox();
                return;
            }
            
            try
            {
                var flowDoc = new FlowDocument();
                var paragraphs = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                foreach (var paraText in paragraphs)
                {
                    if (!string.IsNullOrEmpty(paraText))
                    {
                        var para = new Paragraph
                        {
                            Margin = new Thickness(0),
                            Padding = new Thickness(0)
                        };
                        para.Inlines.Add(paraText);
                        flowDoc.Blocks.Add(para);
                    }
                }
                
                if (flowDoc.Blocks.Count == 0)
                {
                    flowDoc.Blocks.Add(new Paragraph { Margin = new Thickness(0), Padding = new Thickness(0) });
                }
                
                DiaryContentRichTextBox.Document = flowDoc;
            }
            catch
            {
                InitializeRichTextBox();
            }
        }

        private void DiaryTitleTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DiaryTitleTextBox.Text == _originalTitle)
            {
                DiaryTitleTextBox.Text = "";
            }
        }

        private void DiaryTitleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DiaryTitleTextBox.Text))
            {
                DiaryTitleTextBox.Text = _originalTitle;
            }
        }

        private void TagInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TagInputTextBox.Text == _originalTagsPlaceholder)
            {
                TagInputTextBox.Text = "";
                TagInputTextBox.Foreground = Brushes.Black;
            }
            ShowQuickTagPopup();
        }

        private void TagInputTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (QuickTagPopup != null && !QuickTagPopup.IsOpen)
            {
                ShowQuickTagPopup();
            }
        }

        private void ShowQuickTagPopup()
        {
            if (_appData.GlobalTags == null) _appData.GlobalTags = new List<string>();
            
            QuickTagsItemsControl.ItemsSource = null;
            QuickTagsItemsControl.ItemsSource = _appData.GlobalTags;
            
            NoQuickTagsText.Visibility = _appData.GlobalTags.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            QuickTagPopup.IsOpen = true;
        }

        private void QuickTag_Click(object sender, MouseButtonEventArgs e)
        {
            string? tag = null;
            if (sender is Border border && border.DataContext is string t1)
            {
                tag = t1;
            }
            else if (sender is TextBlock textBlock && textBlock.Text is string t2)
            {
                tag = t2;
            }

            if (tag != null)
            {
                if (!_tags.Contains(tag))
                {
                    _tags.Add(tag);
                    RefreshTagsPanel();
                }
                QuickTagPopup.IsOpen = false;
                TagInputTextBox.Focus();
            }
        }

        private void DeleteQuickTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _appData.GlobalTags.Remove(tag);
                ShowQuickTagPopup(); // Refresh list
                e.Handled = true; // Prevent closing popup
            }
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            if (QuickTagPopup != null)
            {
                QuickTagPopup.IsOpen = false;
            }
        }

        private void TagInputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TagInputTextBox.Text))
            {
                TagInputTextBox.Text = _originalTagsPlaceholder;
                TagInputTextBox.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#B2BEC3");
            }
        }

        private void ParamNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ParamNameTextBox.Text == _originalParamNamePlaceholder)
            {
                ParamNameTextBox.Text = "";
                ParamNameTextBox.Foreground = Brushes.Black;
            }
        }

        private void ParamNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ParamNameTextBox.Text))
            {
                ParamNameTextBox.Text = _originalParamNamePlaceholder;
                ParamNameTextBox.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#B2BEC3");
            }
        }

        private void ParamValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ParamValueTextBox.Text == _originalParamValuePlaceholder)
            {
                ParamValueTextBox.Text = "";
                ParamValueTextBox.Foreground = Brushes.Black;
            }
        }

        private void ParamValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ParamValueTextBox.Text))
            {
                ParamValueTextBox.Text = _originalParamValuePlaceholder;
                ParamValueTextBox.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#B2BEC3");
            }
        }

        private void ParamUnitTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ParamUnitTextBox.Text == _originalParamUnitPlaceholder)
            {
                ParamUnitTextBox.Text = "";
                ParamUnitTextBox.Foreground = Brushes.Black;
            }
        }

        private void ParamUnitTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ParamUnitTextBox.Text))
            {
                ParamUnitTextBox.Text = _originalParamUnitPlaceholder;
                ParamUnitTextBox.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#B2BEC3");
            }
        }

        private void TagInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                AddTag();
            }
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private void AddTag()
        {
            var tagText = TagInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(tagText) || tagText == _originalTagsPlaceholder)
            {
                return;
            }
            
            if (!_tags.Contains(tagText))
            {
                _tags.Add(tagText);
                RefreshTagsPanel();
            }
            
            // Save to global tags
            bool globalTagAdded = false;
            if (_appData.GlobalTags == null) _appData.GlobalTags = new List<string>();
            if (!_appData.GlobalTags.Contains(tagText))
            {
                _appData.GlobalTags.Add(tagText);
                globalTagAdded = true;
            }

            if (globalTagAdded)
            {
                SaveAppData();
            }

            TagInputTextBox.Text = "";
            TagInputTextBox.Focus();
        }

        private void RemoveTag(string tag)
        {
            _tags.Remove(tag);
            RefreshTagsPanel();
        }

        private void RemoveParam(DiaryParam param)
        {
            // 这个方法已经被DeleteParam替代，保留空实现以避免编译错误
        }

        private void AddParamButton_Click(object sender, RoutedEventArgs e)
        {
            var paramName = ParamNameTextBox.Text.Trim();
            var paramValue = ParamValueTextBox.Text.Trim();
            var paramUnit = ParamUnitTextBox.Text.Trim();

            // 验证输入
            if (paramName == _originalParamNamePlaceholder || string.IsNullOrWhiteSpace(paramName))
            {
                MessageBox.Show("请输入参数名", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ParamNameTextBox.Focus();
                return;
            }

            if (paramValue == _originalParamValuePlaceholder || string.IsNullOrWhiteSpace(paramValue))
            {
                MessageBox.Show("请输入参数值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ParamValueTextBox.Focus();
                return;
            }

            // 创建新参数
            var param = new DiaryParam
            {
                Id = Guid.NewGuid().ToString(),
                Name = paramName,
                Value = paramValue,
                Unit = paramUnit
            };

            // 添加到参数列表
            _parameters.Add(param);
            
            // 创建参数行UI
            CreateParamRowUI(param);

            // 保持输入框内容不变，不重新聚焦
        }

        private void CreateParamRowUI(DiaryParam param)
        {
            // 创建参数行容器
            var paramRow = new Grid
            {
                Margin = new Thickness(0, 5, 0, 0)
            };
            
            // 设置列定义
            paramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            paramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            paramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            paramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // 检查是否为绑定参数
            bool isBoundParam = IsBoundParameter(param.Name);
            
            // 参数名（不可编辑）
            var nameBorder = new Border
            {
                Background = isBoundParam ? (SolidColorBrush)new BrushConverter().ConvertFrom("#FFE0B2") : (SolidColorBrush)new BrushConverter().ConvertFrom("#E8F5E9"),
                BorderThickness = new Thickness(1),
                BorderBrush = isBoundParam ? (SolidColorBrush)new BrushConverter().ConvertFrom("#FFB74D") : (SolidColorBrush)new BrushConverter().ConvertFrom("#A5D6A7"),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 5, 0)
            };
            
            // 创建参数名和显示值的容器
            var nameStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var nameTextBlock = new TextBlock
            {
                Text = param.Name,
                FontSize = 13,
                Foreground = isBoundParam ? Brushes.DarkRed : Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 5, 0)
            };
            
            // 显示修改后的值
            var newValueTextBlock = new TextBlock
            {
                Text = GetNewValueDisplay(param),
                FontSize = 11,
                Foreground = isBoundParam ? Brushes.DarkRed : Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            
            nameStackPanel.Children.Add(nameTextBlock);
            nameStackPanel.Children.Add(newValueTextBlock);
            nameBorder.Child = nameStackPanel;
            Grid.SetColumn(nameBorder, 0);
            
            // 参数值（可编辑）
            var valueBorder = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E3F2FD"),
                BorderThickness = new Thickness(1),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#64B5F6"),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(5, 0, 5, 0)
            };
            var valueTextBox = new TextBox
            {
                Text = param.Value,
                FontSize = 13,
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4)
            };
            // 绑定参数值变更
            valueTextBox.TextChanged += (s, e) => 
            {
                param.Value = valueTextBox.Text;
                if (isBoundParam)
                {
                    // 更新显示的新值
                    newValueTextBlock.Text = GetNewValueDisplay(param);
                }
            };
            valueBorder.Child = valueTextBox;
            Grid.SetColumn(valueBorder, 1);
            
            // 参数单位（不可编辑）
            var unitBorder = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E8F5E9"),
                BorderThickness = new Thickness(1),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#A5D6A7"),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(5, 0, 5, 0)
            };
            var unitTextBlock = new TextBlock
            {
                Text = param.Unit,
                FontSize = 13,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 4, 8, 4)
            };
            unitBorder.Child = unitTextBlock;
            Grid.SetColumn(unitBorder, 2);
            
            // 删除按钮
            var deleteButton = new Button
            {
                Content = "×",
                Width = 30,
                Height = 26,
                Margin = new Thickness(5, 0, 0, 0),
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6B6B"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Tag = param
            };
            deleteButton.Click += (s, e) => DeleteParam(param, paramRow);
            Grid.SetColumn(deleteButton, 3);
            
            // 添加所有控件到参数�?
            paramRow.Children.Add(nameBorder);
            paramRow.Children.Add(valueBorder);
            paramRow.Children.Add(unitBorder);
            paramRow.Children.Add(deleteButton);
            
            // 添加参数行到容器
            ParamsContainer.Children.Add(paramRow);
        }

        private void DeleteParam(DiaryParam param, Grid paramRow)
        {
            // 从参数列表中移除
            _parameters.Remove(param);
            
            // 从UI中移除
            ParamsContainer.Children.Remove(paramRow);
        }
        
        // 检查是否为绑定参数
        private bool IsBoundParameter(string paramName)
        {
            return _boundParamNames.Contains(paramName.Trim());
        }
        
        // 更新绑定参数到个人信息
        private void UpdateBoundParameters()
        {
            try
            {
                decimal totalSavingsChange = 0;
                var processedIds = new HashSet<string>();
                
                // 1. 处理当前存在的参数（修改或新增）
                foreach (var param in _parameters)
                {
                    if (IsBoundParameter(param.Name))
                    {
                        if (decimal.TryParse(param.Value, out decimal paramValue))
                        {
                            string trimmedName = param.Name.Trim();
                            if (trimmedName.Equals("金钱", StringComparison.OrdinalIgnoreCase) || 
                                trimmedName.Equals("savings", StringComparison.OrdinalIgnoreCase) || 
                                trimmedName.Equals("Savings", StringComparison.OrdinalIgnoreCase))
                            {
                                // 获取原值
                                decimal originalValue = 0;
                                if (!string.IsNullOrEmpty(param.Id) && _originalParamValues.TryGetValue(param.Id, out originalValue))
                                {
                                    processedIds.Add(param.Id);
                                }
                                
                                // 差值 = 新值 - 原值
                                decimal change = paramValue - originalValue;
                                totalSavingsChange += change;
                            }
                        }
                    }
                }

                // 2. 处理已删除的参数（在原值中有，但当前参数列表中没有，或者改名为非绑定参数）
                foreach (var kvp in _originalParamValues)
                {
                    if (!processedIds.Contains(kvp.Key))
                    {
                        // 该参数已被删除或不再是绑定参数，需要减去其原值贡献
                        // 相当于：新值(0) - 原值
                        totalSavingsChange -= kvp.Value;
                    }
                }
                
                // 更新个人信息
                if (totalSavingsChange != 0)
                {
                    _personalInfo.Savings += totalSavingsChange;
                    _personalInfo.LastUpdated = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新绑定参数失败：{ex.Message}");
            }
        }

        // 获取修改后的值显示
        private string GetNewValueDisplay(DiaryParam param)
        {
            if (!IsBoundParameter(param.Name))
            {
                return string.Empty;
            }
            
            try
            {
                // 获取参数值
                if (!decimal.TryParse(param.Value, out decimal paramValue))
                {
                    return string.Empty;
                }
                
                // 获取当前总金额
                decimal currentTotal = 0;
                if (param.Name.Trim().Equals("金钱", StringComparison.OrdinalIgnoreCase) || 
                    param.Name.Trim().Equals("savings", StringComparison.OrdinalIgnoreCase) || 
                    param.Name.Trim().Equals("Savings", StringComparison.OrdinalIgnoreCase))
                {
                    currentTotal = _personalInfo.Savings;
                }
                
                // 获取原始值（如果是新添加的参数，原始值为0）
                decimal originalValue = 0;
                if (!string.IsNullOrEmpty(param.Id))
                {
                    _originalParamValues.TryGetValue(param.Id, out originalValue);
                }
                
                // 计算基础值（排除当前参数贡献后的金额）
                decimal baseValue = currentTotal - originalValue;
                
                // 计算新值
                decimal newValue = baseValue + paramValue;
                
                // 格式化显示: (4995 + 5 = 5000)
                string operatorStr = paramValue >= 0 ? "+" : "";
                return $"({baseValue:N0} {operatorStr} {paramValue:N0} = {newValue:N0})";
            }
            catch
            {
                return string.Empty;
            }
        }

        private void RefreshTagsPanel()
        {
            if (TagsPanel == null) return;
            
            TagsPanel.Children.Clear();
            foreach (var tag in _tags)
            {
                var border = new Border
                {
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#A29BFE"),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 5, 5)
                };
                
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var textBlock = new TextBlock
                {
                    Text = tag,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                
                var removeButton = new Button
                {
                    Content = "×",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Width = 18,
                    Height = 18,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    Background = Brushes.Transparent,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Tag = tag
                };
                removeButton.Click += (s, e) => RemoveTag(tag);
                
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(removeButton);
                border.Child = stackPanel;
                
                TagsPanel.Children.Add(border);
            }
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|所有文件|*.*",
                Multiselect = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    if (!_photoPaths.Contains(filePath))
                    {
                        _photoPaths.Add(filePath);
                    }
                }
                RefreshPhotosPanel();
            }
        }

        private void RemovePhoto(string photoPath)
        {
            _photoPaths.Remove(photoPath);
            RefreshPhotosPanel();
        }

        private void RefreshPhotosPanel()
        {
            if (DiaryPhotosPanel == null) return;
            
            DiaryPhotosPanel.Children.Clear();
            foreach (var photoPath in _photoPaths)
            {
                var border = new Border
                {
                    Width = 100,
                    Height = 100,
                    Margin = new Thickness(5),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#DFE6E9")
                };
                
                var grid = new Grid();
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(photoPath)),
                    Stretch = Stretch.UniformToFill
                };
                
                var removeButton = new Button
                {
                    Content = "×",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Width = 20,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, -5, -5, 0),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6B6B"),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Tag = photoPath
                };
                removeButton.Click += (s, e) => RemovePhoto(photoPath);
                
                grid.Children.Add(image);
                grid.Children.Add(removeButton);
                border.Child = grid;
                
                DiaryPhotosPanel.Children.Add(border);
            }
        }
        
        /// <summary>
        /// 绘制饼状图
        /// </summary>
        private void DrawPieChart()
        {
            // 清空画布
            PieChartCanvas.Children.Clear();
            LegendPanel.Children.Clear();
            
            // 获取当前选中的日期
            DateTime selectedDate = DatePicker.SelectedDate ?? DateTime.Today;
            
            // 获取当日的时间记录
            var dayRecords = GetDayTimeRecords(selectedDate);
            
            // 计算活动统计信息，包含"无"活动类型
            var activityStats = GetActivityStatsWithNoRecord(dayRecords);
            
            // 绘制饼状图
            DrawPieChartSegments(activityStats);
            
            // 绘制图例
            DrawLegend(activityStats);
            
            // 显示今日完成任务和打卡项目
            DisplayTodayTasks();
            DisplayTodayCheckIns();
        }
        
        /// <summary>
        /// 获取当日的时间记录
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>当日的时间记录列表</returns>
        private List<TimeRecordEntry> GetDayTimeRecords(DateTime date)
        {
            return _appData.TimeRecords
                .Where(t => t.Date.Date == date.Date)
                .OrderBy(t => t.StartTime)
                .ToList();
        }
        
        /// <summary>
        /// 按活动名分组计算时间占比
        /// </summary>
        /// <param name="records">时间记录列表</param>
        /// <returns>活动统计信息</returns>
        private List<(string Activity, double DurationHours, double Percentage)> GetActivityStats(List<TimeRecordEntry> records)
        {
            // 按活动名分组
            var grouped = records.GroupBy(r => r.Activity)
                .Select(g => new 
                {
                    Activity = g.Key,
                    DurationHours = g.Sum(r => r.DurationHours)
                })
                .OrderByDescending(g => g.DurationHours)
                .ToList();
            
            // 计算总时间
            double totalHours = grouped.Sum(g => g.DurationHours);
            
            // 计算百分比
            return grouped.Select(g => (g.Activity, g.DurationHours, g.DurationHours / totalHours * 100)).ToList();
        }
        
        /// <summary>
        /// 按活动名分组计算时间占比，包含"无"活动类型
        /// </summary>
        /// <param name="records">时间记录列表</param>
        /// <returns>活动统计信息</returns>
        private List<(string Activity, double DurationHours, double Percentage)> GetActivityStatsWithNoRecord(List<TimeRecordEntry> records)
        {
            const double DAY_HOURS = 24.0;
            
            // 按活动名分组
            var grouped = records.GroupBy(r => r.Activity)
                .Select(g => new 
                {
                    Activity = g.Key,
                    DurationHours = g.Sum(r => r.DurationHours)
                })
                .OrderByDescending(g => g.DurationHours)
                .ToList();
            
            // 计算有记录的总时间
            double recordedHours = grouped.Sum(g => g.DurationHours);
            
            // 计算无记录的时间
            double noRecordHours = Math.Max(0, DAY_HOURS - recordedHours);
            
            // 如果有无记录时间，添加到分组中
            if (noRecordHours > 0)
            {
                grouped.Add(new { Activity = "无", DurationHours = noRecordHours });
            }
            
            // 计算百分比
            return grouped.Select(g => (g.Activity, g.DurationHours, g.DurationHours / DAY_HOURS * 100)).ToList();
        }
        
        /// <summary>
        /// 绘制饼状图的各个扇形
        /// </summary>
        /// <param name="activityStats">活动统计信息</param>
        private void DrawPieChartSegments(List<(string Activity, double DurationHours, double Percentage)> activityStats)
        {
            double width = PieChartCanvas.Width;
            double height = PieChartCanvas.Height;
            double centerX = width / 2;
            double centerY = height / 2;
            double radius = Math.Min(width, height) / 2.5;
            
            // 预定义颜色
            var colors = new List<SolidColorBrush>
            {
                new SolidColorBrush(Color.FromRgb(108, 92, 231)), // 紫色
                new SolidColorBrush(Color.FromRgb(255, 118, 117)), // 红色
                new SolidColorBrush(Color.FromRgb(0, 184, 148)), // 绿色
                new SolidColorBrush(Color.FromRgb(255, 195, 0)), // 黄色
                new SolidColorBrush(Color.FromRgb(52, 152, 219)), // 蓝色
                new SolidColorBrush(Color.FromRgb(155, 89, 182)), // 深紫色
                new SolidColorBrush(Color.FromRgb(230, 126, 34)), // 橙色
                new SolidColorBrush(Color.FromRgb(46, 204, 113)), // 浅绿色
                new SolidColorBrush(Color.FromRgb(149, 165, 166)), // 灰色
                new SolidColorBrush(Color.FromRgb(241, 196, 15)), // 亮黄色
            };
            
            double startAngle = -90; // 从顶部开始绘制
            int colorIndex = 0;
            
            foreach (var (activity, duration, percentage) in activityStats)
            {
                double angle = percentage / 100 * 360;
                
                // 创建扇形路径
                System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                path.Fill = colors[colorIndex % colors.Count];
                
                // 创建路径几何
                PathGeometry geometry = new PathGeometry();
                PathFigure figure = new PathFigure();
                figure.StartPoint = new Point(centerX, centerY);
                
                // 创建扇形弧
                ArcSegment arc = new ArcSegment();
                arc.Point = new Point(
                    centerX + radius * Math.Cos((startAngle + angle) * Math.PI / 180),
                    centerY + radius * Math.Sin((startAngle + angle) * Math.PI / 180));
                arc.Size = new Size(radius, radius);
                arc.IsLargeArc = angle > 180;
                arc.SweepDirection = SweepDirection.Clockwise;
                
                // 添加线段
                figure.Segments.Add(new LineSegment(new Point(
                    centerX + radius * Math.Cos(startAngle * Math.PI / 180),
                    centerY + radius * Math.Sin(startAngle * Math.PI / 180)), true));
                figure.Segments.Add(arc);
                figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));
                
                geometry.Figures.Add(figure);
                path.Data = geometry;
                
                // 添加到画布
                PieChartCanvas.Children.Add(path);
                
                // 在扇形中显示百分比
                if (percentage > 5) // 只在百分比大于5%时显示文字
                {
                    double textAngle = startAngle + angle / 2;
                    double textRadius = radius / 1.5;
                    Point textPosition = new Point(
                        centerX + textRadius * Math.Cos(textAngle * Math.PI / 180),
                        centerY + textRadius * Math.Sin(textAngle * Math.PI / 180));
                    
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = $"{percentage:F1}%";
                    textBlock.FontSize = 12;
                    textBlock.Foreground = Brushes.White;
                    textBlock.FontWeight = FontWeights.Bold;
                    textBlock.TextAlignment = TextAlignment.Center;
                    textBlock.Width = 50;
                    textBlock.Height = 20;
                    
                    Canvas.SetLeft(textBlock, textPosition.X - 25);
                    Canvas.SetTop(textBlock, textPosition.Y - 10);
                    
                    PieChartCanvas.Children.Add(textBlock);
                }
                
                startAngle += angle;
                colorIndex++;
            }
        }
        
        /// <summary>
        /// 绘制图例
        /// </summary>
        /// <param name="activityStats">活动统计信息</param>
        private void DrawLegend(List<(string Activity, double DurationHours, double Percentage)> activityStats)
        {
            // 预定义颜色
            var colors = new List<SolidColorBrush>
            {
                new SolidColorBrush(Color.FromRgb(108, 92, 231)), // 紫色
                new SolidColorBrush(Color.FromRgb(255, 118, 117)), // 红色
                new SolidColorBrush(Color.FromRgb(0, 184, 148)), // 绿色
                new SolidColorBrush(Color.FromRgb(255, 195, 0)), // 黄色
                new SolidColorBrush(Color.FromRgb(52, 152, 219)), // 蓝色
                new SolidColorBrush(Color.FromRgb(155, 89, 182)), // 深紫色
                new SolidColorBrush(Color.FromRgb(230, 126, 34)), // 橙色
                new SolidColorBrush(Color.FromRgb(46, 204, 113)), // 浅绿色
                new SolidColorBrush(Color.FromRgb(149, 165, 166)), // 灰色
                new SolidColorBrush(Color.FromRgb(241, 196, 15)), // 亮黄色
            };
            
            int colorIndex = 0;
            
            foreach (var (activity, duration, percentage) in activityStats)
            {
                // 创建图例项
                StackPanel legendItem = new StackPanel();
                legendItem.Orientation = Orientation.Horizontal;
                legendItem.Margin = new Thickness(0, 5, 0, 0);
                
                // 颜色方块
                Rectangle colorRect = new Rectangle();
                colorRect.Width = 20;
                colorRect.Height = 20;
                colorRect.Fill = colors[colorIndex % colors.Count];
                colorRect.Margin = new Thickness(0, 0, 10, 0);
                
                // 活动信息
                TextBlock activityText = new TextBlock();
                activityText.Text = $"{activity} ({duration:F1}小时, {percentage:F1}%)";
                activityText.FontSize = 13;
                activityText.VerticalAlignment = VerticalAlignment.Center;
                
                // 添加到图例面板
                legendItem.Children.Add(colorRect);
                legendItem.Children.Add(activityText);
                LegendPanel.Children.Add(legendItem);
                
                colorIndex++;
            }
        }
        
        /// <summary>
        /// 显示今日完成任务
        /// </summary>
        private void DisplayTodayTasks()
        {
            // 清空列表
            TodayTasksListBox.Items.Clear();
            
            // 获取当前选中的日期
            DateTime selectedDate = DatePicker.SelectedDate ?? DateTime.Today;
            
            // 获取今日完成的任务
            var todayCompletedTasks = _appData.Tasks
                .Where(t => t.Status == TaskStatus.Completed && 
                           t.CompletedAt.HasValue && 
                           t.CompletedAt.Value.Date == selectedDate.Date)
                .OrderBy(t => t.CompletedAt)
                .ToList();
            
            // 如果没有完成任务，显示提示信息
            if (todayCompletedTasks.Count == 0)
            {
                TodayTasksListBox.Items.Add("今日没有完成的任务");
                return;
            }
            
            // 显示任务
            foreach (var task in todayCompletedTasks)
            {
                var taskItem = new TextBlock
                {
                    Text = $"• {task.Title}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(45, 52, 54)),
                    Margin = new Thickness(0, 2, 0, 2)
                };
                TodayTasksListBox.Items.Add(taskItem);
            }
        }
        
        /// <summary>
        /// 显示今日打卡项目
        /// </summary>
        private void DisplayTodayCheckIns()
        {
            // 清空列表
            TodayCheckInsListBox.Items.Clear();
            
            // 获取当前选中的日期
            DateTime selectedDate = DatePicker.SelectedDate ?? DateTime.Today;
            
            // 获取今日的打卡记录
            var todayCheckIns = _appData.CheckIns
                .Where(c => c.Date.Date == selectedDate.Date)
                .OrderBy(c => c.CreatedAt)
                .ToList();
            
            // 如果没有打卡记录，显示提示信息
            if (todayCheckIns.Count == 0)
            {
                TodayCheckInsListBox.Items.Add("今日没有打卡记录");
                return;
            }
            
            // 显示打卡记录
            foreach (var checkIn in todayCheckIns)
            {
                // 获取打卡项目名称
                var project = _appData.CheckInProjects.FirstOrDefault(p => p.Id == checkIn.ProjectId);
                string projectName = project?.Name ?? checkIn.Type;
                
                var checkInItem = new TextBlock
                {
                    Text = $"• {projectName}: {checkIn.Value}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(45, 52, 54)),
                    Margin = new Thickness(0, 2, 0, 2),
                    Tag = checkIn, // 存储 CheckInEntry 对象
                    Cursor = Cursors.Hand
                };

                // 添加鼠标事件
                checkInItem.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        ViewCheckInDetails(checkIn);
                    }
                };
                
                // 添加右键菜单
                var contextMenu = new ContextMenu();
                var viewAllLogsItem = new MenuItem { Header = "查看全部日志" };
                viewAllLogsItem.Click += (s, e) => ViewProjectLogs(checkIn);
                contextMenu.Items.Add(viewAllLogsItem);
                checkInItem.ContextMenu = contextMenu;

                TodayCheckInsListBox.Items.Add(checkInItem);
            }
        }

        private void ViewCheckInDetails(CheckInEntry checkIn)
        {
            try
            {
                var dialog = new CheckInDialog(_appData, checkIn);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    // 更新打卡记录
                    checkIn.Notes = dialog.Notes;
                    checkIn.Tags = dialog.Tags;
                    checkIn.Photos = dialog.PhotoPaths;
                    checkIn.UpdatedAt = DateTime.Now;
                    
                    // 保存数据
                    _appData.LastSaved = DateTime.Now;
                    // 注意：这里我们只修改了内存中的数据，如果主窗口需要刷新，可能需要重新加载
                    // 但由于 AppData 是引用的，所以主窗口的数据也会更新
                    // 不过我们需要手动触发保存到文件
                    // 这里我们简单地保存整个 AppData
                    // 实际项目中可能需要通过事件或接口通知主窗口保存
                    // 这里假设 DiaryEditWindow 可以直接保存数据或数据是共享的
                    
                    // 重新显示列表以反映可能的更改（虽然目前只显示了 Value，但也许将来会显示 Notes）
                    DisplayTodayCheckIns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查看打卡详情失败: {ex.Message}", "错误");
            }
        }

        private void ViewProjectLogs(CheckInEntry checkIn)
        {
            try
            {
                var project = _appData.CheckInProjects.FirstOrDefault(p => p.Id == checkIn.ProjectId);
                if (project == null) return;

                var projectCheckIns = _appData.CheckIns
                    .Where(c => c.ProjectId == project.Id)
                    .ToList();

                var logWindow = new CheckInLogWindow(project, projectCheckIns);
                logWindow.Owner = this;
                logWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查看项目日志失败: {ex.Message}", "错误");
            }
        }
        
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var color = (Color)ColorConverter.ConvertFromString(button.Background.ToString());
                _currentTextColor = new SolidColorBrush(color);
                ApplyTextFormatting();
            }
        }

        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var colorName = button.Tag?.ToString() ?? "White";
                var color = (Color)ColorConverter.ConvertFromString(button.Background.ToString());
                _currentBackgroundColor = new SolidColorBrush(color);
                ApplyTextFormatting();
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeComboBox.SelectedItem is ComboBoxItem item && double.TryParse(item.Content.ToString(), out double size))
            {
                _currentFontSize = size;
                ApplyTextFormatting();
            }
        }

        private void UnderlineToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            _isUnderline = true;
            ApplyTextFormatting();
        }

        private void UnderlineToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _isUnderline = false;
            ApplyTextFormatting();
        }

        private void ClearFormatButton_Click(object sender, RoutedEventArgs e)
        {
            _currentTextColor = Brushes.Black;
            _currentBackgroundColor = Brushes.Transparent;
            _currentFontSize = 16;
            _isUnderline = false;
            UnderlineToggleButton.IsChecked = false;
            UpdateFontSizeComboBox();
            
            if (DiaryContentRichTextBox == null || DiaryContentRichTextBox.Selection == null)
            {
                return;
            }
            
            var selection = DiaryContentRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                var inline = selection.Start.Parent as Inline;
                if (inline != null)
                {
                    inline.TextDecorations = null;
                }
            }
            
            DiaryContentRichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
            DiaryContentRichTextBox.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
            DiaryContentRichTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, 16.0);
        }

        private void ApplyTextFormatting()
        {
            if (DiaryContentRichTextBox == null || DiaryContentRichTextBox.Selection == null)
            {
                return;
            }
            
            var selection = DiaryContentRichTextBox.Selection;
            
            if (!selection.IsEmpty)
            {
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, _currentTextColor);
                selection.ApplyPropertyValue(TextElement.BackgroundProperty, _currentBackgroundColor);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, _currentFontSize);
                
                if (_isUnderline)
                {
                    var inline = selection.Start.Parent as Inline;
                    if (inline != null)
                    {
                        inline.TextDecorations = TextDecorations.Underline;
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            ResultEntry = null;
            DialogResult = false;
            Close();
        }

        private const string DATA_FILE = "app_data.json";

        private string GetDataFilePath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(appDir, DATA_FILE);
        }

        private void SaveAppData()
        {
            try
            {
                _appData.LastSaved = DateTime.Now;
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var json = System.Text.Json.JsonSerializer.Serialize(_appData, options);
                var dataFile = GetDataFilePath();
                System.IO.File.WriteAllText(dataFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.S && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                e.Handled = true;
                SaveButton_Click(sender, e);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var title = DiaryTitleTextBox.Text;
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    MessageBox.Show("请输入日记标题", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    DiaryTitleTextBox.Focus();
                    return;
                }
                
                var content = ConvertRichTextToXaml(DiaryContentRichTextBox.Document);
                var plainText = new TextRange(DiaryContentRichTextBox.Document.ContentStart, DiaryContentRichTextBox.Document.ContentEnd).Text.Trim();
                
                if (string.IsNullOrWhiteSpace(plainText))
                {
                    MessageBox.Show("请输入日记内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    DiaryContentRichTextBox.Focus();
                    return;
                }
                
                if (DatePicker.SelectedDate == null)
                {
                    MessageBox.Show("请选择日期", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var selectedDate = DatePicker.SelectedDate.Value;
                var createdAt = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, 
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                
                ResultEntry = new DiaryEntry
                {
                    Id = _isNewEntry ? Guid.NewGuid().ToString() : "",
                    Title = title,
                    Content = content,
                    Tags = _tags.ToList(),
                    Photos = _photoPaths.ToList(),
                    Parameters = _parameters.ToList(),
                    CreatedAt = createdAt,
                    PeriodType = (DiaryPeriodType)PeriodTypeComboBox.SelectedIndex
                };
                
                if (_isNewEntry)
                {
                    ResultEntry.Id = Guid.NewGuid().ToString();
                }
                
                // 更新绑定参数到个人信息
                UpdateBoundParameters();
                
                IsSaved = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ConvertRichTextToXaml(FlowDocument document)
        {
            var sb = new System.Text.StringBuilder();
            
            foreach (var block in document.Blocks)
            {
                if (block is Paragraph para)
                {
                    sb.AppendLine(ConvertInlineToXaml(para.Inlines));
                }
            }
            
            return sb.ToString();
        }

        private string ConvertInlineToXaml(InlineCollection inlines)
        {
            var sb = new System.Text.StringBuilder();
            
            foreach (var inline in inlines)
            {
                if (inline is Run run)
                {
                    sb.Append(run.Text);
                }
            }
            
            return sb.ToString();
        }
    }
}
