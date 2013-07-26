using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;

using System.Collections;

namespace GAWetlands
{
    public class BtnSymbolize_Base : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        #region"Get Index Number from Layer Name"
        // ArcGIS Snippet Title:
        // Get Index Number from Layer Name
        // 
        // Long Description:
        // Get the index number for the specified layer name.
        // 
        // Add the following references to the project:
        // ESRI.ArcGIS.Carto
        // 
        // Intended ArcGIS Products for this snippet:
        // ArcGIS Desktop (ArcEditor, ArcInfo, ArcView)
        // ArcGIS Engine
        // ArcGIS Server
        // 
        // Applicable ArcGIS Product Versions:
        // 9.2
        // 9.3
        // 9.3.1
        // 10.0
        // 
        // Required ArcGIS Extensions:
        // (NONE)
        // 
        // Notes:
        // This snippet is intended to be inserted at the base level of a Class.
        // It is not intended to be nested within an existing Method.
        // 

        ///<summary>Get the index number for the specified layer name.</summary>
        /// 
        ///<param name="activeView">An IActiveView interface</param>
        ///<param name="layerName">A System.String that is the layer name in the active view. Example: "states"</param>
        ///  
        ///<returns>A System.Int32 representing a layer number</returns>
        ///  
        ///<remarks>Return values of 0 and greater are valid layers. A return value of -1 means the layer name was not found.</remarks>
        public static System.Int32 GetIndexNumberFromLayerName(ESRI.ArcGIS.Carto.IActiveView activeView, System.String layerName)
        {
            if (activeView == null || layerName == null)
            {
                return -1;
            }
            ESRI.ArcGIS.Carto.IMap map = activeView.FocusMap;

            // Get the number of layers
            int numberOfLayers = map.LayerCount;

            // Loop through the layers and get the correct layer index
            for (System.Int32 i = 0; i < numberOfLayers; i++)
            {
            //    return 0;
            }

            return 0;
        }

        public static void doSymbolizeLayer(IActiveView activeView, int i, string symbType) {
            ESRI.ArcGIS.Carto.IMap map = activeView.FocusMap;
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new ESRI.ArcGIS.Carto.LayerFileClass();
            
            System.Console.WriteLine((System.IO.Directory.GetCurrentDirectory()));

            if (ArcMap.Document.SelectedLayer == null)
            {
                System.Windows.Forms.MessageBox.Show("Select a layer before continuing.");
                return;
            }

            try
            {
                IFeatureLayer2 ifl2 = (IFeatureLayer2)ArcMap.Document.SelectedLayer;//(IFeatureLayer2)map.get_Layer(i);
                IGeoFeatureLayer igfl = (IGeoFeatureLayer)ifl2;
                ITable tbl = (ITable)((IFeatureLayer)igfl).FeatureClass;

                IQueryFilter iqf = new QueryFilterClass();
                iqf.WhereClause = "1=1";
                ICursor csr = (ICursor) ifl2.Search(iqf, true);

                string geomTypeName = "Polygon";

                if (ifl2.ShapeType == esriGeometryType.esriGeometryPolyline)
                    geomTypeName = "Polyline";

                //layerFile.Open("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\" + symbType + "_" + geomTypeName + ".lyr");
                var asmPath = GetAssemblyPath();

                layerFile.Open(asmPath + "/Symbology/" + symbType + "_" + geomTypeName + ".lyr");

                IDataStatistics ids = new DataStatisticsClass();
                ids.Field = symbType;
                ids.Cursor = csr;

                System.Collections.Generic.HashSet<string> hs = new System.Collections.Generic.HashSet<string>();

                IEnumerator iem = ids.UniqueValues;

                while (iem.MoveNext())
                {
                    //object val = rw.get_Value(rw.Fields.FindField(symbType));
                    object val = iem.Current;

                    if (val != null)
                        hs.Add(val.ToString());
                }

                //map.AddLayer(layerFile.Layer);

                IGeoFeatureLayer igfl_lyr = (IGeoFeatureLayer)layerFile.Layer;
                igfl.Renderer = igfl_lyr.Renderer;

                IUniqueValueRenderer urvl = (IUniqueValueRenderer)igfl.Renderer;
                for (int j = 0; j < urvl.ValueCount; j++)
                {
                    string a = urvl.get_Value(j);
                    if (a == null) continue;

                    //System.Windows.Forms.MessageBox.Show(a);

                    if (!hs.Contains(a))
                        urvl.RemoveValue(a);
                }

                ArcMap.Document.CurrentContentsView.Refresh(null);
            }
            catch (System.Exception err)
            {

            }
            finally
            {
                if(layerFile != null) layerFile.Close();
            }
        }

        private static string GetAssemblyPath()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var uriBuilder = new UriBuilder(codeBase);
            var asmPath = Uri.UnescapeDataString(uriBuilder.Path);
            asmPath = System.IO.Path.GetDirectoryName(asmPath);
            return asmPath;
        }
        #endregion

        static void DefineUniqueValueRenderer(IGeoFeatureLayer pGeoFeatureLayer, string
            fieldName)
        {
            IRandomColorRamp pRandomColorRamp = new RandomColorRampClass();
            //Create the color ramp for the symbols in the renderer.
            pRandomColorRamp.MinSaturation = 20;
            pRandomColorRamp.MaxSaturation = 40;
            pRandomColorRamp.MinValue = 85;
            pRandomColorRamp.MaxValue = 100;
            pRandomColorRamp.StartHue = 76;
            pRandomColorRamp.EndHue = 188;
            pRandomColorRamp.UseSeed = true;
            pRandomColorRamp.Seed = 43;

            //Create the renderer.
            IUniqueValueRenderer pUniqueValueRenderer = new UniqueValueRendererClass();

            ISimpleFillSymbol pSimpleFillSymbol = new SimpleFillSymbolClass();
            pSimpleFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
            pSimpleFillSymbol.Outline.Width = 0.4;

            //These properties should be set prior to adding values.
            pUniqueValueRenderer.FieldCount = 1;
            pUniqueValueRenderer.set_Field(0, fieldName);
            pUniqueValueRenderer.DefaultSymbol = pSimpleFillSymbol as ISymbol;
            pUniqueValueRenderer.UseDefaultSymbol = true;

            IDisplayTable pDisplayTable = pGeoFeatureLayer as IDisplayTable;
            IFeatureCursor pFeatureCursor = pDisplayTable.SearchDisplayTable(null, false) as
                IFeatureCursor;
            IFeature pFeature = pFeatureCursor.NextFeature();


            bool ValFound;
            int fieldIndex;

            IFields pFields = pFeatureCursor.Fields;
            fieldIndex = pFields.FindField(fieldName);
            while (pFeature != null)
            {
                ISimpleFillSymbol pClassSymbol = new SimpleFillSymbolClass();
                pClassSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
                pClassSymbol.Outline.Width = 0.4;

                string classValue;
                classValue = pFeature.get_Value(fieldIndex) as string;

                //Test to see if this value was added to the renderer. If not, add it.
                ValFound = false;
                for (int i = 0; i <= pUniqueValueRenderer.ValueCount - 1; i++)
                {
                    if (pUniqueValueRenderer.get_Value(i) == classValue)
                    {
                        ValFound = true;
                        break; //Exit the loop if the value was found.
                    }
                }
                //If the value was not found, it's new and will be added.
                if (ValFound == false)
                {
                    pUniqueValueRenderer.AddValue(classValue, fieldName, pClassSymbol as
                        ISymbol);
                    pUniqueValueRenderer.set_Label(classValue, classValue);
                    pUniqueValueRenderer.set_Symbol(classValue, pClassSymbol as ISymbol);
                }
                pFeature = pFeatureCursor.NextFeature();
            }
            //Since the number of unique values is known, the color ramp can be sized and the colors assigned.
            pRandomColorRamp.Size = pUniqueValueRenderer.ValueCount;
            bool bOK;
            pRandomColorRamp.CreateRamp(out bOK);

            IEnumColors pEnumColors = pRandomColorRamp.Colors;
            pEnumColors.Reset();
            for (int j = 0; j <= pUniqueValueRenderer.ValueCount - 1; j++)
            {
                string xv;
                xv = pUniqueValueRenderer.get_Value(j);
                if (xv != "")
                {
                    ISimpleFillSymbol pSimpleFillColor = pUniqueValueRenderer.get_Symbol(xv)
                        as ISimpleFillSymbol;
                    pSimpleFillColor.Color = pEnumColors.Next();
                    pUniqueValueRenderer.set_Symbol(xv, pSimpleFillColor as ISymbol);

                }
            }

            //'** If you didn't use a predefined color ramp in a style, use "Custom" here. 
            //'** Otherwise, use the name of the color ramp you selected.
            pUniqueValueRenderer.ColorScheme = "Custom";
            ITable pTable = pDisplayTable as ITable;
            bool isString = pTable.Fields.get_Field(fieldIndex).Type ==
                esriFieldType.esriFieldTypeString;
            pUniqueValueRenderer.set_FieldType(0, isString);
            pGeoFeatureLayer.Renderer = pUniqueValueRenderer as IFeatureRenderer;

            //This makes the layer properties symbology tab show the correct interface.
            IUID pUID = new UIDClass();
            pUID.Value = "{683C994E-A17B-11D1-8816-080009EC732A}";
            pGeoFeatureLayer.RendererPropertyPageClassID = pUID as UIDClass;
        }

        protected void doSymbolize(string type)
        {
             IActiveView iav = ArcMap.Document.ActiveView;
             //GetIndexNumberFromLayerName(iav, "NWI_POLY");
             doSymbolizeLayer(iav, 0, type);
        }

    }

    public class BtnSymbolize_System : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("System");
        }
    }

    public class BtnSymbolize_Class : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("Class1");
        }
    }

    public class BtnSymbolize_Regime : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("Water1");
        }
    }

    public class BtnSymbolize_Special : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("Special1");
        }
    }

    public class BtnSymbolize_Chemistry : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("Chem");
        }
    }

    public class BtnSymbolize_Soil : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            doSymbolize("Soil");
        }
    }

    public class BtnQueryAll : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        protected override void OnClick()
        {
            QueryForm qf = new QueryForm();
            qf.Show();
            base.OnClick();
        }
    }
}