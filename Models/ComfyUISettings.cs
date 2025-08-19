namespace ProjectManager.Models;

/// <summary>
/// ComfyUI 专用的可持久化设置（避免仅依赖 StartCommand 解析）。
/// </summary>
public class ComfyUISettings
{
    public bool ListenAllInterfaces { get; set; }
    public bool LowVramMode { get; set; }
    public bool CpuMode { get; set; }
    public int Port { get; set; } = 8188;
    public string PythonPath { get; set; } = string.Empty;
    public string ModelsPath { get; set; } = "./models";
    public string OutputPath { get; set; } = "./output";
    public string ExtraArgs { get; set; } = string.Empty;
    public string CustomNodesPath { get; set; } = "./custom_nodes";
    public bool AutoLoadWorkflow { get; set; } = true;
    public bool EnableWorkflowSnapshots { get; set; } = false;
}
