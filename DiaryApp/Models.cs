using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DiaryApp;

public enum DiaryPeriodType
{
    [Description("日常")]
    Daily = 0,
    [Description("周记")]
    Weekly = 1,
    [Description("月记")]
    Monthly = 2,
    [Description("季记")]
    Quarterly = 3,
    [Description("年记")]
    Yearly = 4
}

public enum TaskStatus
{
    [Description("待完成")]
    Pending = 0,
    [Description("进行中")]
    InProgress = 1,
    [Description("已完成")]
    Completed = 2
}

public enum TaskType
{
    [Description("临时任务")]
    Temporary = 0,
    [Description("项目")]
    Project = 1
}

public class TaskChapter
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<SubTask> SubTasks { get; set; } = new();
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int OrderIndex { get; set; }
}

public class SubTask
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ScheduledTime { get; set; }
    public string Content { get; set; } = "";
    public string Notes { get; set; } = "";
    public int DurationDays { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now;
}

public class TaskEntry
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Priority { get; set; } = 2;
    public int Level { get; set; } = 1;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public TaskType TaskType { get; set; } = TaskType.Temporary;
    public List<string> ProjectTags { get; set; } = new();
    public List<TaskChapter> Chapters { get; set; } = new();

    [Obsolete("Use Chapters instead")]
    public List<SubTask> SubTasks { get; set; } = new();

    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public ReminderSetting? ReminderSettings { get; set; }
    public double FontSize { get; set; } = 14;
    public string TextColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public bool IsUnderline { get; set; }
    public int TotalDays { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now;

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

public class DiaryEntry
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Photos { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<DiaryParam> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DiaryPeriodType PeriodType { get; set; } = DiaryPeriodType.Daily;

    public string DateStr => CreatedAt.ToString("yyyy-MM-dd HH:mm");
    public string DateOnly => CreatedAt.ToString("yyyy-MM-dd");
    public string TimeOnly => CreatedAt.ToString("HH:mm");

    public string ContentPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                return "";
            }

            var lines = Content
                .Split(new[] { '\n', '。' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            return string.Join("。", lines.Take(3)) + (lines.Length > 3 ? "..." : "");
        }
    }

    public string SearchableText => $"{Title} {Content} {string.Join(" ", Tags)}".ToLowerInvariant();

    public string PeriodTypeDescription => PeriodType switch
    {
        DiaryPeriodType.Daily => "日常",
        DiaryPeriodType.Weekly => "周记",
        DiaryPeriodType.Monthly => "月记",
        DiaryPeriodType.Quarterly => "季记",
        DiaryPeriodType.Yearly => "年记",
        _ => "日常"
    };
}

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

    public double DurationHours => (EndTime - StartTime).TotalHours;
    public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
}

public class PersonalInfo
{
    public string Id { get; set; } = "personal_info";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime? Birthday { get; set; }
    public decimal Savings { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public string BirthdayStr => Birthday?.ToString("yyyy-MM-dd") ?? "";
    public string SavingsStr => $"CNY {Savings:N2}";
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "未设置姓名" : Name;
}

public class CheckInProject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime? DeadlineDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class CheckInEntry
{
    public string Id { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string Value { get; set; } = "";
    public int Streak { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; } = "";
    public List<string> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class CheckInStats
{
    public string Type { get; set; } = "";
    public int TotalDays { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastCheckIn { get; set; }
}

public enum ReminderType
{
    Once,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Interval
}

public class ReminderSetting
{
    public bool IsEnabled { get; set; }
    public TimeSpan? ReminderTime { get; set; } = new TimeSpan(20, 0, 0);
    public string ReminderMessage { get; set; } = "该处理任务了。";
    public bool IsMinimizedToTray { get; set; } = true;
    public DateTime? StartDate { get; set; } = DateTime.Now;
    public ReminderType ReminderType { get; set; } = ReminderType.Daily;
    public int? IntervalDays { get; set; } = 1;
    public List<DayOfWeek> WeekDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    public int? MonthlyDayNumber { get; set; } = 1;
    public DayOfWeek? MonthlyDayOfWeek { get; set; } = DayOfWeek.Monday;
    public DateTime? NextReminderDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastTriggeredAt { get; set; }

    public object Clone()
    {
        return new ReminderSetting
        {
            IsEnabled = IsEnabled,
            ReminderTime = ReminderTime,
            ReminderMessage = ReminderMessage,
            IsMinimizedToTray = IsMinimizedToTray,
            StartDate = StartDate,
            ReminderType = ReminderType,
            IntervalDays = IntervalDays,
            WeekDays = new List<DayOfWeek>(WeekDays),
            MonthlyDayNumber = MonthlyDayNumber,
            MonthlyDayOfWeek = MonthlyDayOfWeek,
            NextReminderDate = NextReminderDate,
            IsActive = IsActive,
            LastTriggeredAt = LastTriggeredAt
        };
    }
}

public class CountdownItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public DateTime TargetDate { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MindMapNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = "";
    public bool IsRoot { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<MindMapNode> Children { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public double X { get; set; }
    public double Y { get; set; }
}

public class AppData
{
    public List<DiaryEntry> Diaries { get; set; } = new();
    public List<TaskEntry> Tasks { get; set; } = new();
    public List<TimeRecordEntry> TimeRecords { get; set; } = new();
    public List<CheckInProject> CheckInProjects { get; set; } = new();
    public List<CheckInEntry> CheckIns { get; set; } = new();
    public List<string> GlobalTags { get; set; } = new();
    public PersonalInfo PersonalInfo { get; set; } = new();
    public List<CountdownItem> Countdowns { get; set; } = new();

    [Obsolete("Use Countdowns list instead")]
    public CountdownItem? Countdown { get; set; }

    public ReminderSetting ReminderSetting { get; set; } = new();
    public MindMapNode MindMapRoot { get; set; } = new()
    {
        Content = "个人资料",
        IsRoot = true,
        IsExpanded = true
    };

    public DateTime LastSaved { get; set; } = DateTime.Now;
    public string Version { get; set; } = "0.2.0";
}
