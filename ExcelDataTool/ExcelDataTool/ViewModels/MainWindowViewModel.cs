using System.Collections.ObjectModel;
using System.Windows.Input;
using ExcelDataTool.Core;
using ReactiveUI;

namespace ExcelDataTool.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            ClearConsoleCommand = ReactiveCommand.Create(Logger.Clear);
        }
        
        public FilePathViewModel FilePathModel { get; } = new FilePathViewModel();
        public TypeSettingViewModel TypeSettingModel { get; } = new TypeSettingViewModel();
        
        public ObservableCollection<LogMessage> ConsoleOutput => Logger.Messages;
        
        public ICommand ClearConsoleCommand { get; }
    }
}