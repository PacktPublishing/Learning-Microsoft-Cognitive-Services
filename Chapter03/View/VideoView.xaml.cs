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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chapter3.View
{
    /// <summary>
    /// Interaction logic for VideoView.xaml
    /// </summary>
    public partial class VideoView : UserControl
    {
        public VideoView()
        {
            InitializeComponent();
        }

        private void MediaElement_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            ResultVideo.Play();
        }

        private void SourceVideo_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            SourceVideo.Play();
        }

        private void SourceVideo_Loaded(object sender, RoutedEventArgs e)
        {
            SourceVideo.Play();
        }

        private void ResultVideo_Loaded(object sender, RoutedEventArgs e)
        {
            ResultVideo.Play();
        }
    }
}
