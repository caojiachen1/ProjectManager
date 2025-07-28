using System.Collections.Generic;

namespace ProjectManager.Models
{
    /// <summary>
    /// 框架配置模型，用于定义不同框架的默认设置
    /// </summary>
    public class FrameworkConfig
    {
        public string Name { get; set; } = string.Empty;
        public string DefaultStartCommand { get; set; } = string.Empty;
        public int DefaultPort { get; set; } = 0;
        public List<string> DefaultTags { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string FileExtensions { get; set; } = string.Empty;
        public string RequirementsFile { get; set; } = string.Empty;
        public List<string> CommonCommands { get; set; } = new();
    }

    /// <summary>
    /// 框架配置服务，提供预定义的框架配置
    /// </summary>
    public static class FrameworkConfigService
    {
        public static readonly Dictionary<string, FrameworkConfig> Configs = new()
        {
            ["PyTorch"] = new FrameworkConfig
            {
                Name = "PyTorch",
                DefaultStartCommand = "python train.py",
                DefaultPort = 0,
                DefaultTags = new List<string> { "深度学习", "机器学习", "神经网络" },
                Description = "PyTorch深度学习框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python train.py", "python inference.py", "python main.py" }
            },
            
            ["TensorFlow"] = new FrameworkConfig
            {
                Name = "TensorFlow",
                DefaultStartCommand = "python main.py",
                DefaultPort = 0,
                DefaultTags = new List<string> { "深度学习", "机器学习", "TensorFlow" },
                Description = "TensorFlow机器学习平台",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python main.py", "python train.py", "python model.py" }
            },
            
            ["Transformers"] = new FrameworkConfig
            {
                Name = "Transformers",
                DefaultStartCommand = "python run_model.py",
                DefaultPort = 0,
                DefaultTags = new List<string> { "NLP", "自然语言处理", "Transformer", "BERT", "GPT" },
                Description = "Hugging Face Transformers NLP库",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python run_model.py", "python fine_tune.py", "python inference.py" }
            },
            
            ["LangChain"] = new FrameworkConfig
            {
                Name = "LangChain",
                DefaultStartCommand = "python app.py",
                DefaultPort = 8000,
                DefaultTags = new List<string> { "LLM", "大语言模型", "RAG", "Agent" },
                Description = "LangChain大语言模型应用框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python app.py", "python chat.py", "python agent.py" }
            },
            
            ["OpenAI"] = new FrameworkConfig
            {
                Name = "OpenAI",
                DefaultStartCommand = "python main.py",
                DefaultPort = 8080,
                DefaultTags = new List<string> { "OpenAI", "GPT", "API", "LLM" },
                Description = "OpenAI API集成项目",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python main.py", "python chat.py", "python api.py" }
            },
            
            ["Stable Diffusion"] = new FrameworkConfig
            {
                Name = "Stable Diffusion",
                DefaultStartCommand = "python generate.py",
                DefaultPort = 7860,
                DefaultTags = new List<string> { "图像生成", "AI绘画", "扩散模型", "AIGC" },
                Description = "Stable Diffusion图像生成模型",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python generate.py", "python webui.py", "python inference.py" }
            },
            
            ["FastAPI"] = new FrameworkConfig
            {
                Name = "FastAPI",
                DefaultStartCommand = "uvicorn main:app --host 0.0.0.0 --port 8000 --reload",
                DefaultPort = 8000,
                DefaultTags = new List<string> { "API", "Web服务", "FastAPI", "后端" },
                Description = "FastAPI Web框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> 
                { 
                    "uvicorn main:app --host 0.0.0.0 --port 8000 --reload",
                    "uvicorn app:app --reload",
                    "python -m uvicorn main:app --reload"
                }
            },
            
            ["Gradio"] = new FrameworkConfig
            {
                Name = "Gradio",
                DefaultStartCommand = "python app.py",
                DefaultPort = 7860,
                DefaultTags = new List<string> { "Web界面", "演示", "Gradio", "交互式" },
                Description = "Gradio交互式Web界面",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python app.py", "python demo.py", "python interface.py" }
            },
            
            ["Streamlit"] = new FrameworkConfig
            {
                Name = "Streamlit",
                DefaultStartCommand = "streamlit run app.py",
                DefaultPort = 8501,
                DefaultTags = new List<string> { "Web应用", "数据可视化", "Streamlit", "仪表板" },
                Description = "Streamlit数据应用框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> 
                { 
                    "streamlit run app.py",
                    "streamlit run main.py",
                    "streamlit run dashboard.py --server.port 8501"
                }
            },
            
            ["Flask"] = new FrameworkConfig
            {
                Name = "Flask",
                DefaultStartCommand = "python app.py",
                DefaultPort = 5000,
                DefaultTags = new List<string> { "Web框架", "Flask", "API", "后端" },
                Description = "Flask Web框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python app.py", "flask run", "python main.py" }
            },
            
            ["Django"] = new FrameworkConfig
            {
                Name = "Django",
                DefaultStartCommand = "python manage.py runserver",
                DefaultPort = 8000,
                DefaultTags = new List<string> { "Web框架", "Django", "全栈", "MVC" },
                Description = "Django Web框架",
                FileExtensions = "*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> 
                { 
                    "python manage.py runserver",
                    "python manage.py runserver 0.0.0.0:8000",
                    "python manage.py migrate && python manage.py runserver"
                }
            },
            
            ["Jupyter"] = new FrameworkConfig
            {
                Name = "Jupyter",
                DefaultStartCommand = "jupyter notebook",
                DefaultPort = 8888,
                DefaultTags = new List<string> { "数据科学", "笔记本", "分析", "可视化" },
                Description = "Jupyter Notebook交互式开发环境",
                FileExtensions = "*.ipynb,*.py",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> 
                { 
                    "jupyter notebook",
                    "jupyter lab",
                    "jupyter notebook --ip=0.0.0.0 --port=8888 --no-browser"
                }
            },
            
            ["Node.js"] = new FrameworkConfig
            {
                Name = "Node.js",
                DefaultStartCommand = "npm start",
                DefaultPort = 3000,
                DefaultTags = new List<string> { "JavaScript", "Node.js", "后端", "全栈" },
                Description = "Node.js JavaScript运行时",
                FileExtensions = "*.js,*.ts",
                RequirementsFile = "package.json",
                CommonCommands = new List<string> { "npm start", "node app.js", "npm run dev" }
            },
            
            ["React"] = new FrameworkConfig
            {
                Name = "React",
                DefaultStartCommand = "npm start",
                DefaultPort = 3000,
                DefaultTags = new List<string> { "前端", "React", "JavaScript", "UI" },
                Description = "React前端框架",
                FileExtensions = "*.js,*.jsx,*.ts,*.tsx",
                RequirementsFile = "package.json",
                CommonCommands = new List<string> { "npm start", "npm run dev", "yarn start" }
            },
            
            ["Vue.js"] = new FrameworkConfig
            {
                Name = "Vue.js",
                DefaultStartCommand = "npm run serve",
                DefaultPort = 8080,
                DefaultTags = new List<string> { "前端", "Vue", "JavaScript", "UI" },
                Description = "Vue.js前端框架",
                FileExtensions = "*.js,*.vue,*.ts",
                RequirementsFile = "package.json",
                CommonCommands = new List<string> { "npm run serve", "npm run dev", "yarn serve" }
            },
            
            ["Ollama"] = new FrameworkConfig
            {
                Name = "Ollama",
                DefaultStartCommand = "ollama serve",
                DefaultPort = 11434,
                DefaultTags = new List<string> { "LLM", "本地部署", "大语言模型", "推理" },
                Description = "Ollama本地大语言模型服务",
                FileExtensions = "*.py,*.sh",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "ollama serve", "ollama run llama2", "python client.py" }
            },
            
            ["ComfyUI"] = new FrameworkConfig
            {
                Name = "ComfyUI",
                DefaultStartCommand = "python main.py",
                DefaultPort = 8188,
                DefaultTags = new List<string> { "AI绘画", "图像生成", "工作流", "节点编辑" },
                Description = "ComfyUI图像生成工作流",
                FileExtensions = "*.py,*.json",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python main.py", "python main.py --listen", "python main.py --port 8188" }
            },
            
            ["其他"] = new FrameworkConfig
            {
                Name = "其他",
                DefaultStartCommand = "python main.py",
                DefaultPort = 8080,
                DefaultTags = new List<string> { "自定义" },
                Description = "自定义项目类型",
                FileExtensions = "*.*",
                RequirementsFile = "requirements.txt",
                CommonCommands = new List<string> { "python main.py", "python app.py", "npm start" }
            }
        };

        /// <summary>
        /// 获取所有框架名称
        /// </summary>
        public static List<string> GetFrameworkNames()
        {
            return Configs.Keys.ToList();
        }

        /// <summary>
        /// 获取框架配置
        /// </summary>
        public static FrameworkConfig? GetFrameworkConfig(string frameworkName)
        {
            return Configs.TryGetValue(frameworkName, out var config) ? config : null;
        }

        /// <summary>
        /// 根据项目路径自动检测框架类型
        /// </summary>
        public static string DetectFramework(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return "其他";

            try
            {
                var files = Directory.GetFiles(projectPath, "*", SearchOption.TopDirectoryOnly);
                var fileNames = files.Select(f => Path.GetFileName(f).ToLower()).ToList();

                // 检查特定的框架标识文件
                if (fileNames.Contains("package.json"))
                {
                    var packageJsonPath = Path.Combine(projectPath, "package.json");
                    if (File.Exists(packageJsonPath))
                    {
                        var content = File.ReadAllText(packageJsonPath);
                        if (content.Contains("\"react\"")) return "React";
                        if (content.Contains("\"vue\"")) return "Vue.js";
                        return "Node.js";
                    }
                }

                if (fileNames.Contains("manage.py")) return "Django";
                if (fileNames.Contains("requirements.txt"))
                {
                    var reqPath = Path.Combine(projectPath, "requirements.txt");
                    if (File.Exists(reqPath))
                    {
                        var content = File.ReadAllText(reqPath).ToLower();
                        if (content.Contains("streamlit")) return "Streamlit";
                        if (content.Contains("gradio")) return "Gradio";
                        if (content.Contains("fastapi")) return "FastAPI";
                        if (content.Contains("flask")) return "Flask";
                        if (content.Contains("torch")) return "PyTorch";
                        if (content.Contains("tensorflow")) return "TensorFlow";
                        if (content.Contains("transformers")) return "Transformers";
                        if (content.Contains("langchain")) return "LangChain";
                        if (content.Contains("openai")) return "OpenAI";
                        if (content.Contains("diffusers")) return "Stable Diffusion";
                    }
                }

                // 检查主要的Python文件
                var mainFiles = new[] { "app.py", "main.py", "server.py", "run.py" };
                foreach (var mainFile in mainFiles)
                {
                    var mainPath = Path.Combine(projectPath, mainFile);
                    if (File.Exists(mainPath))
                    {
                        var content = File.ReadAllText(mainPath).ToLower();
                        if (content.Contains("streamlit")) return "Streamlit";
                        if (content.Contains("gradio")) return "Gradio";
                        if (content.Contains("fastapi")) return "FastAPI";
                        if (content.Contains("flask")) return "Flask";
                        if (content.Contains("from django")) return "Django";
                    }
                }

                // 检查是否有Jupyter notebook文件
                if (files.Any(f => f.EndsWith(".ipynb"))) return "Jupyter";

                // 检查是否有ComfyUI相关文件
                if (fileNames.Contains("main.py") && Directory.Exists(Path.Combine(projectPath, "custom_nodes")))
                    return "ComfyUI";

                return "其他";
            }
            catch
            {
                return "其他";
            }
        }
    }
}
