using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDrawings_BEAM.Model;
using ShopDrawings_BEAM.ViewModels;

namespace ShopDrawings_BEAM.Views
{
    /// <summary>
    /// Interaction logic for ViewDimTag.xaml
    /// </summary>
    public partial class DimTagView : Window
    {
        public DimTagView()
        {
            InitializeComponent();
        }

        private void BtnCreateTagsAndDims_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null) return;

            try
            {
                using (Transaction trans = new Transaction(viewModel.Doc, "Chèn Tag & Dimension Shop Drawings"))
                {
                    trans.Start();

                    // Tự động chèn Tag thép & Dimension dầm
                    DrawingCreator.AnnotateBeamDrawings(viewModel);

                    // Hoàn tất
                    viewModel.ProgressValue = 100;

                    trans.Commit();

                    TaskDialog.Show("Shop Drawing", "Đã chèn toàn bộ Tag & Dimension vào bản vẽ dầm thành công!");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", $"Lỗi chèn ký hiệu: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
