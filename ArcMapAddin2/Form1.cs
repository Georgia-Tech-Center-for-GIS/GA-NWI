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
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessing;

namespace GAWetlands
{
    public partial class NWIQuery : Form
    {
        private string[] tags = { "System", "Subsystem", "Class1", "Subclass", "Water1", "Special1", "Chem", "Soil", "GAPlanning" };
        private string selectedRadio = "";

        private QueryHelperClass qhc = new QueryHelperClass();

        RadioButton[] rbs;

        public NWIQuery()
        {
            InitializeComponent();
            rbs = new RadioButton[]
                { radioButton1, radioButton2, radioButton3, radioButton4, radioButton5, radioButton6, radioButton7, radioButton8};

            for (int i = 0; i < rbs.Length; i ++)
            {
                rbs[i].Tag = i;
                rbs[i].CheckedChanged += new EventHandler(QueryForm_CheckedChanged);
            }
        }

        void QueryForm_Load(object sender, EventArgs e)
        {
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

                    switch (selectedRadio)
                    {
                        case "Subsystem":
                            qhc = new QueryHelperSubsystem(); break;
                        case "Subclass":
                            qhc = new QueryHelperSubclass(); break;
                        default:
                            qhc = new QueryHelperClass(); break;
                    };

                    listBox2.Items.AddRange( qhc.getQueryValueOptions(selectedRadio) );
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

        protected void DoQuery(object sender, EventArgs e)
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

        protected void QueryForm_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem tsi = (ToolStripItem)sender;

                ContextMenuStrip cms = (ContextMenuStrip)tsi.Owner;
                StatHelperClass shc = (StatHelperClass) ((object[])cms.Tag) [0];
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

        protected int EnsureColumnsInDataGrid(StatHelperClass shc)
        {
            if (dataGridView1.Columns.Count == 0)
            {
                dataGridView1.Columns.Add("nm", "Statistic");
                dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            string col = shc.GetColumnHeadings();

            if(dataGridView1.Columns.Contains(col)) {
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

        protected void button1_Click(object sender, EventArgs e)
        {
            CalcAllValues.DoCalculation((IFeatureLayer) ArcMap.Document.SelectedLayer);
        }

        protected void button3_Click(object sender, EventArgs e)
        {
            CommonQueryForm.ExportGridControlContentsExcel(dataGridView1);
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
                        //shc.Add(new PolygonPerimeter_HelperClass());
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

                    int foundCount = 0;

                    while ((rw = csr.NextRow()) != null)
                    {
                        foundCount++;

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

                    if (foundCount == 0)
                    {
                        System.Windows.Forms.MessageBox.Show("No Records returned.", "Query");
                    }
                    else
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
                }
            }
            catch (Exception err)
            {
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

        protected void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (progressBar1.Value == progressBar1.Maximum)
            {
                progressBar1.Value = progressBar1.Minimum;
            }

            progressBar1.Increment(1);
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
                        dataGridView1.Rows[8].Cells[colIndex].Value = qhc.LastQueryStrings[0];
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
                        dataGridView1.Rows.Add(new object[] { "Last Query String", qhc.LastQueryStrings[0] } );
                    }
                }
            }
        }
    }

    class CalcAllValues
    {
        public static void DoCalculation(IFeatureLayer ifl_active)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter("C:\\Log.txt");
            List<StatHelperClass> shc = new List<StatHelperClass>();

            try {
                if (ifl_active == null)
                {
                    System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                    return;
                }

                IQueryFilter iqf = new QueryFilterClass();
                iqf.WhereClause = "1=1";

                int featureCount = ifl_active.FeatureClass.FeatureCount(iqf);

                if (featureCount >= 10000)
                {
                    if (System.Windows.Forms.MessageBox.Show("This will (p)re-calculate areas for ALL " + featureCount + " features in the selected layer. Proceed?", "", MessageBoxButtons.YesNo).Equals(DialogResult.No))
                    {
                        return;
                    }
                }

                sw.WriteLine("Started Query at " + System.DateTime.Now);
                IWorkspace ws = ((IDataset)ArcMap.Document.ActiveView.FocusMap.Layer[0]).Workspace;
                IWorkspaceEdit iwe = (IWorkspaceEdit)ws;

                if (ifl_active.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    //shc.Add(new PolygonPerimeter_HelperClass());
                    shc.Add(new PolygonArea_HelperClass());
                }
                else if (ifl_active.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    shc.Add(new PolylineHelperClass());
                }

                for(int i = 0; i < shc.Count; i++)
                    shc[i].SearchForFields((ITable)ifl_active);

                IFeatureCursor csr = ifl_active.FeatureClass.Update(iqf, true);
                IFeature feat = null;

                while ((feat = csr.NextFeature()) != null)
                {
                    for (int i = 0; i < shc.Count; i++)
                    {
                        shc[i].doReCalcValues = true;
                        shc[i].ProcessFeature(iwe, ifl_active, (IRow)feat);
                    }

                    csr.UpdateFeature(feat);
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                sw.WriteLine("Finished Calculation at " + System.DateTime.Now);
                sw.Close();

                try
                {
                    Form2 f2 = new Form2();
                    f2.shc = shc;
                    f2.ShowDialog();
                }
                catch (Exception e)
                {
                }
            }
        }
    }
    public class CommonQueryForm
    {
        public static void ExportGridControlContentsExcel(DataGridView dataGridView1)
        {
            try
            {
                Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);

                // creating new Excelsheet in workbook
                Microsoft.Office.Interop.Excel._Worksheet worksheet = null;

                // see the excel sheet behind the program
                app.Visible = true;

                // get the reference of first sheet. By default its name is Sheet1.
                // store its reference to worksheet
                worksheet = (Microsoft.Office.Interop.Excel._Worksheet)workbook.ActiveSheet;

                // changing the name of active sheet
                worksheet.Name = "Exported from GA NWI Tools";

                // storing header part in Excel
                for (int i = 1; i < dataGridView1.Columns.Count + 1; i++)
                {
                    worksheet.Cells[1, i] = dataGridView1.Columns[i - 1].HeaderText;
                }

                // storing Each row and column value to excel sheet
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    for (int j = 0; j < dataGridView1.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 2, j + 1] = dataGridView1.Rows[i].Cells[j].Value; //.ToString();
                    }
                }
            }
            catch (Exception err)
            {
            }
        }
    }
}
