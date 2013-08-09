using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.esriSystem;

namespace GAWetlands
{
    public partial class SymbolizeByDialog : Form
    {
        bool bUseNwi = true;

        private class DoSymbolizeLayerClass
        {
            public System.Collections.Generic.HashSet<string> hsToRemove = new System.Collections.Generic.HashSet<string>();
            public string symbType = "";
            public bool IsGPBusy = false;
        }

        private static DoSymbolizeLayerClass dslc = new DoSymbolizeLayerClass();
        private static IUniqueValueRenderer urvl = null;
        private static IFeatureLayer srcLayer = null;

        private static Geoprocessor gp = new Geoprocessor();

        private static void doSymbolizeLayer(IActiveView activeView, int i, string symbType, bool doRemove, string filenamePrefix, string filenameSuffix)
        {
            try
            {
                if (ArcMap.Document.SelectedLayer == null)
                {
                    System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                    return;
                }

                IFeatureLayer ifl = (IFeatureLayer)ArcMap.Document.SelectedLayer;

                if (ifl.FeatureClass.FindField(symbType) < 0)
                {
                    System.Windows.Forms.MessageBox.Show("Selected layer does not contain the " + symbType + " field and can't be symbolized " +
                        "with this tool. Select another layer to continue.");
                    return;
                }

                string geomTypeName = "Polygon";

                if (ifl.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    geomTypeName = "Polyline";

                var asmPath = GetAssemblyPath();

                ILayerFile layerFile = new LayerFileClass();
                
                if (filenameSuffix != "Rated")
                {
                    layerFile.Open(asmPath + "/Symbology/" + filenamePrefix + symbType + "_" + geomTypeName + filenameSuffix + ".lyr");
                }
                else
                {
                    layerFile.Open(asmPath + "/Symbology/" + filenamePrefix + "Rated_" + geomTypeName + ".lyr");
                }

                IGeoFeatureLayer igfl = (IGeoFeatureLayer)layerFile.Layer;
                IUniqueValueRenderer iuvr = (IUniqueValueRenderer)igfl.Renderer;
                iuvr.UseDefaultSymbol = false;
                iuvr.Field[0] = symbType;

                if( filenameSuffix == "Rated") {
                    for (int l = 0; l < iuvr.ValueCount; l++)
                    {
                        iuvr.Heading[iuvr.Value[l]] = symbType;
                    }
                }

                /*ILegendInfo li = (ILegendInfo)iuvr;
                ILegendGroup gp = (ILegendGroup)li.LegendGroup[0];
                gp.Heading = symbType;*/

                IFeatureWorkspace iw = (IFeatureWorkspace)((IDataset)ifl.FeatureClass).Workspace;
                ISQLSyntax sql = (ISQLSyntax)iw;

                string prefix = sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierPrefix);
                string suffix = sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierSuffix);

                IQueryFilter f = new QueryFilterClass();
                int vCount = iuvr.ValueCount;

                List<string> values = new List<string>();
                
                if (doRemove)
                {
                    char[] delimiter = { iuvr.FieldDelimiter[0] };

                    for (int j = 0; j < vCount; j++)
                    {
                        f.WhereClause = "";
                        string[] currValues = iuvr.Value[j].Split(delimiter);

                        for (int k = 0; k < currValues.Length; k++)
                        {
                            if (k > 0)
                                f.WhereClause += " AND ";

                            f.WhereClause += prefix + iuvr.Field[k] + suffix + " = '" + currValues[k].Trim() + "'";
                        }

                        ICursor fc = null;
                        bool bFound = false;

                        try
                        {
                            fc = ((ITable)ifl).Search(f, true);
                            bFound = (fc.NextRow() == null) ? false : true;
                        }
                        catch (Exception v)
                        {
                            //fc = null;
                        }

                        if (!bFound)
                            values.Add(iuvr.Value[j]);
                    }
                }

                foreach (string v in values)
                {
                    iuvr.RemoveValue(v);
                }

                if (iuvr.ValueCount > 0)
                {
                    IGeoFeatureLayer igd_dest = (IGeoFeatureLayer)ifl;
                    igd_dest.Renderer = (IFeatureRenderer)iuvr;

                    ArcMap.Document.ActiveView.ContentsChanged();
                    ArcMap.Document.UpdateContents();

                    ArcMap.Document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("No unique values other than null or blank found. No changes will be made to the symbology");
                    return;
                }
                //ArcMap.Document.CurrentContentsView.Deactivate();
                //ArcMap.Document.ContentsView[0].Activate(ArcMap.Application.hWnd, ArcMap.Document);
            }
            catch (Exception e)
            {
            }
            finally
            {
            }
        }

        static void gp_ProgressChanged(object sender, ESRI.ArcGIS.Geoprocessor.ProgressChangedEventArgs e)
        {
            IStepProgressor isp = ArcMap.Application.StatusBar.ProgressBar;
            ArcMap.Application.StatusBar.ProgressBar.Position = Convert.ToInt32((e.ProgressPercentage * (isp.MaxRange - isp.MinRange) + isp.MinRange));
        }

        static void gp_ToolExecuted(object sender, ToolExecutedEventArgs e)
        {
            IGeoProcessorResult igpr = null;

            try
            {
                igpr = e.GPResult;

                if (igpr.Status == esriJobStatus.esriJobSucceeded && dslc.IsGPBusy)
                {
                    if (igpr.Status == esriJobStatus.esriJobFailed || igpr.Status == esriJobStatus.esriJobSucceeded ||
                        igpr.Status == esriJobStatus.esriJobTimedOut)
                    {
                        dslc.IsGPBusy = false;
                    }

                    ProcessResult();
                }

                ArcMap.Document.ActiveView.ContentsChanged();
                ArcMap.Document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);

                //ArcMap.Document.CurrentContentsView.Deactivate();
                //ArcMap.Document.ContentsView[0].Activate(ArcMap.Application.hWnd, ArcMap.Document);
            }
            catch (Exception err)
            {
            }

            finally
            {
            }
        }

        private static void ProcessResult()
        {
            IGPUtilities igpu = new GPUtilitiesClass();
            //IArray tbl = (IArray)igpu.GetGxObjects((string) igpr.ReturnValue);
            ITable tbl = igpu.FindMapTable("SymbolStats" + "_" + dslc.symbType);

            int symbPos = tbl.FindField(dslc.symbType);
            //int countPos = tbl.FindField("Count_" + dslc.symbType);

            IRow rw = null;
            IQueryFilter iqf = new QueryFilterClass();
            iqf.WhereClause = "1=1";

            ICursor csr = tbl.Search(iqf, true);

            int oldCount = dslc.hsToRemove.Count;

            while ((rw = csr.NextRow()) != null)
            {
                dslc.hsToRemove.Remove((string)rw.get_Value(symbPos));
            }

            if (dslc.hsToRemove.Count == oldCount)
            {
                System.Windows.Forms.MessageBox.Show("No unique values other than null or blank found. No changes will be made to the symbology");
                return;
            }

            for (int j = 0; j < urvl.ValueCount; j++)
            {
                string a = urvl.get_Value(j);
                if (a == null) continue;

                if (dslc.hsToRemove.Contains(a))
                    urvl.RemoveValue(a);
            }

            urvl.UseDefaultSymbol = false;
            urvl.set_Field(0, dslc.symbType);

            IGeoFeatureLayer igd_dest = (IGeoFeatureLayer)srcLayer;
            igd_dest.Renderer = (IFeatureRenderer)urvl;
        }

        private static string GetAssemblyPath()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var uriBuilder = new UriBuilder(codeBase);
            var asmPath = Uri.UnescapeDataString(uriBuilder.Path);
            asmPath = System.IO.Path.GetDirectoryName(asmPath);
            return asmPath;
        }

        protected void doSymbolize(string type, string prefix, string filename)
        {
            IActiveView iav = ArcMap.Document.ActiveView;
            doSymbolizeLayer(iav, 0, type, checkBox2.Checked, prefix, filename);
        }

        public SymbolizeByDialog()
        {
            InitializeComponent();
            radioButton1_CheckedChanged(null, null);
        }

        private string[] NWICats = { "System", "Class", "Water Regime", "Chemistry", "Soil","Special Modifier" };
        private string[] NWIPlusCats = { "Landscape", "Landform", "Waterflow", "Waterbody",
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

        private string[] NWIPlusLandscapeMods = { "Lotic", "Lentic", "Estuary" };
        private string[] NWIPlusLandformMods = { "Coastal", "Inland" };
        private string[] NWIPlusWaterbodyMods = { "Estuary", "Lake", "OceanBay", "Pond", "River", "Stream" };

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if ( (bUseNwi = radioButton1.Checked) )
            {
                comboBox1.Items.Clear();

                for (int i = 0; i < NWICats.Count(); i++)
                    comboBox1.Items.Add(NWICats[i]);

                comboBox1.SelectedIndex = 0;

                comboBox2.Items.Clear();
                comboBox2.Enabled = false;

                checkBox1.Checked = false;
                checkBox1.Enabled = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (!(bUseNwi = radioButton1.Checked))
            {
                comboBox1.Items.Clear();

                for (int i = 0; i < NWIPlusCats.Count(); i++)
                    comboBox1.Items.Add(NWIPlusCats[i]);

                comboBox1.SelectedIndex = 0;

                checkBox1.Checked = false;
                checkBox1.Enabled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string fieldName = "";
            string filenameSuffix = "";
            string filenamePrefix = "";

            if (bUseNwi)
            {
                fieldName = comboBox1.SelectedItem.ToString();
                switch (fieldName)
                {
                    case "Water Regime":
                        fieldName = "Water1";
                        break;

                    case "Special Modifier":
                        fieldName = "Special1";
                        break;

                    case "Chemistry":
                        fieldName = "Chem";
                        break;

                    case "Class":
                        fieldName = "Class1";
                        break;
                }
            }
            else // NWI PLUS
            {
                fieldName = comboBox1.SelectedItem.ToString();
                filenamePrefix = "LLWW_";

                switch (fieldName)
                {
                    case "Landscape":
                        break;

                    case "Landform":
                        break;

                    case "Waterflow":
                        break;

                    case "Waterbody":
                        break;

                    case "Surface Water Detention":
                        fieldName = "Surf_Water";
                        goto case "Rated";

                    case "Coast Storm Surge Detention":
                        fieldName = "Coast_Stor";
                        goto case "Rated";

                    case "Streamflow Maintenance":
                        fieldName = "Stream_Mai";
                        goto case "Rated";

                    case "Nutrient Transformation":
                        fieldName = "Nutrient_Tra";
                        goto case "Rated";

                    case "Carbon Sequestration":
                        fieldName = "Carbon_Seq";
                        goto case "Rated";

                    case "Sediment Particulate Retention":
                        fieldName = "Sed_Part_R";
                        goto case "Rated";

                    case "Bank / Shoreline Stabilization":
                        fieldName = "Bank_Shore";
                        goto case "Rated";

                    case "Provision of Fish/Aquatic Invertebrate Habitat":
                        fieldName = "Prov_Fish_";
                        goto case "Rated";

                    case "Provision of Waterfowl and Waterbird Habitat":
                        fieldName = "Prov_WFowl";
                        goto case "Rated";

                    case "Provision of Other Wildlife Habitat":
                        fieldName = "Prov_Other";
                        goto case "Rated";

                    case "Provision of Unique/Uncommon/Highly Diverse Wetlant Plant Communities":
                        fieldName = "Prov_Hab_U";
                        goto case "Rated";

                    case "Rated":
                        filenameSuffix = "Rated";
                        break;
                }

                if (checkBox1.Checked && comboBox2.SelectedItem != null)
                {
                    filenameSuffix = "Modifier" + comboBox2.SelectedItem.ToString();
                }
            }

            try
            {
                doSymbolize(fieldName, filenamePrefix, filenameSuffix);
            }
            catch (Exception eee)
            {
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            comboBox2.Enabled = checkBox1.Checked;

            if (comboBox2.Enabled)
            {
                comboBox1_SelectionChangeCommitted(null, null);
            }
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!bUseNwi)
            {
                comboBox2.Items.Clear();
                string[] ptr = null;

                switch (comboBox1.SelectedItem.ToString())
                {
                    case "Landscape":
                        ptr = NWIPlusLandscapeMods;
                        break;
                    case "Landform":
                        ptr = NWIPlusLandformMods;
                        break;
                    case "Waterbody":
                        ptr = NWIPlusWaterbodyMods;
                        break;
                }

                if (ptr != null)
                {
                    for (int j = 0; j < ptr.Count(); j++)
                    {
                        comboBox2.Items.Add(ptr[j]);
                    }

                    comboBox2.Enabled = true;
                    comboBox2.SelectedIndex = 0;
                }
                else
                {
                    comboBox2.Items.Clear();
                    comboBox2.Enabled = false;
                }
            }
        }
    }
}
