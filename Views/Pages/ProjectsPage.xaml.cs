using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using System.Windows.Controls;

namespace ProjectManager.Views.Pages
{
    public partial class ProjectsPage : Page, INavigableView<ProjectsViewModel>
    {
        public ProjectsViewModel ViewModel { get; }

        public ProjectsPage(ProjectsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
