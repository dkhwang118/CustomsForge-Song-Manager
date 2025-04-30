using CustomsForgeSongManager.Workers;
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
        /// <summary>
        /// The main form control. Holder of the UI thread.
        /// </summary>
        private Control _mainControl = null;

        public frmProgressPanel(Control mainControl, BindingSource activeWorkerSource)
        {
            InitializeComponent();

            // Get the reference to the main form => need for UI thread referencing
            _mainControl = mainControl;

            // Set the binding source for our active process list
            listBox_ActiveProcesses.DisplayMember = "ProcessName";
            listBox_ActiveProcesses.DataSource = activeWorkerSource;

            // Name the window
            this.Text = "Application Paused! ==> Intensive processes currently running!";
            this.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Gets the Application's main form Control.
        /// </summary>
        /// <returns></returns>
        public Control GetMainControl() { return _mainControl; }

    }
}
