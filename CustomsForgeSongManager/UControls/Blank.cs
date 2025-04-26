using System.Windows.Forms;
using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;

//
// Docking.Fill causes screen flicker so only use if needed
//

namespace CustomsForgeSongManager.UControls
{
    public partial class Blank : UserControl
    {
        public Blank()
        {
            InitializeComponent();
            PopulateBlank(); // only done one time
        }

        public void PopulateBlank()
        {
            SMLog.Log("Populating (insert tab name here) GUI ...");
        }
    }
}