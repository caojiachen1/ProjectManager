using System.Text.Json;
using ProjectManager.Models;

namespace ProjectManager.Services
{
    // Web框架检测器
    public class StreamlitDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Streamlit";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("streamlit")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现streamlit依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            // 检查Python文件中的streamlit导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10)) // 限制检查文件数量
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "import streamlit", "from streamlit", "st."))
                {
                    confidence += 0.3;
                    reasons.Add($"在{Path.GetFileName(file)}中发现streamlit相关代码");
                    break;
                }
            }

            // 检查典型的Streamlit文件名
            var streamlitFiles = new[] { "app.py", "main.py", "streamlit_app.py", "dashboard.py" };
            foreach (var fileName in streamlitFiles)
            {
                if (File.Exists(Path.Combine(projectPath, fileName)))
                {
                    confidence += 0.1;
                    reasons.Add($"发现典型的Streamlit文件: {fileName}");
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Streamlit应用";
                result.SuggestedDescription = "基于Streamlit的数据应用";
                result.SuggestedStartCommand = "streamlit run app.py";
                result.SuggestedPort = 8501;
                result.SuggestedTags = new List<string> { "Web应用", "数据可视化", "Streamlit", "仪表板" };
            }

            return result;
        }
    }

    public class GradioDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Gradio";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("gradio")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现gradio依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            // 检查Python文件中的gradio导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "import gradio", "from gradio", "gr.", ".launch()"))
                {
                    confidence += 0.3;
                    reasons.Add($"在{Path.GetFileName(file)}中发现gradio相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Gradio应用";
                result.SuggestedDescription = "基于Gradio的交互式Web界面";
                result.SuggestedStartCommand = "python app.py";
                result.SuggestedPort = 7860;
                result.SuggestedTags = new List<string> { "Web界面", "演示", "Gradio", "交互式" };
            }

            return result;
        }
    }

    public class FastAPIDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "FastAPI";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("fastapi")))
                {
                    confidence += 0.5;
                    reasons.Add("在requirements.txt中发现fastapi依赖");
                }
                
                if (dependencies.Any(d => d.ToLower().Contains("uvicorn")))
                {
                    confidence += 0.2;
                    reasons.Add("在requirements.txt中发现uvicorn依赖");
                }
                
                result.DetectedDependencies = dependencies;
            }

            // 检查Python文件中的FastAPI导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "from fastapi", "FastAPI()", "@app.get", "@app.post"))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现FastAPI相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "FastAPI应用";
                result.SuggestedDescription = "基于FastAPI的Web API服务";
                result.SuggestedStartCommand = "uvicorn main:app --host 0.0.0.0 --port 8000 --reload";
                result.SuggestedPort = 8000;
                result.SuggestedTags = new List<string> { "API", "Web服务", "FastAPI", "后端" };
            }

            return result;
        }
    }

    public class FlaskDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Flask";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("flask")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现flask依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            // 检查Python文件中的Flask导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "from flask", "Flask(__name__)", "@app.route", "app.run()"))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现Flask相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Flask应用";
                result.SuggestedDescription = "基于Flask的Web应用";
                result.SuggestedStartCommand = "python app.py";
                result.SuggestedPort = 5000;
                result.SuggestedTags = new List<string> { "Web框架", "Flask", "API", "后端" };
            }

            return result;
        }
    }

    public class DjangoDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Django";

        public override async Task<ProjectDetectionResult> DetectAsync(string projectPath)
        {
            var result = new ProjectDetectionResult
            {
                DetectedFramework = FrameworkName,
                DetectedLanguage = "Python"
            };

            double confidence = 0.0;
            var reasons = new List<string>();

            // 检查manage.py文件
            if (File.Exists(Path.Combine(projectPath, "manage.py")))
            {
                confidence += 0.7;
                reasons.Add("发现Django的manage.py文件");
            }

            // 检查settings.py文件
            var settingsFiles = Directory.GetFiles(projectPath, "settings.py", SearchOption.AllDirectories);
            if (settingsFiles.Any())
            {
                confidence += 0.3;
                reasons.Add("发现Django的settings.py文件");
            }

            // 检查requirements.txt
            var reqPath = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                var reqContent = await ReadFileContentAsync(reqPath);
                var dependencies = ExtractDependencies(reqContent);
                
                if (dependencies.Any(d => d.ToLower().Contains("django")))
                {
                    confidence += 0.4;
                    reasons.Add("在requirements.txt中发现django依赖");
                    result.DetectedDependencies = dependencies;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Django项目";
                result.SuggestedDescription = "基于Django的Web框架项目";
                result.SuggestedStartCommand = "python manage.py runserver";
                result.SuggestedPort = 8000;
                result.SuggestedTags = new List<string> { "Web框架", "Django", "全栈", "后端" };
            }

            return result;
        }
    }

    // AI/ML框架检测器
    public class PyTorchDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "PyTorch";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("torch")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现torch依赖");
                }
                
                if (dependencies.Any(d => d.ToLower().Contains("torchvision")))
                {
                    confidence += 0.2;
                    reasons.Add("在requirements.txt中发现torchvision依赖");
                }
                
                result.DetectedDependencies = dependencies;
            }

            // 检查Python文件中的PyTorch导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "import torch", "from torch", "torch.nn", "torch.tensor"))
                {
                    confidence += 0.4;
                    reasons.Add($"在{Path.GetFileName(file)}中发现PyTorch相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "PyTorch项目";
                result.SuggestedDescription = "基于PyTorch的深度学习项目";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "深度学习", "机器学习", "PyTorch", "AI" };
            }

            return result;
        }
    }

    public class TransformersDetector : ProjectTypeDetector
    {
        public override string FrameworkName => "Transformers";

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
                
                if (dependencies.Any(d => d.ToLower().Contains("transformers")))
                {
                    confidence += 0.6;
                    reasons.Add("在requirements.txt中发现transformers依赖");
                }
                
                if (dependencies.Any(d => d.ToLower().Contains("datasets")))
                {
                    confidence += 0.2;
                    reasons.Add("在requirements.txt中发现datasets依赖");
                }
                
                result.DetectedDependencies = dependencies;
            }

            // 检查Python文件中的Transformers导入
            var pythonFiles = Directory.GetFiles(projectPath, "*.py", SearchOption.TopDirectoryOnly);
            foreach (var file in pythonFiles.Take(10))
            {
                var content = await ReadFileContentAsync(file, 50);
                if (ContainsAnyKeyword(content, "from transformers", "AutoModel", "AutoTokenizer", "pipeline"))
                {
                    confidence += 0.5;
                    reasons.Add($"在{Path.GetFileName(file)}中发现Transformers相关代码");
                    break;
                }
            }

            result.ConfidenceLevel = Math.Min(confidence, 1.0);
            result.DetectionReason = string.Join("; ", reasons);

            if (confidence > 0.3)
            {
                result.SuggestedName = Path.GetFileName(projectPath) ?? "Transformers项目";
                result.SuggestedDescription = "基于Hugging Face Transformers的NLP项目";
                result.SuggestedStartCommand = "python main.py";
                result.SuggestedPort = 0;
                result.SuggestedTags = new List<string> { "NLP", "Transformers", "Hugging Face", "AI", "语言模型" };
            }

            return result;
        }
    }
}
