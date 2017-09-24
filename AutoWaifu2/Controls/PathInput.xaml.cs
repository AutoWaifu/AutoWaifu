using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
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

namespace AutoWaifu2
{
    /// <summary>
    /// Interaction logic for PathInput.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class PathInput : UserControl
    {
        //  TODO - Use CommonOpenFileDialog - http://stackoverflow.com/questions/11624298/how-to-use-openfiledialog-to-select-a-folder

        public enum PathType
        {
            File,
            ManyFiles,
            Folder
        }

        public PathType InputPathType = PathType.File;

        public PathInput()
        {
            InitializeComponent();
        }


        string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                if (PathTextBox.Text != value)
                    PathTextBox.Text = value;
            }
        }

        public string[] Values { get; set; }



        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            string[] resultFiles = null;

            switch (InputPathType)
            {
                case PathType.File:
                    {
                        var ofd = new System.Windows.Forms.OpenFileDialog();
                        ofd.FileName = this.Value;
                        
                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            resultFiles = ofd.FileNames;
                        }
                        break;
                    }

                case PathType.ManyFiles:
                    {
                        var ofd = new System.Windows.Forms.OpenFileDialog();
                        ofd.FileName = this.Value;
                        ofd.Multiselect = true;
                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            resultFiles = ofd.FileNames;
                        }
                        break;
                    }

                case PathType.Folder:
                    {
                        var ofd = new CommonOpenFileDialog();
                        ofd.IsFolderPicker = true;
                        ofd.InitialDirectory = Value;
                        if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
                        {
                            resultFiles = new string[] { ofd.FileName };
                        }
                        break;
                    }
            }

            if (resultFiles != null)
            {
                Values = resultFiles;
                Value = resultFiles?.FirstOrDefault();
            }
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Value = PathTextBox.Text;
        }
    }
}