using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace GAWetlands
{
    public partial class NWIPlusQuery : Form
    {
        private string[] tags = { "Landscape", "Landform", "Waterflow", "Waterbody", "Landscape+", "Landform+", "Waterbody+" };
        private string selectedRadio = "";

        private QueryHelperClass qhc = new QueryHelperClass();

        RadioButton[] rbs;

        public NWIPlusQuery()
        {
            InitializeComponent();
            rbs = new RadioButton[]
                { radioButton1, radioButton2, radioButton3, radioButton4};

            for (int i = 0; i < rbs.Length; i ++)
            {
                rbs[i].Tag = i;
                rbs[i].CheckedChanged += new EventHandler(QueryForm_CheckedChanged);
            }
        }

        void QueryForm_CheckedChanged(object sender, EventArgs e)
        {
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new ESRI.ArcGIS.Carto.LayerFileClass();
            
            RadioButton btn = sender as RadioButton;
            listBox2.Items.Clear();

            if (btn != null && btn.Checked)
            {
                try
                {
                    selectedRadio = tags[(int)btn.Tag];

                    if (selectedRadio.Contains('+'))
                    {
                        //qhc = new QueryHelperLLWW_Modifier();
                        //listBox2.Items.AddRange(qhc.getQueryValueOptions("Landscape"));
                    }
                    else
                    {
                        qhc = new QueryHelperLLWW();
                        listBox2.Items.AddRange(qhc.getQueryValueOptions(selectedRadio));
                    }
                }
                catch (Exception err)
                {
                }
                finally
                {
                    layerFile.Close();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CalcAllValues.DoCalculation((IFeatureLayer)ArcMap.Document.SelectedLayer);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CommonQueryForm.ExportGridControlContentsExcel(dataGridView1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var i = ArcMap.Document.ActiveView;
            dataGridView1.Rows.Clear();

            if (ArcMap.Document.SelectedLayer == null)
            {
                System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                return;
            }

            try
            {
                button2.Enabled = false;
                DoWork(null, null);
            }
            catch (Exception err)
            {
            }
            finally
            {
                button2.Enabled = true;
            }
        }

        protected void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (progressBar1.Value == progressBar1.Maximum)
            {
                progressBar1.Value = progressBar1.Minimum;
            }

            progressBar1.Increment(1);
        }

        protected int EnsureColumnsInDataGrid(StatHelperClass shc)
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

        protected void DoWork(object sender, DoWorkEventArgs e)
        {
            IWorkspaceEdit iwe = null;

            dataGridView1.Rows.Clear();
            //button2.Enabled = false;

            try
            {
                IFeatureLayer ifl_active = (IFeatureLayer)ArcMap.Document.SelectedLayer;
                IWorkspace ws = ((IDataset)ifl_active.FeatureClass).Workspace;
                iwe = (IWorkspaceEdit)ws;

                //dataGridView1.Rows.Add(new object[] { "Query Started" });

                string[] queryValues = qhc.getQueryValues(listBox2);
                ICursor csr = qhc.doQueryItems(selectedRadio, queryValues);

                IRow rw = null;

                List<StatHelperClass> shc = new List<StatHelperClass>();

                if (csr != null)
                {
                    if (ifl_active.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        shc.Clear();
                        shc.Add(new PolygonPerimeter_HelperClass());
                        shc.Add(new PolygonArea_HelperClass());
                    }
                    else if (ifl_active.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        shc.Clear();
                        shc.Add(new PolylineHelperClass());
                    }

                    if (shc == null) return;

                    if (shc.Count == 1)
                        shc[0].SearchForFields((ITable)ifl_active);
                    else
                    {
                        for (int j = 0; j < shc.Count; j++)
                        {
                            shc[j].SearchForFields((ITable)ifl_active);
                        }
                    }

                    while ((rw = csr.NextRow()) != null)
                    {
                        for (int j = 0; j < shc.Count; j++)
                        {
                            if (shc[j].doReCalcValues && !iwe.IsBeingEdited())
                            {
                                iwe.StartEditing(true);
                                iwe.StartEditOperation();
                            }

                            shc[j].ProcessFeature(iwe, ifl_active, rw);

                            this.ProgressChanged(sender, null);
                        }
                    }

                    if (checkBox1.Checked)
                    {
                        IQueryFilter iqf = qhc.getQueryFilter(selectedRadio, queryValues);
                        IFeatureLayer2 ifl2 = (IFeatureLayer2)ArcMap.Document.SelectedLayer;
                        IGeoFeatureLayer igfl = (IGeoFeatureLayer)ifl2;
                        //ITable tbl = (ITable)((IFeatureLayer)igfl).FeatureClass;
                        IFeatureSelection ifs = (IFeatureSelection)igfl;
                        ifs.SelectFeatures(iqf, esriSelectionResultEnum.esriSelectionResultNew, false);
                        ArcMap.Document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    }

                    Completed(sender, new RunWorkerCompletedEventArgs(shc, null, false));
                }
                else
                {
                    //dataGridView1.Rows.Add(new object[] { "No Records Found" });
                }
            }
            catch (Exception err)
            {
                //dataGridView1.Rows.Add(new object[] { "An Error Occurred" });
            }
            finally
            {
                button2.Enabled = true;

                if (iwe != null)
                    iwe.StopEditOperation();

                if (iwe != null && iwe.IsBeingEdited())
                    iwe.StopEditing(true);
            }
        }

        protected void QueryForm_Click(object sender, EventArgs e)
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

        protected void Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            List<StatHelperClass> shc = (List<StatHelperClass>)e.Result;
            progressBar1.Value = progressBar1.Maximum;

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

                    if (dataGridView1.Rows.Count > 0)
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
    }
}