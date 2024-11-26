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
                    // Ensure that the file hasn't been loaded already
                    if (!LoadedXmlFiles.ContainsKey(file))
                    {
                        // Load the XML document from the file
                        var doc = XDocument.Load(file);

                        // Add the file path as the key and the document as the value
                        LoadedXmlFiles.Add(file, doc);

                        // Optionally, add the file path to a ListBox or another UI control
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













        private XElement NodeToDelete;

        // This method will be triggered when the user right-clicks on a node
        private void XmlTreeView_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            // Get the selected node
            if (XmlTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is XElement element)
            {
                NodeToDelete = element; // Store the selected node for deletion
            }
        }

        // This method will handle the delete button click from the context menu
        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (NodeToDelete == null)
            {
                MessageBox.Show("No node selected for deletion.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Show a confirmation dialog with the node name and options
            var deleteDialog = new DeleteNodeDialog(NodeToDelete.Name.LocalName);
            if (deleteDialog.ShowDialog() == true)
            {
                bool deleteFromAllFiles = deleteDialog.DeleteFromAllFiles;
                
                // Delete node logic based on user's choice
                if (deleteFromAllFiles)
                {
                    DeleteNodeFromAllFiles(NodeToDelete);
                }
                else
                {
                    DeleteNodeFromSelectedFile(NodeToDelete);
                }

                // Refresh the TreeView
                if (XmlFileList.SelectedItem is string selectedFile && LoadedXmlFiles.ContainsKey(selectedFile))
                {
                    var doc = LoadedXmlFiles[selectedFile];
                    XmlTreeView.Items.Clear();
                    XmlTreeView.Items.Add(CreateTreeViewItem(doc.Root));
                }

                MessageBox.Show("Node deleted successfully.", "Delete Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteNodeFromSelectedFile(XElement node)
        {
            // Get the currently selected file
            if (XmlFileList.SelectedItem is string selectedFile && LoadedXmlFiles.ContainsKey(selectedFile))
            {
                var doc = LoadedXmlFiles[selectedFile];

                // Find and remove the node from the file
                node.Remove();

                // Save changes back to the file
                doc.Save(selectedFile);
            }
        }

        private void DeleteNodeFromAllFiles(XElement node)
        {
            // Check if the node is null
            if (node == null)
            {
                MessageBox.Show("Node to delete is null.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Extract the local name (without the namespace) of the node
            string localName = node.Name.LocalName;

            // Iterate over all loaded XML documents and their corresponding paths
            foreach (var kvp in LoadedXmlFiles)
            {
                string filePath = kvp.Key; // The file path
                XDocument doc = kvp.Value; // The XDocument

                try
                {
                    // Find the node by its local name in the document
                    var elementToRemove = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

                    if (elementToRemove != null)
                    {
                        // Remove the found node
                        elementToRemove.Remove();

                        // Save the document after modification
                        if (string.IsNullOrEmpty(filePath))
                        {
                            MessageBox.Show("The document has no valid file path to save.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }

                        doc.Save(filePath);  // Save using the file path
                    }
                    else
                    {
                        MessageBox.Show($"No element found with name '{localName}' in file {filePath}.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    // Handle errors for each file processing
                    MessageBox.Show($"An error occurred while deleting from file {filePath}: {ex.Message}", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




    }
}
