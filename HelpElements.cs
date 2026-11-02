using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace RegistrationETRN
{
    internal class HelpElements
    {
        private readonly Form1 _mainForm;

        public HelpElements(Form1 mainForm)
        {
            _mainForm = mainForm;
        }

        // Очистить данные абонента
        public void CleanAbonent()
        {
            _mainForm.AbonentUserId.Text = "";
            _mainForm.AbonentLastName.Text = "";
            _mainForm.AbonentFirstName.Text = "";
            _mainForm.AbonentMiddleName.Text = "";
            _mainForm.AbonentPhone.Text = "";
            _mainForm.AbonentEmail.Text = "";
            _mainForm.AbonentInn.Text = "";
            _mainForm.AbonentPositionInpit.Text = "";
        }

        // Очистить все
        public void CleanAll()
        {
            _mainForm.textBoxFileQR.Text = "";
            _mainForm.shortNameTextBox.Text = "";
            _mainForm.fullNameTextBox.Text = "";
            _mainForm.InnInput.Text = "";
            _mainForm.OgrnInput.Text = "";
            _mainForm.KppInput.Text = "";
            _mainForm.TypeOPFInput.SelectedIndex = 0;
            _mainForm.textBoxCertificate.Text = "";
            _mainForm.textBoxPhone.Text = "";
            _mainForm.textBoxEmail.Text = "";
            _mainForm.LegalAddressLocalityType.SelectedIndex = 0;
            _mainForm.LegalAddressLocatityName.Text = "";
            _mainForm.LegalAddressCodeOfRegion.Text = "";
            _mainForm.FactAddressLocalityType.SelectedIndex = 0;
            _mainForm.FactAddressLocatityName.Text = "";
            _mainForm.FactAddressCodeOfRegion.Text = "";
            _mainForm.AbonentSertificateId.Text = "";
            _mainForm.AbonentMCHDNumber.Text = "";
            _mainForm.AbonentMCHDDate.Text = "";
            _mainForm.isMCHD.Checked = false;

            CleanAbonent();
        }

        // Все ли поля заполнены
        public bool IsFormValid()
        {
            // Проверка текстовых полей
            (TextBox control, string name, bool required)[] textBoxes = new (TextBox control, string name, bool required)[]
            {
                (_mainForm.shortNameTextBox, "Краткое наименование", true),
                (_mainForm.fullNameTextBox, "Полное наименование", true),
                (_mainForm.InnInput, "ИНН", true),
                (_mainForm.OgrnInput, "ОГРН", true),
                (_mainForm.KppInput, "КПП", true),
                
                // myDSS
                (_mainForm.AbonentSertificateId, "Id сертификата в Smartseeds", 
                   string.IsNullOrWhiteSpace(_mainForm.textBoxCertificate.Text)
                   && _mainForm.SignatureRelations.SelectedTab.Name == "myDss"),
                (_mainForm.textBoxCertificate, "Сертификат", 
                    string.IsNullOrWhiteSpace(_mainForm.AbonentSertificateId.Text)
                    && _mainForm.SignatureRelations.SelectedTab.Name == "myDss"),
                
                // ФНС
                (_mainForm.fnsId, "Id записи ФНС в Smartseeds",
                   string.IsNullOrWhiteSpace(_mainForm.fnsId.Text)
                   && _mainForm.SignatureRelations.SelectedTab.Name == "fns"),
                
                // Госключ
                (_mainForm.goskeyId, "Id записи Госключ в Smartseeds",
                   string.IsNullOrWhiteSpace(_mainForm.goskeyId.Text)
                   && _mainForm.SignatureRelations.SelectedTab.Name == "goskey"),

                (_mainForm.textBoxPhone, "Телефон", true),
                (_mainForm.textBoxEmail, "Email", true),
                (_mainForm.LegalAddressLocatityName, "Населенный пункт (юр. адрес)", true),
                (_mainForm.LegalAddressCodeOfRegion, "Код региона (юр. адрес)", true),
                (_mainForm.FactAddressLocatityName, "Населенный пункт (факт. адрес)", true),
                (_mainForm.FactAddressCodeOfRegion, "Код региона (факт. адрес)", true),
                (_mainForm.AbonentLastName, "Фамилия абонента", true),
                (_mainForm.AbonentFirstName, "Имя абонента", true),
                (_mainForm.AbonentMiddleName, "Отчество абонента", true),
                (_mainForm.AbonentPositionInpit, "Должность абонента", true),
                (_mainForm.AbonentPhone, "Телефон абонента", true),
                (_mainForm.AbonentEmail, "Email абонента", true),
                (_mainForm.AbonentInn, "ИНН абонента", true),
                (_mainForm.AbonentMCHDNumber, "Номер МЧД абонента", true)
            };

            List<string> errors = new List<string>();

            foreach ((TextBox control, string name, bool required) in textBoxes)
            {
                if (required && string.IsNullOrWhiteSpace(control.Text) && control.Visible)
                {
                    errors.Add($"• {name}");
                    control.BackColor = Color.LightPink;
                }
                else
                {
                    control.BackColor = Color.White;
                }
            }

            // Проверка ComboBox
            (ComboBox control, string name)[] comboBoxes = new (ComboBox control, string name)[]
            {
                (_mainForm.TypeOPFInput, "Тип ОПФ"),
                (_mainForm.LegalAddressLocalityType, "Тип населенного пункта (юр. адрес)"),
                (_mainForm.FactAddressLocalityType, "Тип населенного пункта (факт. адрес)")
            };

            foreach ((ComboBox control, string name) in comboBoxes)
            {
                if (control.SelectedIndex <= 0)
                {
                    errors.Add($"• {name}");
                    control.BackColor = Color.LightPink;
                }
                else
                {
                    control.BackColor = Color.White;
                }
            }

            if (errors.Any())
            {
                MessageBox.Show($"Пожалуйста, заполните следующие поля:\n{string.Join("\n", errors)}",
                                "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        // заполнение данных после получения их в админке
        public void ZapolnitDannie(HttpResponseMessage respInformationsDetails)
        {
            try
            {
                JsonElement InformationsDetails = JsonDocument.Parse(respInformationsDetails.Content.ReadAsStringAsync().Result).RootElement;
                JsonElement ProfileDetails = InformationsDetails.GetProperty("profileDetails");

                // Заполнить данные организации
                _mainForm.InnInput.Text = ProfileDetails.GetProperty("individualTaxpayerNumber").ToString();
                _mainForm.OgrnInput.Text = ProfileDetails.GetProperty("primaryStateRegistrationNumberOfSoleProprietor").ToString() == ""
                    ? ProfileDetails.GetProperty("primaryStateRegistrationNumber").ToString()
                    : ProfileDetails.GetProperty("primaryStateRegistrationNumberOfSoleProprietor").ToString();
                _mainForm.shortNameTextBox.Text = ProfileDetails.GetProperty("organizationName").ToString();
                _mainForm.fullNameTextBox.Text = ProfileDetails.GetProperty("organizationFullName").ToString();
                _mainForm.KppInput.Text = ProfileDetails.GetProperty("industrialEnterprisesClassifier").ToString();
                _mainForm.AbonentPositionInpit.Text = ProfileDetails.GetProperty("position").ToString();
                _mainForm.TypeOPFInput.SelectedIndex = ProfileDetails.GetProperty("profileType").ToString() == "company" ? 1 : 2;
                _mainForm.textBoxPhone.Text = InformationsDetails.GetProperty("phoneNumber").ToString().Replace("+", "");
                _mainForm.textBoxEmail.Text = InformationsDetails.GetProperty("email").ToString();

                // Фактический адрес
                string fact = ProfileDetails.GetProperty("actualAddress").ToString();
                AddressParser.AddressResult fact_address = AddressParser.Parse(fact);
                _mainForm.FactAddressCodeOfRegion.Text = fact_address.RegionCode;
                _mainForm.FactAddressLocatityName.Text = fact_address.LocalityName;
                _mainForm.FactAddressLocalityType.SelectedIndex = fact_address.LocalityTypeIndex;

                // Юридический адрес
                string legal = ProfileDetails.GetProperty("legalAddress").ToString();
                AddressParser.AddressResult legal_address = (string.IsNullOrEmpty(legal)) ? AddressParser.Parse(fact) : AddressParser.Parse(legal);
                _mainForm.LegalAddressCodeOfRegion.Text = legal_address.RegionCode;
                _mainForm.LegalAddressLocatityName.Text = legal_address.LocalityName;
                _mainForm.LegalAddressLocalityType.SelectedIndex = legal_address.LocalityTypeIndex;

                // Заполнить данные владельца ЭП
                _mainForm.AbonentUserId.Text = _mainForm.IdUser.Text;
                string fullName = ProfileDetails.GetProperty("ownerFullName").ToString();
                string[] parts = fullName.Split(' ');
                _mainForm.AbonentLastName.Text = parts.Length > 0 ? parts[0] : "";
                _mainForm.AbonentFirstName.Text = parts.Length > 1 ? parts[1] : "";
                _mainForm.AbonentMiddleName.Text = parts.Length > 2 ? parts[2] : "";
                _mainForm.AbonentPhone.Text = InformationsDetails.GetProperty("phoneNumber").ToString().Replace("+", "");
                _mainForm.AbonentEmail.Text = InformationsDetails.GetProperty("email").ToString();

                _mainForm.AbonentInn.Text = ProfileDetails.GetProperty("profileType").ToString() == "entrepreneur"
                    ? ProfileDetails.GetProperty("individualTaxpayerNumber").ToString()
                    : "";
            }
            catch
            {
                MessageBox.Show("Ошибка получения данных профиля из Админки", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // блокировка элементов ввода
        public void BlockElements(bool val)
        {
            _mainForm.IdUser.ReadOnly = val;
            _mainForm.InnInput.ReadOnly = val;
            _mainForm.OgrnInput.ReadOnly = val;
            _mainForm.shortNameTextBox.ReadOnly = val;
            _mainForm.fullNameTextBox.ReadOnly = val;
            _mainForm.KppInput.ReadOnly = val;
            _mainForm.textBoxPhone.ReadOnly = val;
            _mainForm.textBoxEmail.ReadOnly = val;
            _mainForm.AbonentLastName.ReadOnly = val;
            _mainForm.AbonentFirstName.ReadOnly = val;
            _mainForm.AbonentMiddleName.ReadOnly = val;
            _mainForm.AbonentPhone.ReadOnly = val;
            _mainForm.AbonentEmail.ReadOnly = val;
            _mainForm.AbonentInn.ReadOnly = val;
            _mainForm.textBoxCertificate.ReadOnly = val;
            _mainForm.AbonentMCHDNumber.ReadOnly = val;

            _mainForm.SendButton.Enabled = !val;
            _mainForm.buttonSendInAdmin.Enabled = !val;
            _mainForm.addSertificateButton.Enabled = !val;

        }

        // Вывод отправленных данных
        public void OutputRequest(HttpResponseMessage resp, StringContent content)
        {
            HttpRequestMessage requestMessage = resp.RequestMessage;
            _mainForm.textBoxResponse.Text = $"URL: {requestMessage.RequestUri}\r\n" +
              $"Method: {requestMessage.Method}\r\n" +
              $"Headers: {string.Join("\r\n", requestMessage.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}"))}\r\n" +
              $"Body: {content}";
        }
    }
}
