using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DiaryApp
{
    /// <summary>
    /// CheckInProjectEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CheckInProjectEditWindow : Window
    {
        public CheckInProject Project { get; private set; }
        public List<string> Tags { get; private set; } = new List<string>();
        
        public CheckInProjectEditWindow(CheckInProject project)
        {
            InitializeComponent();
            Project = project;
            LoadProjectData();
        }
        
        private void LoadProjectData()
        {
            NameTextBox.Text = Project.Name;
            TypeTextBox.Text = Project.Type;
            
            // 加载标签
            if (Project.Tags != null)
            {
                Tags = new List<string>(Project.Tags);
                UpdateTagsDisplay();
            }
            
            // 设置标签输入框的提示文本
            if (TagsTextBox.Text == "")
            {
                TagsTextBox.Text = TagsTextBox.Tag as string;
                TagsTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }
        
        private void UpdateTagsDisplay()
        {
            TagsItemsControl.ItemsSource = null;
            TagsItemsControl.ItemsSource = Tags;
        }
        
        private void TagsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TagsTextBox.Text == TagsTextBox.Tag as string)
            {
                TagsTextBox.Text = "";
                TagsTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
            }
        }
        
        private void TagsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TagsTextBox.Text == "")
            {
                TagsTextBox.Text = TagsTextBox.Tag as string;
                TagsTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }
        
        private void TagsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.OemComma)
            {
                string tagText = TagsTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(tagText) && tagText != TagsTextBox.Tag as string)
                {
                    // 如果是逗号，移除逗号
                    if (e.Key == Key.OemComma && tagText.EndsWith(","))
                    {
                        tagText = tagText.Substring(0, tagText.Length - 1).Trim();
                    }
                    
                    // 添加标签
                    if (!Tags.Contains(tagText))
                    {
                        Tags.Add(tagText);
                        UpdateTagsDisplay();
                    }
                    
                    // 清空输入框
                    TagsTextBox.Text = "";
                }
                e.Handled = true;
            }
        }
        
        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                Tags.Remove(tag);
                UpdateTagsDisplay();
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newName = NameTextBox.Text.Trim();
                string newType = TypeTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("项目名称不能为空", "提示");
                    return;
                }
                
                // 更新项目信息
                Project.Name = newName;
                Project.Type = newType;
                Project.Tags = Tags;
                Project.UpdatedAt = DateTime.Now;
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误");
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}