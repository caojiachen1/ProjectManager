using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.Views.Pages
{
    public partial class AddProjectPage : INavigableView<AddProjectViewModel>
    {
        public AddProjectViewModel ViewModel { get; }

        public AddProjectPage(AddProjectViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
