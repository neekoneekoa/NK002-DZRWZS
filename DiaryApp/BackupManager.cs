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
    public static string CreateAutoBackup(List<DiaryEntry> diaries, string description = "")
    {
        EnsureBackupFolder();
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{BACKUP_FILE_PREFIX}{timestamp}{BACKUP_FILE_EXTENSION}";
        var filepath = Path.Combine(BACKUP_FOLDER, filename);

        var backupData = new BackupData
        {
            Info = new BackupInfo
            {
                Version = "1.0",
                CreatedAt = DateTime.Now,
                Description = description,
                EntryCount = diaries.Count
            },
            Diaries = diaries
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
    public static List<DiaryEntry>? RestoreBackup(string filepath)
    {
        if (!ValidateBackup(filepath))
            return null;

        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = File.ReadAllText(filepath, Encoding.UTF8);
            var backupData = JsonSerializer.Deserialize<BackupData>(json, options);
            return backupData?.Diaries;
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
    public static string ExportBackupToLocation(List<DiaryEntry> diaries, string exportPath)
    {
        var backupData = new BackupData
        {
            Info = new BackupInfo
            {
                Version = "1.0",
                CreatedAt = DateTime.Now,
                Description = "手动导出备份",
                EntryCount = diaries.Count
            },
            Diaries = diaries
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