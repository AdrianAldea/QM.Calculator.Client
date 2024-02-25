using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ExcelHelper;
using Models;
using QM.Inventory.TunnelsClient;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tunnels.Core.Models;
using PageSetup = Spire.Xls.PageSetup;

namespace Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        string bonuriDirectory = "bonuri";
        string dailyReportName = "raport";
        string dailyReportDirectory = "rapoarte";
        string dailyReportPath = string.Empty;
        //string productListFileNameWithExt = "productList.json";

        private List<Product> _products = new List<Product>();
        private List<Product> _selectedProducts = new List<Product>();
        private Tunnels.Core.Models.User User = null;

        public List<Product> SelectedProducts
        {
            get
            {
                return _selectedProducts;
            }
            set
            {
                _selectedProducts = value;
            }
        }
        public List<Product> Products
        {
            get
            {
                return _products;
            }
            set
            {
                _products = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            SetupInit();
            ShowHideControls();
            DataContext = this;
        }

        private void ShowHideControls()
        {
            if (this.User != null)
            {
                if (this.User.Role == RolesEnum.User)
                {
                    btnReport.Visibility = Visibility.Hidden;
                    btnCalculate.Visibility = Visibility.Visible;
                    btnPrint.Visibility = Visibility.Visible;
                }
                else if (this.User.Role == RolesEnum.Administrator)
                {
                    btnReport.Visibility = Visibility.Visible;
                    btnCalculate.Visibility = Visibility.Visible;
                    btnCalculate.Visibility = Visibility.Visible;
                    btnPrint.Visibility = Visibility.Visible;
                };
            }
            else
            {
                btnReport.Visibility = Visibility.Hidden;
                btnCalculate.Visibility = Visibility.Hidden;
                btnPrint.Visibility = Visibility.Hidden;
            }
        }

        private void SetupInit()
        {
            ClearInterface();
            CreateDirectories();
        }

        private async void CreateDirectories()
        {
            if (!Directory.Exists(bonuriDirectory))
                Directory.CreateDirectory(bonuriDirectory);

            if (!Directory.Exists(dailyReportDirectory))
                Directory.CreateDirectory(dailyReportDirectory);

            dailyReportPath = dailyReportDirectory + "\\" + dailyReportName + ".xlsx";
            if (!File.Exists(dailyReportPath))
            {
                CreateExcelFile.CreateExcelDocument(new List<Product>(), dailyReportPath);
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            LogInWindow logInWindow = new LogInWindow(User);
            logInWindow.ShowDialog();
            if (logInWindow.User != null)
            {
                User = logInWindow.User;
                lblConnectedUser.Content = "Welcome: " + logInWindow.User.Name;
                this.Show();
            }
            else
            {
                lblConnectedUser.Content = "Please LogIn !";
                this.Show();
            }
            ShowHideControls();
        }
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            User = null;
            lblConnectedUser.Content = "Please LogIn !";
            ShowHideControls();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (tbQty.IsFocused)
            {
                tbQty.Text += ((System.Windows.Controls.Button)sender).Content.ToString();
                tbQty.Focus();
            }
            if (tbPrice.IsFocused)
            {
                tbPrice.Text += ((System.Windows.Controls.Button)sender).Content.ToString();
                tbPrice.Focus();
            }
            if (lbProductList.SelectedItem == null)
            {
                MessageBox.Show("Selecteaza produsul din lista din stanga!");
                tbPrice.Text = String.Empty;
                tbQty.Text = String.Empty;
            }
        }

        #region Events
        private void btnClearPrice_Click(object sender, RoutedEventArgs e)
        {
            tbPrice.Text = String.Empty;
            tbPrice.Focus();
        }
        private void btnClearQty_Click(object sender, RoutedEventArgs e)
        {
            tbQty.Text = string.Empty;
            tbQty.Focus();
        }
        private void ProductList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbQty.Text = string.Empty;
            tbPrice.Text = string.Empty;
            Product? selectedProduct = lbProductList.SelectedItem as Product;
            if (selectedProduct != null)
            {
                tbProductType.Text = selectedProduct.Type;
                lblProduct.Content = selectedProduct.Name;
            }
        }

        public void DeleteSelectedProduct(object sender, RoutedEventArgs e)
        {
            var selectedProduct = dgSelectedProducts.SelectedItem as Product;
            if (selectedProduct != null)
            {
                dgSelectedProducts.CancelEdit();
                SelectedProducts.Remove(selectedProduct);
                dgSelectedProducts.Items.Refresh();
            }
            UpdateTotal();
        }


        private void Button_Calculate(object sender, RoutedEventArgs e)
        {
            var selectedProduct = lbProductList.SelectedItem as Product;
            if (SelectedProducts.Any(x => x.Id == selectedProduct.Id))
            {
                MessageBox.Show("Produsul e deja adaugat !");
                return;
            }

            if (double.TryParse(tbQty.Text, out double Qty) &&
                double.TryParse(tbPrice.Text, out double Price) && lbProductList.SelectedItem != null)
            {

                var total = (Price * Qty).ToString("0.##");
                lblTxtTotal.Content = total;

                if (selectedProduct.Quantity - Qty < 0)
                {
                    MessageBox.Show("Produsul nu contine stock suficient !");
                    return;
                }

                SelectedProducts.Add(new Product
                {
                    Name = selectedProduct.Name,
                    Id = selectedProduct.Id,
                    Type = selectedProduct.Type,
                    DistributionCompany = selectedProduct.DistributionCompany,
                    CreatedDate = DateTime.Now,
                    Price = Price,
                    Quantity = Qty,
                    Total = Convert.ToDouble(total)
                });

                dgSelectedProducts.Items.Refresh();
                tbPrice.Text = string.Empty;
                tbQty.Text = string.Empty;
                tbQty.Focus();
                UpdateTotal();
            }
            else
            {
                MessageBox.Show("Completeaza Cantitatea, Pretul si selecteaza Produsul !");
            }
        }

        private async void Button_Print(object sender, RoutedEventArgs e)
        {
            try
            {
            if (dgSelectedProducts.Items != null && dgSelectedProducts.Items.Count > 0)
            {
                // Create Paths
                string fileNameWithoutExtension = "bon -" + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss");
                string fileNameXlsx = fileNameWithoutExtension + ".xlsx";
                string dailyDirectory = DateTime.Now.ToString("dd-MM-yyyy");
                string bonuriDailyDirectory = bonuriDirectory + "\\" + dailyDirectory;

                if (!Directory.Exists(bonuriDailyDirectory))
                    Directory.CreateDirectory(bonuriDailyDirectory);

                // Send To REST API
                var products = MapToTunnelProducts(SelectedProducts);
                var order = new Order()
                {
                    DateAdded = DateTime.Now,
                    OperationType = OperationTypeEnum.OUT,
                    Price = products.Sum(x => x.Price),
                    Quantity = products.Sum(x => x.Quantity),
                    Total = products.Sum(x => x.Total),
                    UserId = User.Id,
                    ProductsEntries = products,
                    IsActive = true
                };
                var orderCreated = await TunnelsClient.CreateOrderWithProductAsync(order);

                // Save to xlsx file
                string excelFilename = bonuriDailyDirectory + "\\" + fileNameXlsx;
                var productsPrint = new List<ProductsPrint>();
                var id = 1;
                foreach (var product in SelectedProducts)
                {
                    productsPrint.Add(new ProductsPrint
                    {
                        Id = id,
                        Nume = product.Name,
                        Tip = product.Type,
                        Cantitate = product.Quantity,
                        Pret = product.Price,
                        Total = product.Total
                    });
                    id++;
                }
                CreateExcelFile.CreateExcelDocument(productsPrint, excelFilename);
                Print(excelFilename, orderCreated.Id);

                ClearInterface();
                SelectedProducts = new List<Product>();
                dgSelectedProducts.ItemsSource = SelectedProducts;
                dgSelectedProducts.Items.Refresh();

                Products = MapToCalculatorProducts(await TunnelsClient.GetAllProductsAsync(true));
                lbProductList.ItemsSource = Products;
                SortProductsList();
            }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private List<Tunnels.Core.Models.ProductEntry> MapToTunnelProducts(List<Product> selectedProducts)
        {
            List<Tunnels.Core.Models.ProductEntry> products = new List<Tunnels.Core.Models.ProductEntry>();
            foreach (Product product in selectedProducts)
            {

                var newProduct = new Tunnels.Core.Models.ProductEntry
                {
                    DateAdded = product.CreatedDate,
                    ProductId = product.Id,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    Total = product.Total,
                    Type = product.Type,
                    Product = new Tunnels.Core.Models.Product
                    {
                        Id = product.Id,
                        CurrentQuantity = product.Quantity
                    }
                };

                products.Add(newProduct);
            }
            return products;
        }

        private List<Product> MapToCalculatorProducts(List<Tunnels.Core.Models.Product> tunnelsProduct)
        {
            List<Product> products = new List<Product>();
            foreach (Tunnels.Core.Models.Product product in tunnelsProduct)
            {
                var newProduct = new Product
                {
                    Id = product.Id,
                    CreatedDate = product.DateAdded,
                    DistributionCompany = product.DistributionCompany,
                    Name = product.Name,
                    Quantity = product.CurrentQuantity,
                    Type = product.Type
                };

                products.Add(newProduct);
            }
            return products;
        }

        private void Print(string filepath, int orderCreatedId)
        {
            Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
            //Load an Excel file
            workbook.LoadFromFile(filepath);
            //Fit worksheet on one page   
            var worksheet = workbook.Worksheets[0];
            worksheet.AutoFitColumn(1);
            PageSetup pageSetup = worksheet.PageSetup;
            //pageSetup.FitToPagesWide = 1;
            pageSetup.FitToPagesTall = 0;
            pageSetup.IsFitToPage = true;
            pageSetup.BottomMargin = 0;
            pageSetup.TopMargin = 0;
            pageSetup.LeftMargin = 0;
            pageSetup.RightMargin = 0;

            //sheet.LastRow returns the last row of the sheet.
            int lastFilledRow = worksheet.LastRow;
            for (int i = worksheet.LastRow; i >= 0; i--)
            {
                CellRange cr = worksheet.Rows[i - 1].Columns[1];
                if (!cr.IsBlank)
                {
                    lastFilledRow = i;
                    break;
                }
            }
            //to find the last filled row of this column
            worksheet.Range["A1:F1"].Style.Font.IsBold = true;
            worksheet.Range["A1:F1"].Style.Font.Underline = FontUnderlineType.DoubleAccounting;
            worksheet.SetRowHeight(1, 50);

            worksheet.Range["A1:F" + lastFilledRow + 1].Style.Font.Size = 40;
            worksheet.Range["A1:F" + lastFilledRow + 1].Style.Font.Color = System.Drawing.Color.Black;
            worksheet.Range["A1:F" + lastFilledRow + 1].Borders.Value = LineStyleType.None;

            worksheet.Range["A1:F" + lastFilledRow + 1].AutoFitColumns();
            worksheet.GridLinesVisible = true;

            // Caculate abosulte value function
            string Formula = "=SUM(F1:F" + lastFilledRow + ")";
            var formulaResult = workbook.CaculateFormulaValue(Formula);
            String value = formulaResult.ToString();

            //Set the Value of TOTAL
            workbook.Worksheets[0].Range["A" + (lastFilledRow + 1)].Value = "TOTAL :";
            workbook.Worksheets[0].Range["A" + (lastFilledRow + 1)].BorderAround(LineStyleType.Double);
            workbook.Worksheets[0].Range["C" + (lastFilledRow + 1)].BorderAround(LineStyleType.Double);
            workbook.Worksheets[0].Range["D" + (lastFilledRow + 1)].BorderAround(LineStyleType.Double);
            workbook.Worksheets[0].Range["F" + (lastFilledRow + 1)].BorderAround(LineStyleType.Double);
            workbook.Worksheets[0].Range["F" + (lastFilledRow + 1)].NumberFormat = "#,##0.00";
            workbook.Worksheets[0].Range["F" + (lastFilledRow + 1)].Value = value;

            //Set Name Multiline
            for (int i = 1; i <= lastFilledRow; i++)
            {
                string productName = workbook.Worksheets[0].Range["B" + i].Value;
                if (productName.Length > 10)
                {
                    int nrOfWords = productName.Split(' ').Length;
                    string[] words = productName.Split(' ');
                    string productNameFormatted = string.Empty;

                    for (int j = 0; j < nrOfWords; j += 3)
                    {
                        //if (words.Length > j + 2)
                            productNameFormatted += words[j];
                        try
                        {
                            productNameFormatted += ' ' + words[j + 1];

                        }
                        catch (Exception ex) { }
                        try
                        {
                            productNameFormatted += ' ' + words[j + 2];
                        }
                        catch (Exception ex)
                        {

                        }
                        productNameFormatted += '\n';
                    }

                    workbook.Worksheets[0].Range["B" + i].Value = productNameFormatted;
                }
            }



            //Set the Value Date
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 4)].Style.Font.IsBold = true;
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 4)].Style.Font.Size = 30;
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 4)].Style.HorizontalAlignment = HorizontalAlignType.Center;
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 4)].DateTimeValue = System.DateTime.Now; //ToString("dd/MM/yyyy HH:mm");
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 4)].NumberFormat = "mm/dd/yyyy HH:mm";

            //Set the Value BON NEFISCAL
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 5)].Value = "BON NEFISCAL: " + orderCreatedId.ToString();
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 5)].Style.Font.IsBold = true;
            workbook.Worksheets[0].Range["B" + (lastFilledRow + 5)].Style.Font.Size = 20;
            //
            worksheet.Range["A2:" + "A"+ lastFilledRow].VerticalAlignment = VerticalAlignType.Top;

            //Create a PrintDocument object based on the workbook
            PrintDocument printDocument = workbook.PrintDocument;
            printDocument.Print();
            printDocument.Print();
        }

        private void ClearInterface()
        {
            tbQty.Text = string.Empty;
            tbPrice.Text = string.Empty;
            lblTxtTotal.Content = 0.ToString();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow reportWindow = new ReportWindow();
            reportWindow.ShowDialog();
        }
        #endregion 
        private void UpdateTotal() =>
                lblTxtTotal.Content = SelectedProducts.Select(x => x.Total).Sum();

        private async void lbProductList_Loaded(object sender, RoutedEventArgs e)
        {
            Products = MapToCalculatorProducts(await TunnelsClient.GetAllProductsAsync(true));
            lbProductList.ItemsSource = Products;
            SortProductsList();
        }

        private void SortProductsList()
        {
            List<Product> q = new List<Product>();
            foreach (Product o in lbProductList.Items)
                q.Add(o);

            Products = q.OrderBy(x => x.Name).ToList();
            lbProductList.ItemsSource = Products;
            lbProductList.Items.Refresh();
        }

        private void tbSearchProduct_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tbSearchProduct.Text))
            {
                lbProductList.ItemsSource = null;
                List<Product> sortedProducts = new List<Product>();
                foreach (Product item in Products)
                {
                    if (item.Name.ToLower().StartsWith(tbSearchProduct.Text.ToLower()))
                    {
                        sortedProducts.Add(item);
                    }
                }
                lbProductList.ItemsSource = sortedProducts;
            }
            else if (string.IsNullOrWhiteSpace(tbSearchProduct.Text))
            {
                lbProductList.ItemsSource = null;
                List<Product> sortedProducts = new List<Product>();
                foreach (Product item in Products)
                {
                    sortedProducts.Add(item);
                }
                lbProductList.ItemsSource = sortedProducts;
            }
        }

        private void dgSelectedProducts_LostFocus(object sender, RoutedEventArgs e)
        {
            var product = dgSelectedProducts.SelectedItem as Product;
            if(product != null)
            {
                product.Total = product.Price * product.Quantity;
                UpdateTotal();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Products = MapToCalculatorProducts(await TunnelsClient.GetAllProductsAsync(true));
            lbProductList.ItemsSource = Products;
            SortProductsList();
        }

        private void tbQty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Return)
            {
                tbPrice.Focus();
            }
        }

        private void dgSelectedProducts_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
