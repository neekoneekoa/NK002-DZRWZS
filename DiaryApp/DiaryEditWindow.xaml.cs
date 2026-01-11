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
        private readonly List<string> _photoPaths = new();
        private readonly List<string> _tags = new();
        private SolidColorBrush _currentTextColor = Brushes.Black;
        private SolidColorBrush _currentBackgroundColor = Brushes.Transparent;
        private double _currentFontSize = 16;
        private bool _isUnderline = false;

        public DiaryEntry? ResultEntry { get; private set; }
        public bool IsSaved { get; private set; }

        public DiaryEditWindow(bool isNewEntry = true)
        {
            InitializeComponent();
            _isNewEntry = isNewEntry;
            this.Title = isNewEntry ? "新增日记" : "编辑日记";
            
            if (isNewEntry)
            {
                DatePicker.SelectedDate = DateTime.Today;
            }
            
            InitializeRichTextBox();
        }

        public DiaryEditWindow(DiaryEntry entry)
        {
            InitializeComponent();
            _isNewEntry = false;
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
            
            LoadRichTextContent(entry.Content);
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
                
                if (title == _originalTitle || string.IsNullOrWhiteSpace(title))
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
                    CreatedAt = createdAt,
                    PeriodType = (DiaryPeriodType)PeriodTypeComboBox.SelectedIndex
                };
                
                if (_isNewEntry)
                {
                    ResultEntry.Id = Guid.NewGuid().ToString();
                }
                
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
