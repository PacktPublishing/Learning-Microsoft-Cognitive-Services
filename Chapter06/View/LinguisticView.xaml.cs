using Chapter6.Contracts;
using Chapter6.ViewModel;
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

namespace Chapter6.View
{
    /// <summary>
    /// Interaction logic for LinguisticView.xaml
    /// </summary>
    public partial class LinguisticView : UserControl
    {
        public LinguisticView()
        {
            InitializeComponent();
        }

        private void Analyzers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            vm.LinguisticVm.SelectedAnalyzers.Clear();

            foreach (Analyzer analyzer in Analyzers.SelectedItems)
            {
                vm.LinguisticVm.SelectedAnalyzers.Add(analyzer);
            }
        }
    }
}
