using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
    public List<CheckInEntry> CheckIns { get; set; } = new();
    public PersonalInfo PersonalInfo { get; set; } = new();
    public ReminderSetting ReminderSetting { get; set; } = new();
    public List<string> GlobalTags { get; set; } = new();
    public List<CountdownItem> Countdowns { get; set; } = new();
    [Obsolete("仅用于兼容旧版本备份")]
    public CountdownItem? Countdown { get; set; }
}

public class BackupEnvelope
{
    public string Format { get; set; } = "DiaryAppBackup";
    public int FormatVersion { get; set; } = 2;
    public string AppVersion { get; set; } = "0.2.0";
    public DateTime ExportedAt { get; set; } = DateTime.Now;
    public BackupData Data { get; set; } = new();
}

public static class BackupManager
{
    private const string BackupFolder = "Backups";
    private const string BackupFilePrefix = "diary_backup_";
    private const string BackupFileExtension = ".diary";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static void EnsureBackupFolder()
    {
        if (!Directory.Exists(BackupFolder))
        {
            Directory.CreateDirectory(BackupFolder);
        }
    }

    private static string CalculateChecksum(string data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    public static string CreateAutoBackup(AppData appData, string description = "")
    {
        EnsureBackupFolder();

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{BackupFilePrefix}{timestamp}{BackupFileExtension}";
        var filepath = Path.Combine(BackupFolder, filename);

        WriteBackupFile(BuildEnvelope(appData, description), filepath);
        return filepath;
    }

    public static bool ValidateBackup(string filepath)
    {
        try
        {
            var json = File.ReadAllText(filepath, Encoding.UTF8);
            var backupData = DeserializeBackupData(json);
            if (backupData?.Info == null)
            {
                return false;
            }

            var originalChecksum = backupData.Info.Checksum;
            backupData.Info.Checksum = "";
            var recomputedJson = JsonSerializer.Serialize(backupData, JsonOptions);
            var recomputedChecksum = CalculateChecksum(recomputedJson);
            return originalChecksum == recomputedChecksum;
        }
        catch
        {
            return false;
        }
    }

    public static AppData? RestoreBackup(string filepath)
    {
        try
        {
            var json = File.ReadAllText(filepath, Encoding.UTF8);
            var backupData = DeserializeBackupData(json);
            if (backupData == null)
            {
                return null;
            }

            var countdowns = backupData.Countdowns ?? new List<CountdownItem>();
#pragma warning disable CS0618
            if (countdowns.Count == 0 && backupData.Countdown != null)
            {
                countdowns.Add(backupData.Countdown);
            }
#pragma warning restore CS0618

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
                Version = backupData.Info.Version,
                LastSaved = DateTime.Now
            };
        }
        catch
        {
            return null;
        }
    }

    public static List<(string filepath, BackupInfo info)> GetAllBackups()
    {
        EnsureBackupFolder();
        var backups = new List<(string filepath, BackupInfo info)>();

        var files = Directory
            .GetFiles(BackupFolder, $"{BackupFilePrefix}*{BackupFileExtension}")
            .OrderByDescending(File.GetCreationTime);

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file, Encoding.UTF8);
                var backupData = DeserializeBackupData(json);
                if (backupData?.Info != null)
                {
                    backups.Add((file, backupData.Info));
                }
            }
            catch
            {
                // Ignore broken backup files in listing.
            }
        }

        return backups;
    }

    public static bool DeleteBackup(string filepath)
    {
        try
        {
            if (!File.Exists(filepath))
            {
                return false;
            }

            File.Delete(filepath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void CleanOldBackups(int keepCount = 10)
    {
        var backups = GetAllBackups();
        if (backups.Count <= keepCount)
        {
            return;
        }

        foreach (var backup in backups.Skip(keepCount))
        {
            DeleteBackup(backup.filepath);
        }
    }

    public static string ExportBackupToLocation(AppData appData, string exportPath)
    {
        WriteBackupFile(BuildEnvelope(appData, "手动导出备份"), exportPath);
        return exportPath;
    }

    private static BackupEnvelope BuildEnvelope(AppData appData, string description)
    {
        return new BackupEnvelope
        {
            AppVersion = appData.Version,
            ExportedAt = DateTime.Now,
            Data = BuildBackupData(appData, description)
        };
    }

    private static BackupData BuildBackupData(AppData appData, string description)
    {
        return new BackupData
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
    }

    private static void WriteBackupFile(BackupEnvelope envelope, string path)
    {
        envelope.Data.Info.Checksum = "";
        var dataJson = JsonSerializer.Serialize(envelope.Data, JsonOptions);
        envelope.Data.Info.Checksum = CalculateChecksum(dataJson);

        var finalJson = JsonSerializer.Serialize(envelope, JsonOptions);
        var parent = Directory.GetParent(path);
        if (parent != null && !parent.Exists)
        {
            Directory.CreateDirectory(parent.FullName);
        }

        File.WriteAllText(path, finalJson, Encoding.UTF8);
    }

    private static BackupData? DeserializeBackupData(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (document.RootElement.TryGetProperty("data", out var dataElement))
        {
            return JsonSerializer.Deserialize<BackupData>(dataElement.GetRawText(), JsonOptions);
        }

        return JsonSerializer.Deserialize<BackupData>(json, JsonOptions);
    }
}
