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
    public sealed partial class ContentDialog2 : ContentDialog
    {
        public ContentDialog2()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
        public void UpdateStackPanel(IReadOnlyList<Assignment> list)
        {
            foreach(var a in list)
            {
                
                if (a!=null)
                    DetectedStackPanel.Children.Add(new CheckBox() { Content = a.FileName, Tag = a, IsChecked = true });
                
            }
        }

        public List<Assignment> GetAllCheckedAssignments() => DetectedStackPanel.Children.OfType<CheckBox>().Where(x=> x.IsChecked==true).Select(x => (x.Tag as Assignment)).ToList();
    }
}
