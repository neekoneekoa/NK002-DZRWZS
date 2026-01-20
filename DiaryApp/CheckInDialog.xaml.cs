using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DiaryApp
{
    public partial class CheckInDialog : Window
    {
        public string Notes { get; private set; } = "";
        public List<string> PhotoPaths { get; private set; } = new List<string>();

        public CheckInDialog(CheckInEntry? existingEntry = null)
        {
            InitializeComponent();
            
            if (existingEntry != null)
            {
                NotesTextBox.Text = existingEntry.Notes;
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
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}