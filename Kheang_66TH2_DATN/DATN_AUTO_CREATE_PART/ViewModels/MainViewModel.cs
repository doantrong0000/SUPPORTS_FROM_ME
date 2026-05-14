using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DATN_AUTO_CREATE_PART.Models;
using DATN_AUTO_CREATE_PART.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using Tekla.Structures.Model.UI;
using TSM = Tekla.Structures.Model;

namespace DATN_AUTO_CREATE_PART.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<BeamInfoCollection> _beamCollections = new ObservableCollection<BeamInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<ColumnInfoCollection> _columnCollections = new ObservableCollection<ColumnInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<FloorInfoCollection> _floorCollections = new ObservableCollection<FloorInfoCollection>();

        [ObservableProperty]
        private GridInfo _gridSettings = new GridInfo();

        // --- Layer Filters (comma-separated keywords) ---
        [ObservableProperty]
        private string _beamLayerFilter = "beam, dam, frame";

        [ObservableProperty]
        private string _columnLayerFilter = "col, cot";

        [ObservableProperty]
        private string _floorLayerFilter = "slab, san, floor";

        [ObservableProperty]
        private string _gridLayerFilter = "grid, axis, truc";

        private TSM.Model _model;

        public MainViewModel()
        {
            _model = new TSM.Model();
            BeamCollections.Clear();
            ColumnCollections.Clear();
            FloorCollections.Clear();
        }

        // =====================================================
        // ORIGIN PICKING - Logic Separation
        // =====================================================

        private XyzData _cadOrigin;
        private Tekla.Structures.Geometry3d.Point _teklaOrigin;

        /// <summary>
        /// Retrieves the origin point from AutoCAD (once per session).
        /// </summary>
        private XyzData RequireCadOrigin()
        {
            if (_cadOrigin != null) return _cadOrigin;

            _cadOrigin = AutoCadInterop.GetCadOrigin();
            if (_cadOrigin == null)
            {
                System.Windows.MessageBox.Show(
                    "CAD origin selection cancelled.",
                    "Cancelled",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            return _cadOrigin;
        }

        /// <summary>
        /// Retrieves the origin point from Tekla (once per session).
        /// </summary>
        private Tekla.Structures.Geometry3d.Point RequireTeklaOrigin()
        {
            if (_teklaOrigin != null) return _teklaOrigin;

            try
            {
                var picker = new Picker();
                _teklaOrigin = picker.PickPoint("Pick origin point in Tekla");
                return _teklaOrigin;
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Tekla origin picking cancelled or failed: " + ex.Message,
                    "Cancelled",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return null;
            }
        }

        [RelayCommand]
        private void SetCadOrigin()
        {
            var newOrigin = AutoCadInterop.GetCadOrigin();
            if (newOrigin != null)
            {
                _cadOrigin = newOrigin;
                System.Windows.MessageBox.Show("AutoCAD Origin updated successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void SetTeklaOrigin()
        {
            if (!EnsureTeklaConnection()) return;
            try
            {
                var picker = new Picker();
                var newOrigin = picker.PickPoint("Pick NEW origin point in Tekla");
                if (newOrigin != null)
                {
                    _teklaOrigin = newOrigin;
                    System.Windows.MessageBox.Show("Tekla Origin updated successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch { }
        }

        /// <summary>
        /// Validates Tekla connection status.
        /// </summary>
        private bool EnsureTeklaConnection()
        {
            if (!_model.GetConnectionStatus())
            {
                System.Windows.MessageBox.Show(
                    "Please open Tekla Structures and a Model before running this command.",
                    "Tekla Not Connected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // =====================================================
        // SCAN CAD - Each scan operation requires CAD origin
        // =====================================================

        [RelayCommand]
        private void ScanCadBeams()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            // Step 2: Extract data from CAD
            AutoCadInterop.ExtractBeams(out var extractedBeams, cadOrigin, BeamLayerFilter);

            var grouped = extractedBeams.GroupBy(x => x.Text);
            BeamCollections.Clear();

            foreach (var group in grouped)
            {
                var col = new BeamInfoCollection
                {
                    Text = group.Key,
                    Number = group.Count()
                };
                
                foreach(var b in group)
                {
                    col.BeamInfos.Add(new BeamInfo(b.StartPoint, b.EndPoint, b.Text));
                }
                
                col.BeamInfos = col.BeamInfos.Distinct(new BeamInfo.BeamInfoComparerByPoint()).ToList();
                col.Number = col.BeamInfos.Count;

                if (col.BeamInfos.Any())
                {
                    col.Width = col.BeamInfos.First().Width;
                    col.Height = col.BeamInfos.First().Height;
                }

                BeamCollections.Add(col);
            }
        }

        [RelayCommand]
        private void ScanCadColumns()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            AutoCadInterop.ExtractColumns(out var extractedColumns, cadOrigin, ColumnLayerFilter);

            var allColumnInfos = extractedColumns.Select(c => new ColumnInfo(c.Points, c.Mask)).ToList();
            
            // Group by dimensions (Width and Height)
            var grouped = allColumnInfos.GroupBy(c => new { c.Width, c.Height });
            
            ColumnCollections.Clear();

            foreach (var group in grouped)
            {
                var col = new ColumnInfoCollection
                {
                    Width = group.Key.Width,
                    Height = group.Key.Height,
                    Text = $"{group.Key.Width}x{group.Key.Height}",
                    Number = group.Count()
                };

                foreach(var c in group)
                {
                    col.ColumnInfos.Add(c);
                }

                // Ensure unique entries if needed, though they should be distinct by Center already
                col.ColumnInfos = col.ColumnInfos.Distinct().ToList();
                col.Number = col.ColumnInfos.Count;
                
                ColumnCollections.Add(col);
            }
        }

        [RelayCommand]
        private void ScanCadFloors()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            AutoCadInterop.ExtractFloors(out var extractedFloors, cadOrigin, FloorLayerFilter);

            FloorCollections.Clear();

            foreach (var floor in extractedFloors)
            {
                var col = new FloorInfoCollection
                {
                    Area = floor.Area,
                    Number = 1
                };
                col.FloorPoints.Add(floor.Points);
                FloorCollections.Add(col);
            }
        }

        [RelayCommand]
        private void ScanCadGrids()
        {
            var cadOrigin = RequireCadOrigin();
            if (cadOrigin == null) return;

            AutoCadInterop.ExtractGrids(out string cX, out string cY, out string lX, out string lY, cadOrigin, GridLayerFilter);
            if (cX != "0" || cY != "0")
            {
                GridSettings.CoordinateX = cX;
                GridSettings.CoordinateY = cY;
                GridSettings.LabelX = lX;
                GridSettings.LabelY = lY;
            }
        }

        // =====================================================
        // GENERATE TEKLA - Requires both CAD and Tekla origins
        // =====================================================

        [RelayCommand]
        private void GenerateGridToTekla()
        {
            if (!EnsureTeklaConnection()) return;

            var teklaOrigin = RequireTeklaOrigin();
            if (teklaOrigin == null) return;

            // Step 2: Generate grid
            TeklaInterop.GenerateStandardGrid(teklaOrigin, GridSettings);
            System.Windows.MessageBox.Show(
                "Grid generated successfully!",
                "Success",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void GenerateComponentsToTekla()
        {
            if (!EnsureTeklaConnection()) return;

            if (_cadOrigin == null)
            {
                System.Windows.MessageBox.Show(
                    "Please scan at least one CAD part first to define the CAD origin.",
                    "CAD Origin Missing",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var teklaOrigin = RequireTeklaOrigin();
            if (teklaOrigin == null) return;

            // Generate components using the separated origins
            TeklaInterop.GenerateBeams(BeamCollections, _cadOrigin, teklaOrigin);
            TeklaInterop.GenerateColumns(ColumnCollections, _cadOrigin, teklaOrigin);
            TeklaInterop.GenerateFloors(FloorCollections, _cadOrigin, teklaOrigin);

            System.Windows.MessageBox.Show(
                "Components generated successfully in Tekla!",
                "Success",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
}
