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
using System.ComponentModel;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.DataSourcesGDB;

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
        #endregion
    }

    public class BtnSymbolize_System : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Class : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Regime : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Special : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Chemistry : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Soil : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnQueryAll : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        protected override void OnClick()
        {
            NWIQuery qf = new NWIQuery();
            qf.Show();
            base.OnClick();
        }
    }

    public class BtnQueryNWIPlus : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        protected override void OnClick()
        {
            NWIPlusQuery qf = new NWIPlusQuery();
            qf.Show();
            base.OnClick();
        }
    }

    public class BtnSymbolize_Landscape : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Landform : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Waterform : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
        }
    }

    public class BtnSymbolize_Universal : BtnSymbolize_Base
    {
        protected override void OnClick()
        {
            SymbolizeByDialog sbd = new SymbolizeByDialog();
            sbd.Show(); // ShowDialog();

            //doSymbolize("Waterbody");
        }
    }
}