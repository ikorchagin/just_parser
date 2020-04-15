using JustParserLib.Avito;
using JustParserLib;
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
using System.Data;
using Microsoft.Win32;
using System.Xml;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Diagnostics;
using forms = System.Windows.Forms;

namespace JustParser
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ParserBase<DataTable> parser;
        public MainWindow()
        {
            this.Title = GetProgramName();
            InitializeComponent();
            CheckForUpdates();
            if (File.Exists("Unpacker.exe"))
            {
                File.Delete("Unpacker.exe");
            }
        }

        private string GetProgramName()
        {
            var reader = new XmlTextReader(@"Data.xml");
            reader.ReadToFollowing("program");
            return $"{reader.GetAttribute("title")} v{reader.GetAttribute("version")}";
        }

        private async void CheckForUpdates()
        {
            var reader = new XmlTextReader(@"Data.xml");
            string gitVersion = "", localVersion = "";

            if (reader.ReadToFollowing("program"))
            {
                localVersion = reader.GetAttribute("version");
            }

            using (WebClient client = new WebClient())
            {
                reader.ReadToFollowing("git-data");
                var response = await client.DownloadStringTaskAsync(reader.ReadElementContentAsString());
                var document = new XmlDocument();
                document.LoadXml(response);
                gitVersion = document.SelectSingleNode(".//program").Attributes.GetNamedItem("version").InnerText;
            }

            if (localVersion != gitVersion)
            {
                EnableUpdateBtn(true);
            }
            else
            {
                EnableUpdateBtn(false);
            }

        }

        private void EnableUpdateBtn(bool val)
        {
            if (val)
            {
                upd.Text = "Доступно обновление";
                updButton.IsEnabled = true;
            }
            else
            {
                upd.Text = "Обновления отсутствуют";
                updButton.IsEnabled = false;
            }
        }

        private async Task<bool> PirateCheck()
        {
            using (WebClient client = new WebClient())
            {
                var reader = new XmlTextReader(@"Data.xml");
                reader.ReadToFollowing("git-data");
                var response = await client.DownloadStringTaskAsync(reader.ReadElementContentAsString());
                var document = new XmlDocument();
                document.LoadXml(response);
                var result = document.SelectSingleNode(".//program/pirate-check").InnerText.Replace(" ", "");
                return result.Contains("true");
            }
        }

        private async void Parser_Start(object sender, RoutedEventArgs e)
        {
            if (await PirateCheck())
            {
                SetError("Парсер временно отключен разработчиком");
                return;
            }
            startBtn.IsEnabled = false;
            progress.Visibility = Visibility.Visible;
            if (hasLimitation.IsChecked.Value && int.TryParse(itemsCount.Text, out var count))
            {
                parser = new AvitoParser(new AvitoParserSettings() { Url = link.Text, ItemsCount = count });
            }

            else
            {
                parser = new AvitoParser(new AvitoParserSettings() { Url = link.Text });
            }
            parser.NewData += Parser_NewData;
            parser.WorkDone += Parser_WorkDone;
            parser.ThrowError += Parser_ThrowError;
            parser.Work();
        }

        private void Parser_ThrowError(ErrorType obj)
        {
            var line = "";

            switch (obj)
            {
                case ErrorType.WrongUrl:
                    line = "Неверный адрес URL";
                    break;
                case ErrorType.Null:
                    line = "Не удаётся найти элементы по указанному url";
                    break;
                default:
                    line = "Неизвестная ошибка";
                    break;
            }

            SetError(line);
        }

        private async void SetError(string errorLine)
        {
            if (errorBox.Text == "")
            {
                errorBox.Text = errorLine;
                await Task.Delay(3000);
                errorBox.Text = "";
            }
        }

        private void Parser_WorkDone(object obj)
        {
            startBtn.IsEnabled = true;
            progress.Value = 0;
            progress.Visibility = Visibility.Hidden;
        }

        private void Parser_NewData(DataTable arg1, double arg2)
        {
            data.Visibility = Visibility.Visible;
            using (DataTable table = new DataTable())
            {
                if (data.ItemsSource != null)
                {
                    table.Merge(((DataView)data.ItemsSource).Table);
                }
                table.Merge(arg1);

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    table.Rows[i][0] = i + 1;
                }

                data.ItemsSource = table.DefaultView;

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    data.Columns[i].Header = table.Columns[i].Caption;
                }

                progress.Value += arg2;
            }
        }

        private void Clear_Data(object sender, RoutedEventArgs e)
        {
            Parser_Stop(sender, e);
            data.ItemsSource = null;
            data.Visibility = Visibility.Hidden;
        }

        private async void Save_Excel(object sender, RoutedEventArgs e)
        {
            if (data.ItemsSource == null || parser == null)
            {
                SetError("Нельзя сохранять таблицу без данных");
                return;
            }

            else if (parser.Status)
            {
                SetError("Нельзя сохранить во время работы программмы");
                return;
            }

            var save = new SaveFileDialog();
            save.Filter = "Книга Excel (*.xlsx)|*.xlsx";
            save.FileName = "Без названия";
            if (save.ShowDialog().Value)
            {
                await Task.Run(() => ((DataView)data.ItemsSource).Table.SaveAsExcel(1, 1, save.FileName));
            }
        }

        private void Parser_Stop(object sender, RoutedEventArgs e)
        {
            if (parser != null)
            {
                parser.Abort();
            }

        }

        private async void DownloadUpdate(object sender, RoutedEventArgs e)
        {
            using (WebClient client = new WebClient())
            {
                var reader = new XmlTextReader(@"Data.xml");
                reader.ReadToFollowing("git-link");
                progress.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    client.DownloadFile(new Uri(reader.ReadElementContentAsString()), "temp.zip");
                });

                progress.Value += 20;

                await Task.Run(() =>
                {
                    if (Directory.Exists("temp"))
                    {
                        Directory.Delete("temp", true);
                    }
                    ZipFile.ExtractToDirectory("temp.zip", "temp");
                });

                progress.Value += 20;

                await Task.Run(() =>
                {
                    File.Delete("temp.zip");
                });

                progress.Value += 20;

                await Task.Run(() =>
                {
                    if (Directory.Exists("patch"))
                    {
                        Directory.Delete("patch", true);
                    }
                    Directory.Move(Directory.GetDirectories("temp").First(), "patch");
                });

                progress.Value += 20;

                await Task.Run(() =>
                {
                    Directory.Delete("temp");
                });

                progress.Value += 20;
                if (!File.Exists("Unpacker.exe"))
                {
                    await Task.Run(() =>
                    {
                        client.DownloadFile("https://raw.githubusercontent.com/TheVenYear/unpacker/release/Unpacker.exe", "Unpacker.exe");
                    });
                }
            }

            Application.Current.Shutdown();
            Process.Start("Unpacker.exe");

        }

        private void ClearLink(object sender, RoutedEventArgs e)
        {
            link.Text = "";
        }
    }
}

