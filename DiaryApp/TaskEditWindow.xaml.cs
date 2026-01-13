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
            
            // 添加标题文本框事件处理
            TitleTextBox.GotFocus += TitleTextBox_GotFocus;
            TitleTextBox.LostFocus += TitleTextBox_LostFocus;
            
            // 初始化提示文字
            InitializeTitlePlaceholder();
        }

        private void LoadTaskData()
    {
        // 防止初始化时控件尚未完全初始化导致的空引用异常
        if (TitleTextBox == null || PriorityComboBox == null || 
            StatusComboBox == null || CompletedDatePicker == null || ChaptersPanel == null)
        {
            return;
        }
        
        if (TaskEntry != null)
        {
            if (!string.IsNullOrEmpty(TaskEntry.Title))
            {
                TitleTextBox.Text = TaskEntry.Title;
                TitleTextBox.Foreground = Brushes.Black;
            }
            else
            {
                InitializeTitlePlaceholder();
            }
            PriorityComboBox.SelectedIndex = TaskEntry.Priority - 1;
            StatusComboBox.SelectedIndex = (int)TaskEntry.Status;
            CompletedDatePicker.SelectedDate = TaskEntry.CompletedAt;
            // 使用新的章节结构，不再依赖任务类型

                // 加载章节
                ChaptersPanel.Children.Clear();
                foreach (var chapter in TaskEntry.Chapters)
                {
                    AddChapterToPanel(chapter);
                }
            }
            else
            {
                // 默认值
                TitleTextBox.Text = "";
                PriorityComboBox.SelectedIndex = 1;
                StatusComboBox.SelectedIndex = 0;
                CompletedDatePicker.SelectedDate = null;

                // 默认添加第一章
                ChaptersPanel.Children.Clear();
                AddDefaultChapter();
            }

            TitleTextBox.Focus();
        }

        private void InitializeTitlePlaceholder()
        {
            if (string.IsNullOrEmpty(TitleTextBox.Text))
            {
                TitleTextBox.Text = TitleTextBox.Tag?.ToString() ?? "";
                TitleTextBox.Foreground = Brushes.Gray;
            }
        }

        private void TitleTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TitleTextBox.Text == TitleTextBox.Tag?.ToString())
            {
                TitleTextBox.Text = "";
                TitleTextBox.Foreground = Brushes.Black;
            }
        }

        private void TitleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                TitleTextBox.Text = TitleTextBox.Tag?.ToString() ?? "";
                TitleTextBox.Foreground = Brushes.Gray;
            }
        }

        private void AddChapterButton_Click(object sender, RoutedEventArgs e)
        {
            var chapter = new TaskChapter
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"第{ChaptersPanel.Children.Count + 1}章",
                Content = "",
                CreatedAt = DateTime.Now,
                OrderIndex = ChaptersPanel.Children.Count
            };
            AddChapterToPanel(chapter);
        }

        private void AddDefaultChapter()
        {
            var chapter = new TaskChapter
            {
                Id = Guid.NewGuid().ToString(),
                Title = "第一章",
                Content = "",
                CreatedAt = DateTime.Now,
                OrderIndex = 0
            };
            AddChapterToPanel(chapter);
        }







        private void AddChapterToPanel(TaskChapter chapter)
        {
            var chapterPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // 章节标题栏
            var chapterHeaderPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var chapterTitleTextBox = new TextBox
            {
                Text = chapter.Title,
                Width = 200,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8)
            };

            var addSubTaskButton = new Button
            {
                Content = "+ 添加子任务",
                Width = 100,
                Height = 30,
                Background = Brushes.LightBlue,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 12
            };

            addSubTaskButton.Click += (sender, e) =>
            {
                var subTask = new SubTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "新子任务",
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    ScheduledTime = DateTime.Now,
                    Content = "",
                    Notes = ""
                };
                AddSubTaskToChapterPanel(subTask, chapterPanel);
            };

            chapterHeaderPanel.Children.Add(chapterTitleTextBox);
            chapterHeaderPanel.Children.Add(addSubTaskButton);

            // 章节内容
            var chapterContentTextBox = new TextBox
            {
                Text = chapter.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.White,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 子任务列表
            var subTasksPanel = new StackPanel
            {
                Margin = new Thickness(20, 0, 0, 0)
            };

            // 加载现有子任务
            foreach (var subTask in chapter.SubTasks)
            {
                AddSubTaskToChapterPanel(subTask, subTasksPanel);
            }

            chapterPanel.Children.Add(chapterHeaderPanel);
            chapterPanel.Children.Add(chapterContentTextBox);
            chapterPanel.Children.Add(subTasksPanel);

            // 添加到章节列表
            ChaptersPanel.Children.Add(chapterPanel);
        }

        private void AddSubTaskToChapterPanel(SubTask subTask, Panel parentPanel)
        {
            var subTaskPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var subTaskBorder = new Border
            {
                Background = Brushes.LightGray,
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.DarkGray,
                Child = subTaskPanel
            };

            // 第一行：时间和名称
            var firstRowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var timePicker = new DatePicker
            {
                SelectedDate = subTask.ScheduledTime,
                Width = 120,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var nameTextBox = new TextBox
            {
                Text = subTask.Title,
                Width = 200,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5)
            };

            var completedCheckBox = new CheckBox
            {
                IsChecked = subTask.IsCompleted,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            firstRowPanel.Children.Add(timePicker);
            firstRowPanel.Children.Add(nameTextBox);
            firstRowPanel.Children.Add(completedCheckBox);

            // 第二行：子任务内容
            var contentTextBox = new TextBox
            {
                Text = subTask.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 60,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.White,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 第三行：章节文本内容
            var chapterContentTextBox = new TextBox
            {
                Text = "章节文本内容",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 40,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.WhiteSmoke,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 第四行：注意事项备注
            var notesTextBox = new TextBox
            {
                Text = subTask.Notes,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 40,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Orange,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8)
            };

            subTaskPanel.Children.Add(firstRowPanel);
            subTaskPanel.Children.Add(contentTextBox);
            subTaskPanel.Children.Add(chapterContentTextBox);
            subTaskPanel.Children.Add(notesTextBox);

            parentPanel.Children.Add(subTaskBorder);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("请输入任务标题", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 收集章节数据
            var chapters = new List<TaskChapter>();
            foreach (var chapterChild in ChaptersPanel.Children)
            {
                if (chapterChild is StackPanel chapterPanel)
                {
                    var chapter = new TaskChapter
                    {
                        Id = Guid.NewGuid().ToString(),
                        SubTasks = new List<SubTask>()
                    };

                    // 获取章节标题
                    if (chapterPanel.Children[0] is StackPanel chapterHeaderPanel)
                    {
                        if (chapterHeaderPanel.Children[0] is TextBox chapterTitleTextBox)
                        {
                            chapter.Title = chapterTitleTextBox.Text;
                        }
                    }

                    // 获取章节内容
                    if (chapterPanel.Children[1] is TextBox chapterContentTextBox)
                    {
                        chapter.Content = chapterContentTextBox.Text;
                    }

                    // 获取子任务
                    if (chapterPanel.Children[2] is StackPanel subTasksPanel)
                    {
                        foreach (var subTaskChild in subTasksPanel.Children)
                        {
                            if (subTaskChild is StackPanel subTaskPanel)
                            {
                                var subTask = new SubTask
                                {
                                    Id = Guid.NewGuid().ToString()
                                };

                                // 解析子任务面板
                                if (subTaskPanel.Children[0] is StackPanel firstRowPanel)
                                {
                                    if (firstRowPanel.Children[0] is DatePicker timePicker)
                                    {
                                        subTask.ScheduledTime = timePicker.SelectedDate;
                                    }
                                    if (firstRowPanel.Children[1] is TextBox nameTextBox)
                                    {
                                        subTask.Title = nameTextBox.Text;
                                    }
                                    if (firstRowPanel.Children[2] is CheckBox completedCheckBox)
                                    {
                                        subTask.IsCompleted = completedCheckBox.IsChecked ?? false;
                                    }
                                }

                                if (subTaskPanel.Children[1] is TextBox contentTextBox)
                                {
                                    subTask.Content = contentTextBox.Text;
                                }

                                if (subTaskPanel.Children[3] is TextBox notesTextBox)
                                {
                                    subTask.Notes = notesTextBox.Text;
                                }

                                subTask.CreatedAt = DateTime.Now;
                                chapter.SubTasks.Add(subTask);
                            }
                        }
                    }

                    chapter.CreatedAt = DateTime.Now;
                    chapter.OrderIndex = chapters.Count;
                    chapters.Add(chapter);
                }
            }

            if (TaskEntry == null)
            {
                // 创建新任务
                TaskEntry = new TaskEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = TitleTextBox.Text,
                    Priority = PriorityComboBox.SelectedIndex + 1,
                    Status = (TaskStatus)StatusComboBox.SelectedIndex,
                    CompletedAt = CompletedDatePicker.SelectedDate,
                    Chapters = chapters,
                    CreatedAt = DateTime.Now
                };
            }
            else
            {
                // 更新现有任务
                TaskEntry.Title = TitleTextBox.Text;
                TaskEntry.Priority = PriorityComboBox.SelectedIndex + 1;
                TaskEntry.Status = (TaskStatus)StatusComboBox.SelectedIndex;
                TaskEntry.CompletedAt = CompletedDatePicker.SelectedDate;
                TaskEntry.Chapters = chapters;
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