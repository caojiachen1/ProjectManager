using System.Text.Json;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    // 继续AI/ML框架检测器
    public class LangChainDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "LangChain";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                var dependencies = ExtractDependencies(reqContent);
                
                if (dependencies.Any(d => d.ToLower().Contains("langchain")))
                {
                    confidence += 0.7;
                    reasons.Add("在requirements.txt中发现langchain依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            // 检查Python文件中的LangChain导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "from langchain", "import langchain", "LLMChain", "ChatOpenAI"))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现LangChain相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "LangChain项目";
                result.SuggestedDescription = "基于LangChain的大语言模型应用";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "LLM", "LangChain", "AI", "聊天机器人", "语言模型" };
            }

            return result;
        }
    }

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

    // 前端框架检测器
    public class ReactDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "React";

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
                var packageJson = await ReadJsonFileAsync(packageJsonPath);
                if (packageJson.HasValue)
                {
                    var json = packageJson.Value;
                    
                    // 检查dependencies
                    if (json.TryGetProperty("dependencies", out var deps))
                    {
                        if (deps.TryGetProperty("react", out _))
                        {
                            confidence += 0.7;
                            reasons.Add("在package.json的dependencies中发现react");
                        }
                        if (deps.TryGetProperty("react-dom", out _))
                        {
                            confidence += 0.2;
                            reasons.Add("在package.json中发现react-dom");
                        }
                    }

                    // 检查scripts
                    if (json.TryGetProperty("scripts", out var scripts))
                    {
                        var scriptsStr = scripts.ToString();
                        if (ContainsAnyKeyword(scriptsStr, "react-scripts", "start", "build"))
                        {
                            confidence += 0.2;
                            reasons.Add("在package.json的scripts中发现React相关脚本");
                        }
                    }
                }
            }

            // 检查src目录
            if (Directory.Exists(Path.Combine(projectPath, "src")))
            {
                confidence += 0.1;
                reasons.Add("发现src目录");

                // 检查App.js或App.tsx
                var appFiles = new[] { "App.js", "App.jsx", "App.ts", "App.tsx" };
                foreach (var appFile in appFiles)
                {
                    if (File.Exists(Path.Combine(projectPath, "src", appFile)))
                    {
                        confidence += 0.1;
                        reasons.Add($"发现{appFile}文件");
                        break;
                    }
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "React应用";
                result.SuggestedDescription = "基于React的前端应用";
                result.SuggestedStartCommand = "npm start";
                result.SuggestedPort = 3000;
                result.SuggestedTags = new List<string> { "前端", "React", "JavaScript", "Web应用" };
            }

            return result;
        }
    }

    public class NextJSDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Next.js";

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
                var packageJson = await ReadJsonFileAsync(packageJsonPath);
                if (packageJson.HasValue)
                {
                    var json = packageJson.Value;
                    
                    if (json.TryGetProperty("dependencies", out var deps))
                    {
                        if (deps.TryGetProperty("next", out _))
                        {
                            confidence += 0.8;
                            reasons.Add("在package.json中发现next依赖");
                        }
                    }
                }
            }

            // 检查next.config.js
            if (File.Exists(Path.Combine(projectPath, "next.config.js")) ||
                File.Exists(Path.Combine(projectPath, "next.config.mjs")))
            {
                confidence += 0.3;
                reasons.Add("发现next.config配置文件");
            }

            // 检查pages或app目录
            if (Directory.Exists(Path.Combine(projectPath, "pages")))
            {
                confidence += 0.2;
                reasons.Add("发现pages目录");
            }

            if (Directory.Exists(Path.Combine(projectPath, "app")))
            {
                confidence += 0.2;
                reasons.Add("发现app目录(App Router)");
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Next.js应用";
                result.SuggestedDescription = "基于Next.js的全栈React应用";
                result.SuggestedStartCommand = "npm run dev";
                result.SuggestedPort = 3000;
                result.SuggestedTags = new List<string> { "全栈", "Next.js", "React", "Web应用" };
            }

            return result;
        }
    }

    public class VueDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Vue.js";

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
                var packageJson = await ReadJsonFileAsync(packageJsonPath);
                if (packageJson.HasValue)
                {
                    var json = packageJson.Value;
                    
                    if (json.TryGetProperty("dependencies", out var deps))
                    {
                        if (deps.TryGetProperty("vue", out _))
                        {
                            confidence += 0.7;
                            reasons.Add("在package.json中发现vue依赖");
                        }
                    }
                }
            }

            // 检查vue.config.js
            if (File.Exists(Path.Combine(projectPath, "vue.config.js")))
            {
                confidence += 0.2;
                reasons.Add("发现vue.config.js配置文件");
            }

            // 检查.vue文件
            var vueFiles = Directory.GetFiles(projectPath, "*.vue", SearchOption.AllDirectories);
            if (vueFiles.Any())
            {
                confidence += 0.3;
                reasons.Add($"发现{vueFiles.Length}个.vue文件");
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Vue.js应用";
                result.SuggestedDescription = "基于Vue.js的前端应用";
                result.SuggestedStartCommand = "npm run serve";
                result.SuggestedPort = 8080;
                result.SuggestedTags = new List<string> { "前端", "Vue.js", "JavaScript", "Web应用" };
            }

            return result;
        }
    }

    // 其他框架检测器
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

    public class JupyterDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Jupyter";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查.ipynb文件
            var notebookFiles = Directory.GetFiles(projectPath, "*.ipynb", SearchOption.AllDirectories);
            if (notebookFiles.Any())
            {
                confidence += 0.8;
                reasons.Add($"发现{notebookFiles.Length}个Jupyter Notebook文件");
            }

            // 检查requirements.txt中的jupyter相关依赖
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                if (ContainsAnyKeyword(reqContent, "jupyter", "notebook", "jupyterlab"))
                {
                    confidence += 0.3;
                    reasons.Add("在requirements.txt中发现jupyter相关依赖");
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Jupyter项目";
                result.SuggestedDescription = "基于Jupyter Notebook的数据科学项目";
                result.SuggestedStartCommand = "jupyter notebook";
                result.SuggestedPort = 8888;
                result.SuggestedTags = new List<string> { "数据科学", "Jupyter", "Notebook", "分析" };
            }

            return result;
        }
    }

    // 其他语言和框架检测器
    public class DotNetDetector : ProjectTypeDetector
    {
        public override string FrameworkName => ".NET";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
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

            return result;
        }
    }

    public class UnityDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Unity";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "C#"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查Unity特有的目录结构
            var unityDirs = new[] { "Assets", "ProjectSettings", "Packages" };
            foreach (var dir in unityDirs)
            {
                if (Directory.Exists(Path.Combine(projectPath, dir)))
                {
                    confidence += 0.3;
                    reasons.Add($"发现Unity目录: {dir}");
                }
            }

            // 检查ProjectSettings/ProjectVersion.txt
            var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
            if (File.Exists(versionFile))
            {
                var content = await ReadFileContentAsync(versionFile);
                if (content.Contains("m_EditorVersion"))
                {
                    confidence += 0.4;
                    reasons.Add("发现Unity版本文件");
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Unity项目";
                result.SuggestedDescription = "Unity游戏开发项目";
                result.SuggestedStartCommand = "Unity编辑器启动";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "游戏开发", "Unity", "C#", "3D" };
            }

            return result;
        }
    }

    public class SpringBootDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Spring Boot";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Java"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查pom.xml
            var pomPath = Path.Combine(projectPath, "pom.xml");
            if (File.Exists(pomPath))
            {
                var pomContent = await ReadFileContentAsync(pomPath);
                if (ContainsAnyKeyword(pomContent, "spring-boot", "SpringBootApplication"))
                {
                    confidence += 0.7;
                    reasons.Add("在pom.xml中发现Spring Boot依赖");
                }
            }

            // 检查build.gradle
            var gradlePath = Path.Combine(projectPath, "build.gradle");
            if (File.Exists(gradlePath))
            {
                var gradleContent = await ReadFileContentAsync(gradlePath);
                if (ContainsAnyKeyword(gradleContent, "spring-boot", "org.springframework.boot"))
                {
                    confidence += 0.7;
                    reasons.Add("在build.gradle中发现Spring Boot依赖");
                }
            }

            // 检查application.properties或application.yml
            var configFiles = new[] { "application.properties", "application.yml", "application.yaml" };
            foreach (var configFile in configFiles)
            {
                if (File.Exists(Path.Combine(projectPath, "src", "main", "resources", configFile)))
                {
                    confidence += 0.2;
                    reasons.Add($"发现Spring Boot配置文件: {configFile}");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Spring Boot项目";
                result.SuggestedDescription = "基于Spring Boot的Java Web应用";
                result.SuggestedStartCommand = "mvn spring-boot:run";
                result.SuggestedPort = 8080;
                result.SuggestedTags = new List<string> { "Java", "Spring Boot", "Web框架", "后端" };
            }

            return result;
        }
    }

    // 补充缺少的检测器
    public class TensorFlowDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "TensorFlow";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                var dependencies = ExtractDependencies(reqContent);
                
                if (dependencies.Any(d => d.ToLower().Contains("tensorflow")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现tensorflow依赖");
                }
                
                result.DetectedDependencies = dependencies;
            }

            // 检查Python文件中的TensorFlow导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "import tensorflow", "from tensorflow", "tf."))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现TensorFlow相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "TensorFlow项目";
                result.SuggestedDescription = "基于TensorFlow的机器学习项目";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "机器学习", "深度学习", "TensorFlow", "AI" };
            }

            return result;
        }
    }

    public class OpenAIDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "OpenAI";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                var dependencies = ExtractDependencies(reqContent);
                
                if (dependencies.Any(d => d.ToLower().Contains("openai")))
                {
                    confidence += 0.7;
                    reasons.Add("在requirements.txt中发现openai依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            // 检查Python文件中的OpenAI导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "import openai", "from openai", "openai."))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现OpenAI相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "OpenAI项目";
                result.SuggestedDescription = "基于OpenAI API的AI应用";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "OpenAI", "GPT", "AI", "语言模型", "API" };
            }

            return result;
        }
    }

    public class StableDiffusionDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Stable Diffusion";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                var dependencies = ExtractDependencies(reqContent);
                
                if (dependencies.Any(d => d.ToLower().Contains("diffusers")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现diffusers依赖");
                }
                
                if (dependencies.Any(d => d.ToLower().Contains("transformers")))
                {
                    confidence += 0.2;
                    reasons.Add("在requirements.txt中发现transformers依赖");
                }
                
                result.DetectedDependencies = dependencies;
            }

            // 检查Python文件中的Stable Diffusion相关导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "diffusers", "StableDiffusionPipeline", "stable_diffusion"))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现Stable Diffusion相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Stable Diffusion项目";
                result.SuggestedDescription = "基于Stable Diffusion的图像生成项目";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "图像生成", "Stable Diffusion", "AI绘画", "扩散模型" };
            }

            return result;
        }
    }

    public class AngularDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Angular";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "TypeScript"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查package.json
            var packageJsonPath = Path.Combine(projectPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var packageJson = await ReadJsonFileAsync(packageJsonPath);
                if (packageJson.HasValue)
                {
                    var json = packageJson.Value;
                    
                    if (json.TryGetProperty("dependencies", out var deps))
                    {
                        if (deps.TryGetProperty("@angular/core", out _))
                        {
                            confidence += 0.8;
                            reasons.Add("在package.json中发现@angular/core依赖");
                        }
                    }
                }
            }

            // 检查angular.json
            if (File.Exists(Path.Combine(projectPath, "angular.json")))
            {
                confidence += 0.3;
                reasons.Add("发现angular.json配置文件");
            }

            // 检查src/app目录
            if (Directory.Exists(Path.Combine(projectPath, "src", "app")))
            {
                confidence += 0.2;
                reasons.Add("发现src/app目录结构");
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.5)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Angular应用";
                result.SuggestedDescription = "基于Angular的前端应用";
                result.SuggestedStartCommand = "ng serve";
                result.SuggestedPort = 4200;
                result.SuggestedTags = new List<string> { "前端", "Angular", "TypeScript", "Web应用" };
            }

            return await Task.FromResult(result);
        }
    }
}
