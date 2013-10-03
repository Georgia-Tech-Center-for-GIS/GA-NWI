using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GAWetlands
{
    public partial class Form2 : Form
    {
        public List<StatHelperClass> shc = null;

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            try
            {
                RunWorkerCompletedEventArgs rcea = new RunWorkerCompletedEventArgs(shc, null, false);
                
                if (shc == null || shc.Count == 0 || shc[0].count == 0) this.Close();

                CommonQueryForm.Completed(null, shc, dataGridView1, QueryForm_Click);
            }
            catch (Exception eee)
            {
            }
        }

        void QueryForm_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem tsi = (ToolStripItem)sender;
                CommonQueryForm.DoConversion(tsi, dataGridView1);
            }
            catch (Exception err)
            {
            }
            finally
            {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
