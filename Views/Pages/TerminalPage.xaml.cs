using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private void OnScrollToTopClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && 
                element.Parent is Panel stackPanel && 
                stackPanel.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is RichTextBox rtb)
                    {
                        rtb.ScrollToHome();
                        break;
                    }
                }
            }
        }

        private void OnScrollToBottomClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && 
                element.Parent is Panel stackPanel && 
                stackPanel.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is RichTextBox rtb)
                    {
                        rtb.ScrollToEnd();
                        break;
                    }
                }
            }
        }
    }
}
