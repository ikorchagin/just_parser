using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using _Excel = Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;

namespace JustParserLib
{
    public static class DataTableExtension
    {
        public static void SaveAsExcel(this DataTable table, int posX, int posY, string path)
        {
            var excel = new _Excel.Application();
            excel.Workbooks.Add();
            _Excel.Workbook book = excel.ActiveWorkbook;
            _Excel.Worksheet sheet = book.ActiveSheet;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sheet.Cells[posY, i + posX] = table.Columns[i].Caption;
                sheet.Cells[posY, i + posX].Borders.LineStyle = _Excel.XlLineStyle.xlContinuous;
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    sheet.Cells[i + posY + 1, j + posX] = table.Rows[i][j];
                    sheet.Cells[i + posY + 1, j + posX].Borders.LineStyle = _Excel.XlLineStyle.xlContinuous;
                }
            }

            sheet.Columns.AutoFit();
            book.SaveAs(path);
            book.Close();
        }
    }

}
