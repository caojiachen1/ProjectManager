using ProjectManager.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace ProjectManager.Views.Pages
{
    /// <summary>
    /// TerminalPage.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalPage : INavigableView<TerminalViewModel>
    {
        public TerminalViewModel ViewModel { get; }

        public TerminalPage(TerminalViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
