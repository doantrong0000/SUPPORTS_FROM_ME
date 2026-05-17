using Beam_Rebar_Design.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Beam_Rebar_Design
{
    public partial class MainWindow : Window
    {
        private RebarCalculatorViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new RebarCalculatorViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;

            bool success = _viewModel.ConnectToEtabs();
            if (success)
            {
                btnLoadBeams.IsEnabled = true;
                btnLoadForces.IsEnabled = true;
                LoadBeamsAndCases();
            }
            else
            {
                MessageBox.Show("Không kết nối được ETABS.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            btnConnect.IsEnabled = true;
        }

        private void BtnLoadBeams_Click(object sender, RoutedEventArgs e)
        {
            LoadBeamsAndCases();
        }

        private void LoadBeamsAndCases()
        {
            bool beamSuccess = _viewModel.LoadBeamsFromEtabs();
            bool caseSuccess = _viewModel.LoadOutputCases();

            if (beamSuccess)
            {
                dgBeamData.Items.Refresh();
                if (_viewModel.OutputCases.Count > 0)
                {
                    _viewModel.SelectedOutputCase = _viewModel.OutputCases[0];
                }
            }
        }

        private void BtnLoadForces_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.SelectedOutputCase))
            {
                MessageBox.Show("Vui lòng chọn Output Case", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnLoadForces.IsEnabled = false;

            bool success = _viewModel.LoadForcesFromEtabs();
            if (success)
            {
                dgBeamData.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Không lấy được nội lực.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            btnLoadForces.IsEnabled = true;
        }

        private void BtnCalculateRebar_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedConcrete == null || _viewModel.SelectedRebarLongitudinal == null)
            {
                MessageBox.Show("Vui lòng chọn vật liệu", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnCalculate.IsEnabled = false;
            _viewModel.CalculateRebarForAllBeams();
            dgBeamData.Items.Refresh();
            btnCalculate.IsEnabled = true;
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.BeamList.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu dầm", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Excel Files (*.csv)|*.csv";
            saveDialog.FileName = "KetQuaTinhThep.csv";

            if (saveDialog.ShowDialog() == true)
            {
                StreamWriter writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8);
                writer.WriteLine("Tên Dầm,Tầng,B,H,L,M1,M2,M3,V1,V2,V3,As Trai,As Duoi,As Phai,Sv1,Sv2,Sv3");
                
                foreach (var b in _viewModel.BeamList)
                {
                    string line = b.Name + "," + b.Story + "," + b.B + "," + b.H + "," + b.Ltt + "," + 
                                  b.Momen1 + "," + b.Momen2 + "," + b.Momen3 + "," + 
                                  b.Shear1 + "," + b.Shear2 + "," + b.Shear3 + "," + 
                                  b.AsRequiredTrai + "," + b.AsRequiredDuoi + "," + b.AsRequiredPhai + "," + 
                                  b.StirrupSpacingV1 + "," + b.StirrupSpacingV2 + "," + b.StirrupSpacingV3;
                    writer.WriteLine(line);
                }
                
                writer.Close();
                MessageBox.Show("Xuất file thành công!");
            }
        }
    }
}
