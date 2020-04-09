using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Windows;

namespace JustParserLib.Avito
{
    public class AvitoParser : ParserBase<DataTable>
    {
        public AvitoParser(IParserSettings settings) : base(settings) { }

        DataColumn[] AddColumns(params string[] args)
        {
            List<DataColumn> columns = new List<DataColumn>();
            foreach (var line in args)
            {
                columns.Add(new DataColumn() { Caption = line });
            }
            return columns.ToArray();
        }

        private IEnumerable<string> ConvertNodes(HtmlNodeCollection nodes)
        {
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    yield return node.InnerText;
                }
            }
        }

        public override string[] FindLinks(HtmlDocument document)
        {
            var links = document.DocumentNode.SelectNodes(".//h3[@data-marker='item-title']/a");
            if (links == null)
            {
                return null;
            }
            var result = new List<string>();
            foreach (var link in links)
            {
                result.Add($"{link.GetAttributeValue("href", "")}");
            }
            return result.ToArray();
        }

        public override async Task<DataTable> GetStrictData(string link)
        {
            using (DataTable table = new DataTable())
            {
                var document = await GetDocument($"https://www.avito.ru{link}");
                var json = document.DocumentNode.SelectNodes(".//script").Where(x => x.InnerText.Contains("window.__initialData__"))
                    .FirstOrDefault().GetDirectInnerText().Split(new[] { "|| {}", "window.__initialData__ = " }, StringSplitOptions.RemoveEmptyEntries)[0];
                var obj = JObject.Parse(json);

                table.Columns.AddRange(AddColumns("№ п/п", "Номер объявления", "Категория", "Подкатегория", "Тип объекта",
                    "Регион", "Город", "Район", "Полный адрес", "Дата добавления (без времени)",
                    "Заголовок объявления", "Описание объявления", "Площадь, кв. м", "Цена, руб.", "Цена, руб./кв. м",
                     "Валюта цены", "Контактное лицо (продавец)", "Телефон продавца", "Ссылка на объявление",
                     "Ссылка на изображения"));

                var row = table.NewRow();
                row[1] = (string)obj.SelectToken("item.item.id");
                row[2] = (string)obj.SelectTokens("item.item.refs.categories..name").Last();
                row[3] = (string)obj.SelectToken("item.item.firebaseParams.offer_type");
                row[4] = (string)obj.SelectToken("item.item.firebaseParams.type");
                row[5] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(0);
                row[6] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(1) ?? obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(0);
                row[7] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(2);
                row[8] = (string)obj.SelectToken("item.item.address");
                row[9] = double.TryParse(obj.SelectToken("item.item.time")?.ToString(), out var value) ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(value).ToShortDateString() : "Не удалось получить дату";
                row[10] = (string)obj.SelectToken("item.item.title");
                row[11] = ((string)obj.SelectToken("item.item.description")).Replace("\n", " ");
                row[12] = obj.SelectToken("item.item.firebaseParams.area")?.ToString().Replace("м²", "");
                row[13] = obj.SelectToken("item.item.firebaseParams.itemPrice")?.ToString();
                row[14] = obj.SelectToken("item.item.firebaseParams.prodazha_tsena_za_m2")?.ToString();
                row[15] = obj.SelectToken("item.item.price.metric")?.ToString().Replace(".", "");
                row[16] = obj.SelectToken("item.item.seller.name")?.ToString();
                document = await GetDocument($"https://avito.ru/api/1/items/{row[1].ToString()}/phone?key=af0deccbgcgidddjgnvljitntccdduijhdinfgjgfjir");
                var phone = document.Text.Split(new[] { "=%2B", "\"}}}" }, StringSplitOptions.RemoveEmptyEntries).Last();
                row[17] = phone.Contains("bad-request") ? "Телефон не доступен" : phone;
                row[18] = $"https://www.avito.ru{link}";
                row[19] = obj.SelectToken("item.item.images[0].640x480")?.ToString();
                table.Rows.Add(row);

                return table;
            }
        }

        public override async Task<DataTable> GetSoftData(string link)
        {
            using (DataTable table = new DataTable())
            {
                var document = await GetDocument($"https://www.avito.ru{link}");
                var json = document.DocumentNode.SelectNodes(".//script").Where(x => x.InnerText.Contains("window.__initialData__"))
                    .FirstOrDefault().GetDirectInnerText().Split(new[] { "|| {}", "window.__initialData__ = " }, StringSplitOptions.RemoveEmptyEntries)[0];
                var obj = JObject.Parse(json);
                table.Columns.AddRange(AddColumns("№ п/п", "Номер объявления",
                        "Регион", "Город", "Район", "Полный адрес", "Дата добавления (без времени)",
                        "Заголовок объявления", "Описание объявления", "Цена, руб.", "Контактное лицо (продавец)", "Телефон продавца", "Ссылка на объявление",
                     "Ссылка на изображения"));

                var columnsTokens = obj.SelectTokens("item.item.parameters.flat..title")?.Select(x => x.ToString()).ToArray();

                foreach (var column in columnsTokens)
                {
                    table.Columns.Add(new DataColumn(column));
                }

                var row = table.NewRow();
                row[1] = (string)obj.SelectToken("item.item.id");
                row[2] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(0);
                row[3] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(1) ?? obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(0);
                row[4] = (string)obj.SelectTokens("item.item.refs.locations..name").ElementAtOrDefault(2);
                row[5] = (string)obj.SelectToken("item.item.address");
                row[6] = double.TryParse(obj.SelectToken("item.item.time")?.ToString(), out var value) ? new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(value).ToShortDateString() : "Не удалось получить дату";
                row[7] = (string)obj.SelectToken("item.item.title");
                row[8] = ((string)obj.SelectToken("item.item.description")).Replace("\n", " ");
                row[9] = obj.SelectToken("item.item.firebaseParams.itemPrice")?.ToString();
                row[10] = obj.SelectToken("item.item.seller.name")?.ToString();
                document = await GetDocument($"https://avito.ru/api/1/items/{row[1].ToString()}/phone?key=af0deccbgcgidddjgnvljitntccdduijhdinfgjgfjir");
                var phone = document.Text.Split(new[] { "=%2B", "\"}}}" }, StringSplitOptions.RemoveEmptyEntries).Last();
                row[11] = phone.Contains("bad-request") ? "Телефон не доступен" : phone;
                row[12] = $"https://www.avito.ru{link}";
                row[13] = obj.SelectToken("item.item.images[0].640x480")?.ToString();
                var rows = obj.SelectTokens("item.item.parameters.flat..description").Select(x => x.ToString()).ToArray();

                for (int i = 0; i < rows.Count(); i++)
                {
                    row[columnsTokens[i]] = rows[i];
                }

                table.Rows.Add(row);
                return table;
            }

        }

        public override bool GetMode(HtmlDocument document)
        {
            if (document.DocumentNode.SelectSingleNode(".//a[@data-category-id='42']") != null && document.DocumentNode.SelectSingleNode(".//a[@data-category-id='42']").GetAttributeValue("data-marker", "").Contains("current"))
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public override int GetItemsCount(HtmlDocument document)
        {
            if (document.DocumentNode.SelectSingleNode(".//span[@data-marker='page-title/count']") != null)
            {
                return int.Parse(document.DocumentNode.SelectSingleNode(".//span[@data-marker='page-title/count']").InnerText.Replace(" ", ""));
            }
            return 0;
        }

        public override async Task<HtmlDocument> ParseDocument(int page)
        {
            if (Settings.Url.Contains("?"))
            {
                Settings.Prefix = "&p={value}";
            }
            else
            {
                Settings.Prefix = "?p={value}";
            }
            return await GetDocument(Settings.Url, page, Settings.MainPageAgent == AgentType.Desktop ? DesktopAgent : PhoneAgent);
        }
    }

}
