using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.SystemUI;

namespace GAWetlands
{
    class ClipNWI : ESRI.ArcGIS.Desktop.AddIns.Tool {
        protected void DoClip(IActiveView activeView, IGeometry geometry)
        {
            Geoprocessor gp = new Geoprocessor();

            try
            {
                ESRI.ArcGIS.Carto.IMap map = activeView.FocusMap;
                ESRI.ArcGIS.Carto.ILayerFile layerFile = new ESRI.ArcGIS.Carto.LayerFileClass();

                if (ArcMap.Document.SelectedLayer == null)
                {
                    System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                    return;
                }

                ISpatialFilter isf = new SpatialFilterClass();
                isf.Geometry = geometry;
                isf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                IFeatureLayer ifl_active = (IFeatureLayer)ArcMap.Document.SelectedLayer;
                //IGeoDataset gds = (IGeoDataset)ifl_active.FeatureClass;
                //ISpatialReference sr = gds.SpatialReference;

                //IQueryFilter iqf = qhc.getQueryFilter(selectedRadio, queryValues);
                //IFeatureLayer2 ifl2 = (IFeatureLayer2)ArcMap.Document.SelectedLayer;
                //IGeoFeatureLayer igfl = (IGeoFeatureLayer)ifl2;
                //ITable tbl = (ITable)((IFeatureLayer)igfl).FeatureClass;
                //IFeatureSelection ifs = (IFeatureSelection)igfl;
                //ifs.SelectFeatures(isf, esriSelectionResultEnum.esriSelectionResultNew, false);

                //IGeometryDef igd = (IGeometryDef)ifl_active.FeatureClass;

                InMemoryWorkspaceFactory wsf = new InMemoryWorkspaceFactoryClass();
                
                // Create a new in-memory workspace. This returns a name object.
                IWorkspaceName workspaceName = wsf.Create(null, "MyWorkspace", null, 0);

                IName name = (IName)workspaceName;

                // Open the workspace through the name object.
                IFeatureWorkspace workspace = (IFeatureWorkspace)name.Open();
                IWorkspaceEdit iwe = (IWorkspaceEdit)workspace;

                iwe.StartEditing(true);
                iwe.StartEditOperation();

                ESRI.ArcGIS.Geodatabase.IObjectClassDescription objectClassDescription = new ESRI.ArcGIS.Geodatabase.FeatureClassDescriptionClass();
                IFields flds = objectClassDescription.RequiredFields;

                IFeatureClass ifc_new = workspace.CreateFeatureClass("AAA", flds, null, null, esriFeatureType.esriFTSimple, ifl_active.FeatureClass.ShapeFieldName, "");
                IFeatureBuffer fb = ifc_new.CreateFeatureBuffer();
                IFeatureCursor csri = ifc_new.Insert(false);

                fb.Shape = geometry;
                csri.InsertFeature(fb);
                csri.Flush();

                iwe.StopEditOperation();
                iwe.StopEditing(true);

                IFeatureLayer fl = new FeatureLayerClass();
                fl.FeatureClass = ifc_new;
                fl.Name = "IntersectingShape";

                //IFeatureClassName ifcn = new FeatureClassNameClass();
                //ifcn.ShapeType = esriGeometryType.esriGeometryPolygon;

#if false
                ESRI.ArcGIS.AnalysisTools.Intersect tool = new ESRI.ArcGIS.AnalysisTools.Intersect();

                IGpValueTableObject tbl = new GpValueTableObjectClass();
                tbl.SetColumns(2);
                object row = "";
                object rank = 1;

                row = ifl_active;
                tbl.SetRow(0, ref row);
                tbl.SetValue(0, 1, ref rank);

                row = fl;
                tbl.SetRow(1, ref row);
                tbl.SetValue(1, 1, ref rank);

                tool.in_features = tbl;
#else
                ESRI.ArcGIS.AnalysisTools.Clip tool = new ESRI.ArcGIS.AnalysisTools.Clip();
                tool.clip_features = fl;
                tool.in_features = ifl_active;
#endif
                //IScratchWorkspaceFactory iwf_result = new ScratchWorkspaceFactoryClass();
                //IWorkspace ws = iwf_result.CreateNewScratchWorkspace();
                tool.out_feature_class = /*ws.PathName*/ "In_memory" + "\\NWI_Clip_Result";

                gp.AddOutputsToMap = true;
                gp.OverwriteOutput = true;
                gp.ToolExecuted += new EventHandler<ToolExecutedEventArgs>(gp_ToolExecuted);
                gp.ProgressChanged += new EventHandler<ProgressChangedEventArgs>(gp_ProgressChanged);

                gp.ExecuteAsync(tool);
            }
            catch (Exception err)
            {
            }
            finally
            {
                SelectArrowToolOnToolbar();
            }
        }

        void gp_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            IStepProgressor isp = ArcMap.Application.StatusBar.ProgressBar;
            ArcMap.Application.StatusBar.ProgressBar.Position = Convert.ToInt32( (e.ProgressPercentage * (isp.MaxRange - isp.MinRange) + isp.MinRange));
        }

        void gp_ToolExecuted(object sender, ToolExecutedEventArgs e)
        {
            IGeoProcessorResult igpr = e.GPResult;

            if (igpr.Status == esriJobStatus.esriJobSucceeded)
            {
                IGPUtilities igpu = new GPUtilitiesClass();
                IFeatureLayer ifl_Results = (IFeatureLayer)igpu.FindMapLayer("NWI_Clip_Result");

#if false
                if (ifl_Results.FeatureClass.Fields.FindField("AREA") > 0 ||
                    ifl_Results.FeatureClass.Fields.FindField("PERIMETER") > 0 ||
                    ifl_Results.FeatureClass.Fields.FindField("LENGTH") > 0)
                {
                    int[] fields = {0,0,0,0,0,0} ;
                    fields[0] = ifl_Results.FeatureClass.Fields.FindField("AREA");
                    fields[1] = ifl_Results.FeatureClass.Fields.FindField("A_UNIT");
                    fields[2] = ifl_Results.FeatureClass.Fields.FindField("PERIMETER");
                    fields[3] = ifl_Results.FeatureClass.Fields.FindField("P_UNIT");
                    fields[4] = ifl_Results.FeatureClass.Fields.FindField("LENGTH");
                    fields[5] = ifl_Results.FeatureClass.Fields.FindField("L_UNIT");

                    for (int j = 0; j < 6; j++)
                    {
                        if (fields[j] > 0)
                        {
                            ifl_Results.FeatureClass.DeleteField( ifl_Results.FeatureClass.Fields.get_Field(j));
                        }
                    }
                }
#else
                var dlg_result = System.Windows.Forms.MessageBox.Show("Re-calculate areas and perimeters? (required before query returns correct results)", "", System.Windows.Forms.MessageBoxButtons.YesNo);
                if (dlg_result == System.Windows.Forms.DialogResult.No) return;

                CalcAllValues.DoCalculation(ifl_Results);
#endif
            }
        }

        protected void SelectArrowToolOnToolbar()
        {
            ESRI.ArcGIS.Framework.ICommandBars commandBars = ArcMap.Application.Document.CommandBars;
            ESRI.ArcGIS.esriSystem.UID commandID = new ESRI.ArcGIS.esriSystem.UIDClass();
            commandID.Value = "esriArcMapUI.SelectTool"; // example: "esriArcMapUI.ZoomInTool";
            ESRI.ArcGIS.Framework.ICommandItem commandItem = commandBars.Find(commandID, false, false);

            if (commandItem != null)
                ArcMap.Application.CurrentTool = commandItem;
        }
    }

    class ClipNWIByPolygon : ClipNWI
    {
        ESRI.ArcGIS.Display.PolygonTracker pt = new ESRI.ArcGIS.Display.PolygonTrackerClass();

        ///<summary>Draws a polygon on the screen in the ActiveView where the mouse is clicked.</summary>
        ///
        ///<param name="activeView">An IActiveView interface</param>
        /// 
        ///<remarks>Ideally, this function would be called from within the OnMouseDown event that was created with the ArcGIS base tool template.</remarks>
        protected IGeometry DrawPolygon(IActiveView activeView)
        {
            if (activeView == null)
            {
                return null;
            }
            
            try
            {
                IScreenDisplay screenDisplay = activeView.ScreenDisplay;

                // Constant
                screenDisplay.StartDrawing(screenDisplay.hDC, (System.Int16)esriScreenCache.esriNoScreenCache); // Explicit Cast
                IRgbColor rgbColor = new RgbColorClass();
                rgbColor.Red = 255;

                IColor color = rgbColor; // Implicit Cast
                ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
                simpleFillSymbol.Color = color;

                ISymbol symbol = simpleFillSymbol as ISymbol; // Dynamic Cast
                IRubberBand rubberBand = new RubberPolygonClass();
                
                IGeometry geometry = rubberBand.TrackNew(screenDisplay, symbol);
                ITopologicalOperator2 ito = (ITopologicalOperator2)geometry;
                ito.Simplify();
                
                screenDisplay.SetSymbol(symbol);
                screenDisplay.DrawPolygon(geometry);
                screenDisplay.FinishDrawing();

                //activeView.Extent = geometry.Envelope;
                //ArcMap.Application.RefreshWindow();
                return geometry;
            }
            catch(Exception e) {
                return null;
            }
        }

        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            IGeometry polygon = DrawPolygon(ArcMap.Document.ActiveView);

            if (polygon != null)
            {
                DoClip(ArcMap.Document.ActiveView, polygon);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("NULL Shape");
            }
        }
    }

    class ClipNWIByRadius : ClipNWI
    {
        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            try
            {
                IRgbColor rgbColor = new RgbColorClass();
                rgbColor.Red = 255;

                IColor color = rgbColor; // Implicit Cast
                ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbolClass();
                simpleMarkerSymbol.Color = rgbColor;
                simpleMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSX;

                ISymbol symbol = simpleMarkerSymbol as ISymbol; // Dynamic Cast

                IScreenDisplay screenDisplay = ArcMap.Document.ActiveView.ScreenDisplay;
                IRubberBand2 rubberBand = new RubberPointClass();

                IGeometry geometry = rubberBand.TrackNew(screenDisplay, symbol);
                geometry.SpatialReference = ArcMap.Document.FocusMap.SpatialReference;

                screenDisplay.SetSymbol(symbol);
                screenDisplay.DrawPoint(geometry);
                screenDisplay.FinishDrawing();

                ITopologicalOperator ito = (ITopologicalOperator)geometry;

                string input = Microsoft.VisualBasic.Interaction.InputBox("Enter radius to use in map units: ", "Radius for Buffered Clip", "500");
                double distance = double.Parse(input);

                IGeometry circle = ito.Buffer(distance);

                DoClip(ArcMap.Document.ActiveView, circle);
            }
            catch (Exception e)
            {
            }
        }
    }
}