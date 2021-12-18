using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Zadatak2.demo
{
    public sealed partial class ParametersDialog : ContentDialog
    {
        List<int> assignmentComboBoxOptions;
        List<int> coresComboBoxOptions;
        public ParametersDialog(int maxNumOfTasks, int numOfEnvironmentCores)
        {
            assignmentComboBoxOptions = Enumerable.Range(1, maxNumOfTasks).ToList();
            coresComboBoxOptions = Enumerable.Range(1, numOfEnvironmentCores).ToList();
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        public (int,int) GetSelectedNumbers()
        {
            return (AssignentsComboBox.SelectedIndex+1, CoresComboBox.SelectedIndex+1);



        }


    }
}
