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
using System.Windows.Threading;

namespace ProjectApp.ModelFromCad
{
    /// <summary>
    /// Interaction logic for progressbar.xaml
    /// </summary>
    public partial class progressbar : Window
    {
        private delegate void ProgressBarDelegate();

        public bool Flag = true;
        public progressbar()
        {
            InitializeComponent();
        }

        public bool Create(int max, string title, bool isNewProcess = false)
        {
            if (isNewProcess)
            {
                pb.Minimum = 0;
            }

            pb.Maximum = max;
            Title = title + " " + Convert.ToInt32(pb.Value) + "/" + pb.Maximum; ;
            TbPercent.Text = Math.Round(pb.Value * 100 / pb.Maximum, 1) + "%";
            pb.Dispatcher?.Invoke(new ProgressBarDelegate(UpdateProgress), DispatcherPriority.Background);
            return Flag;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Flag = false;
        }

        private void UpdateProgress()
        {
            pb.Value++;
        }

        private void ProgressBarView_OnClosed(object sender, EventArgs e)
        {
            Flag = false;
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            Flag = false;
        }
    }
}
