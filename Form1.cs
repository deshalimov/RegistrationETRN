using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegistrationETRN
{
    public partial class Form1 : Form
    {
        private readonly UrlQuerries urlQueries;
        private readonly HelpElements helpElements;

        private readonly string loginadmin;
        private readonly string PasswordAdmin;

        public Form1()
        {
            InitializeComponent();

            string configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "secrets.json"
                );

            if (!File.Exists(configPath))
            {
                MessageBox.Show(
                    "Не найден файл secrets.json!\nПроверьте настройки приложения.",
                    "Ошибка конфигурации",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Application.Exit();
                return;
            }

            string json = File.ReadAllText(configPath);

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                loginadmin = doc.RootElement
                    .GetProperty("LoginAdmin")
                    .GetString();

                PasswordAdmin = doc.RootElement
                    .GetProperty("PasswordAdmin")
                    .GetString();
            }

            urlQueries = new UrlQuerries(this);
            helpElements = new HelpElements(this);
            AbonentMCHDDate.Value = DateTime.Now;
            AbonentMCHDDate.MinDate = DateTime.Now.AddYears(-1);
            AbonentMCHDDate.MaxDate = DateTime.Now.AddYears(+1);

            AbonentMCHDDateEnd.Value = DateTime.Now.AddYears(+1);
            AbonentMCHDDate.MinDate = DateTime.Now;
            AbonentMCHDDate.MaxDate = DateTime.Now.AddYears(+5);

            // ФНС
            /*AbonentFnsDateEnd.Value = DateTime.Now.AddYears(+1);
            AbonentFnsDateEnd.MinDate = DateTime.Now;
            AbonentFnsDateEnd.MaxDate = DateTime.Now.AddYears(+5);
            AbonentFnsDateEnd.Format = DateTimePickerFormat.Custom;
            */
            AbonentFnsDateEnd.CustomFormat = " ";
            
            // Госключ
            AbonentSnilsDateEnd.Value = DateTime.Now.AddYears(+1);
            AbonentSnilsDateEnd.MinDate = DateTime.Now;
            AbonentSnilsDateEnd.MaxDate = DateTime.Now.AddYears(+5);
            AbonentSnilsDateEnd.CustomFormat = " ";
        }


        // Кнопка прикрепления сертификата
        private void GetSertificateButtonClick(object sender, EventArgs e)
        {
            // открыть диалоговое окно для получения расположения файла
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Файлы сертификатов (*.cer)|*.cer";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                // если в диалоговом выбран файл
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Получить расположение файла
                    string fileName = openFileDialog.FileName;
                    byte[] file = File.ReadAllBytes(fileName);
                    // Конвертация файла
                    textBoxCertificate.Text = Convert.ToBase64String(file);
                }
            }
        }

        // Кнопка отправки данных на регистрацию
        private async void SendButton_Click(object sender, EventArgs e)
        {
            if (helpElements.IsFormValid())
            {
                //Авторизация
                string access_token = "";
                HttpResponseMessage response = await urlQueries.Auth(loginadmin, PasswordAdmin, "eq-admin-panel");
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Получение токена пользователя
                        string resAuth = response.Content.ReadAsStringAsync().Result;
                        JsonElement resp = JsonDocument.Parse(resAuth).RootElement;
                        access_token = resp.GetProperty("access_token").ToString();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла ошибка получения авторизационных данных. {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else return;

                // Если id МЧД не заполнен - отправить запрос на добавление его в БД
                if (isMCHD.Checked && string.IsNullOrEmpty(AbonentMchdId.Text) && !string.IsNullOrEmpty(AbonentMCHDNumber.Text))
                {
                    // Отправка запроса на добавление МЧД
                    HttpResponseMessage respPOA = await urlQueries.PowerOfAttorney(access_token);
                    if (respPOA.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Получение id созданного МЧД
                            string resPOA = respPOA.Content.ReadAsStringAsync().Result;
                            JsonElement idCert = JsonDocument.Parse(resPOA).RootElement;
                            AbonentMchdId.Text = idCert.GetProperty("id").ToString();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Произошла ошибка получения id МЧД: {ex.Message}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else return;
                }

                // Добавление сертификата для myDSS
                if (SignatureRelations.SelectedTab.Name == "myDss")
                {
                    // Если id сертификата не заполнен - отправить запрос на добавление его в БД
                    if (string.IsNullOrEmpty(AbonentSertificateId.Text) && !string.IsNullOrEmpty(textBoxCertificate.Text))
                    {
                        // отправка запроса на создание файла с сертификатом
                        HttpResponseMessage respCert = await urlQueries.CertificatePubKey(access_token);
                        if (respCert.IsSuccessStatusCode)
                        {
                            try
                            {
                                // Получение id созданного сертификата
                                string resCert = respCert.Content.ReadAsStringAsync().Result;
                                JsonElement idCert = JsonDocument.Parse(resCert).RootElement;
                                AbonentSertificateId.Text = idCert.GetProperty("id").ToString();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Произошла ошибка получения id сертификата: {ex.Message}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else return;
                    }
                }

                // Добавление ФНС сигнатуры
                if (SignatureRelations.SelectedTab.Name == "fns")
                {
                    // Если id сертификата не заполнен - отправить запрос на добавление его в БД
                    if (string.IsNullOrEmpty(fnsId.Text))
                    {
                        // отправка запроса на создание файла с сертификатом
                        HttpResponseMessage respFNS = await urlQueries.FnsSignature(access_token);
                        if (respFNS.IsSuccessStatusCode)
                        {
                            try
                            {
                                // Получение id созданного сертификата
                                string resFns = respFNS.Content.ReadAsStringAsync().Result;
                                JsonElement idFns = JsonDocument.Parse(resFns).RootElement;
                                fnsId.Text = idFns.GetProperty("id").ToString();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Произошла ошибка получения id ФНС сигнатуры: {ex.Message}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else return;
                    }
                }

                // Добавление госключ сигнатуры
                if (SignatureRelations.SelectedTab.Name == "goskey")
                {
                    // Если id сертификата не заполнен - отправить запрос на добавление его в БД
                    if (string.IsNullOrEmpty(fnsId.Text))
                    {
                        // отправка запроса на создание файла с сертификатом
                        HttpResponseMessage respGoskey = await urlQueries.GoskeySignature(access_token);
                        if (respGoskey.IsSuccessStatusCode)
                        {
                            try
                            {
                                // Получение id созданного сертификата
                                string resGoskey = respGoskey.Content.ReadAsStringAsync().Result;
                                JsonElement idGoskey = JsonDocument.Parse(resGoskey).RootElement;
                                goskeyId.Text = idGoskey.GetProperty("id").ToString();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Произошла ошибка получения id сигнатуры госключа: {ex.Message}. Попробуйте включить доменный VPN", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else return;
                    }
                }


                // Отправить запрос на регистрацию
                HttpResponseMessage respReg = await urlQueries.SendRegistrationRequestNew(access_token);
                //textBoxResponse.Text = respReg.Content.ReadAsStringAsync().Result;
            }
        }

        // Получение данных из админки
        private async void ButtonSendInAdmin_Click(object sender, EventArgs e)
        {
            helpElements.CleanAll();
            isMCHD.Checked = false;

            if (IdUser.Text.Length > 0)
            {
                //Авторизация
                string access_token = "";
                HttpResponseMessage response = await urlQueries.Auth(loginadmin, PasswordAdmin, "eq-admin-panel");
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Получение токена пользователя
                        string resAuth = await response.Content.ReadAsStringAsync();
                        using( JsonDocument doc = JsonDocument.Parse(resAuth))
                        {
                            access_token = doc.RootElement.GetProperty("access_token").GetString();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла ошибка получения авторизационных данных. {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else return;

                // Получение данных из Админки
                HttpResponseMessage GetProfileInformations = await urlQueries.GetProfileDetails(access_token, IdUser.Text);
                if (GetProfileInformations == null) return;
                if (GetProfileInformations.IsSuccessStatusCode)
                {
                    // Заполнить данные из Админки
                    helpElements.ZapolnitDannie(GetProfileInformations);
                }
            }
        }



        private void ValidationInn(object sender, KeyEventArgs e)
        {
            if (InnInput.Text.Length == 10)
                TypeOPFInput.SelectedIndex = 1;
            else if (InnInput.Text.Length == 12)
                TypeOPFInput.SelectedIndex = 2;
            else TypeOPFInput.SelectedIndex = 0;
        }

        private void TypeOpfChanged(object sender, EventArgs e)
        {
            KppInput.Visible = TypeOPFInput.SelectedIndex == 1;
            labelKpp.Visible = TypeOPFInput.SelectedIndex == 1;

            AbonentPositionInpit.Visible = TypeOPFInput.SelectedIndex == 1;
            labelPosition.Visible = TypeOPFInput.SelectedIndex == 1;
        }








        private void ButtonCleanAll_Click(object sender, EventArgs e)
        {
            helpElements.BlockElements(false);
            SendButtonLabel.Text = "";
            IdUser.Text = "";
            helpElements.CleanAll();
        }



        private void CheckMCHD(object sender, EventArgs e)
        {
            labelMCHDNumber.Visible = ((CheckBox)sender).Checked;
            labelMCHDDate.Visible = ((CheckBox)sender).Checked;
            AbonentMCHDDate.Visible = ((CheckBox)sender).Checked;
            AbonentMCHDNumber.Visible = ((CheckBox)sender).Checked;
            AbonentMchdIdLabel.Visible = ((CheckBox)sender).Checked;
            AbonentMchdId.Visible = ((CheckBox)sender).Checked;
            labelMCHDDateEnd.Visible = ((CheckBox)sender).Checked;
            AbonentMCHDDateEnd.Visible = ((CheckBox)sender).Checked;
            helpElements.CleanAbonent();

        }

        private void AbonentFnsDateEndValueChanged(object sender, EventArgs e)
        {
            //AbonentFnsDateEnd.Format = DateTimePickerFormat.Long;
            textBoxResponse.Text = ((TextBox)sender).Text;
        }

        // загрузить QR код
        private async void downloadQR_Click(object sender, EventArgs e)
        {
            HttpResponseMessage response = await urlQueries.getQR(textBoxFileQR.Text);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Получение токена пользователя
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    // Проверяем сигнатуру GIF
                    if (imageBytes.Length < 6 ||
                        System.Text.Encoding.ASCII.GetString(imageBytes, 0, 6) != "GIF89a")
                    {
                        MessageBox.Show("Полученные данные не являются GIF изображением",
                                      "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Диалог сохранения
                    using (SaveFileDialog dialog = new SaveFileDialog())
                    {
                        dialog.Filter = "GIF файлы (*.gif)|*.gif";
                        dialog.DefaultExt = "gif";
                        dialog.FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.gif";

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllBytes(dialog.FileName, imageBytes);
                            MessageBox.Show($"GIF успешно сохранен!\nПуть: {dialog.FileName}",
                                          "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка получения QR кода. {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else return;
            
        }
        public static async Task SaveGifFromResponse(string apiUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Получаем бинарные данные (НЕ base64)
                    byte[] imageBytes = await client.GetByteArrayAsync(apiUrl);

                    // Проверяем сигнатуру GIF
                    string signature = System.Text.Encoding.ASCII.GetString(imageBytes, 0, 6);
                    if (signature != "GIF89a" && signature != "GIF87a")
                    {
                        MessageBox.Show("Полученные данные не являются GIF", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Диалог сохранения
                    using (SaveFileDialog dialog = new SaveFileDialog())
                    {
                        dialog.Filter = "GIF файлы (*.gif)|*.gif";
                        dialog.DefaultExt = "gif";
                        dialog.FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.gif";

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // Сохраняем бинарные данные
                            File.WriteAllBytes(dialog.FileName, imageBytes);
                            MessageBox.Show($"Сохранено: {dialog.FileName}", "Успех");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}