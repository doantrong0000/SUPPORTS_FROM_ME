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

        private void BtnCreateViews_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null) return;

            try
            {
                using (Transaction trans = new Transaction(viewModel.Doc, "Tạo Khung Nhìn Dầm Shop Drawings"))
                {
                    trans.Start();

                    // Thực thi tạo ViewSheet, Mặt cắt dọc và Mặt cắt ngang
                    DrawingCreator.CreateBeamDrawings(viewModel);

                    // Cập nhật tiến trình từng bước thủ công để dễ thuyết trình
                    viewModel.ProgressValue = 60;

                    trans.Commit();
                    
                    TaskDialog.Show("Shop Drawing", "Đã khởi tạo xong Sheet và Khung nhìn (Mặt cắt dọc & Mặt cắt ngang)!");
                }

                // Chuyển qua bước cấu hình Ký hiệu & Kích thước trực tiếp trên cùng giao diện!
                viewModel.CurrentStep = 2;
                
                var viewDimTag = new DimTagView();
                viewDimTag.DataContext = viewModel;
                this.Hide();
                viewDimTag.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", $"Lỗi khởi tạo view: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
