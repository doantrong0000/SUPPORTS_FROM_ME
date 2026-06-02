using System.Windows;

namespace Calculator_Foundation_Etabs_API
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnViewReport_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var foundation = button?.DataContext as Models.FoundationModel;
            if (foundation != null)
            {
                var reportWindow = new ReportWindow(foundation);
                reportWindow.Owner = this;
                reportWindow.ShowDialog();
            }
        }
        
        private void BtnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var foundation = button?.DataContext as Models.FoundationModel;
            if (foundation != null)
            {
                var reportWindow = new ReportWindow(foundation);
                reportWindow.PrintDirectly();
            }
        }

        private void DataGrid_SelectionChanged()
        {

        }
    }
}
