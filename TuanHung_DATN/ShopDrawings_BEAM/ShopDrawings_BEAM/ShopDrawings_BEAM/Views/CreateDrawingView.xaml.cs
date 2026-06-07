using System;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ShopDrawings_BEAM.Model;
using ShopDrawings_BEAM.ViewModels;

namespace ShopDrawings_BEAM.Views
{
    public partial class CreateDrawingView : Window
    {
        public CreateDrawingView()
        {
            InitializeComponent();
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null) return;

            // Lưu cài đặt của người dùng vào AppData
            viewModel.SaveSettings();

            using (Transaction trans = new Transaction(viewModel.Doc, "Triển khai Shop Drawings Dầm"))
            {
                trans.Start();

                // 1. Thực thi tạo ViewSheet, Mặt cắt dọc và Mặt cắt ngang
                DrawingCreator.CreateBeamDrawings(viewModel);

                // Cập nhật Revit Database để có các tham chiếu hình học mới nhất
                viewModel.Doc.Regenerate();

                // 2. Tự động chèn Tag thép & Dimension dầm
                DrawingCreator.AnnotateBeamDrawings(viewModel);

                // Hoàn tất
                viewModel.ProgressValue = 100;
                viewModel.CurrentStep = 2;

                trans.Commit();
                
                TaskDialog.Show("Shop Drawing", "Đã triển khai bản vẽ dầm và chèn toàn bộ Tag & Dimension thành công!");

                this.DialogResult = true;
                this.Close();
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
