using System.Windows;

namespace XMLTreeEditor
{
    public partial class AddNodeDialog : Window
    {
        public string NodeName { get; private set; }
        public string NodeValue { get; private set; }

        public AddNodeDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NodeName = NodeNameTextBox.Text.Trim();
            NodeValue = NodeValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(NodeName))
            {
                MessageBox.Show("Node Name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
