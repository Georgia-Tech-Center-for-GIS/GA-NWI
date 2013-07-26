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
            RunWorkerCompletedEventArgs rcea = new RunWorkerCompletedEventArgs(shc, null, false);
            Completed(sender, rcea);
        }

        private int EnsureColumnsInDataGrid(StatHelperClass shc)
        {
            if (dataGridView1.Columns.Count == 0)
            {
                dataGridView1.Columns.Add("nm", "Statistic");
                dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            string col = shc.GetColumnHeadings();

            if (dataGridView1.Columns.Contains(col))
            {
                return dataGridView1.Columns[col].Index;
            }

            DataGridViewColumn dvc = new DataGridViewColumn();
            dvc.Name = col;
            dvc.HeaderText = col;
            dvc.SortMode = DataGridViewColumnSortMode.NotSortable;
            dvc.CellTemplate = new DataGridViewTextBoxCell();
            dvc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return dataGridView1.Columns.Add(dvc);
        }

        void QueryForm_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem tsi = (ToolStripItem)sender;

                ContextMenuStrip cms = (ContextMenuStrip)tsi.Owner;
                StatHelperClass shc = (StatHelperClass)((object[])cms.Tag)[0];
                int colIndex = (int)((object[])cms.Tag)[1];

                if (shc.DoConversion(tsi.Text))
                {
                    for (int i = 3; i < 8; i++)
                    {
                        double value = double.NaN;
                        switch (i)
                        {
                            case StatHelperClass.SumIndex:
                                value = shc.sum;
                                break;

                            case StatHelperClass.MinIndex:
                                value = shc.min;
                                break;

                            case StatHelperClass.MaxIndex:
                                value = shc.max;
                                break;

                            case StatHelperClass.MeanIndex:
                                value = shc.mean;
                                break;
                        }

                        dataGridView1[colIndex, i].Value = value;
                    }

                    dataGridView1[colIndex, StatHelperClass.RangeIndex].Value = shc.range;
                    dataGridView1[colIndex, 1].Value = tsi.Text;
                }
            }
            catch (Exception err)
            {
            }
            finally
            {
            }
        }

        private void Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            List<StatHelperClass> shc = (List<StatHelperClass>)e.Result;

            for (int j = 0; j < shc.Count; j++)
            {
                if (shc[j].count > 0)
                {
                    int colIndex = EnsureColumnsInDataGrid(shc[j]);

                    dataGridView1.Columns[colIndex].ContextMenuStrip = new ContextMenuStrip();
                    dataGridView1.Columns[colIndex].ContextMenuStrip.Tag = new object[] { shc[j], colIndex };

                    if (shc[j].useArealUnit)
                    {
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Acres", null, false, new EventHandler(QueryForm_Click)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Hectares", null, false, new EventHandler(QueryForm_Click)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Square Feet", null, false, new EventHandler(QueryForm_Click)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Square Meters", null, false, new EventHandler(QueryForm_Click)));
                    }
                    else
                    {
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Feet", null, false, new EventHandler(QueryForm_Click)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Meters", null, false, new EventHandler(QueryForm_Click)));
                    }

                    if (dataGridView1.Rows.Count > 1)
                    {
                        dataGridView1.Rows[0].Cells[colIndex].Value = "";
                        dataGridView1.Rows[1].Cells[colIndex].Value = shc[j].LinearUnit;
                        dataGridView1.Rows[2].Cells[colIndex].Value = shc[j].count;
                        dataGridView1.Rows[3].Cells[colIndex].Value = shc[j].sum;
                        dataGridView1.Rows[4].Cells[colIndex].Value = shc[j].min;
                        dataGridView1.Rows[5].Cells[colIndex].Value = shc[j].max;
                        dataGridView1.Rows[6].Cells[colIndex].Value = shc[j].mean;
                        dataGridView1.Rows[7].Cells[colIndex].Value = shc[j].range;
                    }
                    else
                    {
                        dataGridView1.Rows.Add(new object[] { "Coordinate System", "" });                     //0
                        dataGridView1.Rows.Add(new object[] { "Linear Unit", shc[j].LinearUnit });                         //1
                        dataGridView1.Rows.Add(new object[] { "Count", shc[j].count });                                        //2
                        dataGridView1.Rows.Add(new object[] { "Sum", shc[j].sum });                                            //3
                        dataGridView1.Rows.Add(new object[] { "Min", shc[j].min });                                            //4
                        dataGridView1.Rows.Add(new object[] { "Max", shc[j].max });                                            //5
                        dataGridView1.Rows.Add(new object[] { "Mean", shc[j].mean });                                          //6
                        dataGridView1.Rows.Add(new object[] { "Range", shc[j].range });                                        //7
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
