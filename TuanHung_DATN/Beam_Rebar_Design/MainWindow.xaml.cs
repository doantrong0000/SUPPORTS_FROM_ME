using Beam_Rebar_Design.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Beam_Rebar_Design
{
    public partial class MainWindow : Window
    {
        private RebarCalculatorViewModel _viewModel;
        private DispatcherTimer _notificationTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotificationTimer();
            _viewModel = new RebarCalculatorViewModel();
            this.DataContext = _viewModel;
        }

        private void InitializeNotificationTimer()
        {
            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(5);
            _notificationTimer.Tick += (s, e) => HideNotification();
        }

        private void ShowNotification(string message, bool isError = false)
        {
            TxtNotification.Text = message;
            NotificationPanel.Background = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45475A"));
            NotificationPanel.BorderBrush = isError
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89B4FA"));
            TxtNotification.Foreground = isError
                ? Brushes.White
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDD6F4"));
            NotificationPanel.Visibility = Visibility.Visible;
            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        private void HideNotification()
        {
            _notificationTimer.Stop();
            NotificationPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateStatus(string message)
        {
            if (txtStatus != null)
                txtStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Đang kết nối ETABS...");
                ShowNotification("Đang kết nối đến ETABS...");
                btnConnect.IsEnabled = false;

                bool success = _viewModel.ConnectToEtabs();
                if (success)
                {
                    StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1"));
                    btnLoadBeams.IsEnabled = true;
                    btnLoadForces.IsEnabled = true;
                    UpdateStatus("Đã kết nối ETABS thành công");
                    ShowNotification("✓ Đã kết nối ETABS thành công!");

                    // Tự động load dầm và output cases
                    LoadBeamsAndCases();
                }
                else
                {
                    StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"));
                    UpdateStatus("Không kết nối được ETABS - Hãy đảm bảo ETABS đang mở");
                    ShowNotification("✗ Không kết nối được ETABS. Hãy đảm bảo ETABS đang mở và có model.", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}");
                ShowNotification($"✗ Lỗi: {ex.Message}", true);
            }
            finally
            {
                btnConnect.IsEnabled = true;
            }
        }

        private void BtnLoadBeams_Click(object sender, RoutedEventArgs e)
        {
            LoadBeamsAndCases();
        }

        private void LoadBeamsAndCases()
        {
            try
            {
                UpdateStatus("Đang lấy danh sách dầm từ ETABS...");

                bool beamSuccess = _viewModel.LoadBeamsFromEtabs();
                bool caseSuccess = _viewModel.LoadOutputCases();

                if (beamSuccess)
                {
                    dgBeamData.Items.Refresh();
                    ShowNotification($"✓ Đã tải {_viewModel.BeamList.Count} dầm, {_viewModel.OutputCases.Count} load cases");
                    UpdateStatus($"Đã tải {_viewModel.BeamList.Count} dầm từ ETABS");

                    if (_viewModel.OutputCases.Count > 0)
                        _viewModel.SelectedOutputCase = _viewModel.OutputCases.First();
                }
                else
                {
                    ShowNotification("⚠ Không tìm thấy dầm trong model ETABS", true);
                    UpdateStatus("Không tìm thấy dầm");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}");
                ShowNotification($"✗ Lỗi: {ex.Message}", true);
            }
        }

        private void BtnLoadForces_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_viewModel?.SelectedOutputCase))
                {
                    ShowNotification("✗ Vui lòng chọn Output Case", true);
                    return;
                }

                UpdateStatus($"Đang lấy nội lực cho: {_viewModel.SelectedOutputCase}");
                ShowNotification($"Đang lấy nội lực cho: {_viewModel.SelectedOutputCase}...");
                btnLoadForces.IsEnabled = false;

                bool success = _viewModel.LoadForcesFromEtabs();
                if (success)
                {
                    dgBeamData.Items.Refresh();
                    int count = _viewModel.BeamList.Count(b => b.Momen1 != 0 || b.Momen2 != 0 || b.Momen3 != 0);
                    UpdateStatus($"Đã lấy nội lực cho {count}/{_viewModel.BeamList.Count} dầm");
                    ShowNotification($"✓ Lấy nội lực thành công: {count} dầm có nội lực");
                }
                else
                {
                    UpdateStatus("Không lấy được nội lực - Kiểm tra đã chạy phân tích chưa");
                    ShowNotification("✗ Không lấy được nội lực. Kiểm tra đã Run Analysis chưa.", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}");
                ShowNotification($"✗ Lỗi: {ex.Message}", true);
            }
            finally
            {
                btnLoadForces.IsEnabled = true;
            }
        }

        private void BtnCalculateRebar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedConcrete == null)
                {
                    ShowNotification("✗ Vui lòng chọn mác bê tông", true);
                    return;
                }
                if (_viewModel?.SelectedRebarLongitudinal == null)
                {
                    ShowNotification("✗ Vui lòng chọn loại thép dọc", true);
                    return;
                }

                UpdateStatus("Đang tính toán thép...");
                btnCalculate.IsEnabled = false;

                int count = _viewModel.CalculateRebarForAllBeams();
                dgBeamData.Items.Refresh();

                if (count > 0)
                {
                    UpdateStatus($"Đã tính thép cho {count} dầm");
                    ShowNotification($"✓ Đã tính thép thành công cho {count}/{_viewModel.BeamList.Count} dầm");
                }
                else
                {
                    ShowNotification("⚠ Không có dầm nào có nội lực để tính", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}");
                ShowNotification($"✗ Lỗi: {ex.Message}", true);
            }
            finally
            {
                btnCalculate.IsEnabled = true;
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.BeamList == null || _viewModel.BeamList.Count == 0)
                {
                    ShowNotification("✗ Chưa có dữ liệu dầm", true);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.csv)|*.csv",
                    FileName = "BeamRebarResults.csv",
                    Title = "Xuất kết quả tính thép"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Tên Dầm,Tầng,B (mm),H (mm),L (mm),M1 (kNm),M2 (kNm),M3 (kNm),V1 (kN),V2 (kN),V3 (kN),As Trái (mm²),As Dưới (mm²),As Phải (mm²),Sv1 (mm),Sv2 (mm),Sv3 (mm)");
                        foreach (var b in _viewModel.BeamList)
                        {
                            writer.WriteLine($"{b.Name},{b.Story},{b.B:F0},{b.H:F0},{b.Ltt:F0},{b.Momen1:F2},{b.Momen2:F2},{b.Momen3:F2},{b.Shear1:F2},{b.Shear2:F2},{b.Shear3:F2},{b.AsRequiredTrai:F0},{b.AsRequiredDuoi:F0},{b.AsRequiredPhai:F0},{b.StirrupSpacingV1:F0},{b.StirrupSpacingV2:F0},{b.StirrupSpacingV3:F0}");
                        }
                    }
                    ShowNotification($"✓ Đã xuất {_viewModel.BeamList.Count} dầm ra: {Path.GetFileName(saveDialog.FileName)}");
                    UpdateStatus($"Đã xuất Excel: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"✗ Lỗi xuất Excel: {ex.Message}", true);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _notificationTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
