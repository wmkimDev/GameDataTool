using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ExcelDataTool.ViewModels;
using ReactiveUI;

namespace ExcelDataTool.Views
{
    public partial class InteractionView : ReactiveUserControl<FilePathViewModel>
    {
        public InteractionView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(ViewModel!.SelectFilesInteraction.RegisterHandler(InteractionHandler));
            });
        }

        private async Task InteractionHandler(IInteractionContext<string?, string?> context)
        {
            // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
            var topLevel = TopLevel.GetTopLevel(this);

            var storageFiles = await topLevel!.StorageProvider
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        AllowMultiple = true,
                        Title         = context.Input
                    });
               
            context.SetOutput(storageFiles.Select(x => x.Path.LocalPath).FirstOrDefault());
        }
    }
}