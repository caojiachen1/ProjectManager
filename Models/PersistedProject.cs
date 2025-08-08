using System.Text.Json.Serialization;

namespace ProjectManager.Models;

/// <summary>
/// 仅用于磁盘持久化的精简项目 DTO，避免序列化运行时对象(Process/GitInfo等)。
/// </summary>
public class PersistedProject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string StartCommand { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string PythonEnvironment { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public ProjectStatus Status { get; set; } = ProjectStatus.Stopped; // 读取时会重置为 Stopped
    public string LogOutput { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool AutoStart { get; set; }
    public Dictionary<string,string> EnvironmentVariables { get; set; } = new();
}

internal static class ProjectPersistenceMapper
{
    public static PersistedProject ToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        LocalPath = p.LocalPath,
        StartCommand = p.StartCommand,
        WorkingDirectory = p.WorkingDirectory,
        PythonEnvironment = p.PythonEnvironment,
        Framework = p.Framework,
        CreatedDate = p.CreatedDate,
        LastModified = p.LastModified,
        Status = p.Status, // 保存当前状态（读取时会安全处理）
        LogOutput = p.LogOutput,
        Tags = new List<string>(p.Tags),
        Version = p.Version,
        Author = p.Author,
        Port = p.Port,
        AutoStart = p.AutoStart,
        EnvironmentVariables = new Dictionary<string,string>(p.EnvironmentVariables)
    };

    public static Project ToModel(PersistedProject dto)
    {
        var model = new Project
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            LocalPath = dto.LocalPath,
            StartCommand = dto.StartCommand,
            WorkingDirectory = dto.WorkingDirectory,
            PythonEnvironment = dto.PythonEnvironment,
            Framework = dto.Framework,
            CreatedDate = dto.CreatedDate,
            LastModified = dto.LastModified,
            Status = ProjectStatus.Stopped, // 启动时不恢复 Running 状态
            LogOutput = dto.LogOutput,
            Tags = new List<string>(dto.Tags),
            Version = dto.Version,
            Author = dto.Author,
            Port = dto.Port,
            AutoStart = dto.AutoStart,
            EnvironmentVariables = new Dictionary<string,string>(dto.EnvironmentVariables)
        };
        return model;
    }
}
