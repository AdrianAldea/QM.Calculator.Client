using ExcelHelper;
using OfficeOpenXml;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Calculator {
    /// <summary>
    /// Interaction logic for ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : Window
    {
        readonly string bonuriDirectory = "bonuri";
        readonly string dailyReportDirectory = "rapoarte";

        public ReportWindow()
        {
            InitializeComponent();
        }

        private void CbDate_Loaded(object sender, RoutedEventArgs e)
        {
            cbDate.ItemsSource = Directory.GetDirectories("bonuri").Select(x => x.Split("\\")[x.Split("\\").Length - 1]);
            if (cbDate.ItemsSource != null)
                cbDate.SelectedIndex = 0;
        }

        private void Button_Cancel_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Button_DeleteFolder(object sender, RoutedEventArgs e)
        {
            string folderToDelete = bonuriDirectory + "\\" + cbDate.SelectedItem.ToString();
            if (Directory.Exists(folderToDelete))
            {
                Directory.Delete(folderToDelete, true);
                cbDate.ItemsSource = Directory.GetDirectories(bonuriDirectory).Select(x => x.Split("\\")[x.Split("\\").Length - 1]);
            }
        }

        private void Button_Generate(object sender, RoutedEventArgs e)
        {
            if (cbDate.ItemsSource != null)
            {
                string dailyDirectory = cbDate.SelectedItem.ToString();
                string bonuriDailyDirectory = bonuriDirectory + "\\" + dailyDirectory;

                // Load Excel Reports
                List<string> xlsxFiles = Directory.GetFiles(bonuriDailyDirectory, "*.*", SearchOption.AllDirectories)
                  .Where(file => new string[] { ".xlsx" }
                  .Contains(System.IO.Path.GetExtension(file)))
                  .ToList(); // Looking into directory and filtering files

                if (xlsxFiles.Any())
                {
                    CreateDailyRaport(xlsxFiles, dailyDirectory);

                    MessageBox.Show("Raport Generat !");
                }
                else
                {
                    MessageBox.Show("Selectati un folder !");
                }
            }
        }
        private void OpenPrintDialog(string filePathToPrint)
        {
            Workbook workbook = new Workbook();
            workbook.LoadFromFile(filePathToPrint);
            PrintDialog dialog = new PrintDialog();
            dialog.UserPageRangeEnabled = true;
            PageRange rang = new PageRange(1, 3);
            dialog.PageRange = rang;
            PageRangeSelection seletion = PageRangeSelection.UserPages;
            dialog.PageRangeSelection = seletion;
            PrintDocument pd = workbook.PrintDocument;
            if (dialog.ShowDialog() == true)
            {
                pd.Print();
            }
        }

        private void CreateDailyRaport(List<string> xlsxFiles, string? dailyDirectory)
        {
            DataTable totalSumDt = new();
            DataTable destDt = new();
            DataSet dSet = new();
            foreach (string xlsFile in xlsxFiles)
            {
                FileInfo fInfoSrc = new FileInfo(xlsFile);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var source = new ExcelPackage(fInfoSrc);
                var srcWorksheet = source.Workbook.Worksheets[0];
                DataTable sourceDt = srcWorksheet.Cells[1, 1, srcWorksheet.Dimension.End.Row, srcWorksheet.Dimension.End.Column].ToDataTable(c =>
                {
                    c.FirstRowIsColumnNames = true;
                });

                destDt.Merge(sourceDt);
            }

            double TotalPrice = destDt.AsEnumerable().Sum(row => row.Field<double>("Total"));
            totalSumDt.Columns.Add("Data");
            totalSumDt.Columns.Add("TOTAL");
            totalSumDt.Rows.Add(DateTime.Now.ToString(), TotalPrice);

            string dailyReportName = $"raport - {dailyDirectory}" + ".txt";

            using (StreamWriter writer = new StreamWriter(dailyReportDirectory + "\\" + dailyReportName))
            {
                writer.WriteLine($"Data: {dailyReportDirectory}, TOTAL: {TotalPrice}");
            }
            CreateExcelFile.CreateExcelDocument(totalSumDt, dailyReportDirectory + "\\" + dailyReportName);
            //using (Process p = new Process())
            //{
            //    p.StartInfo = new ProcessStartInfo()
            //    {
            //        CreateNoWindow = true,
            //        UseShellExecute = true,
            //        Verb = "print",
            //        FileName = @Directory.GetCurrentDirectory() + @"\" + dailyReportDirectory + @"\" + dailyReportName
            //    };

            //    p.Start();
            //}
            OpenPrintDialog(dailyReportDirectory + "\\" + dailyReportName);
        }
    }
}
