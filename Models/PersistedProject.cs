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
    public string Framework { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public ProjectStatus Status { get; set; } = ProjectStatus.Stopped; // 读取时会重置为 Stopped
    public string LogOutput { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool AutoStart { get; set; }
    public Dictionary<string,string> EnvironmentVariables { get; set; } = new();
    public List<string> GitRepositories { get; set; } = new();
    public ComfyUISettings? ComfyUISettings { get; set; }
    public NodeJSSettings? NodeJSSettings { get; set; }
    public DotNetSettings? DotNetSettings { get; set; }
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
        Framework = p.Framework,
        CreatedDate = p.CreatedDate,
        LastModified = p.LastModified,
        Status = p.Status, // 保存当前状态（读取时会安全处理）
        LogOutput = p.LogOutput,
        Tags = new List<string>(p.Tags),
        AutoStart = p.AutoStart,
        EnvironmentVariables = new Dictionary<string,string>(p.EnvironmentVariables),
        GitRepositories = new List<string>(p.GitRepositories),
        ComfyUISettings = p.ComfyUISettings == null ? null : new ComfyUISettings
        {
            ListenAllInterfaces = p.ComfyUISettings.ListenAllInterfaces,
            LowVramMode = p.ComfyUISettings.LowVramMode,
            CpuMode = p.ComfyUISettings.CpuMode,
            Port = p.ComfyUISettings.Port,
            PythonPath = p.ComfyUISettings.PythonPath,
            ModelsPath = p.ComfyUISettings.ModelsPath,
            OutputPath = p.ComfyUISettings.OutputPath,
            ExtraArgs = p.ComfyUISettings.ExtraArgs,
            CustomNodesPath = p.ComfyUISettings.CustomNodesPath,
            AutoLoadWorkflow = p.ComfyUISettings.AutoLoadWorkflow,
            EnableWorkflowSnapshots = p.ComfyUISettings.EnableWorkflowSnapshots
        },
        NodeJSSettings = p.NodeJSSettings == null ? null : new NodeJSSettings
        {
            Port = p.NodeJSSettings.Port,
            NodeVersion = p.NodeJSSettings.NodeVersion,
            PackageManager = p.NodeJSSettings.PackageManager,
            DevelopmentMode = p.NodeJSSettings.DevelopmentMode,
            HotReload = p.NodeJSSettings.HotReload,
            DebugMode = p.NodeJSSettings.DebugMode,
            BuildCommand = p.NodeJSSettings.BuildCommand,
            TestCommand = p.NodeJSSettings.TestCommand,
            BuildOutputPath = p.NodeJSSettings.BuildOutputPath,
            RunTestsBeforeBuild = p.NodeJSSettings.RunTestsBeforeBuild,
            MinifyOutput = p.NodeJSSettings.MinifyOutput,
            EnvironmentFile = p.NodeJSSettings.EnvironmentFile,
            CustomEnvironmentVars = p.NodeJSSettings.CustomEnvironmentVars
        },
        DotNetSettings = p.DotNetSettings == null ? null : new DotNetSettings
        {
            TargetFramework = p.DotNetSettings.TargetFramework,
            ProjectType = p.DotNetSettings.ProjectType,
            Port = p.DotNetSettings.Port,
            EnableHotReload = p.DotNetSettings.EnableHotReload,
            EnableHttpsRedirection = p.DotNetSettings.EnableHttpsRedirection,
            EnableDeveloperExceptionPage = p.DotNetSettings.EnableDeveloperExceptionPage,
            BuildConfiguration = p.DotNetSettings.BuildConfiguration,
            BuildCommand = p.DotNetSettings.BuildCommand,
            TestCommand = p.DotNetSettings.TestCommand,
            OutputPath = p.DotNetSettings.OutputPath,
            RunTestsBeforeBuild = p.DotNetSettings.RunTestsBeforeBuild,
            EnableCodeAnalysis = p.DotNetSettings.EnableCodeAnalysis,
            TreatWarningsAsErrors = p.DotNetSettings.TreatWarningsAsErrors,
            PublishCommand = p.DotNetSettings.PublishCommand,
            TargetRuntime = p.DotNetSettings.TargetRuntime,
            PublishPath = p.DotNetSettings.PublishPath,
            SingleFilePublish = p.DotNetSettings.SingleFilePublish,
            SelfContainedPublish = p.DotNetSettings.SelfContainedPublish,
            EnableReadyToRun = p.DotNetSettings.EnableReadyToRun
        }
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
            Framework = dto.Framework,
            CreatedDate = dto.CreatedDate,
            LastModified = dto.LastModified,
            Status = ProjectStatus.Stopped, // 启动时不恢复 Running 状态
            LogOutput = dto.LogOutput,
            Tags = new List<string>(dto.Tags),
            AutoStart = dto.AutoStart,
            EnvironmentVariables = new Dictionary<string,string>(dto.EnvironmentVariables),
            GitRepositories = new List<string>(dto.GitRepositories ?? new())
        };
        if (dto.ComfyUISettings != null)
        {
            model.ComfyUISettings = new ComfyUISettings
            {
                ListenAllInterfaces = dto.ComfyUISettings.ListenAllInterfaces,
                LowVramMode = dto.ComfyUISettings.LowVramMode,
                CpuMode = dto.ComfyUISettings.CpuMode,
                Port = dto.ComfyUISettings.Port,
                PythonPath = dto.ComfyUISettings.PythonPath,
                ModelsPath = dto.ComfyUISettings.ModelsPath,
                OutputPath = dto.ComfyUISettings.OutputPath,
                ExtraArgs = dto.ComfyUISettings.ExtraArgs,
                CustomNodesPath = dto.ComfyUISettings.CustomNodesPath,
                AutoLoadWorkflow = dto.ComfyUISettings.AutoLoadWorkflow,
                EnableWorkflowSnapshots = dto.ComfyUISettings.EnableWorkflowSnapshots
            };
        }
        if (dto.NodeJSSettings != null)
        {
            model.NodeJSSettings = new NodeJSSettings
            {
                Port = dto.NodeJSSettings.Port,
                NodeVersion = dto.NodeJSSettings.NodeVersion,
                PackageManager = dto.NodeJSSettings.PackageManager,
                DevelopmentMode = dto.NodeJSSettings.DevelopmentMode,
                HotReload = dto.NodeJSSettings.HotReload,
                DebugMode = dto.NodeJSSettings.DebugMode,
                BuildCommand = dto.NodeJSSettings.BuildCommand,
                TestCommand = dto.NodeJSSettings.TestCommand,
                BuildOutputPath = dto.NodeJSSettings.BuildOutputPath,
                RunTestsBeforeBuild = dto.NodeJSSettings.RunTestsBeforeBuild,
                MinifyOutput = dto.NodeJSSettings.MinifyOutput,
                EnvironmentFile = dto.NodeJSSettings.EnvironmentFile,
                CustomEnvironmentVars = dto.NodeJSSettings.CustomEnvironmentVars
            };
        }
        if (dto.DotNetSettings != null)
        {
            model.DotNetSettings = new DotNetSettings
            {
                TargetFramework = dto.DotNetSettings.TargetFramework,
                ProjectType = dto.DotNetSettings.ProjectType,
                Port = dto.DotNetSettings.Port,
                EnableHotReload = dto.DotNetSettings.EnableHotReload,
                EnableHttpsRedirection = dto.DotNetSettings.EnableHttpsRedirection,
                EnableDeveloperExceptionPage = dto.DotNetSettings.EnableDeveloperExceptionPage,
                BuildConfiguration = dto.DotNetSettings.BuildConfiguration,
                BuildCommand = dto.DotNetSettings.BuildCommand,
                TestCommand = dto.DotNetSettings.TestCommand,
                OutputPath = dto.DotNetSettings.OutputPath,
                RunTestsBeforeBuild = dto.DotNetSettings.RunTestsBeforeBuild,
                EnableCodeAnalysis = dto.DotNetSettings.EnableCodeAnalysis,
                TreatWarningsAsErrors = dto.DotNetSettings.TreatWarningsAsErrors,
                PublishCommand = dto.DotNetSettings.PublishCommand,
                TargetRuntime = dto.DotNetSettings.TargetRuntime,
                PublishPath = dto.DotNetSettings.PublishPath,
                SingleFilePublish = dto.DotNetSettings.SingleFilePublish,
                SelfContainedPublish = dto.DotNetSettings.SelfContainedPublish,
                EnableReadyToRun = dto.DotNetSettings.EnableReadyToRun
            };
        }
        return model;
    }
}
