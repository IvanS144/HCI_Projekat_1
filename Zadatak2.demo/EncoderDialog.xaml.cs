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
//using muxc = Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Zadatak2.demo
{
    public sealed partial class EncoderDialog : ContentDialog
    {
        private int angle;
        private string tag;
        public EncoderDialog()
        {
        
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
        

        private void NegativeRadioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SepiaRadioButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            if(radioButton!=null)
            {
                tag = radioButton.Tag.ToString();
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            angle = Convert.ToInt32(e.NewValue);
        }
        public (int, int, int, string) getData()
        {
            return (angle, Convert.ToInt32(HeightNumberBox.Text), Convert.ToInt32(WidthNumberBox.Text), tag);
        }
    }
}
