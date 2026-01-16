using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DiaryApp
{
    public partial class ReminderSettingsWindow : Window
    {
        // 公共属性
        public TaskEntry TaskEntry { get; private set; }
        public ReminderSetting? ReminderSettings { get; private set; }
        public bool IsSaveRequested { get; private set; }
        
        // 构造函数
        public ReminderSettingsWindow(TaskEntry taskEntry, ReminderSetting? reminderSettings)
        {
            InitializeComponent();
            
            TaskEntry = taskEntry;
            ReminderSettings = reminderSettings?.Clone() as ReminderSetting ?? new ReminderSetting();
            
            // 初始化UI元素
            InitializeUI();
            
            // 添加提醒类型变化事件
            ReminderTypeComboBox.SelectionChanged += ReminderTypeComboBox_SelectionChanged;
        }
        
        // 初始化UI元素
        private void InitializeUI()
        {
            // 设置开始日期
            if (ReminderSettings.StartDate.HasValue)
            {
                StartDatePicker.SelectedDate = ReminderSettings.StartDate;
            }
            else
            {
                StartDatePicker.SelectedDate = DateTime.Now;
            }
            
            // 设置提醒时间
            if (ReminderSettings.ReminderTime.HasValue)
            {
                // 确保TimeSpan是有效的时间格式
                var time = ReminderSettings.ReminderTime.Value;
                // 只提取小时和分钟部分，确保格式正确
                ReminderTimeTextBox.Text = $"{time.Hours:D2}:{time.Minutes:D2}";
            }
            else
            {
                ReminderTimeTextBox.Text = DateTime.Now.ToString("HH:mm");
            }
            
            // 设置提醒类型
            switch (ReminderSettings.ReminderType)
            {
                case ReminderType.Daily:
                    ReminderTypeComboBox.SelectedIndex = 0;
                    break;
                case ReminderType.Weekly:
                    ReminderTypeComboBox.SelectedIndex = 1;
                    break;
                case ReminderType.Monthly:
                    ReminderTypeComboBox.SelectedIndex = 2;
                    break;
                case ReminderType.Yearly:
                    ReminderTypeComboBox.SelectedIndex = 3;
                    break;
                case ReminderType.Interval:
                    ReminderTypeComboBox.SelectedIndex = 4;
                    break;
                default:
                    ReminderTypeComboBox.SelectedIndex = 0;
                    break;
            }
            
            // 设置每周设置
            if (ReminderSettings.WeekDays != null)
            {
                MondayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Monday);
                TuesdayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Tuesday);
                WednesdayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Wednesday);
                ThursdayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Thursday);
                FridayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Friday);
                SaturdayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Saturday);
                SundayCheckBox.IsChecked = ReminderSettings.WeekDays.Contains(DayOfWeek.Sunday);
            }
            else
            {
                // 默认选择周一到周五
                MondayCheckBox.IsChecked = true;
                TuesdayCheckBox.IsChecked = true;
                WednesdayCheckBox.IsChecked = true;
                ThursdayCheckBox.IsChecked = true;
                FridayCheckBox.IsChecked = true;
            }
            
            // 设置每月设置
            if (ReminderSettings.MonthlyDayNumber.HasValue)
            {
                int dayNumber = ReminderSettings.MonthlyDayNumber.Value;
                // 确保dayNumber在1-5之间
                dayNumber = Math.Max(1, Math.Min(5, dayNumber));
                MonthlyDayNumberComboBox.SelectedIndex = dayNumber - 1;
            }
            else
            {
                MonthlyDayNumberComboBox.SelectedIndex = 0;
            }
            
            if (ReminderSettings.MonthlyDayOfWeek.HasValue)
            {
                int index = (int)ReminderSettings.MonthlyDayOfWeek.Value;
                // 确保index在0-6之间（对应DayOfWeek枚举）
                index = Math.Max(0, Math.Min(6, index));
                MonthlyDayOfWeekComboBox.SelectedIndex = index;
            }
            else
            {
                MonthlyDayOfWeekComboBox.SelectedIndex = 0;
            }
            
            // 设置间隔设置
            if (ReminderSettings.IntervalDays.HasValue)
            {
                IntervalDaysTextBox.Text = ReminderSettings.IntervalDays.Value.ToString();
            }
            else
            {
                IntervalDaysTextBox.Text = "1";
            }
            
            // 设置状态
            StatusActiveRadio.IsChecked = ReminderSettings.IsActive;
            StatusInactiveRadio.IsChecked = !ReminderSettings.IsActive;
            
            // 更新可见性
            UpdateSettingsVisibility();
        }
        
        // 提醒类型变化事件
        private void ReminderTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsVisibility();
        }
        
        // 更新设置区域的可见性
        private void UpdateSettingsVisibility()
        {
            int selectedIndex = ReminderTypeComboBox.SelectedIndex;
            
            // 确保selectedIndex有效（0-4，对应5个提醒类型）
            selectedIndex = Math.Max(0, Math.Min(4, selectedIndex));
            
            WeeklySettingsGrid.Visibility = selectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            MonthlySettingsGrid.Visibility = selectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            IntervalSettingsGrid.Visibility = selectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // 保存按钮点击事件
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证数据
            if (!ValidateData())
            {
                return;
            }
            
            // 保存设置
            SaveReminderSettings();
            
            IsSaveRequested = true;
            DialogResult = true;
            Close();
        }
        
        // 取消按钮点击事件
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaveRequested = false;
            DialogResult = false;
            Close();
        }
        
        // 验证数据
        private bool ValidateData()
        {
            // 验证开始日期
            if (!StartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("请选择开始日期", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            // 验证提醒时间
            TimeSpan reminderTime;
            if (!TimeSpan.TryParse(ReminderTimeTextBox.Text, out reminderTime))
            {
                MessageBox.Show("请输入有效的提醒时间（格式：HH:mm）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            // 验证间隔天数
            if (ReminderTypeComboBox.SelectedIndex == 4) // 间隔类型
            {
                if (!int.TryParse(IntervalDaysTextBox.Text, out int intervalDays) || intervalDays < 1)
                {
                    MessageBox.Show("请输入有效的间隔天数（至少1天）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            
            return true;
        }
        
        // 保存提醒设置
        private void SaveReminderSettings()
        {
            // 设置基本属性
            ReminderSettings.StartDate = StartDatePicker.SelectedDate;
            
            // 解析提醒时间
            if (TimeSpan.TryParse(ReminderTimeTextBox.Text, out TimeSpan reminderTime))
            {
                ReminderSettings.ReminderTime = reminderTime;
            }
            
            ReminderSettings.IsActive = StatusActiveRadio.IsChecked ?? false;
            
            // 设置提醒类型
            switch (ReminderTypeComboBox.SelectedIndex)
            {
                case 0:
                    ReminderSettings.ReminderType = ReminderType.Daily;
                    break;
                case 1:
                    ReminderSettings.ReminderType = ReminderType.Weekly;
                    break;
                case 2:
                    ReminderSettings.ReminderType = ReminderType.Monthly;
                    break;
                case 3:
                    ReminderSettings.ReminderType = ReminderType.Yearly;
                    break;
                case 4:
                    ReminderSettings.ReminderType = ReminderType.Interval;
                    break;
            }
            
            // 设置每周设置
            if (ReminderSettings.ReminderType == ReminderType.Weekly)
            {
                var weekDays = new System.Collections.Generic.List<DayOfWeek>();
                
                if (MondayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Monday);
                if (TuesdayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Tuesday);
                if (WednesdayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Wednesday);
                if (ThursdayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Thursday);
                if (FridayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Friday);
                if (SaturdayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Saturday);
                if (SundayCheckBox.IsChecked ?? false) weekDays.Add(DayOfWeek.Sunday);
                
                if (weekDays.Count == 0)
                {
                    // 如果没有选择任何星期几，默认选择周一
                    weekDays.Add(DayOfWeek.Monday);
                }
                
                ReminderSettings.WeekDays = weekDays;
            }
            
            // 设置每月设置
            if (ReminderSettings.ReminderType == ReminderType.Monthly)
            {
                if (MonthlyDayNumberComboBox.SelectedItem is ComboBoxItem dayNumberItem && dayNumberItem.Tag != null)
                {
                    if (int.TryParse(dayNumberItem.Tag.ToString(), out int dayNumber))
                    {
                        ReminderSettings.MonthlyDayNumber = dayNumber;
                    }
                }
                
                if (MonthlyDayOfWeekComboBox.SelectedItem is ComboBoxItem dayOfWeekItem && dayOfWeekItem.Tag != null)
                {
                    string dayOfWeekStr = dayOfWeekItem.Tag.ToString();
                    if (Enum.TryParse(typeof(DayOfWeek), dayOfWeekStr, out object dayOfWeekObj))
                    {
                        ReminderSettings.MonthlyDayOfWeek = (DayOfWeek)dayOfWeekObj;
                    }
                }
            }
            
            // 设置间隔设置
            if (ReminderSettings.ReminderType == ReminderType.Interval)
            {
                if (int.TryParse(IntervalDaysTextBox.Text, out int intervalDays))
                {
                    ReminderSettings.IntervalDays = intervalDays;
                }
            }
            
            // 计算下次提醒日期
            ReminderSettings.NextReminderDate = CalculateNextReminderDate();
        }
        
        // 计算下次提醒日期
        private DateTime? CalculateNextReminderDate()
        {
            if (!ReminderSettings.StartDate.HasValue || !ReminderSettings.ReminderTime.HasValue)
            {
                return null;
            }
            
            var nextDate = ReminderSettings.StartDate.Value.Date.Add(ReminderSettings.ReminderTime.Value);
            var today = DateTime.Now;
            
            // 如果下次提醒日期已经过去，计算下一个
            if (nextDate <= today)
            {
                switch (ReminderSettings.ReminderType)
                {
                    case ReminderType.Daily:
                        while (nextDate <= today)
                        {
                            nextDate = nextDate.AddDays(1);
                        }
                        break;
                    
                    case ReminderType.Weekly:
                        if (ReminderSettings.WeekDays != null && ReminderSettings.WeekDays.Count > 0)
                        {
                            // 找到下一个符合条件的星期几，最多尝试365天
                            bool found = false;
                            int daysToAdd = 1;
                            int maxAttempts = 365;
                            int attempts = 0;
                            
                            while (!found && attempts < maxAttempts)
                            {
                                nextDate = nextDate.AddDays(daysToAdd);
                                if (ReminderSettings.WeekDays.Contains(nextDate.DayOfWeek))
                                {
                                    found = true;
                                }
                                daysToAdd++;
                                attempts++;
                            }
                            
                            // 如果在一年内没有找到，默认加7天
                            if (!found)
                            {
                                nextDate = nextDate.AddDays(7);
                            }
                        }
                        else
                        {
                            // 如果没有选择星期几，默认加7天
                            nextDate = nextDate.AddDays(7);
                        }
                        break;
                    
                    case ReminderType.Monthly:
                        if (ReminderSettings.MonthlyDayNumber.HasValue && ReminderSettings.MonthlyDayOfWeek.HasValue)
                        {
                            // 找到下一个符合条件的日期，最多尝试365天
                            int daysToAdd = 1;
                            int maxAttempts = 365;
                            int attempts = 0;
                            while (nextDate <= today && attempts < maxAttempts)
                            {
                                nextDate = nextDate.AddDays(daysToAdd);
                                daysToAdd++;
                                attempts++;
                                
                                // 检查是否是当月的第N个星期几
                                int weekNumber = (nextDate.Day - 1) / 7 + 1;
                                if (weekNumber == ReminderSettings.MonthlyDayNumber.Value && nextDate.DayOfWeek == ReminderSettings.MonthlyDayOfWeek.Value)
                                {
                                    break;
                                }
                            }
                            
                            // 如果在一年内没有找到，默认加30天
                            if (attempts >= maxAttempts)
                            {
                                nextDate = nextDate.AddDays(30);
                            }
                        }
                        else
                        {
                            // 如果缺少必要的每月设置，默认加30天
                            nextDate = nextDate.AddDays(30);
                        }
                        break;
                    
                    case ReminderType.Yearly:
                        while (nextDate <= today)
                        {
                            nextDate = nextDate.AddYears(1);
                        }
                        break;
                    
                    case ReminderType.Interval:
                        if (ReminderSettings.IntervalDays.HasValue && ReminderSettings.IntervalDays.Value > 0)
                        {
                            while (nextDate <= today)
                            {
                                nextDate = nextDate.AddDays(ReminderSettings.IntervalDays.Value);
                            }
                        }
                        else
                        {
                            // 如果间隔天数无效，默认加1天
                            nextDate = nextDate.AddDays(1);
                        }
                        break;
                    
                    default:
                        // 默认情况下，加1天
                        nextDate = nextDate.AddDays(1);
                        break;
                }
            }
            
            return nextDate;
        }
        
        // 选择时间按钮点击事件
        private void SelectTimeButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用InputBox来获取时间输入
            string result = Microsoft.VisualBasic.Interaction.InputBox("请输入时间（格式：HH:mm）", "选择时间", ReminderTimeTextBox.Text);
            
            if (!string.IsNullOrEmpty(result))
            {
                // 验证时间格式
                if (TimeSpan.TryParse(result, out TimeSpan time))
                {
                    ReminderTimeTextBox.Text = time.ToString("HH:mm");
                }
                else
                {
                    MessageBox.Show("时间格式不正确，请使用HH:mm格式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
