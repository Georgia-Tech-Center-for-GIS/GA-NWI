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
using ESRI.ArcGIS.GeoprocessingUI;

using ESRI.ArcGIS.DataSourcesGDB;

namespace GAWetlands
{
    public class BtnSymbolize_Base : ESRI.ArcGIS.Desktop.AddIns.Button
    {
    }

    public class NWIQueryButton : ESRI.ArcGIS.Desktop.AddIns.Button {
         public void doQuery() {
            OnClick();
        }
    }

    public class BtnQueryAll : NWIQueryButton
    {
    }

    public class BtnQueryNWIPlus : NWIQueryButton
    {
        protected override void OnClick()
        {
            NWIPlusQuery qf = new NWIPlusQuery();
            qf.Show();
            base.OnClick();
        }
    }

    public class BtnQueryCombined : NWIQueryButton
    {
        private static CombinedQueryForm qf = new CombinedQueryForm();
        protected override void OnClick()
        {
            try
            {
                if ((System.Windows.Forms.Application.OpenForms["CombinedQueryForm"] as CombinedQueryForm) == null)
                    qf.Show();
                else
                    qf.BringToFront();
            }
            catch (Exception ggg)
            {
                qf = new CombinedQueryForm();
                qf.Show();
            }

            base.OnClick();
        }
    }
    
    public class BtnSymbolize_Universal : BtnSymbolize_Base
    {
        private static SymbolizeByDialog sbd = new SymbolizeByDialog();
        protected override void OnClick()
        {
            try
            {
                if ((System.Windows.Forms.Application.OpenForms["SymbolizeByDialog"] as SymbolizeByDialog) == null)
                    sbd.Show(); // ShowDialog();
                else
                    sbd.BringToFront();
            }
            catch (Exception ggg)
            {
                sbd = new SymbolizeByDialog();
                sbd.Show();
            }
            //doSymbolize("Waterbody");
        }
    }

    public class BtnHelp : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        protected static string GetAssemblyPath()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var uriBuilder = new UriBuilder(codeBase);
            var asmPath = Uri.UnescapeDataString(uriBuilder.Path);
            asmPath = System.IO.Path.GetDirectoryName(asmPath);
            return asmPath;
        }

        protected override void OnClick()
        {
            System.Windows.Forms.Help.ShowHelp(null, "file://" + GetAssemblyPath() + "\\Help\\Georgia NWI Tools.chm");
        }
    }

    public class BtnInvokeParser : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        protected static string GetAssemblyPath()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var uriBuilder = new UriBuilder(codeBase);
            var asmPath = Uri.UnescapeDataString(uriBuilder.Path);
            asmPath = System.IO.Path.GetDirectoryName(asmPath);
            return asmPath;
        }

        protected override void OnClick()
        {
            try
            {
                //Set a reference to the IGPCommandHelper2 interface.

                IGPToolCommandHelper2 pToolHelper = new GPToolCommandHelperClass() as IGPToolCommandHelper2;

                //Set the tool you want to invoke.
                string toolboxName = GetAssemblyPath() + "\\Parser\\NWI Tools.tbx";
                pToolHelper.SetToolByName(toolboxName, "NWIAttributeParser");

                //Create the messages object and a bool to pass to the InvokeModal method.
                IGPMessages msgs;
                msgs = new GPMessagesClass();
                bool pok = true;

                //Invoke the tool. 
                pToolHelper.InvokeModal(0, null, out pok, out msgs);
            }
            catch (Exception e)
            {
            }
        }
    }
}