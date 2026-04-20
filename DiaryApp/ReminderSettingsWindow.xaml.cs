using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DiaryApp
{
    public partial class ReminderSettingsWindow : Window
    {
        public TaskEntry TaskEntry { get; }
        public ReminderSetting? ReminderSettings { get; private set; }
        public bool IsSaveRequested { get; private set; }

        public ReminderSettingsWindow(TaskEntry taskEntry, ReminderSetting? reminderSettings)
        {
            InitializeComponent();

            TaskEntry = taskEntry;
            ReminderSettings = reminderSettings?.Clone() as ReminderSetting ?? new ReminderSetting();

            InitializeUI();
            ReminderTypeComboBox.SelectionChanged += ReminderTypeComboBox_SelectionChanged;
        }

        private void InitializeUI()
        {
            if (ReminderSettings == null)
            {
                ReminderSettings = new ReminderSetting();
            }

            StartDatePicker.SelectedDate = ReminderSettings.StartDate?.Date ?? DateTime.Today;
            ReminderTimeTextBox.Text = ReminderSettings.ReminderTime?.ToString(@"hh\:mm") ?? "20:00";
            ReminderMessageTextBox.Text = string.IsNullOrWhiteSpace(ReminderSettings.ReminderMessage)
                ? $"任务提醒：{TaskEntry.Title}"
                : ReminderSettings.ReminderMessage;

            ReminderTypeComboBox.SelectedIndex = ReminderSettings.ReminderType switch
            {
                ReminderType.Once => 0,
                ReminderType.Daily => 1,
                ReminderType.Weekly => 2,
                ReminderType.Monthly => 3,
                ReminderType.Yearly => 4,
                ReminderType.Interval => 5,
                _ => 1
            };

            var weekDays = ReminderSettings.WeekDays ?? new List<DayOfWeek>();
            MondayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Monday);
            TuesdayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Tuesday);
            WednesdayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Wednesday);
            ThursdayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Thursday);
            FridayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Friday);
            SaturdayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Saturday);
            SundayCheckBox.IsChecked = weekDays.Contains(DayOfWeek.Sunday);

            MonthlyDayNumberComboBox.SelectedIndex = Math.Max(0, Math.Min(4, (ReminderSettings.MonthlyDayNumber ?? 1) - 1));
            MonthlyDayOfWeekComboBox.SelectedIndex = ReminderSettings.MonthlyDayOfWeek switch
            {
                DayOfWeek.Monday => 0,
                DayOfWeek.Tuesday => 1,
                DayOfWeek.Wednesday => 2,
                DayOfWeek.Thursday => 3,
                DayOfWeek.Friday => 4,
                DayOfWeek.Saturday => 5,
                DayOfWeek.Sunday => 6,
                _ => 0
            };

            IntervalDaysTextBox.Text = Math.Max(1, ReminderSettings.IntervalDays ?? 1).ToString();
            StatusActiveRadio.IsChecked = ReminderSettings.IsEnabled && ReminderSettings.IsActive;
            StatusInactiveRadio.IsChecked = !(StatusActiveRadio.IsChecked ?? false);

            UpdateSettingsVisibility();
        }

        private void ReminderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsVisibility();
        }

        private void UpdateSettingsVisibility()
        {
            var selectedIndex = Math.Max(0, ReminderTypeComboBox.SelectedIndex);
            WeeklySettingsGrid.Visibility = selectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            MonthlySettingsGrid.Visibility = selectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
            IntervalSettingsGrid.Visibility = selectedIndex == 5 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
            {
                return;
            }

            SaveReminderSettings();
            IsSaveRequested = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaveRequested = false;
            DialogResult = false;
            Close();
        }

        private bool ValidateData()
        {
            if (!StartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("请选择开始日期。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!TimeSpan.TryParse(ReminderTimeTextBox.Text, out _))
            {
                MessageBox.Show("请输入有效的提醒时间，格式为 HH:mm。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (ReminderTypeComboBox.SelectedIndex == 5 &&
                (!int.TryParse(IntervalDaysTextBox.Text, out var intervalDays) || intervalDays < 1))
            {
                MessageBox.Show("请输入大于等于 1 的间隔天数。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void SaveReminderSettings()
        {
            if (ReminderSettings == null)
            {
                ReminderSettings = new ReminderSetting();
            }

            ReminderSettings.StartDate = StartDatePicker.SelectedDate?.Date;
            ReminderSettings.ReminderTime = TimeSpan.Parse(ReminderTimeTextBox.Text);
            ReminderSettings.ReminderMessage = string.IsNullOrWhiteSpace(ReminderMessageTextBox.Text)
                ? $"任务提醒：{TaskEntry.Title}"
                : ReminderMessageTextBox.Text.Trim();
            ReminderSettings.IsEnabled = StatusActiveRadio.IsChecked ?? false;
            ReminderSettings.IsActive = ReminderSettings.IsEnabled;
            ReminderSettings.LastTriggeredAt = null;

            ReminderSettings.ReminderType = ReminderTypeComboBox.SelectedIndex switch
            {
                0 => ReminderType.Once,
                1 => ReminderType.Daily,
                2 => ReminderType.Weekly,
                3 => ReminderType.Monthly,
                4 => ReminderType.Yearly,
                5 => ReminderType.Interval,
                _ => ReminderType.Daily
            };

            ReminderSettings.WeekDays = new List<DayOfWeek>();
            if (MondayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Monday);
            if (TuesdayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Tuesday);
            if (WednesdayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Wednesday);
            if (ThursdayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Thursday);
            if (FridayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Friday);
            if (SaturdayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Saturday);
            if (SundayCheckBox.IsChecked == true) ReminderSettings.WeekDays.Add(DayOfWeek.Sunday);

            if (ReminderSettings.ReminderType == ReminderType.Weekly && ReminderSettings.WeekDays.Count == 0)
            {
                ReminderSettings.WeekDays.Add(ReminderSettings.StartDate?.DayOfWeek ?? DayOfWeek.Monday);
            }

            ReminderSettings.MonthlyDayNumber = MonthlyDayNumberComboBox.SelectedIndex + 1;
            ReminderSettings.MonthlyDayOfWeek = MonthlyDayOfWeekComboBox.SelectedIndex switch
            {
                0 => DayOfWeek.Monday,
                1 => DayOfWeek.Tuesday,
                2 => DayOfWeek.Wednesday,
                3 => DayOfWeek.Thursday,
                4 => DayOfWeek.Friday,
                5 => DayOfWeek.Saturday,
                6 => DayOfWeek.Sunday,
                _ => DayOfWeek.Monday
            };
            ReminderSettings.IntervalDays = int.TryParse(IntervalDaysTextBox.Text, out var interval) ? Math.Max(1, interval) : 1;
            ReminderSettings.NextReminderDate = ReminderScheduler.CalculateNextReminderDate(ReminderSettings);
        }

        private void SelectTimeButton_Click(object sender, RoutedEventArgs e)
        {
            var result = Interaction.InputBox("请输入提醒时间，格式为 HH:mm", "选择时间", ReminderTimeTextBox.Text);
            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if (TimeSpan.TryParse(result, out var time))
            {
                ReminderTimeTextBox.Text = time.ToString(@"hh\:mm");
                return;
            }

            MessageBox.Show("时间格式不正确，请使用 HH:mm。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
