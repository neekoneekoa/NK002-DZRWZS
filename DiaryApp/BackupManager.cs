using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiaryApp;

public class BackupInfo
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = "";
    public string Checksum { get; set; } = "";
    public int EntryCount { get; set; }
}

public class BackupData
{
    public BackupInfo Info { get; set; } = new();
    public List<DiaryEntry> Diaries { get; set; } = new();
    public List<TaskEntry> Tasks { get; set; } = new();
    public List<TimeRecordEntry> TimeRecords { get; set; } = new();
    public List<CheckInProject> CheckInProjects { get; set; } = new();
    public List<CheckInEntry> CheckIns { get; set; } = new List<CheckInEntry>();
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
    public ReminderSetting ReminderSetting { get; set; } = new ReminderSetting();
    public List<string> GlobalTags { get; set; } = new List<string>();
    public List<CountdownItem> Countdowns { get; set; } = new List<CountdownItem>();
    [Obsolete("仅用于兼容旧版本备份")]
    public CountdownItem? Countdown { get; set; } // 用于兼容旧版本备份
}

public static class BackupManager
{
    private const string BACKUP_FOLDER = "Backups";
    private const string BACKUP_FILE_PREFIX = "diary_backup_";
    private const string BACKUP_FILE_EXTENSION = ".diary";

    // 创建备份文件夹
    private static void EnsureBackupFolder()
    {
        if (!Directory.Exists(BACKUP_FOLDER))
        {
            Directory.CreateDirectory(BACKUP_FOLDER);
        }
    }

    // 生成校验和
    private static string CalculateChecksum(string data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }

    // 创建自动备份
    public static string CreateAutoBackup(AppData appData, string description = "")
    {
        EnsureBackupFolder();
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{BACKUP_FILE_PREFIX}{timestamp}{BACKUP_FILE_EXTENSION}";
        var filepath = Path.Combine(BACKUP_FOLDER, filename);

        var backupData = new BackupData
        {
            Info = new BackupInfo
            {
                Version = appData.Version,
                CreatedAt = DateTime.Now,
                Description = description,
                EntryCount = appData.Diaries.Count + appData.Tasks.Count + appData.TimeRecords.Count + appData.CheckIns.Count
            },
            Diaries = appData.Diaries,
            Tasks = appData.Tasks,
            TimeRecords = appData.TimeRecords,
            CheckInProjects = appData.CheckInProjects,
            CheckIns = appData.CheckIns,
            PersonalInfo = appData.PersonalInfo,
            ReminderSetting = appData.ReminderSetting,
            GlobalTags = appData.GlobalTags,
            Countdowns = appData.Countdowns
        };

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(backupData, options);
        backupData.Info.Checksum = CalculateChecksum(json);

        // 重新序列化以包含校验和
        json = JsonSerializer.Serialize(backupData, options);
        File.WriteAllText(filepath, json, Encoding.UTF8);

        return filepath;
    }

    // 验证备份文件完整性
    public static bool ValidateBackup(string filepath)
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = File.ReadAllText(filepath, Encoding.UTF8);
            var backupData = JsonSerializer.Deserialize<BackupData>(json, options);
            
            if (backupData?.Info == null)
                return false;

            // 重新计算校验和 - 使用与创建备份时相同的序列化选项
            var originalChecksum = backupData.Info.Checksum;
            backupData.Info.Checksum = "";
            
            var recomputedJson = JsonSerializer.Serialize(backupData, options);
            var recomputedChecksum = CalculateChecksum(recomputedJson);

            return originalChecksum == recomputedChecksum;
        }
        catch
        {
            return false;
        }
    }

    // 恢复备份
    public static AppData? RestoreBackup(string filepath)
    {
        // 移除强制验证，允许导入手动修改过的备份文件
        // if (!ValidateBackup(filepath))
        //    return null;

        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true // 增加对大小写不敏感的支持
            };
            
            var json = File.ReadAllText(filepath, Encoding.UTF8);
            var backupData = JsonSerializer.Deserialize<BackupData>(json, options);
            
            if (backupData != null)
            {
                // 兼容旧版本备份中的倒数日数据
                var countdowns = backupData.Countdowns ?? new List<CountdownItem>();
                if (countdowns.Count == 0 && backupData.Countdown != null)
                {
                    countdowns.Add(backupData.Countdown);
                }

                return new AppData
                {
                    Diaries = backupData.Diaries ?? new List<DiaryEntry>(),
                    Tasks = backupData.Tasks ?? new List<TaskEntry>(),
                    TimeRecords = backupData.TimeRecords ?? new List<TimeRecordEntry>(),
                    CheckInProjects = backupData.CheckInProjects ?? new List<CheckInProject>(),
                    CheckIns = backupData.CheckIns ?? new List<CheckInEntry>(),
                    PersonalInfo = backupData.PersonalInfo ?? new PersonalInfo(),
                    ReminderSetting = backupData.ReminderSetting ?? new ReminderSetting(),
                    GlobalTags = backupData.GlobalTags ?? new List<string>(),
                    Countdowns = countdowns,
                    Version = backupData.Info?.Version ?? "1.0",
                    LastSaved = DateTime.Now
                };
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    // 获取所有备份文件信息
    public static List<(string filepath, BackupInfo info)> GetAllBackups()
    {
        EnsureBackupFolder();
        var backups = new List<(string, BackupInfo)>();

        var files = Directory.GetFiles(BACKUP_FOLDER, $"{BACKUP_FILE_PREFIX}*{BACKUP_FILE_EXTENSION}")
                             .OrderByDescending(f => File.GetCreationTime(f));

        foreach (var file in files)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = File.ReadAllText(file, Encoding.UTF8);
                var backupData = JsonSerializer.Deserialize<BackupData>(json, options);
                if (backupData?.Info != null)
                {
                    backups.Add((file, backupData.Info));
                }
            }
            catch
            {
                // 忽略损坏的备份文件
            }
        }

        return backups;
    }

    // 删除备份文件
    public static bool DeleteBackup(string filepath)
    {
        try
        {
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // 清理旧备份（保留最近N个）
    public static void CleanOldBackups(int keepCount = 10)
    {
        var backups = GetAllBackups();
        if (backups.Count <= keepCount)
            return;

        var toDelete = backups.Skip(keepCount).ToList();
        foreach (var backup in toDelete)
        {
            DeleteBackup(backup.filepath);
        }
    }

    // 手动导出备份到指定位置
    public static string ExportBackupToLocation(AppData appData, string exportPath)
    {
        var backupData = new BackupData
        {
            Info = new BackupInfo
            {
                Version = appData.Version,
                CreatedAt = DateTime.Now,
                Description = "手动导出备份",
                EntryCount = appData.Diaries.Count + appData.Tasks.Count + appData.TimeRecords.Count + appData.CheckIns.Count
            },
            Diaries = appData.Diaries,
            Tasks = appData.Tasks,
            TimeRecords = appData.TimeRecords,
            CheckInProjects = appData.CheckInProjects,
            CheckIns = appData.CheckIns,
            PersonalInfo = appData.PersonalInfo,
            ReminderSetting = appData.ReminderSetting,
            GlobalTags = appData.GlobalTags,
            Countdowns = appData.Countdowns
        };

        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(backupData, options);
        backupData.Info.Checksum = CalculateChecksum(json);

        // 重新序列化以包含校验和
        json = JsonSerializer.Serialize(backupData, options);
        
        var parentDir = Directory.GetParent(exportPath);
        if (parentDir != null && !parentDir.Exists)
        {
            Directory.CreateDirectory(parentDir.FullName);
        }
        
        File.WriteAllText(exportPath, json, Encoding.UTF8);
        return exportPath;
    }
}