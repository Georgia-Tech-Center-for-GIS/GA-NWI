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
using System.Text.RegularExpressions;

namespace GAWetlands
{
    public partial class CombinedQueryForm : Form
    {
        private QueryHelperClass qhc = null;
        private bool bUseNwi = false;
        private string selectedRadio = "";

        public CombinedQueryForm()
        {
            InitializeComponent();

            radioButton1_CheckedChanged(null, null);
        }

        protected void DoWork(object sender, DoWorkEventArgs e)
        {
            QCompleteLabel.Hide();
            progressBar1.Show();

            IWorkspaceEdit iwe = null;

            dataGridView1.Rows.Clear();

            try
            {
                IFeatureLayer ifl_active = (IFeatureLayer)ArcMap.Document.SelectedLayer;
                IWorkspace ws = ((IDataset)ifl_active.FeatureClass).Workspace;
                iwe = (IWorkspaceEdit)ws;

                string[] queryValues = qhc.getQueryValues(listBox2);
                ICursor csr = qhc.doQueryItems(queryValues);

                IRow rw = null;

                List<StatHelperClass> shc = new List<StatHelperClass>();

                if (csr != null)
                {
                    if (ifl_active.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        shc.Clear();
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

                            this.ProgressChanged();
                        }
                    }

                    if (foundCount == 0)
                    {
                        System.Windows.Forms.MessageBox.Show("No Records returned.", "Query");
                    }
                    else
                        if (checkBox1.Checked)
                        {
                            IQueryFilter iqf = qhc.getQueryFilter(queryValues);
                            IFeatureLayer2 ifl2 = (IFeatureLayer2)ArcMap.Document.SelectedLayer;
                            IGeoFeatureLayer igfl = (IGeoFeatureLayer)ifl2;
                            //ITable tbl = (ITable)((IFeatureLayer)igfl).FeatureClass;
                            IFeatureSelection ifs = (IFeatureSelection)igfl;
                            ifs.SelectFeatures(iqf, esriSelectionResultEnum.esriSelectionResultNew, false);
                            ArcMap.Document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                        }

                    CommonQueryForm.Completed(qhc, shc, dataGridView1, QueryForm_Click);
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

                progressBar1.Hide();
                QCompleteLabel.Show();
            }
        }

        protected void ProgressChanged()
        {
            if (progressBar1.Value == progressBar1.Maximum)
            {
                progressBar1.Value = progressBar1.Minimum;
            }

            progressBar1.Increment(1);
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

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                selectedRadio = comboBox1.SelectedItem.ToString();

                string filename = selectedRadio;
                string fieldName = selectedRadio;

                listBox2.Items.Clear();

                if (bUseNwi) //NWI
                {
                    switch (selectedRadio)
                    {
                        case "Subsystem":
                            goto default;

                        case "Subclass":

                            qhc = new QueryHelperSubclass();
                            break;

                        case "Class":
                            fieldName = "Class1";
                            goto default;
                        case "Water Chemistry":
                            fieldName = "Chemistry1";
                            filename = "Chemistry";
                            goto default;
                        case "Water Regime":
                            fieldName = "Water1";
                            filename = "Water";
                            goto default;
                        case "Special Modifiers":
                            fieldName = "Special1";
                            filename = "Special";
                            goto default;

                        default:
                            qhc = new QueryHelperClass();
                            break;
                    };
                }
                else // NWI+
                {
                    switch (selectedRadio)
                    {
                        case "Landscape":
                            break;

                        case "Landform":
                            break;

                        case "Waterflow":
                            fieldName = "Water_flow";
                            break;

                        case "Waterbody":
                            break;

                        default:
                            filename = Regex.Replace(selectedRadio, "[\\(\\) ]", ""); //fieldName.Replace(" ", "");

                            if (filename.IndexOf("Waterbody") > -1)
                            {
                                filename += "Q";
                            }

                            fieldName = selectedRadio.Split(' ')[0];
                            break;

                        case "Surface Water Detention":
                            fieldName = "Surf_Water";
                            filename = "SWD";

                            break;

                        case "Coast Storm Surge Detention":
                            fieldName = "Coast_Stor";
                            filename = "CSSD";

                            break;

                        case "Streamflow Maintenance":
                            fieldName = "Stream_Mai";
                            filename = "SM";

                            break;

                        case "Nutrient Transformation":
                            fieldName = "Nutrnt_Tra";
                            filename = "NT";

                            break;

                        case "Carbon Sequestration":
                            fieldName = "Carbon_Seq";
                            filename = "CS";

                            break;

                        case "Sediment Particulate Retention":
                            fieldName = "Sed_Part_R";
                            filename = "SR";

                            break;

                        case "Bank / Shoreline Stabilization":
                            fieldName = "Bank_Shore";
                            filename = "BS";

                            break;

                        case "Provision of Fish/Aquatic Invertebrate Habitat":
                            fieldName = "Prov_Fish_";
                            filename = "ProvFish";

                            break;

                        case "Provision of Waterfowl and Waterbird Habitat":
                            fieldName = "Prov_WFowl";
                            filename = "ProvBird";

                            break;

                        case "Provision of Other Wildlife Habitat":
                            fieldName = "Prov_Other";
                            filename = "ProvOtherWildHab";

                            break;

                        case "Provision of Unique/Uncommon/Highly Diverse Wetlant Plant Communities":
                            fieldName = "Prov_Hab_U";
                            filename = "ProvUWPC";

                            break;
                    }

                    filename = "NWIPlus" + filename;
                }

                listBox2.Items.AddRange(qhc.getQueryValueOptions(fieldName, filename));
            }
            catch (Exception ee)
            {
            }
        }

        private static string[] nwi_query_types = {
                                                      "System", "Subsystem", "Class", "Subclass",
                                                      "Water Regime", "Special Modifiers",
                                                      "Water Chemistry", "Soil"
                                                  };

        //NWI
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if ((bUseNwi = radioButton1.Checked))
                {
                    comboBox1.Items.Clear();

                    for (int i = 0; i < nwi_query_types.Count(); i++)
                        comboBox1.Items.Add(nwi_query_types[i]);

                    comboBox1.SelectedIndex = 0;
                    comboBox1_SelectionChangeCommitted(comboBox1, null);
                }

                //listBox2.Items.Clear();
            }
            catch (Exception ee)
            {
            }
        }

        private string[] NWIPlusCats = { "Landscape",
                                         "Landscape Type",
                                         "Landform",
                                         "Landform Type",
                                         "Waterflow",
                                         "Waterbody",
                                         "Waterbody (Flow)",
                                         "Waterbody (Modifier)",
                                         "Waterbody (Type)",
                                         "Waterbody (Other Modifier)",
                                         "Surface Water Detention",
                                         "Coast Storm Surge Detention",
                                         "Streamflow Maintenance",
                                         "Nutrient Transformation",
                                         "Carbon Sequestration",
                                         "Sediment Particulate Retention",
                                         "Bank / Shoreline Stabilization",
                                         "Provision of Fish/Aquatic Invertebrate Habitat",
                                         "Provision of Waterfowl and Waterbird Habitat",
                                         "Provision of Other Wildlife Habitat",
                                         "Provision of Unique/Uncommon/Highly Diverse Wetlant Plant Communities"};

        //NWI Plus
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!(bUseNwi = radioButton1.Checked))
                {
                    comboBox1.Items.Clear();

                    for (int i = 0; i < NWIPlusCats.Count(); i++)
                        comboBox1.Items.Add(NWIPlusCats[i]);

                    comboBox1.SelectedIndex = 0;
                    comboBox1_SelectionChangeCommitted(comboBox1, null);
                }

                //listBox2.Items.Clear();
            }
            catch (Exception ee)
            {
            }
        }

        private void DoQuery_Click(object sender, EventArgs e)
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

        private void PreCalculate_Click(object sender, EventArgs e)
        {
            CalcAllValues.DoCalculation((IFeatureLayer)ArcMap.Document.SelectedLayer);
        }

        private void ExportToExcel_Click(object sender, EventArgs e)
        {
            CommonQueryForm.ExportGridControlContentsExcel(dataGridView1);
        }

        private void CopySelectedFeatures_Click(object sender, EventArgs e)
        {
            CommonQueryForm.DoSaveSelectedFeatures(this);
        }

    }
}
