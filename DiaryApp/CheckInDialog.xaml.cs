using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Linq;
using System.Windows.Input;

namespace DiaryApp
{
    public partial class CheckInDialog : Window
    {
        public string Notes { get; private set; } = "";
        public List<string> Tags { get; private set; } = new List<string>();
        public List<string> PhotoPaths { get; private set; } = new List<string>();
        private AppData _appData;

        public CheckInDialog(AppData appData, CheckInEntry? existingEntry = null)
        {
            InitializeComponent();
            _appData = appData;
            
            if (existingEntry != null)
            {
                NotesTextBox.Text = existingEntry.Notes;
                if (existingEntry.Tags != null)
                {
                    TagsTextBox.Text = string.Join(",", existingEntry.Tags);
                }
                foreach (var photo in existingEntry.Photos)
                {
                    PhotoPaths.Add(photo);
                    AddPhotoToPreview(photo);
                }
                
                // 如果是查看模式，修改标题和按钮文字
                Title = "打卡详情";
                ConfirmButton.Content = "保存修改";
            }
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filename in openFileDialog.FileNames)
                {
                    PhotoPaths.Add(filename);
                    AddPhotoToPreview(filename);
                }
            }
        }

        private void AddPhotoToPreview(string filePath)
        {
            try
            {
                var border = new Border
                {
                    Width = 100,
                    Height = 100,
                    Margin = new Thickness(0, 0, 10, 0),
                    CornerRadius = new CornerRadius(4),
                    ClipToBounds = true
                };

                var image = new Image
                {
                    Source = new BitmapImage(new Uri(filePath)),
                    Stretch = System.Windows.Media.Stretch.UniformToFill,
                };
                
                border.Child = image;
                border.ToolTip = filePath;

                PhotosPanel.Children.Add(border);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片: {filePath}\n{ex.Message}", "错误");
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Notes = NotesTextBox.Text;
            Tags = TagsTextBox.Text.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => !string.IsNullOrEmpty(t))
                                   .ToList();
            
            // Sync to GlobalTags
            bool globalTagAdded = false;
            foreach (var tag in Tags)
            {
                if (!_appData.GlobalTags.Contains(tag))
                {
                    _appData.GlobalTags.Add(tag);
                    globalTagAdded = true;
                }
            }

            if (globalTagAdded)
            {
                SaveAppData();
            }

            DialogResult = true;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TagsTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (QuickTagPopup != null && !QuickTagPopup.IsOpen)
            {
                ShowQuickTagPopup();
            }
        }

        private void TagsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowQuickTagPopup();
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
            if (sender is TextBlock textBlock)
            {
                string tag = textBlock.Text;
                var currentTags = TagsTextBox.Text.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(t => t.Trim())
                                                  .ToList();
                if (!currentTags.Contains(tag))
                {
                    if (TagsTextBox.Text.Length > 0 && !TagsTextBox.Text.EndsWith(",") && !TagsTextBox.Text.EndsWith("，"))
                    {
                        TagsTextBox.Text += ",";
                    }
                    TagsTextBox.Text += tag;
                }
                QuickTagPopup.IsOpen = false;
            }
        }

        private void DeleteQuickTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagToDelete)
            {
                if (_appData.GlobalTags.Contains(tagToDelete))
                {
                    _appData.GlobalTags.Remove(tagToDelete);
                    ShowQuickTagPopup(); // 刷新列表
                }
            }
            e.Handled = true; // 防止触发其他点击事件
        }
    }
}
