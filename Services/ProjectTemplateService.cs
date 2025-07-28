using ProjectManager.Models;
using System.IO;

namespace ProjectManager.Services
{
    /// <summary>
    /// 项目模板服务，用于创建不同类型的项目模板
    /// </summary>
    public interface IProjectTemplateService
    {
        Task CreateProjectTemplateAsync(string projectPath, string framework, string projectName);
        Task<bool> IsValidProjectDirectoryAsync(string projectPath);
    }

    public class ProjectTemplateService : IProjectTemplateService
    {
        public async Task CreateProjectTemplateAsync(string projectPath, string framework, string projectName)
        {
            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(framework))
                return;

            try
            {
                // 确保目录存在
                Directory.CreateDirectory(projectPath);

                var config = FrameworkConfigService.GetFrameworkConfig(framework);
                if (config == null) return;

                // 根据框架类型创建不同的模板文件
                switch (framework)
                {
                    case "Streamlit":
                        await CreateStreamlitTemplateAsync(projectPath, projectName);
                        break;
                    case "Gradio":
                        await CreateGradioTemplateAsync(projectPath, projectName);
                        break;
                    case "FastAPI":
                        await CreateFastAPITemplateAsync(projectPath, projectName);
                        break;
                    case "Flask":
                        await CreateFlaskTemplateAsync(projectPath, projectName);
                        break;
                    default:
                        await CreateBasicPythonTemplateAsync(projectPath, projectName);
                        break;
                }

                // 创建通用文件
                await CreateCommonFilesAsync(projectPath, projectName, config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"创建项目模板失败: {ex.Message}", ex);
            }
        }

        public Task<bool> IsValidProjectDirectoryAsync(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                    return Task.FromResult(false);

                // 检查目录是否存在且为空或只包含基本文件
                if (!Directory.Exists(projectPath))
                    return Task.FromResult(true); // 不存在的目录可以创建

                var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories);
                return Task.FromResult(files.Length <= 3); // 允许少量文件存在（如.gitkeep等）
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private async Task CreateStreamlitTemplateAsync(string projectPath, string projectName)
        {
            var appContent = "import streamlit as st\nimport pandas as pd\nimport numpy as np\n\n" +
                "st.set_page_config(\n" +
                $"    page_title=\"{projectName}\",\n" +
                "    page_icon=\"🚀\",\n" +
                "    layout=\"wide\"\n" +
                ")\n\n" +
                "def main():\n" +
                $"    st.title(\"{projectName}\")\n" +
                $"    st.write(\"欢迎使用 {projectName}！\")\n\n" +
                "if __name__ == \"__main__\":\n" +
                "    main()";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "app.py"), appContent);
        }

        private async Task CreateGradioTemplateAsync(string projectPath, string projectName)
        {
            var appContent = "import gradio as gr\n\n" +
                "def predict(text_input):\n" +
                "    return f\"处理结果: {text_input}\"\n\n" +
                "demo = gr.Interface(\n" +
                "    fn=predict,\n" +
                "    inputs=gr.Textbox(label=\"输入文本\"),\n" +
                "    outputs=gr.Textbox(label=\"输出结果\"),\n" +
                $"    title=\"{projectName}\"\n" +
                ")\n\n" +
                "if __name__ == \"__main__\":\n" +
                "    demo.launch()";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "app.py"), appContent);
        }

        private async Task CreateFastAPITemplateAsync(string projectPath, string projectName)
        {
            var mainContent = "from fastapi import FastAPI\nfrom pydantic import BaseModel\n\n" +
                $"app = FastAPI(title=\"{projectName}\")\n\n" +
                "class Item(BaseModel):\n" +
                "    name: str\n" +
                "    value: int\n\n" +
                "@app.get(\"/\")\n" +
                "async def root():\n" +
                $"    return {{\"message\": \"欢迎使用 {projectName} API\"}}\n\n" +
                "@app.post(\"/items\")\n" +
                "async def create_item(item: Item):\n" +
                "    return item\n\n" +
                "if __name__ == \"__main__\":\n" +
                "    import uvicorn\n" +
                "    uvicorn.run(app, host=\"0.0.0.0\", port=8000)";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "main.py"), mainContent);
        }

        private async Task CreateFlaskTemplateAsync(string projectPath, string projectName)
        {
            var appContent = "from flask import Flask, jsonify, request\n\n" +
                "app = Flask(__name__)\n\n" +
                "@app.route('/')\n" +
                "def index():\n" +
                $"    return jsonify({{\"message\": \"欢迎使用 {projectName}\"}})\n\n" +
                "@app.route('/api/process', methods=['POST'])\n" +
                "def process():\n" +
                "    data = request.get_json()\n" +
                "    return jsonify({\"result\": f\"处理结果: {data}\"})\n\n" +
                "if __name__ == '__main__':\n" +
                "    app.run(host='0.0.0.0', port=5000, debug=True)";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "app.py"), appContent);
        }

        private async Task CreateBasicPythonTemplateAsync(string projectPath, string projectName)
        {
            var mainContent = $"#!/usr/bin/env python3\n" +
                $"# {projectName}\n\n" +
                "def main():\n" +
                $"    print(\"欢迎使用 {projectName}！\")\n" +
                "    # 在这里添加你的主要逻辑\n" +
                "    pass\n\n" +
                "if __name__ == \"__main__\":\n" +
                "    main()";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "main.py"), mainContent);
        }

        private async Task CreateCommonFilesAsync(string projectPath, string projectName, FrameworkConfig config)
        {
            // 创建 requirements.txt
            var requirements = GetRequirementsForFramework(config.Name);
            await File.WriteAllTextAsync(Path.Combine(projectPath, "requirements.txt"), requirements);

            // 创建 README.md
            var readmeContent = $"# {projectName}\n\n" +
                $"基于 {config.Name} 的AI项目\n\n" +
                $"## 描述\n\n{config.Description}\n\n" +
                "## 安装\n\n```bash\npip install -r requirements.txt\n```\n\n" +
                $"## 运行\n\n```bash\n{config.DefaultStartCommand}\n```";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readmeContent);

            // 创建 .gitignore
            var gitignoreContent = "# Python\n__pycache__/\n*.py[cod]\n.Python\nbuild/\n" +
                "dist/\n*.egg-info/\n.installed.cfg\n*.egg\n\n" +
                "# Virtual Environment\nvenv/\nENV/\nenv/\n.venv/\n\n" +
                "# IDE\n.vscode/\n.idea/\n*.swp\n*.swo\n\n" +
                "# OS\n.DS_Store\nThumbs.db\n\n" +
                "# Environment variables\n.env\n\n" +
                "# Logs\n*.log\n\n" +
                "# Model files\n*.pth\n*.pkl\n*.h5\n*.model";
            
            await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), gitignoreContent);
        }

        private string GetRequirementsForFramework(string framework)
        {
            return framework switch
            {
                "Streamlit" => "streamlit>=1.28.0\npandas>=2.0.0\nnumpy>=1.24.0",
                "Gradio" => "gradio>=4.0.0\nnumpy>=1.24.0\npandas>=2.0.0",
                "FastAPI" => "fastapi>=0.104.0\nuvicorn[standard]>=0.23.0\npydantic>=2.4.0",
                "Flask" => "Flask>=3.0.0\nFlask-CORS>=4.0.0\nrequests>=2.31.0",
                "PyTorch" => "torch>=2.1.0\ntorchvision>=0.16.0\nnumpy>=1.24.0",
                "TensorFlow" => "tensorflow>=2.14.0\nnumpy>=1.24.0\nmatplotlib>=3.7.0",
                "Transformers" => "transformers>=4.35.0\ntorch>=2.1.0\ntokenizers>=0.14.0",
                "LangChain" => "langchain>=0.0.340\npython-dotenv>=1.0.0\nstreamlit>=1.28.0",
                "OpenAI" => "openai>=1.3.0\npython-dotenv>=1.0.0\nrequests>=2.31.0",
                "Stable Diffusion" => "diffusers>=0.24.0\ntransformers>=4.35.0\ntorch>=2.1.0",
                _ => "numpy>=1.24.0\npandas>=2.0.0\nrequests>=2.31.0"
            };
        }
    }
}
