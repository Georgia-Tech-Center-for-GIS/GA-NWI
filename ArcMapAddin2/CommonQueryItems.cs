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
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.Catalog;

namespace GAWetlands
{
    class CalcAllValues
    {
        public static void DoCalculation(IFeatureLayer ifl_active)
        {
            //System.IO.StreamWriter sw = new System.IO.StreamWriter("C:\\Log.txt");
            List<StatHelperClass> shc = new List<StatHelperClass>();

            bool bAborted = false;

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
                        bAborted = true;
                        return;
                    }
                }

                //sw.WriteLine("Started Query at " + System.DateTime.Now);

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
                //sw.WriteLine("Finished Calculation at " + System.DateTime.Now);
                //sw.Close();

                try
                {
                    if (!bAborted)
                    {
                        Form2 f2 = new Form2();
                        f2.shc = shc;
                        f2.ShowDialog();
                    }
                }
                catch (Exception e)
                {
                }
            }
        }
    }
    public class CommonQueryForm
    {
        private static Geoprocessor gp = new Geoprocessor();
        private static IGxDialog gd = new GxDialogClass();

        public static void Completed(QueryHelperClass qhc, List<StatHelperClass> shc, DataGridView dataGridView1, EventHandler eh)
        {
            for (int j = 0; j < shc.Count; j++)
            {
                if (shc[j].count > 0)
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();
                    int colIndex = EnsureColumnsInDataGrid(shc[j], dataGridView1);

                    dataGridView1.Columns[colIndex].ContextMenuStrip = new ContextMenuStrip();
                    dataGridView1.Columns[colIndex].ContextMenuStrip.Tag = new object[] { shc[j], colIndex };

                    if (shc[j].useArealUnit)
                    {
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Acres", null, false, new EventHandler(eh)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Square Feet", null, false, new EventHandler(eh)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Square Miles", null, false, new EventHandler(eh)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripSeparator());
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Hectares", null, false, new EventHandler(eh)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Square Meters", null, false, new EventHandler(eh)));
                    }
                    else
                    {
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Feet", null, false, new EventHandler(eh)));
                        dataGridView1.Columns[colIndex].ContextMenuStrip.Items.Add(new ToolStripLabel("Meters", null, false, new EventHandler(eh)));
                    }

                    if (dataGridView1.Rows.Count >= 7)
                    {
                        //dataGridView1.Rows[0].Cells[colIndex].Value = shc[j].CoordinateSystem;
                        dataGridView1.Rows[0].Cells[colIndex].Value = shc[j].LinearUnit;
                        dataGridView1.Rows[1].Cells[colIndex].Value = shc[j].count;
                        dataGridView1.Rows[2].Cells[colIndex].Value = shc[j].sum;
                        dataGridView1.Rows[3].Cells[colIndex].Value = shc[j].min;
                        dataGridView1.Rows[4].Cells[colIndex].Value = shc[j].max;
                        dataGridView1.Rows[5].Cells[colIndex].Value = shc[j].mean;
                        //dataGridView1.Rows[7].Cells[colIndex].Value = shc[j].range;
                        dataGridView1.Rows[6].Cells[colIndex].Value = (qhc == null)? "" : qhc.LastQueryStrings.FirstOrDefault();
                    }
                    else
                    {
                        dataGridView1.Rows.Clear();
                        //dataGridView1.Rows.Add(new object[] { "Coordinate System", shc[j].CoordinateSystem }); //0
                        dataGridView1.Rows.Add(new object[] { "Units", shc[j].LinearUnit }); //1
                        dataGridView1.Rows.Add(new object[] { "Number of Features", shc[j].count }); //2
                        dataGridView1.Rows.Add(new object[] { "Sum", shc[j].sum.ToString("N") }); //3
                        dataGridView1.Rows.Add(new object[] { "Min", shc[j].min.ToString("N") }); //4
                        dataGridView1.Rows.Add(new object[] { "Max", shc[j].max.ToString("N") }); //5
                        dataGridView1.Rows.Add(new object[] { "Mean", shc[j].mean.ToString("N") });                                          //6
                        dataGridView1.Rows.Add(new object[] { "Query String", (qhc == null)? "" : qhc.LastQueryStrings.FirstOrDefault() });
                        //dataGridView1.Rows.Add(new object[] { "Range", shc[j].range.ToString("N") }); //7
                    }

                    DoConversion(dataGridView1, shc[j], "Acres", colIndex);
                }
            }
        }

        public static int EnsureColumnsInDataGrid(StatHelperClass shc, DataGridView dataGridView1)
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

        public static void DoConversion(DataGridView dataGridView1, StatHelperClass shc, string Text, int colIndex)
        {
            if (shc.DoConversion(Text))
            {
                for (int i = 2; i < 6; i++)
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

                    dataGridView1[colIndex, i].Value = value.ToString("N");
                }
                dataGridView1[colIndex, 0].Value = Text;
            }
        }

        public static void DoConversion(ToolStripItem tsi, DataGridView dataGridView1)
        {
            ContextMenuStrip cms = (ContextMenuStrip)tsi.Owner;
            StatHelperClass shc = (StatHelperClass)((object[])cms.Tag)[0];
            int colIndex = (int)((object[])cms.Tag)[1];

            DoConversion(dataGridView1, shc, tsi.Text, colIndex);
        }

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
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
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

        public static void DoSaveSelectedFeatures(Form frmSrc)
        {
            if (ArcMap.Document.SelectedLayer == null)
            {
                System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                return;
            }

            if (frmSrc != null)
            {
                frmSrc.TopMost = false;
            }

            IFeatureLayer ifl_active = (IFeatureLayer)ArcMap.Document.SelectedLayer;

            gd.Title = "Save selected features";
            gd.ObjectFilter = new GxFilterFeatureClassesClass(); //new GxFilterFeatureClassesClass();

            if (gd.DoModalSave(ArcMap.Application.hWnd) == false)
            {
                return;
            }

            ESRI.ArcGIS.DataManagementTools.CopyFeatures cf = new ESRI.ArcGIS.DataManagementTools.CopyFeatures();
            cf.in_features = ifl_active;
            cf.out_feature_class = gd.FinalLocation.FullName + "\\" + gd.Name;

            if (frmSrc != null)
            {
                frmSrc.TopMost = false;
            }

            gp.ExecuteAsync(cf);
        }
    }
}
