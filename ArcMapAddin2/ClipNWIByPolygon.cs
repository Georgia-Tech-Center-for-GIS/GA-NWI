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
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.Catalog;

namespace GAWetlands
{
    class ClipNWI : ESRI.ArcGIS.Desktop.AddIns.Tool {
        IGPUtilities igpu = new GPUtilitiesClass();
        IGxDialog gd = new GxDialogClass();
        Geoprocessor gp = new Geoprocessor();

        protected void DoClip(IActiveView activeView, IGeometry geometry)
        {           
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

                gd.Title = "Save clipped feature class";

                gd.ObjectFilter = new GxFilterFeatureClassesClass (); //new GxFilterFeatureClassesClass();
                if (gd.DoModalSave(ArcMap.Application.hWnd) == false)
                {
                    return;
                }

                while (System.IO.File.Exists(gd.FinalLocation.FullName + "\\" + gd.Name) || gd.ReplacingObject)
                {
                    if (System.Windows.Forms.MessageBox.Show("You've selected a feature class that already exists. Select a different feature class name.", "Overwrite Feature Class", System.Windows.Forms.MessageBoxButtons.RetryCancel) == System.Windows.Forms.DialogResult.Cancel)
                    {
                        return;
                    }

                    if (gd.DoModalSave(ArcMap.Application.hWnd) == false)
                    {
                        return;
                    }
                }

                IGeoDataset igd_dest = (IGeoDataset)ifl_active.FeatureClass;

                if (igd_dest.SpatialReference.Name != geometry.SpatialReference.Name)
                {
                    geometry.Project(igd_dest.SpatialReference);
                }

                // Create a new in-memory workspace. This returns a name object.
                InMemoryWorkspaceFactory wsf = new InMemoryWorkspaceFactoryClass();
                IWorkspaceName workspaceName = wsf.Create(null, "MyWorkspace", null, 0);

                IName name = (IName)workspaceName;

                // Open the workspace through the name object.
                IFeatureWorkspace workspace = (IFeatureWorkspace)name.Open();
                IWorkspaceEdit iwe = (IWorkspaceEdit)workspace;

                ESRI.ArcGIS.Geodatabase.IObjectClassDescription objectClassDescription = new ESRI.ArcGIS.Geodatabase.FeatureClassDescriptionClass();
                IFields flds = objectClassDescription.RequiredFields;

                IFeatureClass ifc_new = workspace.CreateFeatureClass("AAA", flds, null, null, esriFeatureType.esriFTSimple, ifl_active.FeatureClass.ShapeFieldName, "");
                IFeatureLayer fl = new FeatureLayerClass();
                IGeoFeatureLayer gfl = (IGeoFeatureLayer)fl;

                IRgbColor rgbColor = new RgbColorClass();
                rgbColor.Red = 255;
                rgbColor.Green = 0;
                rgbColor.Blue = 0;

                IColor color = rgbColor; // Implicit Cast

                fl.FeatureClass = ifc_new;
                fl.Name = "IntersectingShape";

                ISimpleFillSymbol sfs = new SimpleFillSymbolClass();
                sfs.Outline.Color = color;

                color.NullColor = true;
                sfs.Color = color;

                sfs.Outline.Width = 5;

                ISimpleRenderer isr = new SimpleRendererClass();
                isr.Symbol = (ISymbol)sfs;

                gfl.Renderer = (IFeatureRenderer)isr;
                gfl.SpatialReference = igd_dest.SpatialReference;

                iwe.StartEditing(true);
                iwe.StartEditOperation();

                IFeatureBuffer fb = ifc_new.CreateFeatureBuffer();
                IFeatureCursor csri = ifc_new.Insert(false);
                
                fb.Shape = geometry;
                csri.InsertFeature(fb);
                csri.Flush();

                iwe.StopEditOperation();
                iwe.StopEditing(true);

                map.AddLayer(fl);

                ESRI.ArcGIS.AnalysisTools.Clip tool = new ESRI.ArcGIS.AnalysisTools.Clip();
                tool.clip_features = fl;
                tool.in_features = ifl_active;

                tool.out_feature_class = gd.FinalLocation.FullName + "\\" + gd.Name; /*ws.PathName*/ //"In_memory" + "\\NWI_Clip_Result";

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

            switch (igpr.Status)
            {
                case esriJobStatus.esriJobExecuting:
                    break;

                case esriJobStatus.esriJobSucceeded:
                    {
                        try
                        {
                            IFeatureLayer ifl_Results = (IFeatureLayer)igpu.FindMapLayer(gd.Name.Split('.')[0]);

                            /*var dlg_result = System.Windows.Forms.MessageBox.Show("Re-calculate areas? (required before query returns correct results)", "", System.Windows.Forms.MessageBoxButtons.YesNo);
                            if (dlg_result == System.Windows.Forms.DialogResult.No) return;*/

                            CalcAllValues.DoCalculation(ifl_Results);
                        }
                        catch (Exception eeeee)
                        {
                        }
                    }
                    goto default;

                default:
                    gp.ToolExecuted -= gp_ToolExecuted;
                    gp.ProgressChanged -= gp_ProgressChanged;
                    break;
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
                System.Windows.Forms.MessageBox.Show("Shape drawing aborted!");
                SelectArrowToolOnToolbar();
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