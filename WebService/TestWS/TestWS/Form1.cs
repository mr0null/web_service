using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.ServiceModel;
using System.Windows.Forms;

namespace TestWS
{
    public partial class Form1 : Form
    {
        private TextBox dniTextBox;
        private TextBox surnameTextBox;
        private TextBox namesTextBox;
        private TextBox addressTextBox;
        private TextBox localityTextBox;
        private ComboBox motiveComboBox;
        private TextBox observationsTextBox;
        private DataGridView resultsGrid;
        private Label serverStatusLabel;

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
        }

        private void BuildInterface()
        {
            button1.Visible = false;
            listBox1.Visible = false;
            Text = "Gestión remota de incidencias";
            ClientSize = new Size(920, 600);
            MinimumSize = new Size(760, 500);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.TabPages.Add(CreateRegistrationTab());
            tabs.TabPages.Add(CreateQueriesTab());
            tabs.TabPages.Add(CreateStatusTab());
            Controls.Add(tabs);
        }

        private TabPage CreateRegistrationTab()
        {
            TabPage page = new TabPage("Alta de cliente / incidencia");
            FlowLayoutPanel content = new FlowLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.FlowDirection = FlowDirection.TopDown;
            content.WrapContents = false;
            content.AutoScroll = true;
            content.Padding = new Padding(12);

            GroupBox serverGroup = new GroupBox();
            serverGroup.Text = "Servidor SOAP";
            serverGroup.Width = 850;
            serverGroup.Height = 75;
            serverStatusLabel = new Label();
            serverStatusLabel.AutoSize = true;
            serverStatusLabel.Location = new Point(14, 33);
            serverStatusLabel.Text = "Sin verificar";
            Button checkServerButton = new Button();
            checkServerButton.Text = "Verificar servidor";
            checkServerButton.Location = new Point(650, 26);
            checkServerButton.Click += CheckServer;
            serverGroup.Controls.Add(serverStatusLabel);
            serverGroup.Controls.Add(checkServerButton);
            content.Controls.Add(serverGroup);

            GroupBox clientGroup = new GroupBox();
            clientGroup.Text = "Datos del cliente y de la incidencia";
            clientGroup.Width = 850;
            clientGroup.Height = 390;

            TableLayoutPanel fields = new TableLayoutPanel();
            fields.Dock = DockStyle.Top;
            fields.Padding = new Padding(12);
            fields.Height = 315;
            fields.ColumnCount = 2;
            fields.RowCount = 7;
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dniTextBox = CreateTextBox(18);
            surnameTextBox = CreateTextBox(30);
            namesTextBox = CreateTextBox(50);
            addressTextBox = CreateTextBox(100);
            localityTextBox = CreateTextBox(50);
            motiveComboBox = new ComboBox();
            motiveComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            motiveComboBox.Items.AddRange(new object[] { "RECLAMO", "PEDIDO DE INFO", "DENUNCIA", "OTRO" });
            motiveComboBox.SelectedIndex = 0;
            observationsTextBox = CreateTextBox(512);
            observationsTextBox.Multiline = true;
            observationsTextBox.Height = 60;
            AddInputRow(fields, 0, "DNI", dniTextBox, 38);
            AddInputRow(fields, 1, "Apellido", surnameTextBox, 38);
            AddInputRow(fields, 2, "Nombres", namesTextBox, 38);
            AddInputRow(fields, 3, "Dirección", addressTextBox, 38);
            AddInputRow(fields, 4, "Localidad", localityTextBox, 38);
            AddInputRow(fields, 5, "Motivo", motiveComboBox, 38);
            AddInputRow(fields, 6, "Observaciones", observationsTextBox, 74);
            clientGroup.Controls.Add(fields);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 50;
            actions.Padding = new Padding(12, 4, 12, 4);
            Button saveClientButton = new Button();
            saveClientButton.Text = "Guardar / actualizar cliente";
            saveClientButton.AutoSize = true;
            saveClientButton.Click += SaveClient;
            Button generateButton = new Button();
            generateButton.Text = "Generar incidencia";
            generateButton.AutoSize = true;
            generateButton.Click += GenerateIncidence;
            actions.Controls.Add(saveClientButton);
            actions.Controls.Add(generateButton);
            clientGroup.Controls.Add(actions);
            content.Controls.Add(clientGroup);
            page.Controls.Add(content);
            return page;
        }

        private TabPage CreateQueriesTab()
        {
            TabPage page = new TabPage("Consultas");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 175));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel filters = new FlowLayoutPanel();
            filters.Dock = DockStyle.Fill;
            filters.WrapContents = true;
            filters.Padding = new Padding(8);

            GroupBox numberGroup = new GroupBox();
            numberGroup.Text = "Por número de incidencia";
            numberGroup.Width = 280;
            numberGroup.Height = 145;
            TextBox incidenceNumber = CreateTextBox(18);
            incidenceNumber.Location = new Point(12, 42);
            incidenceNumber.Width = 245;
            Button queryStatusButton = new Button();
            queryStatusButton.Text = "Consultar estado";
            queryStatusButton.Location = new Point(12, 82);
            queryStatusButton.Click += delegate { QueryStatus(incidenceNumber.Text); };
            Button queryIncidenceButton = new Button();
            queryIncidenceButton.Text = "Ver datos";
            queryIncidenceButton.Location = new Point(145, 82);
            queryIncidenceButton.Click += delegate { QueryIncidence(incidenceNumber.Text); };
            numberGroup.Controls.Add(incidenceNumber);
            numberGroup.Controls.Add(queryStatusButton);
            numberGroup.Controls.Add(queryIncidenceButton);
            filters.Controls.Add(numberGroup);

            GroupBox dateGroup = new GroupBox();
            dateGroup.Text = "Por fecha (yyyy/MM/dd)";
            dateGroup.Width = 285;
            dateGroup.Height = 145;
            TextBox fromDate = CreateTextBox(10);
            fromDate.Location = new Point(12, 36);
            fromDate.Width = 120;
            fromDate.Text = DateTime.Today.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            TextBox toDate = CreateTextBox(10);
            toDate.Location = new Point(145, 36);
            toDate.Width = 120;
            toDate.Text = DateTime.Today.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            Label fromLabel = new Label();
            fromLabel.Text = "Desde";
            fromLabel.Location = new Point(12, 22);
            Label toLabel = new Label();
            toLabel.Text = "Hasta";
            toLabel.Location = new Point(145, 22);
            Button queryDatesButton = new Button();
            queryDatesButton.Text = "Buscar incidencias";
            queryDatesButton.Location = new Point(12, 82);
            queryDatesButton.Click += delegate { QueryByDate(fromDate.Text, toDate.Text); };
            dateGroup.Controls.Add(fromLabel);
            dateGroup.Controls.Add(toLabel);
            dateGroup.Controls.Add(fromDate);
            dateGroup.Controls.Add(toDate);
            dateGroup.Controls.Add(queryDatesButton);
            filters.Controls.Add(dateGroup);

            GroupBox clientGroup = new GroupBox();
            clientGroup.Text = "Por DNI de cliente";
            clientGroup.Width = 220;
            clientGroup.Height = 145;
            TextBox clientDni = CreateTextBox(18);
            clientDni.Location = new Point(12, 42);
            clientDni.Width = 190;
            Button queryClientButton = new Button();
            queryClientButton.Text = "Buscar incidencias";
            queryClientButton.Location = new Point(12, 82);
            queryClientButton.Click += delegate { QueryByClient(clientDni.Text); };
            clientGroup.Controls.Add(clientDni);
            clientGroup.Controls.Add(queryClientButton);
            filters.Controls.Add(clientGroup);

            resultsGrid = new DataGridView();
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            layout.Controls.Add(filters, 0, 0);
            layout.Controls.Add(resultsGrid, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateStatusTab()
        {
            TabPage page = new TabPage("Cambio de estado");
            GroupBox group = new GroupBox();
            group.Text = "Actualizar estado de una incidencia";
            group.Location = new Point(20, 20);
            group.Size = new Size(580, 180);

            Label numberLabel = new Label();
            numberLabel.Text = "Número de incidencia";
            numberLabel.Location = new Point(18, 38);
            numberLabel.AutoSize = true;
            TextBox number = CreateTextBox(18);
            number.Location = new Point(180, 34);
            number.Width = 300;

            Label statusLabel = new Label();
            statusLabel.Text = "Nuevo estado";
            statusLabel.Location = new Point(18, 78);
            statusLabel.AutoSize = true;
            ComboBox status = new ComboBox();
            status.DropDownStyle = ComboBoxStyle.DropDownList;
            status.Location = new Point(180, 74);
            status.Width = 300;
            status.Items.AddRange(new object[] { "DESPACHADO", "SOLUCIONADO", "CERRADO", "ANULADO" });
            status.SelectedIndex = 0;

            Button updateButton = new Button();
            updateButton.Text = "Cambiar estado";
            updateButton.Location = new Point(180, 118);
            updateButton.Click += delegate { ChangeStatus(number.Text, Convert.ToString(status.SelectedItem)); };
            group.Controls.Add(numberLabel);
            group.Controls.Add(number);
            group.Controls.Add(statusLabel);
            group.Controls.Add(status);
            group.Controls.Add(updateButton);
            page.Controls.Add(group);
            return page;
        }

        private static TextBox CreateTextBox(int maxLength)
        {
            TextBox textBox = new TextBox();
            textBox.MaxLength = maxLength;
            return textBox;
        }

        private static void AddInputRow(TableLayoutPanel table, int row, string labelText, Control input, int height)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Label label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            input.Dock = DockStyle.Fill;
            table.Controls.Add(label, 0, row);
            table.Controls.Add(input, 1, row);
        }

        private void CheckServer(object sender, EventArgs e)
        {
            try
            {
                string[] status = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return new string[] { client.ServerStatus(), client.ServerDate() };
                });
                serverStatusLabel.Text = status[0] + " - " + status[1];
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void SaveClient(object sender, EventArgs e)
        {
            decimal dni;
            if (!TryParseDni(dniTextBox.Text, out dni))
            {
                ShowValidation("El DNI debe ser un número entero positivo de hasta 18 dígitos.");
                return;
            }

            try
            {
                string response = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.InsertClient(dni, surnameTextBox.Text, namesTextBox.Text,
                        addressTextBox.Text, localityTextBox.Text);
                });
                MessageBox.Show(this, response, "Alta de cliente", MessageBoxButtons.OK,
                    response == "OK" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void GenerateIncidence(object sender, EventArgs e)
        {
            decimal dni;
            if (!TryParseDni(dniTextBox.Text, out dni))
            {
                ShowValidation("El DNI debe ser un número entero positivo de hasta 18 dígitos.");
                return;
            }

            try
            {
                string saved = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.InsertClient(dni, surnameTextBox.Text, namesTextBox.Text,
                        addressTextBox.Text, localityTextBox.Text);
                });
                if (saved != "OK")
                {
                    MessageBox.Show(this, saved, "Alta de cliente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                long number = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.IncidenceGenerate(dni, Convert.ToString(motiveComboBox.SelectedItem),
                        observationsTextBox.Text);
                });
                string message = number > 0 ? "Número de incidencia: " + number.ToString(CultureInfo.InvariantCulture) :
                    number == 0 ? "No existe un cliente con ese DNI." :
                    number == -2 ? "El motivo no es válido. Seleccione un motivo de la lista." :
                    "No se pudo generar la incidencia. Consulte el log del servidor.";
                MessageBox.Show(this, message, "Generar incidencia", MessageBoxButtons.OK,
                    number > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void QueryStatus(string text)
        {
            long number;
            if (!TryParseIncidenceNumber(text, out number))
            {
                ShowValidation("Ingrese un número de incidencia válido.");
                return;
            }
            try
            {
                string status = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.QueryStatus(number);
                });
                MessageBox.Show(this, status, "Estado de incidencia", MessageBoxButtons.OK,
                    status == "ERROR" ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void QueryIncidence(string text)
        {
            long number;
            if (!TryParseIncidenceNumber(text, out number))
            {
                ShowValidation("Ingrese un número de incidencia válido.");
                return;
            }
            try
            {
                ServiceReference1.ClaseTipoIncidencia result =
                    UseService(delegate(ServiceReference1.Service1SoapClient client)
                    {
                        return client.QueryIncidence(number);
                    });
                if (result == null || result.Nro_Incidencia == -1)
                {
                    MessageBox.Show(this, "No se encontró la incidencia.", "Consulta", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string details = String.Format(CultureInfo.InvariantCulture,
                    "Nro: {0}\r\nDNI: {1}\r\nFecha: {2:yyyy/MM/dd HH:mm:ss}\r\nMotivo: {3}\r\nEstado: {4}\r\n" +
                    "Observaciones: {5}\r\nApellido: {6}\r\nNombres: {7}\r\nDirección: {8}\r\nLocalidad: {9}",
                    result.Nro_Incidencia, result.DNI, result.FechaHora, result.Motivo, result.Estado,
                    result.Observaciones, result.Apellido, result.Nombres, result.Direccion, result.Localidad);
                MessageBox.Show(this, details, "Datos de incidencia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void QueryByDate(string from, string to)
        {
            try
            {
                DataTable results = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.QueryIncidences(from, to);
                });
                resultsGrid.DataSource = results;
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void QueryByClient(string text)
        {
            decimal dni;
            if (!TryParseDni(text, out dni))
            {
                ShowValidation("El DNI debe ser un número entero positivo de hasta 18 dígitos.");
                return;
            }
            try
            {
                DataTable results = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.QueryIncidencesByClient(dni);
                });
                resultsGrid.DataSource = results;
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void ChangeStatus(string text, string newStatus)
        {
            long number;
            if (!TryParseIncidenceNumber(text, out number))
            {
                ShowValidation("Ingrese un número de incidencia válido.");
                return;
            }
            try
            {
                string response = UseService(delegate(ServiceReference1.Service1SoapClient client)
                {
                    return client.ChangeStatus(number, newStatus);
                });
                MessageBox.Show(this, response, "Cambio de estado", MessageBoxButtons.OK,
                    response == "OK" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private static bool TryParseDni(string text, out decimal dni)
        {
            return Decimal.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out dni) &&
                dni > 0 && dni <= 999999999999999999m;
        }

        private static bool TryParseIncidenceNumber(string text, out long number)
        {
            return Int64.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
        }

        private static T UseService<T>(Func<ServiceReference1.Service1SoapClient, T> action)
        {
            ServiceReference1.Service1SoapClient client = new ServiceReference1.Service1SoapClient();
            try
            {
                T result = action(client);
                client.Close();
                return result;
            }
            catch
            {
                client.Abort();
                throw;
            }
        }

        private void ShowValidation(string message)
        {
            MessageBox.Show(this, message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Error de comunicación SOAP",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CheckServer(sender, e);
        }
    }
}
