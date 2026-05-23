using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using ShopDrawings_BEAM.Views;
using ShopDrawings_BEAM.ViewModels;

namespace ShopDrawings_BEAM.Commands
{
    /// <summary>
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class DrawingCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new MainViewModel();
            var view = new MainView
            {
                DataContext = viewModel
            };
            
            view.ShowDialog();
        }
    }
}