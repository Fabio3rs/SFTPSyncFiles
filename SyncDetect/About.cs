using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncDetect
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            abouttext.Text = "Programa desenvolvido por\nFábio Rossini Sluzala.\n\nEndereço do código fonte\ne mais informações:\nhttps://github.com/Fabio3rs/SFTPSyncFiles";
        }
    }
}
