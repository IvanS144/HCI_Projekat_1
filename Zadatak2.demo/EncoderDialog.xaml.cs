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
        public EncoderDialog(AssignmentData assignmentData)
        {

            this.InitializeComponent();
            if (assignmentData.Width > 0)
                WidthNumberBox.Value = assignmentData.Width;
            if (assignmentData.Height > 0)
                HeightNumberBox.Value = assignmentData.Height;
            switch (assignmentData.Effect)
            {
                case "Negative": NegativeRadioButton.IsChecked = true; tag = "Negative"; break;
                case "Sepia": SepiaRadioButton.IsChecked = true; tag = "Sepia"; break;
                case "Greyscale": GrayscaleRadioButton.IsChecked = true; tag = "Greyscale"; break;
                default: NoEffectRadioButton.IsChecked = true; tag = "NoEffect"; break;
            }
            if (assignmentData.Angle > 0)
            {
                Slider.Value = assignmentData.Angle;
                angle = assignmentData.Angle;
            }

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
            if (radioButton != null)
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
            return (angle, SizeAdjustSwitch.IsOn ? Convert.ToInt32(HeightNumberBox.Text) : -1, SizeAdjustSwitch.IsOn ? Convert.ToInt32(WidthNumberBox.Text) : -1, tag);
        }

        private void SizeAdjustSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch ts = sender as ToggleSwitch;
            if (ts != null)
            {
                if (ts.IsOn)
                {
                    HeightNumberBox.IsEnabled = true;
                    WidthNumberBox.IsEnabled = true;
                }
                else
                {
                    HeightNumberBox.IsEnabled = false;
                    WidthNumberBox.IsEnabled = false;
                }
            }

        }
    }
}
