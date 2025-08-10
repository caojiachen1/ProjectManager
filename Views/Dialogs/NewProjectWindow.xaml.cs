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
            // 关闭新建项目窗口
            DialogResult = true;
            
            // 打开ProjectEditWindow进行详细设置
            if (ViewModel.CreatedProject != null)
            {
                var editWindow = _serviceProvider.GetRequiredService<Views.Dialogs.ProjectEditWindow>();
                editWindow.ViewModel.LoadProject(ViewModel.CreatedProject);
                editWindow.Owner = Owner ?? this;
                editWindow.ShowDialog();
            }
            
            Close();
        }
        
        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }
    }
}
