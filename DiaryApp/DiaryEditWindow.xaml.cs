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
        private readonly HashSet<string> _boundParamNames = new HashSet<string> { "金钱", "savings", "Savings" }; // 绑定参数名集合
        private readonly Dictionary<string, decimal> _originalParamValues = new Dictionary<string, decimal>(); // 存储原始参数值，用于计算差值

        public DiaryEntry? ResultEntry { get; private set; }
        public bool IsSaved { get; private set; }

        public DiaryEditWindow(PersonalInfo personalInfo, bool isNewEntry = true)
        {
            InitializeComponent();
            _isNewEntry = isNewEntry;
            _personalInfo = personalInfo;
            this.Title = isNewEntry ? "新增日记" : "编辑日记";
            
            if (isNewEntry)
            {
                DatePicker.SelectedDate = DateTime.Today;
                // 显示初始星期信息
                UpdateWeekDayDisplay();
            }
            
            InitializeRichTextBox();
        }

        public DiaryEditWindow(PersonalInfo personalInfo, DiaryEntry entry)
        {
            InitializeComponent();
            _isNewEntry = false;
            _personalInfo = personalInfo;
            _originalTitle = entry.Title;
            _originalContent = entry.Content;
            this.Title = "编辑日记";
            InitializeRichTextBox();
            LoadEntry(entry);
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
                
                // 遍历所有参数
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
                                // 计算差值
                                decimal originalValue = 0;
                                _originalParamValues.TryGetValue(param.Id, out originalValue);
                                
                                // 差值 = 新值 - 原值
                                decimal change = paramValue - originalValue;
                                totalSavingsChange += change;
                            }
                        }
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
                
                // 获取当前值
                decimal currentValue = 0;
                if (param.Name.Trim().Equals("金钱", StringComparison.OrdinalIgnoreCase))
                {
                    currentValue = _personalInfo.Savings;
                }
                
                // 计算新值
                decimal newValue = currentValue + paramValue;
                
                // 格式化显示
                return $"({(paramValue >= 0 ? "+" : "")}{param.Value} → {newValue.ToString("N2")})";
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
