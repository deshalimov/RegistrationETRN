using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RegistrationETRN
{
    public class AddressParser
    {
        // Словарь: ключевые слова региона -> код региона
        private static readonly Dictionary<string, string> RegionCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "алтайский край",                              "22" },
            { "амурская область",                            "28" },
            { "архангельская область",                       "29" },
            { "астраханская область",                        "30" },
            { "белгородская область",                        "31" },
            { "брянская область",                            "32" },
            { "владимирская область",                        "33" },
            { "волгоградская область",                       "34" },
            { "вологодская область",                         "35" },
            { "воронежская область",                         "36" },
            { "еврейская автономная область",                "79" },
            { "забайкальский край",                          "75" },
            { "ивановская область",                          "37" },
            { "иркутская область",                           "38" },
            { "калининградская область",                     "39" },
            { "калужская область",                           "40" },
            { "камчатский край",                             "41" },
            { "кемеровская область",                         "42" },
            { "кировская область",                           "43" },
            { "костромская область",                         "44" },
            { "краснодарский край",                          "23" },
            { "красноярский край",                           "24" },
            { "курганская область",                          "45" },
            { "курская область",                             "46" },
            { "ленинградская область",                       "47" },
            { "липецкая область",                            "48" },
            { "магаданская область",                         "49" },
            { "г. москва",                                   "77" },
            { "город москва",                                "77" },
            { "г.москва",                                    "77" },
            { "москва",                                      "77" },
            { "московская область",                          "50" },
            { "мурманская область",                          "51" },
            { "ненецкий автономный округ",                   "83" },
            { "нижегородская область",                       "52" },
            { "новгородская область",                        "53" },
            { "новосибирская область",                       "54" },
            { "омская область",                              "55" },
            { "оренбургская область",                        "56" },
            { "орловская область",                           "57" },
            { "пензенская область",                          "58" },
            { "пермский край",                               "59" },
            { "приморский край",                             "25" },
            { "псковская область",                           "60" },
            { "республика адыгея",                           "01" },
            { "республика алтай",                            "04" },
            { "республика башкортостан",                     "02" },
            { "республика бурятия",                          "03" },
            { "республика дагестан",                         "05" },
            { "республика ингушетия",                        "06" },
            { "кабардино-балкарская республика",             "07" },
            { "республика калмыкия",                         "08" },
            { "республика карачаево-черкессия",              "09" },
            { "республика карелия",                          "10" },
            { "республика коми",                             "11" },
            { "республика крым",                             "91" },
            { "республика марий эл",                         "12" },
            { "республика мордовия",                         "13" },
            { "республика саха",                             "14" },
            { "республика северная осетия",                  "15" },
            { "республика татарстан",                        "16" },
            { "республика тыва",                             "17" },
            { "удмуртская республика",                       "18" },
            { "республика хакасия",                          "19" },
            { "чеченская республика",                        "20" },
            { "чувашская республика",                        "21" },
            { "ростовская область",                          "61" },
            { "ростовская обл",                              "61" },
            { "рязанская область",                           "62" },
            { "самарская область",                           "63" },
            { "г. санкт-петербург",                          "78" },
            { "санкт-петербург",                             "78" },
            { "саратовская область",                         "64" },
            { "сахалинская область",                         "65" },
            { "свердловская область",                        "66" },
            { "г. севастополь",                              "92" },
            { "смоленская область",                          "67" },
            { "ставропольский край",                         "26" },
            { "тамбовская область",                          "68" },
            { "тверская область",                            "69" },
            { "томская область",                             "70" },
            { "тульская область",                            "71" },
            { "тюменская область",                           "72" },
            { "ульяновская область",                         "73" },
            { "хабаровский край",                            "27" },
            { "ханты-мансийский автономный округ",           "86" },
            { "челябинская область",                         "74" },
            { "чукотский автономный округ",                  "87" },
            { "ямало-ненецкий автономный округ",             "89" },
            { "ярославская область",                         "76" },
            { "херсонская область",                          "95" },
            { "донецкая народная республика",                "93" },
            { "луганская народная республика",               "94" },
            { "запорожская область",                         "90" },
        };

        // Префиксы населённых пунктов типа City
        private static readonly HashSet<string> CityPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "г", "г.", "город", "гор", "г-д", "г.о. город", "г "
        };

        // Префиксы населённых пунктов типа Settlement
        private static readonly HashSet<string> SettlementPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "с", "с.", "село", "д", "д.", "деревня",
            "п", "п.", "пос", "пос.", "посёлок", "поселок",
            "рп", "р.п.", "пгт", "пгт.", "поселок городского типа",
            "ст", "ст.", "станица", "ст-ца",
            "х", "х.", "хутор", "аул", "слобода", "слоб", "сл", "сл.",
            "к", "к.", "кишлак"
        };

        public class AddressResult
        {
            public string LocalityType { get; set; } = "";   // "City" или "Settlement"
            public string LocalityName { get; set; } = "";
            public string RegionCode { get; set; } = "";
            public int LocalityTypeIndex { get; set; } = 0; // 0=не выбрано, 1=City, 2=Settlement
        }

        public static AddressResult Parse(string address)
        {
            AddressResult result = new AddressResult();

            if (string.IsNullOrWhiteSpace(address) || address.Trim() == "\\N")
                return result;

            // Нормализуем: убираем лишние пробелы, приводим к нижнему регистру для поиска
            string normalized = Regex.Replace(address.Trim(), @"\s+", " ");

            // Определяем код региона
            result.RegionCode = FindRegionCode(normalized);

            // Извлекаем населённый пункт
            ExtractLocality(normalized, result);

            return result;
        }

        public static string FindRegionCode(string address)
        {
            string lower = address.ToLower();

            // Сортируем по длине ключа (длинные сначала — чтобы "московская область" нашлась раньше чем "москва")
            foreach (KeyValuePair<string, string> kvp in RegionCodes.OrderByDescending(k => k.Key.Length))
            {
                if (lower.Contains(kvp.Key.ToLower()))
                    return kvp.Value;
            }

            return "";
        }

        private static void ExtractLocality(string address, AddressResult result)
        {
            // Разбиваем по запятым и пробелам (адреса бывают с запятыми и без)
            List<string> parts = address
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            foreach (var part in parts)
            {
                // Паттерн: "ПРЕФИКС ИМЯ" или "ПРЕФИКС. ИМЯ" или "ИМЯ ПРЕФИКС" (для ГОРОД МОСКВА)
                (string prefix, string name) = SplitPrefixAndName(part);

                if (string.IsNullOrEmpty(prefix))
                    continue;

                if (CityPrefixes.Contains(prefix))
                {
                    result.LocalityName = CapitalizeFirst(name);
                    result.LocalityType = "City";
                    result.LocalityTypeIndex = 2;
                    return;
                }

                if (SettlementPrefixes.Contains(prefix))
                {
                    result.LocalityName = CapitalizeFirst(name);
                    result.LocalityType = "Settlement";
                    result.LocalityTypeIndex = 1;
                    return;
                }
            }
        }

        private static (string prefix, string name) SplitPrefixAndName(string part)
        {
            // Формат 1: "г Новоалександровск", "г. Москва", "ГОРОД КРЫМСК", "с. Литвиновка"
            // Формат 2: "ГОРОД МОСКВА" (слово целиком)
            Match match = Regex.Match(part, @"^([А-Яа-яЁёA-Za-z\-\.]+\.?)\s+(.+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value.TrimEnd('.');
                string name = match.Groups[2].Value.Trim();
                return (prefix, name);
            }

            return ("", "");
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.ToLower();
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
