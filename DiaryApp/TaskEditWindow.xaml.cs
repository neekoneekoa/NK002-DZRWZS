using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiaryApp
{
    public partial class TaskEditWindow : Window
    {
        public TaskEntry? TaskEntry { get; private set; }
        public bool IsDeleteRequested { get; private set; } = false;

        public TaskEditWindow(TaskEntry? taskEntry = null)
        {
            InitializeComponent();
            TaskEntry = taskEntry;
            LoadTaskData();
            
            // 添加颜色按钮点击事件
            TextColorButton.Click += TextColorButton_Click;
            BackgroundColorButton.Click += BackgroundColorButton_Click;
        }

        private void LoadTaskData()
    {
        // 防止初始化时控件尚未完全初始化导致的空引用异常
        if (TitleTextBox == null || ContentTextBox == null || PriorityComboBox == null || LevelComboBox == null || 
            StatusComboBox == null || CompletedDatePicker == null || TaskTypeComboBox == null || SubTasksPanel == null ||
            FontSizeComboBox == null || TextColorButton == null || BackgroundColorButton == null || UnderlineCheckBox == null ||
            TotalDaysTextBox == null)
        {
            return;
        }
        
        if (TaskEntry != null)
        {
            TitleTextBox.Text = TaskEntry.Title;
            ContentTextBox.Text = TaskEntry.Content;
            PriorityComboBox.SelectedIndex = TaskEntry.Priority - 1;
            LevelComboBox.SelectedIndex = TaskEntry.Level - 1;
            StatusComboBox.SelectedIndex = (int)TaskEntry.Status;
            CompletedDatePicker.SelectedDate = TaskEntry.CompletedAt;
            TaskTypeComboBox.SelectedIndex = TaskEntry.SubTasks.Count > 0 ? 1 : 0;
            
            // 加载文本样式属性
            FontSizeComboBox.SelectedValuePath = "Tag";
            FontSizeComboBox.SelectedValue = TaskEntry.FontSize;
            
            // 加载文字颜色（添加错误处理）
            try
            {
                TextColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TaskEntry.TextColor));
            }
            catch (Exception)
            {
                TextColorButton.Background = new SolidColorBrush(Colors.Black); // 默认黑色
            }
            
            // 加载背景颜色（添加错误处理）
            try
            {
                BackgroundColorButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TaskEntry.BackgroundColor));
            }
            catch (Exception)
            {
                BackgroundColorButton.Background = new SolidColorBrush(Colors.White); // 默认白色
            }
            
            UnderlineCheckBox.IsChecked = TaskEntry.IsUnderline;
            
            // 加载时间计划属性
            TotalDaysTextBox.Text = TaskEntry.TotalDays.ToString();

                // 加载子任务
                SubTasksPanel.Children.Clear();
                foreach (var subTask in TaskEntry.SubTasks)
                {
                    AddSubTaskToPanel(subTask);
                }
            }
            else
            {
                // 默认值
                TitleTextBox.Text = "";
                ContentTextBox.Text = "";
                PriorityComboBox.SelectedIndex = 1;
                LevelComboBox.SelectedIndex = 0;
                StatusComboBox.SelectedIndex = 0;
                CompletedDatePicker.SelectedDate = null;
                TaskTypeComboBox.SelectedIndex = 0;

                // 根据任务类型决定是否添加子任务
                SubTasksPanel.Children.Clear();
                if (TaskTypeComboBox.SelectedIndex == 1) // 项目任务
                {
                    AddDefaultSubTask();
                }
            }

            TitleTextBox.Focus();
        }

        private void AddSubTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var subTask = new SubTask
            {
                Id = Guid.NewGuid().ToString(),
                Title = "新子任务",
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };
            AddSubTaskToPanel(subTask);
        }

        private void AddDefaultSubTask()
        {
            var subTask = new SubTask
            {
                Id = Guid.NewGuid().ToString(),
                Title = "子任务1",
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };
            AddSubTaskToPanel(subTask);
        }

        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 防止初始化时SubTasksPanel尚未完全初始化导致的空引用异常
            if (SubTasksPanel == null)
                return;
                
            if (TaskTypeComboBox.SelectedIndex == 1) // 切换到项目任务
            {
                if (SubTasksPanel.Children.Count == 0)
                {
                    AddDefaultSubTask();
                }
            }
            else if (TaskTypeComboBox.SelectedIndex == 0) // 切换到临时任务
            {
                // 清空子任务
                SubTasksPanel.Children.Clear();
            }
        }

        // 预定义颜色列表
        private readonly List<string> _predefinedColors = new List<string>
        {
            "#000000", "#FFFFFF", "#FF0000", "#00FF00", "#0000FF",
            "#FFFF00", "#FF00FF", "#00FFFF", "#800000", "#008000",
            "#000080", "#808000", "#800080", "#008080", "#C0C0C0",
            "#FFA500", "#FFC0CB", "#808080", "#A52A2A", "#FFE4C4"
        };
        private Button? _currentColorButton = null; // 记录当前点击的颜色按钮

        // 文字颜色按钮点击事件
        private void TextColorButton_Click(object sender, RoutedEventArgs e)
        {
            _currentColorButton = (Button)sender;
            ShowColorPalette();
        }

        // 文字背景色按钮点击事件
        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            _currentColorButton = (Button)sender;
            ShowColorPalette();
        }

        // 显示颜色调色板
        private void ShowColorPalette()
        {
            // 创建或更新颜色调色板
            ColorPalette.Children.Clear();
            foreach (var colorHex in _predefinedColors)
            {
                var colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                var colorButton = new Button
                {
                    Width = 30,
                    Height = 30,
                    Background = colorBrush,
                    Margin = new Thickness(5),
                    Tag = colorHex
                };
                colorButton.Click += ColorButton_Click;
                ColorPalette.Children.Add(colorButton);
            }
            ColorPickerGrid.Visibility = Visibility.Visible;
        }

        // 颜色按钮点击事件
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentColorButton == null) return;
            
            var colorButton = (Button)sender;
            var colorHex = colorButton.Tag.ToString();
            var colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            _currentColorButton.Background = colorBrush;
            ColorPickerGrid.Visibility = Visibility.Collapsed;
        }

        private void AddSubTaskToPanel(SubTask subTask)
        {
            var subTaskPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 创建带边框的容器
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 10),
                Child = subTaskPanel
            };

            // 子任务基本信息
            var basicInfoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var checkBox = new CheckBox
            {
                IsChecked = subTask.IsCompleted,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBox = new TextBox
            {
                Text = subTask.Title,
                Width = 250,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.White,
                Padding = new Thickness(5)
            };

            basicInfoPanel.Children.Add(checkBox);
            basicInfoPanel.Children.Add(textBox);

            // 子任务时间计划
            var timePlanPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(25, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // 持续天数
            var durationPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var durationLabel = new TextBlock
            {
                Text = "天数：",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var durationTextBox = new TextBox
            {
                Text = subTask.DurationDays.ToString(),
                Width = 40,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.White,
                Padding = new Thickness(5),
                ToolTip = "子任务持续天数"
            };

            durationPanel.Children.Add(durationLabel);
            durationPanel.Children.Add(durationTextBox);

            // 开始日期
            var startDatePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var startDateLabel = new TextBlock
            {
                Text = "开始：",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var startDatePicker = new DatePicker
            {
                SelectedDate = subTask.StartDate,
                Width = 100,
                FontSize = 12,
                ToolTip = "子任务开始日期"
            };

            startDatePanel.Children.Add(startDateLabel);
            startDatePanel.Children.Add(startDatePicker);

            // 结束日期
            var endDatePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var endDateLabel = new TextBlock
            {
                Text = "结束：",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var endDatePicker = new DatePicker
            {
                SelectedDate = subTask.EndDate,
                Width = 100,
                FontSize = 12,
                ToolTip = "子任务结束日期"
            };

            endDatePanel.Children.Add(endDateLabel);
            endDatePanel.Children.Add(endDatePicker);

            // 将所有控件添加到时间计划面板
            timePlanPanel.Children.Add(durationPanel);
            timePlanPanel.Children.Add(startDatePanel);
            timePlanPanel.Children.Add(endDatePanel);

            // 将基本信息和时间计划面板添加到子任务面板
            subTaskPanel.Children.Add(basicInfoPanel);
            subTaskPanel.Children.Add(timePlanPanel);

            // 添加到子任务列表
            SubTasksPanel.Children.Add(border);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("请输入任务标题", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 收集子任务
            var subTasks = new List<SubTask>();
            foreach (var child in SubTasksPanel.Children)
            {
                // 处理Border控件的情况
                StackPanel subTaskPanel = null;
                if (child is Border border)
                {
                    subTaskPanel = border.Child as StackPanel;
                }
                else if (child is StackPanel panel)
                {
                    subTaskPanel = panel;
                }

                if (subTaskPanel == null) continue;
                if (subTaskPanel.Children.Count >= 2)
                {
                    // 获取基本信息面板
                    if (subTaskPanel.Children[0] is StackPanel basicInfoPanel)
                    {
                        if (basicInfoPanel.Children[0] is CheckBox checkBox && basicInfoPanel.Children[1] is TextBox textBox)
                        {
                            if (!string.IsNullOrWhiteSpace(textBox.Text))
                            {
                                // 获取时间计划面板
                                if (subTaskPanel.Children[1] is StackPanel timePlanPanel)
                                {
                                    // 提取持续天数
                                    int durationDays = 1;
                                    if (timePlanPanel.Children[0] is StackPanel durationPanel)
                                    {
                                        if (durationPanel.Children[1] is TextBox durationTextBox)
                                        {
                                            int.TryParse(durationTextBox.Text, out durationDays);
                                            if (durationDays < 1)
                                            {
                                                durationDays = 1;
                                            }
                                        }
                                    }

                                    // 提取开始日期
                                    DateTime startDate = DateTime.Now;
                                    if (timePlanPanel.Children[1] is StackPanel startDatePanel)
                                    {
                                        if (startDatePanel.Children[1] is DatePicker startDatePicker)
                                        {
                                            if (startDatePicker.SelectedDate.HasValue)
                                            {
                                                startDate = startDatePicker.SelectedDate.Value;
                                            }
                                        }
                                    }

                                    // 提取结束日期
                                    DateTime endDate = startDate.AddDays(durationDays - 1);
                                    if (timePlanPanel.Children[2] is StackPanel endDatePanel)
                                    {
                                        if (endDatePanel.Children[1] is DatePicker endDatePicker)
                                        {
                                            if (endDatePicker.SelectedDate.HasValue)
                                            {
                                                endDate = endDatePicker.SelectedDate.Value;
                                            }
                                        }
                                    }

                                    subTasks.Add(new SubTask
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Title = textBox.Text,
                                        IsCompleted = checkBox.IsChecked ?? false,
                                        CreatedAt = DateTime.Now,
                                        DurationDays = durationDays,
                                        StartDate = startDate,
                                        EndDate = endDate
                                    });
                                }
                                else
                                {
                                    // 兼容没有时间计划的旧格式
                                    subTasks.Add(new SubTask
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Title = textBox.Text,
                                        IsCompleted = checkBox.IsChecked ?? false,
                                        CreatedAt = DateTime.Now,
                                        DurationDays = 1,
                                        StartDate = DateTime.Now,
                                        EndDate = DateTime.Now
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // 保存文本样式属性
            double fontSize = 14;
            if (FontSizeComboBox.SelectedValue != null)
            {
                fontSize = Convert.ToDouble(FontSizeComboBox.SelectedValue);
            }

            string textColor = "#000000";
            if (TextColorButton.Background is SolidColorBrush textColorBrush)
            {
                // 转换为简单的十六进制格式 (#RRGGBB)
                Color color = textColorBrush.Color;
                textColor = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            }

            string backgroundColor = "#FFFFFF";
            if (BackgroundColorButton.Background is SolidColorBrush backgroundColorBrush)
            {
                // 转换为简单的十六进制格式 (#RRGGBB)
                Color color = backgroundColorBrush.Color;
                backgroundColor = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            }

            // 保存时间计划属性
            int totalDays = 1;
            if (!string.IsNullOrWhiteSpace(TotalDaysTextBox.Text))
            {
                int.TryParse(TotalDaysTextBox.Text, out totalDays);
                if (totalDays < 1)
                {
                    totalDays = 1;
                }
            }

            if (TaskEntry == null)
            {
                // 创建新任务
                TaskEntry = new TaskEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = TitleTextBox.Text,
                    Content = ContentTextBox.Text,
                    Priority = PriorityComboBox.SelectedIndex + 1,
                    Level = LevelComboBox.SelectedIndex + 1,
                    Status = (TaskStatus)StatusComboBox.SelectedIndex,
                    CompletedAt = CompletedDatePicker.SelectedDate,
                    SubTasks = subTasks,
                    CreatedAt = DateTime.Now,
                    
                    // 文本样式属性
                    FontSize = fontSize,
                    TextColor = textColor,
                    BackgroundColor = backgroundColor,
                    IsUnderline = UnderlineCheckBox.IsChecked ?? false,
                    
                    // 时间计划属性
                    TotalDays = totalDays,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(totalDays - 1)
                };
            }
            else
            {
                // 更新现有任务
                TaskEntry.Title = TitleTextBox.Text;
                TaskEntry.Content = ContentTextBox.Text;
                TaskEntry.Priority = PriorityComboBox.SelectedIndex + 1;
                TaskEntry.Level = LevelComboBox.SelectedIndex + 1;
                TaskEntry.Status = (TaskStatus)StatusComboBox.SelectedIndex;
                TaskEntry.CompletedAt = CompletedDatePicker.SelectedDate;
                TaskEntry.SubTasks = subTasks;
                
                // 文本样式属性
                TaskEntry.FontSize = fontSize;
                TaskEntry.TextColor = textColor;
                TaskEntry.BackgroundColor = backgroundColor;
                TaskEntry.IsUnderline = UnderlineCheckBox.IsChecked ?? false;
                
                // 时间计划属性
                TaskEntry.TotalDays = totalDays;
                TaskEntry.StartDate = DateTime.Now;
                TaskEntry.EndDate = DateTime.Now.AddDays(totalDays - 1);
            }

            DialogResult = true;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskEntry != null)
            {
                var result = MessageBox.Show("确定要删除此任务吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    IsDeleteRequested = true;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}