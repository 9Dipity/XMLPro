using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace XMLTreeEditor
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, XDocument> LoadedXmlFiles = new();
        private string CurrentSelectedNodeName;
        private string CurrentSelectedNodeValue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadXmlButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "XML Files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    if (!LoadedXmlFiles.ContainsKey(file))
                    {
                        var doc = XDocument.Load(file);
                        LoadedXmlFiles.Add(file, doc);
                        XmlFileList.Items.Add(file);
                    }
                }
            }
        }

        private void XmlFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (XmlFileList.SelectedItem is string selectedFile && LoadedXmlFiles.ContainsKey(selectedFile))
            {
                var doc = LoadedXmlFiles[selectedFile];
                XmlTreeView.Items.Clear();
                XmlTreeView.Items.Add(CreateTreeViewItem(doc.Root));
            }
        }

        private TreeViewItem CreateTreeViewItem(XElement element)
        {
            // Create the TreeViewItem for the current element
            var treeViewItem = new TreeViewItem
            {
                // Include the value if the element has no children (not a parent)
                Header = element.HasElements ? element.Name.LocalName : $"{element.Name.LocalName}: {element.Value}",
                Tag = element
            };

            // Add attributes as separate TreeViewItems
            foreach (var attribute in element.Attributes())
            {
                treeViewItem.Items.Add(new TreeViewItem
                {
                    Header = $"{attribute.Name.LocalName}: {attribute.Value}",
                    Tag = attribute
                });
            }

            // Recursively add child elements
            foreach (var child in element.Elements())
            {
                treeViewItem.Items.Add(CreateTreeViewItem(child));
            }

            return treeViewItem;
        }


        private void XmlTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (XmlTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                if (selectedItem.Tag is XElement element)
                {
                    CurrentSelectedNodeName = element.Name.LocalName;
                    CurrentSelectedNodeValue = element.Value;
                }
                else if (selectedItem.Tag is XAttribute attribute)
                {
                    CurrentSelectedNodeName = attribute.Name.LocalName;
                    CurrentSelectedNodeValue = attribute.Value;
                }

                NodeNameTextBox.Text = CurrentSelectedNodeName;
                NodeValueTextBox.Text = CurrentSelectedNodeValue;
            }
        }

        private void BulkChangeButton_Click(object sender, RoutedEventArgs e)
        {
            // Apply changes to all loaded XML files.
            string newValue = NewValueTextBox.Text;

            if (!string.IsNullOrEmpty(CurrentSelectedNodeName) && !string.IsNullOrEmpty(newValue))
            {
                foreach (var doc in LoadedXmlFiles.Values)
                {
                    var elements = doc.Descendants()
                                      .Where(el => el.Name.LocalName == CurrentSelectedNodeName);

                    foreach (var element in elements)
                    {
                        element.Value = newValue;
                    }

                    var attributes = doc.Descendants()
                                        .Attributes()
                                        .Where(attr => attr.Name.LocalName == CurrentSelectedNodeName);

                    foreach (var attribute in attributes)
                    {
                        attribute.Value = newValue;
                    }
                }

                foreach (var file in LoadedXmlFiles.Keys.ToList())
                {
                    LoadedXmlFiles[file].Save(file);
                }

                MessageBox.Show("Changes saved to all files.", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddChildNode_Click(object sender, RoutedEventArgs e)
        {
            if (XmlTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is XElement parentElement)
            {
                // Open the AddNodeDialog
                var addNodeDialog = new AddNodeDialog();
                if (addNodeDialog.ShowDialog() == true)
                {
                    // Get the user input from the dialog
                    string newNodeName = addNodeDialog.NodeName;
                    string newNodeValue = addNodeDialog.NodeValue;

                    // Ensure the new node inherits the parent element's namespace
                    XNamespace parentNamespace = parentElement.Name.Namespace;

                    // Create the new child node within the parent's namespace
                    var newChild = new XElement(parentNamespace + newNodeName, newNodeValue);
                    parentElement.Add(newChild);

                    // Update the TreeView
                    selectedItem.Items.Add(CreateTreeViewItem(newChild));

                    // Save changes back to the file
                    foreach (var file in LoadedXmlFiles.Keys.ToList())
                    {
                        LoadedXmlFiles[file].Save(file);
                    }

                    MessageBox.Show("Child node added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid parent node.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void RenameNodeButton_Click(object sender, RoutedEventArgs e)
        {
            string newNodeName = NewNodeNameTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(CurrentSelectedNodeName) && !string.IsNullOrEmpty(newNodeName))
            {
                foreach (var doc in LoadedXmlFiles.Values)
                {
                    var elements = doc.Descendants()
                                      .Where(el => el.Name.LocalName == CurrentSelectedNodeName);

                    foreach (var element in elements)
                    {
                        element.Name = newNodeName;
                    }
                }

                foreach (var file in LoadedXmlFiles.Keys.ToList())
                {
                    LoadedXmlFiles[file].Save(file);
                }

                MessageBox.Show("Node names updated across all files.", "Rename Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the TreeView to reflect changes
                if (XmlFileList.SelectedItem is string selectedFile && LoadedXmlFiles.ContainsKey(selectedFile))
                {
                    var doc = LoadedXmlFiles[selectedFile];
                    XmlTreeView.Items.Clear();
                    XmlTreeView.Items.Add(CreateTreeViewItem(doc.Root));
                }
            }
            else
            {
                MessageBox.Show("Please select a node and enter a new name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
