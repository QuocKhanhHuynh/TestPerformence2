using System;
using System.Windows.Forms;

namespace TestPerformence
{
    public partial class FormBatchConfig : Form
    {
        public bool UseOpenVINO { get; private set; }

        public FormBatchConfig(bool currentUseOpenVINO)
        {
            InitializeComponent();
            chkUseOpenVINO.Checked = currentUseOpenVINO;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            UseOpenVINO = chkUseOpenVINO.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
