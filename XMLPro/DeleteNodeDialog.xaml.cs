using System.Windows;

namespace XMLTreeEditor
{
    public partial class DeleteNodeDialog : Window
    {
        public bool DeleteFromAllFiles { get; private set; }
        public string ElementName { get; private set; }

        public DeleteNodeDialog(string elementName)
        {
            InitializeComponent();
            ElementName = elementName;
            DataContext = this;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFromAllFiles = DeleteFromAllFilesRadioButton.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
