using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegistrationETRN
{
    internal class UrlQuerries
    {
        private readonly Form1 _mainForm;

        private static readonly string xapikey;
        private static readonly string prod;
        private static readonly string enviroment;

        static UrlQuerries()
        {
            string configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "secrets.json"
            );

            using (JsonDocument doc =
                JsonDocument.Parse(File.ReadAllText(configPath)))
            {
                xapikey = doc.RootElement
                    .GetProperty("XApiKey")
                    .GetString();

                prod = doc.RootElement
                    .GetProperty("Prod")
                    .GetString();
            }

            // Выбираем окружение для всего класса
            enviroment = prod;
        }

        private readonly HelpElements helpElements;

        private readonly HttpClient _client = new HttpClient();




        public UrlQuerries(Form1 form)
        {
            _mainForm = form;
            helpElements = new HelpElements(form);
        }

        // Получение авторизационных данных
        public async Task<HttpResponseMessage> Auth(string login, string password, string scope)
        {
            string url = $"https://smpanel.{enviroment}/jwt-auth/connect/token";

            Dictionary<string, string> Params = new Dictionary<string, string>()
            {
                { "grant_type", "password" },
                { "username", $"{login}" },
                { "password", $"{password}" },
                { "scope", $"{scope} offline_access" }
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(Params);

            //отправить запрос
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.NotFound;

            try
            {
                response = await _client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Ошибка авторизации: {response.Content.ReadAsStringAsync().Result}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                MessageBox.Show("Ошибка авторизации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return response;
        }

        //Получение информации профиля
        public async Task<HttpResponseMessage> GetProfileDetails(string access_token, string id_user)
        {
            string url = $"https://smpanel.{enviroment}/api/native/v1.0/users/{id_user}";

            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");

            try
            {
                HttpResponseMessage response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else
                {
                    MessageBox.Show($"Ошибка получения данных профиля по Id из Админки: {response.Content.ReadAsStringAsync().Result}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            catch
            {
                MessageBox.Show("Ошибка получения данных профиля по Id из Админки", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Отправка сертификата в БД
        public async Task<HttpResponseMessage> CertificatePubKey(string access_token)
        {
            string url = $"https://operator-epd-reg-server.production.kube.{enviroment}/v1/certificate-pub-key";

            Dictionary<string, object> Params = new Dictionary<string, object>
            {
                ["certificateFile"] = _mainForm.textBoxCertificate.Text,
                ["lastName"] = _mainForm.AbonentLastName.Text,
                ["firstName"] = _mainForm.AbonentFirstName.Text,
                ["middleName"] = _mainForm.AbonentMiddleName.Text,
                ["signatureType"] = "CMS_TIMESTAMP"
            };

            // AbonentUserId не обязателен
            if (!string.IsNullOrWhiteSpace(_mainForm.AbonentUserId.Text))
                Params["userId"] = _mainForm.AbonentUserId.Text;
            
            return await RequestSender(url, Params, access_token, "сертификата"); ;
        }

        // Отправка ФНС сигнатуры в БД
        public async Task<HttpResponseMessage> FnsSignature(string access_token)
        {
            string url = $"https://operator-epd-reg-server.production.kube.{enviroment}/v1/certificate-pub-key";

            Dictionary<string, object> Params = new Dictionary<string, object>
            {
                ["userId"] = _mainForm.AbonentUserId.Text,
                ["lastName"] = _mainForm.AbonentLastName.Text,
                ["firstName"] = _mainForm.AbonentFirstName.Text,
                ["middleName"] = _mainForm.AbonentMiddleName.Text,
                ["phone"] = _mainForm.AbonentPhone.Text      
            };

            if(string.IsNullOrEmpty(_mainForm.SignatureRelations.Text))
                Params["signatureRelationId"] = _mainForm.SignatureRelations.Text;

            return await RequestSender(url, Params, access_token, "ФНС сигнатуры");
        }


        public async Task<HttpResponseMessage> GoskeySignature(string access_token)
        {
            string url = $"https://operator-epd-reg-server.production.kube.{enviroment}/v1/";
            Dictionary<string, object> Params = new Dictionary<string, object>
            {
                [""] = ""
            };

            return await RequestSender(url, Params, access_token, "сигнатуры Госключ"); ;
        }

        // Отправка МЧД в БД
        public async Task<HttpResponseMessage> PowerOfAttorney(string access_token)
        {
            string url = $"https://operator-epd-reg-server.production.kube.{enviroment}/v1/power-of-attorney";

            Dictionary<string, object> Params = new Dictionary<string, object>
            {
                ["identifier"] = _mainForm.AbonentMCHDNumber.Text,
                ["date"] = _mainForm.AbonentMCHDDate.Text,
                ["expire"] = _mainForm.AbonentMCHDDateEnd.Text
            };

            // AbonentUserId не обязателен
            if (!string.IsNullOrWhiteSpace(_mainForm.AbonentUserId.Text))
                Params["userId"] = _mainForm.AbonentUserId.Text;

            return await RequestSender(url, Params, access_token, "доверенности");
        }

        // Получение QR
        internal async Task<HttpResponseMessage> getQR(string fileName)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.NotFound;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"http://10.240.36.15:8000/files/{fileName}");
                request.Headers.Add("X-API-Key", xapikey);
                response = await _client.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка получения QR-кода", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return response;
            }
        }

        // Отправка запроса на регистрацию
        internal async Task<HttpResponseMessage> SendRegistrationRequestNew(string access_token)
        {
            string url = $"https://operator-epd-reg-server.production.kube.{enviroment}/v1/registration";

            Dictionary < string, string> signature = new Dictionary<string, string>
            {
                ["myDss"] = "Токен",
                ["fns"] = "МобильнаяПодписьФНС",
                ["goskey"] = "Госключ"
            }; 

            Dictionary<string, object> abonentInfo = new Dictionary<string, object>
            {
                ["fullName"] = _mainForm.fullNameTextBox.Text,
                ["shortName"] = _mainForm.shortNameTextBox.Text,
                ["mail"] = _mainForm.textBoxEmail.Text,
                ["phone"] = _mainForm.textBoxPhone.Text
            };
            if (!string.IsNullOrWhiteSpace(_mainForm.KppInput.Text))
                abonentInfo["kpp"] = _mainForm.KppInput.Text;

            // Абонент
            Dictionary<string, object> Abonent = new Dictionary<string, object>
            {
                ["type"] = _mainForm.TypeOPFInput.Text,
                ["inn"] = _mainForm.InnInput.Text,
                ["ogrn"] = _mainForm.OgrnInput.Text,
                ["abonentCategory"] = "Налогоплательщик",
                ["abonentInfo"] = abonentInfo,
                ["legalAddress"] = new
                {
                    localityType = _mainForm.LegalAddressLocalityType.Text,
                    localityName = _mainForm.LegalAddressLocatityName.Text,
                    RegionCode = _mainForm.LegalAddressCodeOfRegion.Text
                },
                ["factAddress"] = new
                {
                    localityType = _mainForm.FactAddressLocalityType.Text,
                    localityName = _mainForm.FactAddressLocatityName.Text,
                    RegionCode = _mainForm.FactAddressCodeOfRegion.Text
                }
            };
            

            //Информация о владелеце ЭП
            Dictionary<string, object> ownerES = new Dictionary<string, object>()
            {
                ["userId"] = _mainForm.AbonentUserId.Text,
                ["lastName"] = _mainForm.AbonentLastName.Text,
                ["firstName"] = _mainForm.AbonentFirstName.Text,
                ["middleName"] = _mainForm.AbonentMiddleName.Text,
                ["inn"] = _mainForm.AbonentInn.Text
            };

            // Связь Абонента+Владельца+подписи
            Dictionary<string, object> signatureRelation = new Dictionary<string, object>
            {
                ["storageLocationEs"] = signature[_mainForm.SignatureRelations.SelectedTab.Name],
                ["mail"] = _mainForm.AbonentEmail.Text,
                ["phone"] = _mainForm.AbonentPhone.Text,
                ["prioritySignatureType"] = _mainForm.SignatureRelations.SelectedTab.Name
                
            };

            switch (_mainForm.SignatureRelations.SelectedTab.Name)
            {
                case "myDss":
                    signatureRelation["certificatePubKeyId"] = _mainForm.AbonentSertificateId.Text;
                    signatureRelation["signatureId"] = _mainForm.AbonentSertificateId.Text;
                    break;
                case "fns":
                    signatureRelation["fnsSignatureId"] = _mainForm.fnsId.Text;
                    signatureRelation["signatureId"] = _mainForm.AbonentSertificateId.Text;
                    break;
                case "goskey":
                    signatureRelation["goskeySignatureId"] = _mainForm.goskeyId.Text;
                    signatureRelation["signatureId"] = _mainForm.AbonentSertificateId.Text;
                    break;
            }

            // id абонента не обязательно
            if (!string.IsNullOrWhiteSpace(_mainForm.AbonentUserId.Text))
                signatureRelation["userId"] = _mainForm.AbonentUserId.Text;

            // Если есть МЧД - добавляем в запрос
            if (!string.IsNullOrWhiteSpace(_mainForm.AbonentMchdId.Text))
                signatureRelation["powerOfAttorneyId"] = _mainForm.AbonentMchdId.Text;

            // Если есть должность - добавляем в запрос
            if (!string.IsNullOrWhiteSpace(_mainForm.AbonentPositionInpit.Text))
                signatureRelation["jobTitle"] = _mainForm.AbonentPositionInpit.Text;


            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                ["abonent"] = Abonent,
                ["ownerES"] = ownerES,
                ["signatureRelation"] = signatureRelation
            };

            HttpResponseMessage resp = await RequestSender(url, requestBody, access_token, "данных на регистрацию");

            if(resp.IsSuccessStatusCode)
                MessageBox.Show(resp.Content.ReadAsStringAsync().Result, "Запрос отправлен", MessageBoxButtons.OK);

            _mainForm.textBoxResponse.Text = requestBody.ToString();

            return resp;
        }

        private async Task<HttpResponseMessage> RequestSender(string url, Dictionary<string, object> Params, string access_token, string comment)
        {
            // Удаляем существующий заголовок, если он есть
            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");

            string jsonData = JsonSerializer.Serialize(Params);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.NotFound;

            try
            {
                response = await _client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Произошла ошибка при загрузке {comment}: {response.Content.ReadAsStringAsync().Result}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при загрузке {comment}: {ex.Message}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return response;
        }
    }
}
