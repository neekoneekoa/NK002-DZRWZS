using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DiaryApp
{
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

    // ===== 任务数据模型 =====
    public class SubTask
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class TaskEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int Priority { get; set; } = 2; // 1-3级优先级
        public int Level { get; set; } = 1; // 1-3级标题
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        
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

    // ===== 日记数据模型 =====
    public class DiaryEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public List<string> Photos { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 用于界面显示
        public string DateStr => CreatedAt.ToString("yyyy-MM-dd HH:mm");
        
        // 用于搜索
        public string SearchableText => $"{Title} {Content} {string.Join(" ", Tags)}".ToLower();
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

    // ===== 打卡数据模型 =====
    public class CheckInEntry
    {
        public string Id { get; set; } = "";
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

    // ===== 应用统一数据模型 =====
    public class AppData
    {
        public List<DiaryEntry> Diaries { get; set; } = new List<DiaryEntry>();
        public List<TaskEntry> Tasks { get; set; } = new List<TaskEntry>();
        public List<TimeRecordEntry> TimeRecords { get; set; } = new List<TimeRecordEntry>();
        public List<CheckInEntry> CheckIns { get; set; } = new List<CheckInEntry>();
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
    }
}