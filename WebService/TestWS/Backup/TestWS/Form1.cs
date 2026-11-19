using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestWS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ServiceReference1.Service1SoapClient loSC = null;
            loSC = new ServiceReference1.Service1SoapClient();
            listBox1.Items.Add("Metodo Server Status: " + loSC.ServerStatus());
            listBox1.Items.Add("Metodos Server Date: " + loSC.ServerDate());
            listBox1.Items.Add("Metodos Generar Incidencia: " + loSC.IncidenceGenerate("1","PEREZ","JUAN","REAL 1000","LUJAN")); 
        }
    }
}
