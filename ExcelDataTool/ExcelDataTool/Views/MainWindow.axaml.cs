using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ExcelDataTool.Core;
using ExcelDataTool.ViewModels;

namespace ExcelDataTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = new MainWindowViewModel();
        
        var consoleScrollViewer = this.FindControl<ScrollViewer>("ConsoleScrollViewer");
        Logger.OnMessageLogged += () =>
        {
            if (consoleScrollViewer != null) 
                Dispatcher.UIThread.InvokeAsync(consoleScrollViewer.ScrollToEnd, DispatcherPriority.Background);
        };
    }
}