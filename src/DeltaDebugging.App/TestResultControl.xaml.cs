using DeltaDebugging.App.Helper;
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

namespace DeltaDebugging.App
{
    /// <summary>
    /// Interaction logic for TestResultControl.xaml
    /// </summary>
    public partial class TestResultControl : UserControl
    {
        public TestResultControl(List<Change> changes, string result)
        {
            InitializeComponent();
            ChangesList.ItemsSource = changes;
            ResultText.Text = result;
        }
    }
}
