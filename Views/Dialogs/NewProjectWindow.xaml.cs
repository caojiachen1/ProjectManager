using System.Windows;
using ProjectManager.ViewModels.Dialogs;
using ProjectManager.Services;
using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectManager.Views.Dialogs
{
    public partial class NewProjectWindow : FluentWindow
    {
        public NewProjectDialogViewModel ViewModel { get; }
        private readonly IServiceProvider _serviceProvider;

        public NewProjectWindow(NewProjectDialogViewModel viewModel, IServiceProvider serviceProvider)
        {
            ViewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = viewModel;

            InitializeComponent();
            
            // 设置事件处理
            ViewModel.ProjectCreated += OnProjectCreated;
            ViewModel.DialogCancelled += (s, e) => { DialogResult = false; Close(); };
        }

        private void OnProjectCreated(object? sender, EventArgs e)
        {
            // 不设置DialogResult = true，避免调用者认为项目创建成功
            // 只有在ProjectEditWindow中确认保存后才算真正创建成功
            
            // 打开ProjectEditWindow进行详细设置
            if (ViewModel.CreatedProject != null)
            {
                var editWindow = _serviceProvider.GetRequiredService<Views.Dialogs.ProjectEditWindow>();
                editWindow.ViewModel.LoadProject(ViewModel.CreatedProject);
                editWindow.Owner = Owner ?? this;
                
                var editResult = editWindow.ShowDialog();
                
                // 如果在ProjectEditWindow中保存成功，则设置DialogResult = true
                if (editResult == true)
                {
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
            }
            else
            {
                DialogResult = false;
            }
            
            // 关闭新建项目窗口
            Close();
        }
        
        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }
    }
}
