using System.ComponentModel;

namespace Beam_Rebar_Design.Models
{
    /// <summary>
    /// Model dầm cho tính toán thép - lấy dữ liệu từ ETABS
    /// </summary>
    public class BeamModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Story { get; set; }
        public string SectionName { get; set; }

        #region Etabs Properties

        private double _station1;
        public double Station1
        {
            get => _station1;
            set
            {
                _station1 = value;
                OnPropertyChanged(nameof(Station1));
            }
        }

        private double _momen1;
        public double Momen1
        {
            get => _momen1;
            set
            {
                _momen1 = value;
                OnPropertyChanged(nameof(Momen1));
            }
        }

        private double _station2;
        public double Station2
        {
            get => _station2;
            set
            {
                _station2 = value;
                OnPropertyChanged(nameof(Station2));
            }
        }

        private double _momen2;
        public double Momen2
        {
            get => _momen2;
            set
            {
                _momen2 = value;
                OnPropertyChanged(nameof(Momen2));
            }
        }

        private double _station3;
        public double Station3
        {
            get => _station3;
            set
            {
                _station3 = value;
                OnPropertyChanged(nameof(Station3));
            }
        }

        private double _momen3;
        public double Momen3
        {
            get => _momen3;
            set
            {
                _momen3 = value;
                OnPropertyChanged(nameof(Momen3));
            }
        }

        private double _shear1;
        public double Shear1
        {
            get => _shear1;
            set
            {
                _shear1 = value;
                OnPropertyChanged(nameof(Shear1));
            }
        }

        private double _shear2;
        public double Shear2
        {
            get => _shear2;
            set
            {
                _shear2 = value;
                OnPropertyChanged(nameof(Shear2));
            }
        }

        private double _shear3;
        public double Shear3
        {
            get => _shear3;
            set
            {
                _shear3 = value;
                OnPropertyChanged(nameof(Shear3));
            }
        }

        #endregion

        #region Section Properties (từ ETABS)
        /// <summary>
        /// Bề rộng dầm (mm)
        /// </summary>
        public double B { get; set; }

        /// <summary>
        /// Chiều cao dầm (mm)
        /// </summary>
        public double H { get; set; }

        /// <summary>
        /// Chiều dài dầm (mm)
        /// </summary>
        public double Ltt { get; set; }

        /// <summary>
        /// Nhịp trái (mm) - nhập tay hoặc tính từ model
        /// </summary>
        private double _spanLeft;
        public double SpanLeft
        {
            get => _spanLeft;
            set
            {
                _spanLeft = value;
                OnPropertyChanged(nameof(SpanLeft));
            }
        }

        /// <summary>
        /// Nhịp phải (mm) - nhập tay hoặc tính từ model
        /// </summary>
        private double _spanRight;
        public double SpanRight
        {
            get => _spanRight;
            set
            {
                _spanRight = value;
                OnPropertyChanged(nameof(SpanRight));
            }
        }

        /// <summary>
        /// Chiều dày sàn trái (mm) - nhập tay
        /// </summary>
        private double _floorThicknessLeft;
        public double FloorThicknessLeft
        {
            get => _floorThicknessLeft;
            set
            {
                _floorThicknessLeft = value;
                OnPropertyChanged(nameof(FloorThicknessLeft));
            }
        }

        /// <summary>
        /// Chiều dày sàn phải (mm) - nhập tay
        /// </summary>
        private double _floorThicknessRight;
        public double FloorThicknessRight
        {
            get => _floorThicknessRight;
            set
            {
                _floorThicknessRight = value;
                OnPropertyChanged(nameof(FloorThicknessRight));
            }
        }

        /// <summary>
        /// Chiều cao tường/cột (mm) - dùng cho tính thép đai
        /// </summary>
        private double _wallHeight = 3000;
        public double WallHeight
        {
            get => _wallHeight;
            set
            {
                _wallHeight = value;
                OnPropertyChanged(nameof(WallHeight));
            }
        }
        #endregion

        #region Steel Area Required Properties
        private double _asRequiredTrai;
        public double AsRequiredTrai
        {
            get => _asRequiredTrai;
            set
            {
                _asRequiredTrai = value;
                OnPropertyChanged(nameof(AsRequiredTrai));
            }
        }

        private double _asRequiredDuoi;
        public double AsRequiredDuoi
        {
            get => _asRequiredDuoi;
            set
            {
                _asRequiredDuoi = value;
                OnPropertyChanged(nameof(AsRequiredDuoi));
            }
        }

        private double _asRequiredPhai;
        public double AsRequiredPhai
        {
            get => _asRequiredPhai;
            set
            {
                _asRequiredPhai = value;
                OnPropertyChanged(nameof(AsRequiredPhai));
            }
        }
        #endregion

        #region Stirrup Spacing Required Properties
        private double _stirrupSpacingV1;
        public double StirrupSpacingV1
        {
            get => _stirrupSpacingV1;
            set
            {
                _stirrupSpacingV1 = value;
                OnPropertyChanged(nameof(StirrupSpacingV1));
            }
        }

        private double _stirrupSpacingV2;
        public double StirrupSpacingV2
        {
            get => _stirrupSpacingV2;
            set
            {
                _stirrupSpacingV2 = value;
                OnPropertyChanged(nameof(StirrupSpacingV2));
            }
        }

        private double _stirrupSpacingV3;
        public double StirrupSpacingV3
        {
            get => _stirrupSpacingV3;
            set
            {
                _stirrupSpacingV3 = value;
                OnPropertyChanged(nameof(StirrupSpacingV3));
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
