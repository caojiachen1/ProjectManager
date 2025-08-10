using System.Text.Json;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    public class ComfyUIDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "ComfyUI";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查典型的ComfyUI目录结构
            var comfyDirs = new[] { "custom_nodes", "models", "web", "comfy" };
            foreach (var dir in comfyDirs)
            {
                if (Directory.Exists(Path.Combine(projectPath, dir)))
                {
                    confidence += 0.2;
                    reasons.Add($"发现ComfyUI目录结构: {dir}");
                }
            }

            // 检查main.py和典型的ComfyUI文件
            if (File.Exists(Path.Combine(projectPath, "main.py")))
            {
                var mainContent = await ReadFileContentAsync(Path.Combine(projectPath, "main.py"));
                if (ContainsAnyKeyword(mainContent, "comfy", "ComfyUI", "server.py"))
                {
                    confidence += 0.4;
                    reasons.Add("在main.py中发现ComfyUI相关代码");
                }
            }

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                if (ContainsAnyKeyword(reqContent, "torch", "transformers", "diffusers"))
                {
                    confidence += 0.2;
                    reasons.Add("发现与图像生成相关的依赖");
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "ComfyUI项目";
                result.SuggestedDescription = "ComfyUI图像生成工作流";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 8188;
                result.SuggestedTags = new List<string> { "图像生成", "ComfyUI", "Stable Diffusion", "AI绘画" };
            }

            return result;
        }
    }

    public class NodeJSDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Node.js";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "JavaScript/TypeScript"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查package.json
            var packageJsonPath = Path.Combine(projectPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                confidence += 0.5;
                reasons.Add("发现package.json文件");

                var packageJson = await ReadJsonFileAsync(packageJsonPath);
                if (packageJson.HasValue)
                {
                    var json = packageJson.Value;
                    
                    if (json.TryGetProperty("main", out _))
                    {
                        confidence += 0.1;
                        reasons.Add("package.json中定义了main入口");
                    }

                    if (json.TryGetProperty("scripts", out var scripts))
                    {
                        if (scripts.TryGetProperty("start", out _))
                        {
                            confidence += 0.1;
                            reasons.Add("定义了start脚本");
                        }
                    }
                }
            }

            // 检查server.js, index.js, app.js等典型的Node.js入口文件
            var nodeFiles = new[] { "server.js", "index.js", "app.js", "main.js" };
            foreach (var file in nodeFiles)
            {
                if (File.Exists(Path.Combine(projectPath, file)))
                {
                    confidence += 0.1;
                    reasons.Add($"发现Node.js入口文件: {file}");
                    break;
                }
            }

            // 检查node_modules目录
            if (Directory.Exists(Path.Combine(projectPath, "node_modules")))
            {
                confidence += 0.1;
                reasons.Add("发现node_modules目录");
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Node.js应用";
                result.SuggestedDescription = "基于Node.js的服务端应用";
                result.SuggestedStartCommand = "npm start";
                result.SuggestedPort = 3000;
                result.SuggestedTags = new List<string> { "后端", "Node.js", "JavaScript", "服务器" };
            }

            return result;
        }
    }

    public class DotNetDetector : ProjectTypeDetector
    {
        public override string FrameworkName => ".NET";

        public override Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "C#"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查.csproj文件
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Any())
            {
                confidence += 0.8;
                reasons.Add($"发现{csprojFiles.Length}个.csproj项目文件");
            }

            // 检查.sln文件
            var slnFiles = Directory.GetFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly);
            if (slnFiles.Any())
            {
                confidence += 0.3;
                reasons.Add($"发现{slnFiles.Length}个解决方案文件");
            }

            // 检查Program.cs或Startup.cs
            if (File.Exists(Path.Combine(projectPath, "Program.cs")))
            {
                confidence += 0.2;
                reasons.Add("发现Program.cs文件");
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? ".NET项目";
                result.SuggestedDescription = "基于.NET的C#应用程序";
                result.SuggestedStartCommand = "dotnet run";
                result.SuggestedPort = 5000;
                result.SuggestedTags = new List<string> { ".NET", "C#", "后端", "Web API" };
            }

            return Task.FromResult(result);
        }
    }
}
