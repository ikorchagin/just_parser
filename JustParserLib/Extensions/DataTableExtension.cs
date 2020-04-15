using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using _Excel = Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Net;

namespace JustParserLib
{
    public static class DataTableExtension
    {
        public static void SaveAsExcel(this DataTable tableRaw, int posX, int posY, string path)
        {
            var table = tableRaw.Copy();
            var excel = new _Excel.Application();
            excel.Workbooks.Add();
            _Excel.Workbook book = excel.ActiveWorkbook;
            _Excel.Worksheet sheet = book.ActiveSheet;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                table.Columns[i].ColumnName = table.Columns[i].Caption;
                sheet.Cells[posY, i + posX] = table.Columns[i].ColumnName;
                sheet.Cells[posY, i + posX].Borders.LineStyle = _Excel.XlLineStyle.xlContinuous;
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    sheet.Cells[i + posY + 1, j + posX] = table.Rows[i][j];
                    sheet.Cells[i + posY + 1, j + posX].Borders.LineStyle = _Excel.XlLineStyle.xlContinuous;
                    if (table.Columns[j].Caption == "Ссылка на изображения" && Uri.IsWellFormedUriString(table.Rows[i][j].ToString(), UriKind.Absolute))
                    {
                        using (WebClient client = new WebClient())
                        {
                            if (!Directory.Exists($"{Path.GetDirectoryName(path)}/Фото"))
                            {
                                Directory.CreateDirectory($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)} фото");
                            }
                            client.DownloadFileAsync(new Uri(table.Rows[i][j].ToString()), $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)} фото/{table.Rows[i]["№ п/п"]}.jpg");
                        }
                    }
                }
            }
            sheet.Columns.AutoFit();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            book.SaveAs(path);
            book.Close();
        }
    }

}
