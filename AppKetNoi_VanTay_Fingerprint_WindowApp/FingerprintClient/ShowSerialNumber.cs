using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FingerPrintForm
{
    public partial class ShowSerialNumber : Form
    {
        public string serialNumber { get; set; }
        public ShowSerialNumber()
        {
            InitializeComponent();
        }

        private void ShowSerialNumber_Load(object sender, EventArgs e)
        {
            txtSerialNumber.Text = serialNumber;
        }
    }
}
