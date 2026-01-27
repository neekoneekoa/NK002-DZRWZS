using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

namespace DiaryApp
{
    /// <summary>
    /// TimeRecordEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TimeRecordEditWindow : Window
    {
        // 用于传递编辑的时间记录
        public TimeRecordEntry EditedRecord { get; private set; }
        private AppData _appData;
        
        public TimeRecordEditWindow(AppData appData, TimeRecordEntry record)
        {
            InitializeComponent();
            _appData = appData;
            // 创建记录的副本，避免直接修改原始对象
            EditedRecord = new TimeRecordEntry
            {
                Id = record.Id,
                Date = record.Date,
                StartTime = record.StartTime,
                EndTime = record.EndTime,
                Activity = record.Activity,
                Category = record.Category,
                Notes = record.Notes,
                CreatedAt = record.CreatedAt
            };
            InitializeTimeComboBoxes();
            LoadRecordData();
        }
        
        // 初始化时间选择下拉框
        private void InitializeTimeComboBoxes()
        {
            // 生成00:00到23:00的时间选项
            for (int hour = 0; hour < 24; hour++)
            {
                string timeStr = $"{hour:D2}:00";
                StartTimeComboBox.Items.Add(timeStr);
                EndTimeComboBox.Items.Add(timeStr);
            }
            // 添加24:00到结束时间选项
            EndTimeComboBox.Items.Add("24:00");
        }
        
        // 加载时间记录数据
        private void LoadRecordData()
        {
            DatePicker.SelectedDate = EditedRecord.Date;
            
            // 设置时间
            string startTimeStr = $"{EditedRecord.StartTime.Hours:D2}:00";
            
            string endTimeStr;
            // 检查是否为24:00（即1天）
            if (Math.Abs(EditedRecord.EndTime.TotalHours - 24) < 0.01)
            {
                endTimeStr = "24:00";
            }
            else
            {
                endTimeStr = $"{EditedRecord.EndTime.Hours:D2}:00";
            }
            
            StartTimeComboBox.SelectedItem = startTimeStr;
            EndTimeComboBox.SelectedItem = endTimeStr;
            
            ActivityTextBox.Text = EditedRecord.Activity;
            
            // 设置分类
            CategoryTextBox.Text = EditedRecord.Category;
            
            NotesTextBox.Text = EditedRecord.Notes;
        }
        
        // 保存按钮点击事件
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("请选择日期！", "提示");
                return;
            }
            
            if (StartTimeComboBox.SelectedItem == null || EndTimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择开始和结束时间！", "提示");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ActivityTextBox.Text))
            {
                MessageBox.Show("请输入活动名称！", "提示");
                return;
            }
            
            // 获取选择的时间
            var startSelected = StartTimeComboBox.SelectedItem;
            var endSelected = EndTimeComboBox.SelectedItem;
            
            if (startSelected == null || endSelected == null)
            {
                MessageBox.Show("请选择开始和结束时间！", "提示");
                return;
            }
            
            string startTimeStr = startSelected.ToString() ?? "08:00";
            string endTimeStr = endSelected.ToString() ?? "09:00";
            
            TimeSpan startTime;
            if (startTimeStr == "24:00")
            {
                startTime = TimeSpan.FromDays(1);
            }
            else
            {
                startTime = TimeSpan.Parse(startTimeStr);
            }

            TimeSpan endTime;
            if (endTimeStr == "24:00")
            {
                endTime = TimeSpan.FromDays(1);
            }
            else
            {
                endTime = TimeSpan.Parse(endTimeStr);
            }
            
            // 验证时间顺序
            if (endTime <= startTime)
            {
                MessageBox.Show("结束时间必须晚于开始时间！", "提示");
                return;
            }
            
            // 更新记录
            EditedRecord.Date = DatePicker.SelectedDate.Value;
            EditedRecord.StartTime = startTime;
            EditedRecord.EndTime = endTime;
            EditedRecord.Activity = ActivityTextBox.Text;
            EditedRecord.Category = CategoryTextBox.Text.Trim();
            if (string.IsNullOrEmpty(EditedRecord.Category))
            {
                EditedRecord.Category = "其他";
            }
            EditedRecord.Notes = NotesTextBox.Text;
            
            // Save category to GlobalTags if new
            bool globalTagAdded = false;
            if (_appData.GlobalTags == null) _appData.GlobalTags = new List<string>();
            if (!_appData.GlobalTags.Contains(EditedRecord.Category))
            {
                _appData.GlobalTags.Add(EditedRecord.Category);
                globalTagAdded = true;
            }

            if (globalTagAdded)
            {
                SaveAppData();
            }

            // 设置对话框结果为OK
            this.DialogResult = true;
            this.Close();
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

        // 删除按钮点击事件
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要删除这条时间记录吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.DialogResult = null; // null表示删除
                this.Close();
            }
        }
        
        // 取消按钮点击事件
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CategoryTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (QuickTagPopup != null && !QuickTagPopup.IsOpen)
            {
                ShowQuickTagPopup();
            }
        }

        private void CategoryTextBox_GotFocus(object sender, RoutedEventArgs e)
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
                CategoryTextBox.Text = textBlock.Text;
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