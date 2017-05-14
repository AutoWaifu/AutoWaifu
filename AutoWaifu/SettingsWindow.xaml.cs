using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using static AutoWaifu.DataModel.AppSettings;

namespace AutoWaifu
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        bool _isClosing = false;


        public DataModel.AppSettings ViewModel
        {
            get { return this.DataContext as DataModel.AppSettings; }
            set
            {
                this.DataContext = value;



                var bestScaleItem = (from item in ScaleComboBox.Items.Cast<ComboBoxItem>()
                                     orderby Math.Abs(float.Parse((item.Content as string).Replace("x", "")) - ViewModel.Scale)
                                     select item).First();
                ScaleComboBox.SelectedItem = bestScaleItem;

                /*
                if (ViewModel.SuperSamples <= 1)
                    SmoothingComboBox.SelectedIndex = 0;
                else
                {
                    var bestSmoothItem = (from item in SmoothingComboBox.Items.Cast<ComboBoxItem>().Where(i => i.Content.ToString() != "None")
                                          orderby Math.Abs(float.Parse((item.Content as string).Replace("x", "")) - (ViewModel.SuperSamples - 1))
                                          select item).First();
                    SmoothingComboBox.SelectedItem = bestSmoothItem;
                }
                */

                if (ViewModel.UseScaleInsteadOfSize)
                    ScaleFactorOption.IsChecked = true;
                else
                    SpecificSizeOption.IsChecked = true;


                MaxWidthTextBox.Text = ViewModel.DesiredWidth.ToString();
                MaxHeightTextBox.Text = ViewModel.DesiredHeight.ToString();

                MethodComboBox.SelectedItem = MethodComboBox.Items.Cast<ComboBoxItem>().Single(i => i.Content.ToString() == ViewModel.ConversionMode.ToString());
                PriorityComboBox.SelectedItem = PriorityComboBox.Items.Cast<ComboBoxItem>().Single(i => i.Content.ToString().Replace(" ", "") == ViewModel.Priority.ToString());
                MaxThreadsComboBox.SelectedItem = MaxThreadsComboBox.Items.Cast<ComboBoxItem>().Single(i => i.Content.ToString() == ViewModel.MaxParallel.ToString());
            }
        }

        public DataModel.AppSettings Result = null;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _isClosing = true;

            base.OnClosing(e);
        }


        bool Validate()
        {
            if (ViewModel.InputDir == null || !Directory.Exists(ViewModel.InputDir))
            {
                MessageBox.Show(this, "The input directory can't be found!");
                return false;
            }

            if (ViewModel.OutputDir == null || !Directory.Exists(ViewModel.OutputDir))
            {
                MessageBox.Show(this, "The output directory can't be found!");
                return false;
            }

            if (ViewModel.Waifu2xCaffeDir == null || !Directory.Exists(ViewModel.Waifu2xCaffeDir))
            {
                MessageBox.Show(this, "The Waifu2x-Caffe directory can't be found!");
                return false;
            }
            else
            {
                if (!File.Exists(Path.Combine(ViewModel.Waifu2xCaffeDir, "waifu2x-caffe-cui.exe")))
                {
                    MessageBox.Show(this, "The Waifu2x-Caffe directory is invalid! Couldn't find waifu2x-caffe-cui.exe!");
                    return false;
                }
            }

            if (ViewModel.Scale <= 1)
            {
                var result = MessageBox.Show(this, "You've selected scaling that will either shrink or have no effect on the image, are you sure this is what you want?", "", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return false;
            }


            return true;
        }



        private void BrowseInputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = ViewModel.InputDir;

            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            ViewModel.InputDir = dialog.SelectedPath;
        }

        private void BrowseOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = ViewModel.OutputDir;

            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            ViewModel.OutputDir = dialog.SelectedPath;
        }

        private void BrowseWaifuCaffeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = ViewModel.Waifu2xCaffeDir;

            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            ViewModel.Waifu2xCaffeDir = dialog.SelectedPath;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
                return;

            Result = ViewModel;
            this.DialogResult = true;
            this.Close();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _isClosing = true;
            Result = null;
            this.Close();
        }

        private void ScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var text = (e.AddedItems[0] as ComboBoxItem).Content as String;

            float scale = float.Parse(text.Replace("x", ""));
            ViewModel.Scale = scale;
        }
        
        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var text = (e.AddedItems[0] as ComboBoxItem).Content as String;

            ViewModel.ConversionMode = (WaifuConvertMode)Enum.Parse(typeof(WaifuConvertMode), text);
        }

        private void PriorityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var text = (e.AddedItems[0] as ComboBoxItem).Content as String;
            text = text.Replace(" ", "");

            ViewModel.Priority = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), text);
        }

        private void MaxThreadsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var text = (e.AddedItems[0] as ComboBoxItem).Content as String;

            ViewModel.MaxParallel = int.Parse(text);
        }


        private void MaxWidthTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            int maxWidth;
            if (!int.TryParse(textbox.Text, out maxWidth))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_isClosing)
                        return;

                    MessageBox.Show(this, "Please enter an integer for Width! (ie 1000, 1500, etc)");
                    textbox.Focus();
                }));
                return;
            }

            if (maxWidth < 10)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_isClosing)
                        return;

                    MessageBox.Show(this, "Please enter a max width of at least 10!");
                    textbox.Focus();
                }));
                return;
            }

            ViewModel.DesiredWidth = maxWidth;
        }

        private void MaxHeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            int maxHeight;
            if (!int.TryParse(textbox.Text, out maxHeight))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_isClosing)
                        return;

                    MessageBox.Show(this, "Please enter an integer for Height! (ie 1000, 1500, etc)");
                    textbox.Focus();
                }));
                return;
            }

            if (maxHeight < 10)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_isClosing)
                        return;

                    MessageBox.Show(this, "Please enter a Height of at least 10!");
                    textbox.Focus();
                }));
                return;
            }

            ViewModel.DesiredWidth = maxHeight;
        }

        private void ScaleFactorOption_Checked(object sender, RoutedEventArgs e)
        {
            SpecificSizeOption.IsChecked = false;
            ViewModel.UseScaleInsteadOfSize = true;
        }

        private void SpecificSizeOption_Checked(object sender, RoutedEventArgs e)
        {
            ScaleFactorOption.IsChecked = false;
            ViewModel.UseScaleInsteadOfSize = false;
        }
    }
}
