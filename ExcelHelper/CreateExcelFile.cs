#define USING_ASP_NET                //  Uncomment this line, if you're using the old ASP.NET
// #define USING_ASP_NET_CORE              //  Uncomment this line, if you're using ASP.NET Core 2 or later
// #define DATA_CONTAINS_FORMULAE

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
#if USING_ASP_NET_CORE
using Microsoft.AspNetCore.Mvc;
#endif

namespace ExcelHelper {
    //
    //  July 2019
    //  http://www.mikesknowledgebase.com
    //
    //  Note: if you plan to use this in a regular ASP.Net application, remember to add a reference to "System.Web", and to uncomment
    //  the "USING_ASP_NET" definition at the top of this file.
    //
    //  Release history
    //  -  Sep 2019:
    //        Added a "StreamExcelDocument" function, to make the library easy to use from ASP.NET Core.
    //  -  Jul 2017:
    //        The ReplaceHexadecimalSymbols function was incorrectly removing any & characters in the data to be exported.
    //  -  Sep 2016: 
    //        Make sure figures with a decimal part are formatted with a full-stop as a decimal point.
    //  -  February 2016: 
    //        Added some code, to demonstrate how to add Named Ranges to your Excel file.
    //  -  July 2015: 
    //        Numeric columns are now written with different styling, depending on if they are integers or floats/doubles (floats are now displayed with 2 decimal places by default)
    //        BugFix: would create invalid Excel files when the user's language setting had a comma as the decimal point character.
    //  -  May 2015: 
    //        I now save DateTime values to the Excel file as a number, then format them in Excel to be displayed as a date or date-time
    //        Added a handful of Styles to the exported file, in GenerateStyleSheet(), to demonstrate how to do this in OpenXML
    //        Optionally allow formulae to be exported  (you need to uncomment the DATA_CONTAINS_FORMULAE definition at the top of this file)
    //  -  Feb 2015: 
    //        Needed to replace "Response.End();" with some other code, to make sure the Excel was fully written to the HTTP Response
    //        New ReplaceHexadecimalSymbols() function to prevent non-ASCII characters from creating invalid Excel files. 
    //        Changed GetExcelColumnName() to cope with more than 702 columns (!)
    //   - Jan 2015: 
    //        Throwing an exception when trying to export a DateTime containing null.
    //        Was missing the function declaration for "CreateExcelDocument(DataSet ds, string filename, System.Web.HttpResponse Response)"
    //        Removed the "Response.End();" from the web version, as recommended in: https://support.microsoft.com/kb/312629/EN-US/?wa=wsignin1.0
    //   - Mar 2014: 
    //        Now writes the Excel data using the OpenXmlWriter classes, which are much more memory efficient.
    //   - Nov 2013: 
    //        Changed "CreateExcelDocument(DataTable dt, string xlsxFilePath)" to remove the DataTable from the DataSet after creating the Excel file.
    //        You can now create an Excel file via a Stream (making it more ASP.Net friendly)
    //   - Jan 2013: Fix: Couldn't open .xlsx files using OLEDB  (was missing "WorkbookStylesPart" part)
    //   - Nov 2012: 
    //        List<>s with Nullable columns weren't be handled properly.
    //        If a value in a numeric column doesn't have any data, don't write anything to the Excel file (previously, it'd write a '0')
    //   - Jul 2012: Fix: Some worksheets weren't exporting their numeric data properly, causing "Excel found unreadable content in '___.xslx'" errors.
    //   - Mar 2012: Fixed issue, where Microsoft.ACE.OLEDB.12.0 wasn't able to connect to the Excel files created using this class.
    //
    //
    //   (c) www.mikesknowledgebase.com 2019
    //   
    //   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
    //   (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
    //   publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
    //   subject to the following conditions:
    //   
    //   The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    //   
    //   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
    //   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
    //   FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
    //   WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    //   
    public class CreateExcelFile {
        const int DEFAULT_COLUMN_WIDTH = 20;        // Set each column to be 20 wide
        private static bool TotalOrderPrice;

        public static bool CreateExcelDocument<T>(List<T> list, string xlsxFilePath, bool addTotalOrderPrice = false) {
            TotalOrderPrice = addTotalOrderPrice;
            DataSet ds = new DataSet();
            ds.Tables.Add(ListToDataTable(list));

            return CreateExcelDocument(ds, xlsxFilePath);
        }

        public static bool CreateExcelDocumentFromDataTable(DataTable dataTable, string xlsxFilePath)
        {
            DataSet ds = new DataSet();
            ds.Tables.Add(dataTable);

            return CreateExcelDocument(ds, xlsxFilePath);
        }

        #region HELPER_FUNCTIONS
        //  This function is adapated from: http://www.codeguru.com/forum/showthread.php?t=450171
        //  My thanks to Carl Quirion, for making it "nullable-friendly".
        public static DataTable ListToDataTable<T>(List<T> list) {
            DataTable dt = new DataTable();

            foreach (PropertyInfo info in typeof(T).GetProperties()) {
                dt.Columns.Add(new DataColumn(info.Name, GetNullableType(info.PropertyType)));
            }
            foreach (T t in list) {
                DataRow row = dt.NewRow();
                foreach (PropertyInfo info in typeof(T).GetProperties()) {
                    if (!IsNullableType(info.PropertyType))
                        row[info.Name] = info.GetValue(t, null);
                    else
                        row[info.Name] = info.GetValue(t, null) ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
        private static Type GetNullableType(Type t) {
            Type returnType = t;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
                returnType = Nullable.GetUnderlyingType(t);
            }
            return returnType;
        }
        private static bool IsNullableType(Type type) {
            return type == typeof(string) ||
                    type.IsArray ||
                    type.IsGenericType &&
                     type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        public static bool CreateExcelDocument(DataTable dt, string xlsxFilePath) {
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            bool result = CreateExcelDocument(ds, xlsxFilePath);
            ds.Tables.Remove(dt);
            return result;
        }
        #endregion

        /// <summary>
        /// Create an Excel file, and write it to a file.
        /// </summary>
        /// <param name="ds">DataSet containing the data to be written to the Excel.</param>
        /// <param name="excelFilename">Name of file to be written.</param>
        /// <returns>True if successful, false if something went wrong.</returns>
        public static bool CreateExcelDocument(DataSet ds, string excelFilename) {
            try {
                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(excelFilename, SpreadsheetDocumentType.Workbook)) {
                    WriteExcelFile(ds, spreadsheet);
                }
                Trace.WriteLine("Successfully created: " + excelFilename);
                return true;
            }
            catch (Exception ex) {
                Trace.WriteLine("Failed, exception thrown: " + ex.Message);
                return false;
            }
        }

#if USING_ASP_NET_CORE
        public static FileStreamResult StreamExcelDocument<T>(List<T> list, string filename)
        {
            //  If you're using ASP.NET Core 2.x+, then this is the function to call !
			//  It will create a temporary Excel file, stream its contents, then delete it.
            string TempFilename = Path.Join(Path.GetTempPath(), filename);
            CreateExcelDocument(list, TempFilename);

            byte[] rawData = File.ReadAllBytes(TempFilename);
            MemoryStream ms = new MemoryStream(rawData);

            FileStreamResult fr = new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fr.FileDownloadName = filename;

            File.Delete(TempFilename);      //  We don't need this anymore

            return fr;
        }
#endif //  USING_ASP_NET_CORE

        private static void WriteExcelFile(DataSet ds, SpreadsheetDocument spreadsheet) {
            //  Create the Excel file contents.  This function is used when creating an Excel file either writing 
            //  to a file, or writing to a MemoryStream.
            spreadsheet.AddWorkbookPart();
            spreadsheet.WorkbookPart.Workbook = new Workbook();

            DefinedNames definedNamesCol = new DefinedNames();

            //  My thanks to James Miera for the following line of code (which prevents crashes in Excel 2010)
            spreadsheet.WorkbookPart.Workbook.Append(new BookViews(new WorkbookView()));

            //  If we don't add a "WorkbookStylesPart", OLEDB will refuse to connect to this .xlsx file !
            WorkbookStylesPart workbookStylesPart = spreadsheet.WorkbookPart.AddNewPart<WorkbookStylesPart>("rIdStyles");
            workbookStylesPart.Stylesheet = GenerateStyleSheet();
            workbookStylesPart.Stylesheet.Save();



            //  Loop through each of the DataTables in our DataSet, and create a new Excel Worksheet for each.
            uint worksheetNumber = 1;
            Sheets sheets = spreadsheet.WorkbookPart.Workbook.AppendChild(new Sheets());

            foreach (DataTable dt in ds.Tables) {
                //  For each worksheet you want to create
                string worksheetName = dt.TableName;

                //  Create worksheet part, and add it to the sheets collection in workbook
                WorksheetPart newWorksheetPart = spreadsheet.WorkbookPart.AddNewPart<WorksheetPart>();
                Sheet sheet = new Sheet() { Id = spreadsheet.WorkbookPart.GetIdOfPart(newWorksheetPart), SheetId = worksheetNumber, Name = worksheetName };

                // If you want to define the Column Widths for a Worksheet, you need to do this *before* appending the SheetData
                // http://social.msdn.microsoft.com/Forums/en-US/oxmlsdk/thread/1d93eca8-2949-4d12-8dd9-15cc24128b10/

                sheets.Append(sheet);

                //  Append this worksheet's data to our Workbook, using OpenXmlWriter, to prevent memory problems
                WriteDataTableToExcelWorksheet(dt, newWorksheetPart, definedNamesCol);

                worksheetNumber++;
            }
            spreadsheet.WorkbookPart.Workbook.Append(definedNamesCol);
            spreadsheet.WorkbookPart.Workbook.Save();
        }

        private static Stylesheet GenerateStyleSheet() {
            //  If you want certain Excel cells to have a different Format, color, border, fonts, etc, then you need to define a "CellFormats" records containing 
            //  these attributes, then assign that style number to your cell.
            //
            //  For example, we'll define "Style # 3" with the attributes we'd like for our header row (Row #1) on each worksheet, where the text is a bit bigger,
            //  and is white text on a dark-gray background.
            // 
            //  NB: The NumberFormats from 0 to 163 are hardcoded in Excel (described in the following URL), and we'll define a couple of custom number formats below.
            //  https://msdn.microsoft.com/en-us/library/documentformat.openxml.spreadsheet.numberingformat.aspx
            //  http://lateral8.com/articles/2010/6/11/openxml-sdk-20-formatting-excel-values.aspx
            //
            uint iExcelIndex = 164;

            return new Stylesheet(
                new NumberingFormats(
                    //  
                    new NumberingFormat()                                                  // Custom number format # 164: especially for date-times
                    {
                        NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                        FormatCode = StringValue.FromString("dd/MMM/yyyy hh:mm:ss")
                    },
                    new NumberingFormat()                                                   // Custom number format # 165: especially for date times (with a blank time)
                    {
                        NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                        FormatCode = StringValue.FromString("dd/MMM/yyyy")
                    }
               ),
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 10 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Arial" }),
                    new Font(                                                               // Index 1 - A 12px bold font, in white.
                        new Bold(),
                        new FontSize() { Val = 12 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "FFFFFF" } },
                        new FontName() { Val = "Arial" }),
                    new Font(                                                               // Index 2 - An Italic font.
                        new Italic(),
                        new FontSize() { Val = 10 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Times New Roman" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        ) { PatternType = PatternValues.Solid }),
                    new Fill(                                                           // Index 3 - Dark-gray fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FF404040" } }
                        ) { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                         // Style # 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat() { NumberFormatId = 164 },                                         // Style # 1 - DateTimes
                    new CellFormat() { NumberFormatId = 165 },                                         // Style # 2 - Dates (with a blank time)
                    new CellFormat(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }) { FontId = 1, FillId = 3, BorderId = 0, ApplyFont = true, ApplyAlignment = true },       // Style # 3 - Header row 
                    new CellFormat() { NumberFormatId = 3 },                                           // Style # 4 - Number format: #,##0
                    new CellFormat() { NumberFormatId = 4 },                                           // Style # 5 - Number format: #,##0.00
                    new CellFormat() { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },       // Style # 6 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Style # 7 - Italic
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Style # 8 - Times Roman
                    new CellFormat() { FontId = 0, FillId = 2, BorderId = 0, ApplyFill = true },       // Style # 9 - Yellow Fill
                    new CellFormat(                                                                    // Style # 10 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    ) { FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true },
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }      // Style # 11 - Border
                )
            ); // return
        }

        private static void WriteDataTableToExcelWorksheet(DataTable dt, WorksheetPart worksheetPart, DefinedNames definedNamesCol) {
            OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart, Encoding.ASCII);
            writer.WriteStartElement(new Worksheet());

            //  To demonstrate how to set column-widths in Excel, here's how to set the width of all columns to our default of "25":
            uint inx = 1;
            writer.WriteStartElement(new Columns());
            foreach (DataColumn dc in dt.Columns) {
                writer.WriteElement(new Column { Min = inx, Max = inx, CustomWidth = true, Width = DEFAULT_COLUMN_WIDTH });
                inx++;
            }
            writer.WriteEndElement();


            writer.WriteStartElement(new SheetData());

            string cellValue = "";
            string cellReference = "";

            //  Create a Header Row in our Excel file, containing one header for each Column of data in our DataTable.
            //
            //  We'll also create an array, showing which type each column of data is (Text or Numeric), so when we come to write the actual
            //  cells of data, we'll know if to write Text values or Numeric cell values.
            int numberOfColumns = dt.Columns.Count;
            bool[] IsIntegerColumn = new bool[numberOfColumns];
            bool[] IsFloatColumn = new bool[numberOfColumns];
            bool[] IsDateColumn = new bool[numberOfColumns];

            string[] excelColumnNames = new string[numberOfColumns];
            for (int n = 0; n < numberOfColumns; n++)
                excelColumnNames[n] = GetExcelColumnName(n);

            //
            //  Create the Header row in our Excel Worksheet
            //  We'll set the row-height to 20px, and (using the "AppendHeaderTextCell" function) apply some formatting to the cells.
            //
            uint rowIndex = 1;

            writer.WriteStartElement(new Row { RowIndex = rowIndex, Height = 20, CustomHeight = true });
            for (int colInx = 0; colInx < numberOfColumns; colInx++) {
                DataColumn col = dt.Columns[colInx];
                AppendHeaderTextCell(excelColumnNames[colInx] + "1", col.ColumnName, writer);
                IsIntegerColumn[colInx] = col.DataType.FullName.StartsWith("System.Int");
                IsFloatColumn[colInx] = col.DataType.FullName == "System.Decimal" || col.DataType.FullName == "System.Double" || col.DataType.FullName == "System.Single";
                IsDateColumn[colInx] = col.DataType.FullName == "System.DateTime";

                //  Uncomment the following lines, for an example of how to create some Named Ranges in your Excel file
#if FALSE
                //  For each column of data in this worksheet, let's create a Named Range, showing where there are values in this column
                //       eg  "NamedRange_UserID"  = "Drivers!$A2:$A6"
                //           "NamedRange_Surname" = "Drivers!$B2:$B6"
                string columnHeader = col.ColumnName.Replace(" ", "_");
                string NamedRange = string.Format("{0}!${1}2:${2}{3}", worksheetName, excelColumnNames[colInx], excelColumnNames[colInx], dt.Rows.Count + 1);
                DefinedName definedName = new DefinedName() { 
                    Name = "NamedRange_" + columnHeader,
                    Text = NamedRange 
                };       
                definedNamesCol.Append(definedName);        
#endif
            }
            writer.WriteEndElement();   //  End of header "Row"

            //
            //  Now, step through each row of data in our DataTable...
            //
            double cellFloatValue = 0;
            CultureInfo ci = new CultureInfo("en-US");
            foreach (DataRow dr in dt.Rows) {
                // ...create a new row, and append a set of this row's data to it.
                ++rowIndex;

                writer.WriteStartElement(new Row { RowIndex = rowIndex });

                for (int colInx = 0; colInx < numberOfColumns; colInx++) {
                    cellValue = dr.ItemArray[colInx].ToString();
                    cellValue = ReplaceHexadecimalSymbols(cellValue);
                    cellReference = excelColumnNames[colInx] + rowIndex.ToString();

                    // Create cell with data
                    if (IsIntegerColumn[colInx] || IsFloatColumn[colInx]) {
                        //  For numeric cells without any decimal places.
                        //  If this numeric value is NULL, then don't write anything to the Excel file.
                        cellFloatValue = 0;
                        bool bIncludeDecimalPlaces = IsFloatColumn[colInx];
                        if (double.TryParse(cellValue, out cellFloatValue)) {
                            cellValue = cellFloatValue.ToString(ci);
                            AppendNumericCell(cellReference, cellValue, bIncludeDecimalPlaces, writer);
                        }
                    }
                    else if (IsDateColumn[colInx]) {
                        //  For date values, we save the value to Excel as a number, but need to set the cell's style to format
                        //  it as either a date or a date-time.
                        DateTime dateValue;
                        if (DateTime.TryParse(cellValue, out dateValue)) {
                            AppendDateCell(cellReference, dateValue, writer);
                        }
                        else {
                            //  This should only happen if we have a DataColumn of type "DateTime", but this particular value is null/blank.
                            AppendTextCell(cellReference, cellValue, writer);
                        }
                    }
                    else {
                        //  For text cells, just write the input data straight out to the Excel file.
                        AppendTextCell(cellReference, cellValue, writer);
                    }
                }
                writer.WriteEndElement(); //  End of Row
            }

            if (TotalOrderPrice) {
                // Calculate Total Orders Price
                dt.DefaultView.ToTable(true, "OrderId");
                double TotalPrice = dt.AsEnumerable().Sum(row => row.Field<double>("TotalOrder"));
                ++rowIndex;
                writer.WriteStartElement(new Row { RowIndex = rowIndex });
                cellReference = excelColumnNames[7] + rowIndex.ToString(); // TotalOrder value
                AppendNumericCell(cellReference, TotalPrice.ToString(), true, writer); // ADD Total value
                writer.WriteEndElement(); //  End of TotalOrderPrice Row
            }

            writer.WriteEndElement(); //  End of SheetData
            writer.WriteEndElement(); //  End of worksheet

            writer.Close();
        }

        private static void AppendHeaderTextCell(string cellReference, string cellStringValue, OpenXmlWriter writer) {
            //  Add a new "text" Cell to the first row in our Excel worksheet
            //  We set these cells to use "Style # 3", so they have a gray background color & white text.
            writer.WriteElement(new Cell {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.String,
                StyleIndex = 3
            });
        }

        private static void AppendTextCell(string cellReference, string cellStringValue, OpenXmlWriter writer) {
            //  Add a new "text" Cell to our Row 

#if DATA_CONTAINS_FORMULAE
            //  If this item of data looks like a formula, let's store it in the Excel file as a formula rather than a string.
            if (cellStringValue.StartsWith("="))
            {
                AppendFormulaCell(cellReference, cellStringValue, writer);
                return;
            }
#endif

            //  Add a new Excel Cell to our Row 
            writer.WriteElement(new Cell {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.String
            });
        }

        private static void AppendDateCell(string cellReference, DateTime dateTimeValue, OpenXmlWriter writer) {
            //  Add a new "datetime" Excel Cell to our Row.
            //
            //  If the "time" part of the DateTime is blank, we'll format the cells as "dd/MMM/yyyy", otherwise ""dd/MMM/yyyy hh:mm:ss"
            //  (Feel free to modify this logic if you wish.)
            //
            //  In our GenerateStyleSheet() function, we defined two custom styles, to make this work:
            //      Custom style#1 is a style containing our custom NumberingFormat # 164 (show each date as "dd/MMM/yyyy hh:mm:ss")
            //      Custom style#2 is a style containing our custom NumberingFormat # 165 (show each date as "dd/MMM/yyyy")
            //  
            //  So, if our time element is blank, we'll assign style 2, but if there IS a time part, we'll apply style 1.
            //
            string cellStringValue = dateTimeValue.ToOADate().ToString(CultureInfo.InvariantCulture);
            bool bHasBlankTime = dateTimeValue.Date == dateTimeValue;

            writer.WriteElement(new Cell {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                StyleIndex = UInt32Value.FromUInt32(bHasBlankTime ? 2 : (uint)1),
                DataType = CellValues.Number        //  Use this, rather than CellValues.Date
            });
        }

        private static void AppendFormulaCell(string cellReference, string cellStringValue, OpenXmlWriter writer) {
            //  Add a new "formula" Excel Cell to our Row 
            writer.WriteElement(new Cell {
                CellFormula = new CellFormula(cellStringValue),
                CellReference = cellReference,
                DataType = CellValues.Number
            });
        }

        private static void AppendNumericCell(string cellReference, string cellStringValue, bool bIncludeDecimalPlaces, OpenXmlWriter writer) {
            //  Add a new numeric Excel Cell to our Row.
            uint cellStyle = (uint)(bIncludeDecimalPlaces ? 5 : 4);
            writer.WriteElement(new Cell {
                CellValue = new CellValue(cellStringValue),
                CellReference = cellReference,
                StyleIndex = cellStyle,                                 //  Style #4 formats with 0 decimal places, style #5 formats with 2 decimal places
                DataType = CellValues.Number
            });
        }

        private static string ReplaceHexadecimalSymbols(string txt) {
            //  I've often seen cases when a non-ASCII character will slip into the data you're trying to export, and this will cause an invalid Excel to be created.
            //  This function removes such characters.
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        //  Convert a zero-based column index into an Excel column reference  (A, B, C.. Y, Z, AA, AB, AC... AY, AZ, BA, BB..)
        public static string GetExcelColumnName(int columnIndex) {
            //  Convert a zero-based column index into an Excel column reference  (A, B, C.. Y, Z, AA, AB, AC... AY, AZ, BA, BB..)
            //  eg GetExcelColumnName(0) should return "A"
            //     GetExcelColumnName(1) should return "B"
            //     GetExcelColumnName(25) should return "Z"
            //     GetExcelColumnName(26) should return "AA"
            //     GetExcelColumnName(27) should return "AB"
            //     GetExcelColumnName(701) should return "ZZ"
            //     GetExcelColumnName(702) should return "AAA"
            //     GetExcelColumnName(1999) should return "BXX"
            //     ..etc..

            int firstInt = columnIndex / 676;
            int secondInt = columnIndex % 676 / 26;
            if (secondInt == 0) {
                secondInt = 26;
                firstInt = firstInt - 1;
            }
            int thirdInt = columnIndex % 26;

            char firstChar = (char)('A' + firstInt - 1);
            char secondChar = (char)('A' + secondInt - 1);
            char thirdChar = (char)('A' + thirdInt);

            if (columnIndex < 26)
                return thirdChar.ToString();

            if (columnIndex < 702)
                return string.Format("{0}{1}", secondChar, thirdChar);

            return string.Format("{0}{1}{2}", firstChar, secondChar, thirdChar);
        }
    }
}
