using System;
using System.Windows;
using System.Windows.Controls;

namespace DiaryApp
{
    /// <summary>
    /// TimeRecordEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TimeRecordEditWindow : Window
    {
        // 用于传递编辑的时间记录
        public TimeRecordEntry EditedRecord { get; private set; }
        
        public TimeRecordEditWindow(TimeRecordEntry record)
        {
            InitializeComponent();
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
        }
        
        // 加载时间记录数据
        private void LoadRecordData()
        {
            DatePicker.SelectedDate = EditedRecord.Date;
            
            // 设置时间
            string startTimeStr = $"{EditedRecord.StartTime.Hours:D2}:00";
            string endTimeStr = $"{EditedRecord.EndTime.Hours:D2}:00";
            
            StartTimeComboBox.SelectedItem = startTimeStr;
            EndTimeComboBox.SelectedItem = endTimeStr;
            
            ActivityTextBox.Text = EditedRecord.Activity;
            
            // 设置分类
            CategoryComboBox.SelectedItem = CategoryComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == EditedRecord.Category);
            if (CategoryComboBox.SelectedItem == null && CategoryComboBox.Items.Count > 0)
            {
                CategoryComboBox.SelectedIndex = 0;
            }
            
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
            
            TimeSpan startTime = TimeSpan.Parse(startTimeStr);
            TimeSpan endTime = TimeSpan.Parse(endTimeStr);
            
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
            EditedRecord.Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "其他";
            EditedRecord.Notes = NotesTextBox.Text;
            
            // 设置对话框结果为OK
            this.DialogResult = true;
            this.Close();
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
    }
}