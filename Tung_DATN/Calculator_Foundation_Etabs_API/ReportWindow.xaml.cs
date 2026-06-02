using System.Windows;
using System.Windows.Controls;

namespace Calculator_Foundation_Etabs_API
{
    public partial class ReportWindow : Window
    {
        public ReportWindow(Models.FoundationModel foundation)
        {
            InitializeComponent();
            LoadData(foundation);
        }

        private void LoadData(Models.FoundationModel foundation)
        {
            RunFoundationName.Text = foundation.Name;
            RunDimensions.Text = $"B={foundation.B:F2}m, Bd={foundation.Bd:F2}m, H={foundation.H:F2}m, Hd={foundation.Hd:F2}m";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDirectly();
        }

        public void PrintDirectly()
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)ReportViewer.Document).DocumentPaginator, "In Thuyết Minh Tính Toán");
            }
        }
    }
}
