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
        private string[] tags = { "Landscape", "Landform", "Water_Flow", "Waterbody", "Landscape+", "Landform+", "Waterbody+" };
        private string selectedRadio = "";

        private QueryHelperLLWW qhc = new QueryHelperLLWW();

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
                        listBox2.Items.AddRange(qhc.getQueryValueOptions(selectedRadio, ""));
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

                    CommonQueryForm.Completed(qhc,shc, dataGridView1, QueryForm_Click);
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

                QCompleteLabel.Show();
            }
        }

        protected void QueryForm_Click(object sender, EventArgs e)
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

        private void CopySelectedFeatures_Click(object sender, EventArgs e)
        {
            CommonQueryForm.DoSaveSelectedFeatures(this);
        }
    }
}