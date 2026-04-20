using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiaryApp
{
    public partial class TimeRecordEditWindow : Window
    {
        public TimeRecordEntry EditedRecord { get; private set; }

        private readonly AppData _appData;

        public TimeRecordEditWindow(AppData appData, TimeRecordEntry record)
        {
            InitializeComponent();
            _appData = appData;

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

        private void InitializeTimeComboBoxes()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                string timeStr = $"{hour:D2}:00";
                StartTimeComboBox.Items.Add(timeStr);
                EndTimeComboBox.Items.Add(timeStr);
            }

            EndTimeComboBox.Items.Add("24:00");
        }

        private void LoadRecordData()
        {
            DatePicker.SelectedDate = EditedRecord.Date;

            string startTimeStr = $"{EditedRecord.StartTime.Hours:D2}:00";
            string endTimeStr = Math.Abs(EditedRecord.EndTime.TotalHours - 24) < 0.01
                ? "24:00"
                : $"{EditedRecord.EndTime.Hours:D2}:00";

            StartTimeComboBox.SelectedItem = startTimeStr;
            EndTimeComboBox.SelectedItem = endTimeStr;

            ActivityTextBox.Text = EditedRecord.Activity;
            CategoryTextBox.Text = EditedRecord.Category;
            NotesTextBox.Text = EditedRecord.Notes;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
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

            string startTimeStr = StartTimeComboBox.SelectedItem?.ToString() ?? "08:00";
            string endTimeStr = EndTimeComboBox.SelectedItem?.ToString() ?? "09:00";

            TimeSpan startTime = startTimeStr == "24:00" ? TimeSpan.FromDays(1) : TimeSpan.Parse(startTimeStr);
            TimeSpan endTime = endTimeStr == "24:00" ? TimeSpan.FromDays(1) : TimeSpan.Parse(endTimeStr);

            if (endTime <= startTime)
            {
                MessageBox.Show("结束时间必须晚于开始时间！", "提示");
                return;
            }

            EditedRecord.Date = DatePicker.SelectedDate.Value;
            EditedRecord.StartTime = startTime;
            EditedRecord.EndTime = endTime;
            EditedRecord.Activity = ActivityTextBox.Text.Trim();
            EditedRecord.Category = CategoryTextBox.Text.Trim();
            if (string.IsNullOrEmpty(EditedRecord.Category))
            {
                EditedRecord.Category = "其他";
            }

            EditedRecord.Notes = NotesTextBox.Text;

            bool globalTagAdded = false;
            _appData.GlobalTags ??= new List<string>();
            if (!_appData.GlobalTags.Contains(EditedRecord.Category))
            {
                _appData.GlobalTags.Add(EditedRecord.Category);
                globalTagAdded = true;
            }

            if (globalTagAdded)
            {
                SaveAppData();
            }

            DialogResult = true;
            Close();
        }

        private const string DataFile = "app_data.json";

        private string GetDataFilePath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(appDir, DataFile);
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
                System.IO.File.WriteAllText(GetDataFilePath(), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要删除这条时间记录吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DialogResult = null;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
            _appData.GlobalTags ??= new List<string>();

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
            if (sender is Button button && button.Tag is string tagToDelete && _appData.GlobalTags.Contains(tagToDelete))
            {
                _appData.GlobalTags.Remove(tagToDelete);
                ShowQuickTagPopup();
            }

            e.Handled = true;
        }
    }
}
