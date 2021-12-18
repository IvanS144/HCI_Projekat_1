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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Zadatak2.demo
{
    public sealed partial class MyUserControl2 : UserControl
    {
        public delegate void AssignmentActionCompletedDelegate(Assignment a, object sender);

        private Assignment assignment;

        public Assignment Assignment { get => assignment; set => assignment = value; }

        public event AssignmentActionCompletedDelegate AssignmentCancelled;
        public event AssignmentActionCompletedDelegate AssignmentPaused;
        public event AssignmentActionCompletedDelegate AssignmentStarted;
        public event AssignmentActionCompletedDelegate AssignmentCompleted;
        public event AssignmentActionCompletedDelegate AssignmentRemoved;


        public MyUserControl2(Assignment a)
        {
            this.InitializeComponent();
            this.assignment = a;
            ImeFotografije.Text = a.FileName; a.ProgressChanged += Assignment_ProgressChanged;
            a.SavingInProgress += A_SavingInProgress;
        }

        private async void A_SavingInProgress(bool value, string name)
        {
            if(value==true)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    //SavingProgressRing.IsActive = true;
                    //SavingProgressRing.Visibility = Visibility.Visible;
                    CurrentStateTextBlock.Text = "Saving...";

                });



            }
            else
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    //SavingProgressRing.IsActive = false;
                    //SavingProgressRing.Visibility = Visibility.Collapsed;
                    SaveTextBox.Text = $"\nSaved to {name} in {assignment.DestinationFolder.Name}";

                });

                //SavingProgressRing.IsActive = false;
                //SavingProgressRing.Visibility = Visibility.Collapsed;
                //ImeFotografije.Text += $"\nSaved to {name} in {assignment.DestinationFolder.Name}";


            }
        }

        private async void Assignment_ProgressChanged(double progress, Assignment.AssignmentState assignmentState)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (!double.IsNaN(progress))
                    ImageProcessingProgressBar.Value = progress;
                

                CurrentStateTextBlock.Text = assignmentState.ToString();

                UpdateControlVisibility();

                if (assignmentState == Assignment.AssignmentState.Done)
                    AssignmentCompleted?.Invoke(assignment, this);
            });

        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as Grid);
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            AssignmentRemoved?.Invoke(assignment, this);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            AssignmentPaused?.Invoke(assignment, this);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AssignmentCancelled?.Invoke(assignment, this);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            AssignmentStarted?.Invoke(assignment, this);
        }

        public void UpdateControlVisibility()
        {




            CancelButton.Visibility = PauseButton.Visibility = (!(assignment.Finished || assignment.IsPending || assignment.Saving)) ? Visibility.Visible : Visibility.Collapsed;

            StartButton.Visibility = (assignment.Finished || assignment.IsPending || assignment.Paused) ? Visibility.Visible : Visibility.Collapsed;

            CancelButton.IsEnabled = assignment.CurrentState != Assignment.AssignmentState.Cancelling && assignment.CurrentState != Assignment.AssignmentState.Cancelled && assignment.CurrentState != Assignment.AssignmentState.Saving; ;
            PauseButton.IsEnabled = assignment.CurrentState != Assignment.AssignmentState.Pausing && assignment.CurrentState != Assignment.AssignmentState.Paused && assignment.CurrentState != Assignment.AssignmentState.Saving; ;
        }

 
    }
}
