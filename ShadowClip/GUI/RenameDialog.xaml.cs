using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ShadowClip.GUI
{
    partial class RenameDialog
    {

        public RenameDialog()
        {
            InitializeComponent();
        }

        public string ResponseText
        {
            get => ResponseTextBox.Text;
            set => ResponseTextBox.Text = value;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ResponseTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DialogResult = true;
            e.Handled = true;
        }

        private void RenameDialog_OnActivated(object sender, EventArgs e)
        {
            Keyboard.Focus(ResponseTextBox);
            ResponseTextBox.SelectAll();
        }
    }
}
