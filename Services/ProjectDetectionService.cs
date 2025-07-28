using System.Text.Json;
using System.Text.RegularExpressions;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    /// <summary>
    /// 项目检测结果
    /// </summary>
    public class ProjectDetectionResult
    {
        public string DetectedFramework { get; set; } = string.Empty;
        public string SuggestedName { get; set; } = string.Empty;
        public string SuggestedDescription { get; set; } = string.Empty;
        public string SuggestedStartCommand { get; set; } = string.Empty;
        public int SuggestedPort { get; set; } = 0;
        public List<string> SuggestedTags { get; set; } = new();
        public string DetectedLanguage { get; set; } = string.Empty;
        public List<string> DetectedDependencies { get; set; } = new();
        public double ConfidenceLevel { get; set; } = 0.0; // 0-1之间的置信度
        public string DetectionReason { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// 项目检测服务接口
    /// </summary>
    public interface IProjectDetectionService
    {
        Task<ProjectDetectionResult> DetectProjectTypeAsync(string projectPath);
        Task<List<ProjectDetectionResult>> GetMultipleCandidatesAsync(string projectPath);
        Task<bool> ValidateProjectStructureAsync(string projectPath, string framework);
    }

    /// <summary>
    /// 增强的项目检测服务
    /// </summary>
    public class ProjectDetectionService : IProjectDetectionService
    {
        private readonly Dictionary<string, ProjectTypeDetector> _detectors;

        public ProjectDetectionService()
        {
            _detectors = InitializeDetectors();
        }

        public async Task<ProjectDetectionResult> DetectProjectTypeAsync(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                return new ProjectDetectionResult
                {
                    DetectedFramework = "其他",
                    ConfidenceLevel = 0.0,
                    DetectionReason = "路径无效或不存在"
                };
            }

            var candidates = await GetMultipleCandidatesAsync(projectPath);
            
            // 返回置信度最高的结果
            var bestMatch = candidates.OrderByDescending(c => c.ConfidenceLevel).FirstOrDefault();
            
            return bestMatch ?? new ProjectDetectionResult
            {
                DetectedFramework = "其他",
                ConfidenceLevel = 0.0,
                DetectionReason = "未能识别项目类型"
            };
        }

        public async Task<List<ProjectDetectionResult>> GetMultipleCandidatesAsync(string projectPath)
        {
            var results = new List<ProjectDetectionResult>();

            foreach (var detector in _detectors.Values)
            {
                try
                {
                    var result = await detector.DetectAsync(projectPath);
                    if (result.ConfidenceLevel > 0.1) // 只包含有一定置信度的结果
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    // 记录但不中断检测过程
                    Console.WriteLine($"检测器 {detector.FrameworkName} 出现错误: {ex.Message}");
                }
            }

            return results.OrderByDescending(r => r.ConfidenceLevel).ToList();
        }

        public async Task<bool> ValidateProjectStructureAsync(string projectPath, string framework)
        {
            if (_detectors.TryGetValue(framework, out var detector))
            {
                return await detector.ValidateStructureAsync(projectPath);
            }
            return false;
        }

        private Dictionary<string, ProjectTypeDetector> InitializeDetectors()
        {
            var detectors = new Dictionary<string, ProjectTypeDetector>();

            // Web框架检测器
            detectors["Streamlit"] = new StreamlitDetector();
            detectors["Gradio"] = new GradioDetector();
            detectors["FastAPI"] = new FastAPIDetector();
            detectors["Flask"] = new FlaskDetector();
            detectors["Django"] = new DjangoDetector();

            // AI/ML框架检测器
            detectors["PyTorch"] = new PyTorchDetector();
            detectors["TensorFlow"] = new TensorFlowDetector();
            detectors["Transformers"] = new TransformersDetector();
            detectors["LangChain"] = new LangChainDetector();
            detectors["OpenAI"] = new OpenAIDetector();
            detectors["Stable Diffusion"] = new StableDiffusionDetector();
            detectors["ComfyUI"] = new ComfyUIDetector();

            // 前端框架检测器
            detectors["React"] = new ReactDetector();
            detectors["Vue.js"] = new VueDetector();
            detectors["Next.js"] = new NextJSDetector();
            detectors["Angular"] = new AngularDetector();

            // 其他框架检测器
            detectors["Node.js"] = new NodeJSDetector();
            detectors["Jupyter"] = new JupyterDetector();
            detectors["Unity"] = new UnityDetector();
            detectors[".NET"] = new DotNetDetector();
            detectors["Spring Boot"] = new SpringBootDetector();

            return detectors;
        }
    }

    /// <summary>
    /// 项目类型检测器基类
    /// </summary>
    public abstract class ProjectTypeDetector
    {
        public abstract string FrameworkName { get; }
        
        public abstract Task<ProjectDetectionResult> DetectAsync(string projectPath);
        
        public virtual async Task<bool> ValidateStructureAsync(string projectPath)
        {
            var result = await DetectAsync(projectPath);
            return result.ConfidenceLevel > 0.5;
        }

        protected async Task<string> ReadFileContentAsync(string filePath, int maxLines = 100)
        {
            try
            {
                if (!File.Exists(filePath)) return string.Empty;
                
                var lines = await File.ReadAllLinesAsync(filePath);
                return string.Join("\n", lines.Take(maxLines));
            }
            catch
            {
                return string.Empty;
            }
        }

        protected bool ContainsAnyKeyword(string content, params string[] keywords)
        {
            if (string.IsNullOrEmpty(content)) return false;
            content = content.ToLowerInvariant();
            return keywords.Any(keyword => content.Contains(keyword.ToLowerInvariant()));
        }

        protected List<string> ExtractDependencies(string requirementsContent)
        {
            var dependencies = new List<string>();
            if (string.IsNullOrEmpty(requirementsContent)) return dependencies;

            var lines = requirementsContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) continue;
                
                // 提取包名（去除版本号）
                var packageName = trimmed.Split(new[] { '=', '>', '<', '!', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (!string.IsNullOrEmpty(packageName))
                {
                    dependencies.Add(packageName);
                }
            }

            return dependencies;
        }

        protected async Task<JsonElement?> ReadJsonFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                
                var content = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            catch
            {
                return null;
            }
        }
    }
}
