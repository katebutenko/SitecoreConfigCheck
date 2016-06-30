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
using Microsoft.WindowsAPICodePack.Dialogs;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace CheckConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool SolrIsUsed;
        bool isCM;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            

            var dlg = new CommonOpenFileDialog();
            dlg.Title = "My Title";
            dlg.IsFolderPicker = true;
           // dlg.InitialDirectory = currentDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
           // dlg.DefaultDirectory = currentDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {                
                AppConfigTextBox.Text = dlg.FileName;
                // Do something with selected folder string
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "Excel files (*.xlsx)|*.xlsx";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                ExcelFileTextBox.Text =  dlg.FileName;
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            


            if (!String.IsNullOrEmpty(ExcelFileTextBox.Text) && !String.IsNullOrEmpty(AppConfigTextBox.Text))
            {
                Dictionary<string, bool?> resultsList = new Dictionary<string, bool?>();
                Dictionary<string, bool> shouldBeDict = new Dictionary<string, bool>();
                List<string> filesList = new List<string>();

                ReadTheExcel(ref shouldBeDict);

                //get list of files in the AppConfig folder
                GetTheFiles(ref filesList, AppConfigTextBox.Text);

                Dictionary<string, bool> actualConfigDict = new Dictionary<string, bool>();
                //prepare the files list to look like shouldBeDict
                //var normalizedFilesList = 
                    filesList.ForEach(x => {
                        if (x.EndsWith(".config"))
                        {
                            x = StripExtension(x);
                            if (actualConfigDict.Keys.Contains(x))
                            {
                                actualConfigDict[x] = true; //in case there are 2 same named files, one .config and one .config.disabled
                            }
                            else actualConfigDict.Add(x, true);
                        }
                        else
                        {
                            x = StripExtension(x);
                            if (!actualConfigDict.Keys.Contains(x))
                            {
                                actualConfigDict.Add(x, false);
                            }
                        }
                });

                if (shouldBeDict.Count > 0)
                {
                    //compare
                   foreach(var entry in shouldBeDict)
                    {
                        //search fileslist for entries starting with entry.Key
                        if (actualConfigDict.Keys.Contains(entry.Key))
                        {
                            if (actualConfigDict[entry.Key] != shouldBeDict[entry.Key])
                            {
                                //var result = shouldBeDict[entry.Key] ? "enabled" : "disabled";
                                resultsList.Add(entry.Key, shouldBeDict[entry.Key]);
                            }
                            actualConfigDict.Remove(entry.Key);
                        }
                        else //if file doesn't exist in current config at all
                        {
                            if (shouldBeDict[entry.Key] == true)
                                resultsList.Add(entry.Key, null);
                        }
                    }
                }

                //fill textboxes with the results
                
                var list = resultsList.Keys.Where(x => resultsList[x] == true).ToList();

                PutListToTextBox(list, shouldBeEnabledTextBox);

                list = resultsList.Keys.Where(x => resultsList[x] == null).ToList();
                PutListToTextBox(list, dontExistTextBox);


                list = resultsList.Keys.Where(x => resultsList[x] == false).ToList();
                PutListToTextBox(list, shouldBeDisabledTextBox);

                list = actualConfigDict.Keys.Where(x => actualConfigDict[x] == true).ToList();
                PutListToTextBox(list, customFilesTextBox);
                
            }


        }

        private void PutListToTextBox(List<string> list, TextBox textBox)
        {
            var text = "";
            foreach (var item in list)
            {
                text += item.ToString() + "\r\n";
            }
            textBox.Text = text;
        }

        private void GetTheFiles(ref List<string> filesList, string sDir)
        {


            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    filesList.Add(StripThePath(f, AppConfigTextBox.Text).TrimStart('\\', '/'));
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {                    
                    GetTheFiles(ref filesList, d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }

        }

        private string StripThePath(string f, string path)
        {
            if (f.Contains(path))
                return f.Substring(path.Length);

            return f;
        }

        private void ReadTheExcel(ref Dictionary<string, bool> shouldBeDict)
        {
            Excel.Application MyApp = new Excel.Application();
            MyApp.Visible = false;
            Excel.Workbook MyBook = MyApp.Workbooks.Open(ExcelFileTextBox.Text);

            var searchEngineName = SolrRadioButton.IsChecked == true ? "solr" : "lucene";
            var isCM = CMCheckBox.IsChecked == true;
            var isProcessing = ProcessingCheckBox.IsChecked == true;
            var isReporting = ReportingCheckBox.IsChecked == true;
            var isCD = CDCheckBox.IsChecked == true;

            try
            {
                Excel.Worksheet dataSheet = (Excel.Worksheet)MyBook.Sheets[1];
                
                //int lastRow = MySheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell).Row;
                //    Metadata.ClientPartnerName = dataSheet.get_Range("B1").Cells.Value.ToString();
                //    string folderPath = System.IO.Path.GetDirectoryName(filePath);
                //    Metadata.DocName = folderPath + @"\" + metadataSheet.get_Range("B9").Cells.Value.ToString();                              
                //    Metadata.Introduction = Metadata.Introduction.Replace("WEBSITENAME", Metadata.WebsiteName).Replace("REVIEWTYPE", Metadata.ReviewType);
                //    Metadata.ParagraphFormatSize = float.Parse(metadataSheet.get_Range("B17").Cells.Value.ToString());

                //11 is where the configs column starts to have values, 227 the total  number of lines with content in the excel
                for (int index = 11; index <= 227; index++)
                {
                    System.Array MyValues = (System.Array)dataSheet.get_Range("C" +
                       index.ToString(), "K" + index.ToString()).Cells.Value;
                    
                    string fileName = MyValues.GetValue(1, 1).ToString() + @"\" + MyValues.GetValue(1, 2).ToString();
                    fileName = StripThePath(fileName, @"\website").TrimStart('\\', '/');
                    fileName = StripThePath(fileName, @"App_Config").TrimStart('\\', '/');
                    fileName = StripExtension(fileName);

                    bool shouldBeEnabled = false;

                    //Check for SOLR/Lucene - 4th column
                    var searchEngine = MyValues.GetValue(1, 4);
                    var searchEngineStr = searchEngine == null ? "" : searchEngine.ToString().ToLower();
                    if (String.IsNullOrEmpty(searchEngineStr) || searchEngineStr.Contains(searchEngineName) || searchEngineStr.Contains("base"))
                    {
                        if (isCM)
                        {
                            //for content management it is 6th column
                            shouldBeEnabled = shouldBeEnabled || MyValues.GetValue(1, 6).ToString() == "Enable";
                        }
                        if (isProcessing)
                        {
                            shouldBeEnabled = shouldBeEnabled || MyValues.GetValue(1, 7).ToString() == "Enable";
                        }
                        if (isReporting)
                        {
                            shouldBeEnabled = shouldBeEnabled || MyValues.GetValue(1, 9).ToString() == "Enable";
                        }
                        if (isCD)
                        {
                            shouldBeEnabled = shouldBeEnabled || MyValues.GetValue(1, 5).ToString() == "Enable";
                        }
                    }


                    shouldBeDict.Add(fileName, shouldBeEnabled);                    
                    
                }
            }            
            catch
            {
                MessageBox.Show("Excel document cannot be read.");
            }
            finally
            {
                MyBook.Close();
            }
        }

        private string StripExtension(string fileName)
        {
            int indexOfExtension = fileName.ToLower().LastIndexOf(".config");
            if (indexOfExtension > 0)
            {
                return fileName.Substring(0, indexOfExtension);
            }
            return fileName;
        }
    }
}
