using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomsForgeSongManager.Forms
{
    public partial class frmProgressPanel : Form
    {
        public frmProgressPanel()
        {
            InitializeComponent();

            // Name the window
            this.Text = "Application Paused! ==> Intensive processes currently running!";
        }
    }
}
