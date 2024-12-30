using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace ExcelDataTool.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            ConsoleOutput = new ObservableCollection<string>();
            ClearConsoleCommand = ReactiveCommand.Create(ClearConsole);
            FilePathModel = new FilePathViewModel(message => ConsoleOutput.Add(message));
        }
        
        public FilePathViewModel FilePathModel { get; }
        public TypeSettingViewModel TypeSettingModel { get; } = new TypeSettingViewModel();
        
        private ObservableCollection<string> _consoleOutput;
        public ObservableCollection<string> ConsoleOutput
        {
            get => _consoleOutput;
            set => this.RaiseAndSetIfChanged(ref _consoleOutput, value);
        }
        
        public ICommand ClearConsoleCommand { get; }
        private void ClearConsole()
        {
            ConsoleOutput.Clear();
        }
    }
}