using System.Threading;
using System.Windows;
using Uninstaller.ViewModel;

namespace Uninstaller.View
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(SynchronizationContext.Current);
        }
    }
}
