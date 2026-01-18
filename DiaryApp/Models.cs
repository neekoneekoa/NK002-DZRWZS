using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DiaryApp
{
    // ===== 日记周期类型枚举 =====
public enum DiaryPeriodType
{
    [Description("日常")]
    Daily = 0,
    [Description("周记")]
    Weekly = 1,
    [Description("月计")]
    Monthly = 2,
    [Description("季记")]
    Quarterly = 3,
    [Description("年记")]
    Yearly = 4
}

// ===== 任务状态枚举 =====
public enum TaskStatus
{
    [Description("待完成")]
    Pending = 0,
    [Description("进行中")]
    InProgress = 1,
    [Description("已完成")]
    Completed = 2
}

// ===== 任务类型枚举 =====
public enum TaskType
{
    [Description("临时任务")]
    Temporary = 0,
    [Description("项目")]
    Project = 1
}

    // ===== 任务数据模型 =====
    public class TaskChapter
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public string Notes { get; set; } = ""; // 注意事项备注
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int OrderIndex { get; set; } = 0; // 章节顺序
    }

    public class SubTask
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ScheduledTime { get; set; } // 计划时间
        public string Content { get; set; } = ""; // 子任务内容
        public string Notes { get; set; } = ""; // 注意事项备注
        
        // 时间计划属性
        public int DurationDays { get; set; } = 1; // 子任务持续天数
        public DateTime StartDate { get; set; } = DateTime.Now; // 开始日期
        public DateTime EndDate { get; set; } = DateTime.Now; // 结束日期
    }

    public class TaskEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int Priority { get; set; } = 2; // 1-3级优先级
        public int Level { get; set; } = 1; // 1-3级标题
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskType TaskType { get; set; } = TaskType.Temporary; // 任务类型
        public List<string> ProjectTags { get; set; } = new List<string>(); // 项目标签
        public List<TaskChapter> Chapters { get; set; } = new List<TaskChapter>();
        [Obsolete("使用Chapters替代")]
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public ReminderSetting? ReminderSettings { get; set; } = null;
        
        // 文本样式属性
        public double FontSize { get; set; } = 14; // 字号
        public string TextColor { get; set; } = "#000000"; // 文字颜色
        public string BackgroundColor { get; set; } = "#FFFFFF"; // 文字背景色
        public bool IsUnderline { get; set; } = false; // 是否有下划线
        
        // 时间计划属性
        public int TotalDays { get; set; } = 1; // 总天数
        public DateTime StartDate { get; set; } = DateTime.Now; // 开始日期
        public DateTime EndDate { get; set; } = DateTime.Now; // 结束日期
        
        // 用于显示
        public string StatusDescription => Status switch
        {
            TaskStatus.Pending => "待完成",
            TaskStatus.InProgress => "进行中", 
            TaskStatus.Completed => "已完成",
            _ => "未知"
        };
        
        public string PriorityDescription => Priority switch
        {
            1 => "高",
            2 => "中",
            3 => "低",
            _ => "中"
        };
    }

    // ===== 参数数据模型 =====
    public class DiaryParam
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(Unit) ? $"{Name}: {Value}" : $"{Name}: {Value}{Unit}";
        }
    }

    // ===== 日记数据模型 =====
    public class DiaryEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public List<string> Photos { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<DiaryParam> Parameters { get; set; } = new List<DiaryParam>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DiaryPeriodType PeriodType { get; set; } = DiaryPeriodType.Daily;
        
        // 用于界面显示
        public string DateStr => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        
        public string DateOnly => CreatedAt.ToString("yyyy-MM-dd");
        
        public string TimeOnly => CreatedAt.ToString("HH:mm");
        
        public string ContentPreview
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return "";
                var lines = Content.Split(new[] { '\n', '。' }, StringSplitOptions.RemoveEmptyEntries);
                return string.Join("。", lines.Take(3)) + (lines.Length > 3 ? "..." : "");
            }
        }
        
        // 用于搜索
        public string SearchableText => $"{Title} {Content} {string.Join(" ", Tags)}".ToLower();
        
        // 周期类型描述
        public string PeriodTypeDescription => PeriodType switch
        {
            DiaryPeriodType.Daily => "日常",
            DiaryPeriodType.Weekly => "周记",
            DiaryPeriodType.Monthly => "月计",
            DiaryPeriodType.Quarterly => "季记",
            DiaryPeriodType.Yearly => "年记",
            _ => "日常"
        };
    }

    // ===== 时间记录数据模型 =====
    public class TimeRecordEntry
    {
        public string Id { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Activity { get; set; } = "";
        public string Category { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 计算持续时间（小时）
        public double DurationHours => (EndTime - StartTime).TotalHours;
        
        // 用于显示
        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }

    // ===== 个人信息数据模型 =====
    public class PersonalInfo
    {
        public string Id { get; set; } = "personal_info";
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime? Birthday { get; set; }
        public decimal Savings { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // 用于显示
        public string BirthdayStr => Birthday?.ToString("yyyy-MM-dd") ?? "";
        public string SavingsStr => $"¥{Savings:N2}";
        public string DisplayName => string.IsNullOrEmpty(Name) ? "未设置姓名" : Name;
    }

    // ===== 打卡数据模型 =====
    public class CheckInProject
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = ""; // 项目名称
        public string Type { get; set; } = ""; // 项目类型：习惯、运动、学习等
        public DateTime? DeadlineDate { get; set; } // 到期日期（可选）
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class CheckInEntry
    {
        public string Id { get; set; } = "";
        public string ProjectId { get; set; } = ""; // 所属项目ID
        public string Type { get; set; } = ""; // 打卡类型：习惯、运动、学习等
        public DateTime Date { get; set; } = DateTime.Today;
        public string Value { get; set; } = ""; // 打卡值或状态
        public int Streak { get; set; } = 0; // 连续打卡天数
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // ===== 打卡统计模型 =====
    public class CheckInStats
    {
        public string Type { get; set; } = "";
        public int TotalDays { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public int LongestStreak { get; set; } = 0;
        public double SuccessRate { get; set; } = 0.0; // 成功率百分比
        public DateTime LastCheckIn { get; set; }
    }

    // ===== 提醒数据模型 =====
    // 提醒类型枚举
    public enum ReminderType
    {
        Daily,     // 每日
        Weekly,    // 每周
        Monthly,   // 每月
        Yearly,    // 每年
        Interval   // 间隔
    }
    
    public class ReminderSetting
    {
        public bool IsEnabled { get; set; } = false;
        public TimeSpan? ReminderTime { get; set; } = new TimeSpan(20, 0, 0); // 默认晚上8点
        public string ReminderMessage { get; set; } = "该写日记了哦！";
        public bool IsMinimizedToTray { get; set; } = true; // 是否最小化到系统托盘
        public DateTime? StartDate { get; set; } = DateTime.Now;
        public ReminderType ReminderType { get; set; } = ReminderType.Daily;
        public int? IntervalDays { get; set; } = 1;
        
        // 每周设置
        public List<DayOfWeek> WeekDays { get; set; } = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        
        // 每月设置
        public int? MonthlyDayNumber { get; set; } = 1; // 第几个星期
        public DayOfWeek? MonthlyDayOfWeek { get; set; } = DayOfWeek.Monday; // 星期几
        
        // 下次提醒日期
        public DateTime? NextReminderDate { get; set; } = null;
        
        public bool IsActive { get; set; } = false;
        
        // 克隆方法
        public object Clone()
        {
            return new ReminderSetting
            {
                IsEnabled = this.IsEnabled,
                ReminderTime = this.ReminderTime,
                ReminderMessage = this.ReminderMessage,
                IsMinimizedToTray = this.IsMinimizedToTray,
                StartDate = this.StartDate,
                ReminderType = this.ReminderType,
                IntervalDays = this.IntervalDays,
                WeekDays = new List<DayOfWeek>(this.WeekDays),
                MonthlyDayNumber = this.MonthlyDayNumber,
                MonthlyDayOfWeek = this.MonthlyDayOfWeek,
                NextReminderDate = this.NextReminderDate,
                IsActive = this.IsActive
            };
        }
    }

    // ===== 应用统一数据模型 =====
    public class AppData
    {
        public List<DiaryEntry> Diaries { get; set; } = new List<DiaryEntry>();
        public List<TaskEntry> Tasks { get; set; } = new List<TaskEntry>();
        public List<TimeRecordEntry> TimeRecords { get; set; } = new List<TimeRecordEntry>();
        public List<CheckInProject> CheckInProjects { get; set; } = new List<CheckInProject>();
        public List<CheckInEntry> CheckIns { get; set; } = new List<CheckInEntry>();
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public ReminderSetting ReminderSetting { get; set; } = new ReminderSetting();
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public string Version { get; set; } = "0.2.0";
    }
}