using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiaryApp
{


    public partial class ReminderSettingsWindow : Window
    {
        public TaskEntry TaskEntry { get; private set; }
        public ReminderSettings? ReminderSettings { get; set; }
        public bool IsSaveRequested { get; private set; } = false;
        public bool IsDeleteRequested { get; private set; } = false;

        public ReminderSettingsWindow(TaskEntry taskEntry, ReminderSettings? reminderSettings = null)
        {
            InitializeComponent();
            TaskEntry = taskEntry;
            ReminderSettings = reminderSettings;
            InitializeUI();
            LoadReminderSettings();
            SetupEventHandlers();
        }

        private void InitializeUI()
        {
            // 显示任务标题
            if (TaskEntry != null && !string.IsNullOrEmpty(TaskEntry.Title))
            {
                TaskTitleTextBlock.Text = TaskEntry.Title;
            }
            else
            {
                TaskTitleTextBlock.Text = "未命名任务";
            }

            // 初始化每月日期选项
            InitializeMonthlyDays();

            // 只有在没有提醒设置的情况下才设置默认值
            if (ReminderSettings == null)
            {
                // 初始化开始日期为当前日期
                StartDatePicker.SelectedDate = DateTime.Now;

                // 默认选择每天
                ReminderTypeComboBox.SelectedIndex = 0;
            }
        }

        private void InitializeMonthlyDays()
        {
            // 创建1-31的日期列表
            var days = new List<int>();
            for (int i = 1; i <= 31; i++)
            {
                days.Add(i);
            }
            MonthlyDaysItemsControl.ItemsSource = days;
        }

        private void SetupEventHandlers()
        {
            // 提醒类型选择事件
            ReminderTypeComboBox.SelectionChanged += ReminderTypeComboBox_SelectionChanged;
            // 日期变化事件
            StartDatePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;
            // 文本框变化事件
            IntervalDaysTextBox.TextChanged += IntervalDaysTextBox_TextChanged;
            ConsecutiveCountTextBox.TextChanged += ConsecutiveCountTextBox_TextChanged;
            // 连续单位变化事件
            ConsecutiveUnitComboBox.SelectionChanged += ConsecutiveUnitComboBox_SelectionChanged;
        }

        private void LoadReminderSettings()
        {
            if (ReminderSettings == null) return;

            // 设置提醒类型
            for (int i = 0; i < ReminderTypeComboBox.Items.Count; i++)
            {
                var item = ReminderTypeComboBox.Items[i] as ComboBoxItem;
                if (item != null && item.Tag as string == ReminderSettings.ReminderType)
                {
                    ReminderTypeComboBox.SelectedIndex = i;
                    break;
                }
            }

            // 设置间隔天数
            IntervalDaysTextBox.Text = ReminderSettings.IntervalDays.ToString();

            // 设置连续时间
            ConsecutiveCountTextBox.Text = ReminderSettings.ConsecutiveCount.ToString();
            for (int i = 0; i < ConsecutiveUnitComboBox.Items.Count; i++)
            {
                var item = ConsecutiveUnitComboBox.Items[i] as ComboBoxItem;
                if (item != null && item.Tag as string == ReminderSettings.ConsecutiveUnit)
                {
                    ConsecutiveUnitComboBox.SelectedIndex = i;
                    break;
                }
            }

            // 设置每周特定天数
            foreach (var checkBox in FindChildren<CheckBox>(WeeklyDaysPanel))
            {
                if (checkBox.Tag is string dayStr && int.TryParse(dayStr, out int day))
                {
                    checkBox.IsChecked = ReminderSettings.WeeklyDays.Contains(day);
                }
            }

            // 设置每月特定天数
            foreach (var checkBox in FindChildren<CheckBox>(MonthlyDaysItemsControl))
            {
                if (checkBox.Tag is int day)
                {
                    checkBox.IsChecked = ReminderSettings.MonthlyDays.Contains(day);
                }
            }

            // 设置开始日期
            StartDatePicker.SelectedDate = ReminderSettings.StartDate;
        }

        private void ReminderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 隐藏所有设置面板
            IntervalSettingsPanel.Visibility = Visibility.Collapsed;
            ConsecutiveSettingsPanel.Visibility = Visibility.Collapsed;
            WeeklyDaysPanel.Visibility = Visibility.Collapsed;
            MonthlyDaysPanel.Visibility = Visibility.Collapsed;

            // 根据选择的提醒类型显示对应的设置面板
            if (ReminderTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string reminderType = selectedItem.Tag as string ?? "Daily";

                switch (reminderType)
                {
                    case "IntervalDays":
                        IntervalSettingsPanel.Visibility = Visibility.Visible;
                        break;
                    case "ConsecutiveDays":
                    case "ConsecutiveWeeks":
                    case "ConsecutiveMonths":
                        ConsecutiveSettingsPanel.Visibility = Visibility.Visible;
                        // 只有在没有提醒设置的情况下才设置默认单位
                        // 如果有保存的提醒设置，应该已经在LoadReminderSettings中设置了单位
                        break;
                    case "WeeklySpecific":
                        WeeklyDaysPanel.Visibility = Visibility.Visible;
                        break;
                    case "MonthlySpecific":
                        MonthlyDaysPanel.Visibility = Visibility.Visible;
                        break;
                }
            }

            // 更新日历和预览
            UpdateCalendarAndPreview();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCalendarAndPreview();
        }

        private void IntervalDaysTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalendarAndPreview();
        }

        private void ConsecutiveCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalendarAndPreview();
        }

        private void ConsecutiveUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCalendarAndPreview();
        }

        private void UpdateCalendarAndPreview()
        {
            // 清除之前的标记
            ReminderCalendar.SelectedDates.Clear();
            ReminderDatesListBox.Items.Clear();

            // 获取当前设置
            string reminderType = "Daily";
            if (ReminderTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                reminderType = selectedItem.Tag as string ?? "Daily";
            }

            DateTime startDate = StartDatePicker.SelectedDate ?? DateTime.Now;

            // 生成预览日期
            var previewDates = GeneratePreviewDates(reminderType, startDate);

            // 清空现有的日期标记和列表
            ReminderCalendar.SelectedDates.Clear();
            ReminderDatesListBox.Items.Clear();

            // 在日历上标记日期
            foreach (var date in previewDates)
            {
                ReminderCalendar.SelectedDates.Add(date);
                ReminderDatesListBox.Items.Add(date.ToString("yyyy-MM-dd"));
            }
        }

        private List<DateTime> GeneratePreviewDates(string reminderType, DateTime startDate)
        {
            var dates = new List<DateTime>();
            int maxPreviewDays = 90; // 预览未来90天

            switch (reminderType)
            {
                case "Daily":
                    // 每天
                    for (int i = 0; i < maxPreviewDays; i++)
                    {
                        dates.Add(startDate.AddDays(i));
                    }
                    break;

                case "IntervalDays":
                    // 间隔几天
                    if (int.TryParse(IntervalDaysTextBox.Text, out int intervalDays) && intervalDays > 0)
                    {
                        for (int i = 0; i < maxPreviewDays; i += intervalDays)
                        {
                            dates.Add(startDate.AddDays(i));
                        }
                    }
                    break;

                case "WeeklySpecific":
                    // 每周特定几天
                    var weeklyDays = new List<int>();
                    foreach (var checkBox in FindChildren<CheckBox>(WeeklyDaysPanel))
                    {
                        if (checkBox.IsChecked == true && checkBox.Tag is string dayStr && int.TryParse(dayStr, out int day))
                        {
                            weeklyDays.Add(day);
                        }
                    }

                    if (weeklyDays.Count > 0)
                    {
                        DateTime currentDate = startDate;
                        while (currentDate <= startDate.AddDays(maxPreviewDays))
                        {
                            if (weeklyDays.Contains((int)currentDate.DayOfWeek + 1)) // 1=周一, 7=周日
                            {
                                dates.Add(currentDate);
                            }
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                    break;

                case "MonthlySpecific":
                    // 每月特定几天
                    var monthlyDays = new List<int>();
                    foreach (var checkBox in FindChildren<CheckBox>(MonthlyDaysItemsControl))
                    {
                        if (checkBox.IsChecked == true && checkBox.Tag is int day)
                        {
                            monthlyDays.Add(day);
                        }
                    }

                    if (monthlyDays.Count > 0)
                    {
                        DateTime currentDate = startDate;
                        while (currentDate <= startDate.AddDays(maxPreviewDays))
                        {
                            if (monthlyDays.Contains(currentDate.Day))
                            {
                                dates.Add(currentDate);
                            }
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                    break;

                case "ConsecutiveDays":
                case "ConsecutiveWeeks":
                case "ConsecutiveMonths":
                    // 连续几天/几周/几个月
                    if (int.TryParse(ConsecutiveCountTextBox.Text, out int consecutiveCount) && consecutiveCount > 0)
                    {
                        string unit = "Days";
                        if (ConsecutiveUnitComboBox.SelectedItem is ComboBoxItem unitItem)
                        {
                            unit = unitItem.Tag as string ?? "Days";
                        }

                        for (int i = 0; i < consecutiveCount; i++)
                        {
                            DateTime date;
                            switch (unit)
                            {
                                case "Days":
                                    date = startDate.AddDays(i);
                                    break;
                                case "Weeks":
                                    date = startDate.AddDays(i * 7);
                                    break;
                                case "Months":
                                    date = startDate.AddMonths(i);
                                    break;
                                default:
                                    date = startDate.AddDays(i);
                                    break;
                            }
                            dates.Add(date);
                        }
                    }
                    break;

                case "ForgettingCurve":
                    // 记忆遗忘曲线：1, 2, 4, 7, 15, 30天
                    var curveIntervals = new[] { 0, 1, 3, 6, 14, 29 };
                    foreach (var interval in curveIntervals)
                    {
                        dates.Add(startDate.AddDays(interval));
                    }
                    break;
            }

            return dates;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建或更新提醒设置
            ReminderSettings = new ReminderSettings
            {
                ReminderType = "Daily",
                IntervalDays = 1,
                ConsecutiveCount = 1,
                ConsecutiveUnit = "Days",
                WeeklyDays = new List<int>(),
                MonthlyDays = new List<int>(),
                StartDate = StartDatePicker.SelectedDate ?? DateTime.Now,
                IsActive = true
            };

            // 获取提醒类型
            if (ReminderTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ReminderSettings.ReminderType = selectedItem.Tag as string ?? "Daily";
            }

            // 获取间隔天数
            if (int.TryParse(IntervalDaysTextBox.Text, out int intervalDays) && intervalDays > 0)
            {
                ReminderSettings.IntervalDays = intervalDays;
            }

            // 获取连续设置
            if (int.TryParse(ConsecutiveCountTextBox.Text, out int consecutiveCount) && consecutiveCount > 0)
            {
                ReminderSettings.ConsecutiveCount = consecutiveCount;
            }
            if (ConsecutiveUnitComboBox.SelectedItem is ComboBoxItem unitItem)
            {
                ReminderSettings.ConsecutiveUnit = unitItem.Tag as string ?? "Days";
            }

            // 获取每周选择的天数
            foreach (var checkBox in FindChildren<CheckBox>(WeeklyDaysPanel))
            {
                if (checkBox.IsChecked == true && checkBox.Tag is string dayStr && int.TryParse(dayStr, out int day))
                {
                    ReminderSettings.WeeklyDays.Add(day);
                }
            }

            // 获取每月选择的天数
            foreach (var checkBox in FindChildren<CheckBox>(MonthlyDaysItemsControl))
            {
                if (checkBox.IsChecked == true && checkBox.Tag is int day)
                {
                    ReminderSettings.MonthlyDays.Add(day);
                }
            }

            IsSaveRequested = true;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 确认删除
            MessageBoxResult result = MessageBox.Show(
                "确定要删除这个任务的提醒设置吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsDeleteRequested = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 辅助方法：查找所有子控件
        private IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    yield return tChild;
                }

                foreach (var grandChild in FindChildren<T>(child))
                {
                    yield return grandChild;
                }
            }
        }
    }
}