using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShopDrawings_BEAM.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            // TODO: Khởi tạo dữ liệu và Load Family từ Revit vào đây
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
