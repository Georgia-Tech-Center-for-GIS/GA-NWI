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
    public abstract class StatHelperClass
    {
        /** Are values to be recalculated from geoemetry */
        public bool doReCalcValues = false;

        /** Seaches for convenience (pre-calculated) fields */
        public abstract void SearchForFields(ITable tab);

        protected abstract double DoMeasure(IRow rw);
        protected abstract int GetFieldIndex();
        protected abstract int GetFieldUnitIndex();

        /** Returns additional column headings for table display */
        public abstract string GetColumnHeadings();

        /** Collection of Values */
        public System.Collections.Generic.HashSet<double> values = new HashSet<double>();

        /** from coordinate system */
        public string LinearUnit = "";

        /** ESRI flavored current Linear Unit */
        private esriUnits currentLinearUnit = esriUnits.esriUnknownUnits;
        /** ESRI flavored current Area Unit */
        private esriAreaUnits currentAreaUnit = esriAreaUnits.esriUnknownAreaUnits;

        protected int fieldTier = -1;
        public virtual bool useArealUnit
        {
            get
            {
                return false;
            }
        }

        private static IGPArealUnit igpau = new GPArealUnitClass();
        private static IUnitConverter iuc = new UnitConverterClass();

        public bool DoConversion(string newCoordSystem)
        {
            esriUnits newLinearUnits = esriUnits.esriUnknownUnits;
            esriAreaUnits newArealUnits = esriAreaUnits.esriUnknownAreaUnits;

            switch (newCoordSystem)
            {
                case "Acres":
                    newArealUnits = esriAreaUnits.esriAcres;
                    break;

                case "Hectares":
                    newArealUnits = esriAreaUnits.esriHectares;
                    break;

                case "Square Feet":
                    newArealUnits = esriAreaUnits.esriSquareFeet;
                    break;

                case "Feet":
                    newLinearUnits = esriUnits.esriFeet;
                    break;

                case "Square Meters":
                    newArealUnits = esriAreaUnits.esriSquareMeters;
                    break;

                case "Meters":
                    newLinearUnits = esriUnits.esriMeters;
                    break;
            }

            igpau.Units = this.currentAreaUnit;

            for (int i = SumIndex; i < MeanIndex; i++)
            {
                double oldValue = double.NaN;
                switch (i)
                {
                    case SumIndex:
                        oldValue = values.Sum();
                        break;

                    case MinIndex:
                        oldValue = values.Min();
                        break;

                    case MaxIndex:
                        oldValue = values.Max();
                        break;

                    case MeanIndex:
                        oldValue = values.Average();
                        break;
                }

                double newValue = double.NaN;

                if (useArealUnit)
                {
                    if (newArealUnits == esriAreaUnits.esriUnknownAreaUnits) return false;

                    igpau.Value = oldValue;
                    newValue = igpau.ConvertValue(newArealUnits);
                }
                else
                {
                    if (newLinearUnits == esriUnits.esriUnknownUnits) return false;
                    newValue = iuc.ConvertUnits(oldValue, currentLinearUnit, newLinearUnits);
                }

                measuresInCurrentUnits[i] = newValue;
            }

            //currentAreaUnit = newArealUnits;
            //currentLinearUnit = newLinearUnits;

            //dataGridView1.Rows[1].Cells[1].Value = newCoordSystem;

            return true;
        }

        private double[] measuresInCurrentUnits = new double[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };

        public const int CountIndex = 2;
        public const int SumIndex = 3;
        public const int MinIndex = 4;
        public const int MaxIndex = 5;
        public const int MeanIndex = 6;
        public const int RangeIndex = 7;

        public double sum
        {
            get
            {
                if (values == null)
                    return double.NaN;
                else
                {
                    if (double.IsNaN(measuresInCurrentUnits[SumIndex])) return Math.Round(values.Sum(), 4);
                    else return Math.Round(measuresInCurrentUnits[SumIndex], 4);
                }
            }
        }

        public double min
        {
            get
            {
                if (values == null)
                    return double.NaN;
                else
                {
                    if (double.IsNaN(measuresInCurrentUnits[MinIndex])) return Math.Round(values.Min(), 4);
                    else return Math.Round(measuresInCurrentUnits[MinIndex], 4);
                }
            }
        }

        public double max
        {
            get
            {
                if (values == null)
                    return double.NaN;
                else
                {
                    if (double.IsNaN(measuresInCurrentUnits[MaxIndex])) return Math.Round(values.Max(), 4);
                    else return Math.Round(measuresInCurrentUnits[MaxIndex], 4);
                }
            }
        }

        public int count { get { if (values != null) return values.Count(); else return 0; } }

        public double mean
        {
            get
            {
                if (values == null)
                    return double.NaN;
                else
                {
                    if (double.IsNaN(measuresInCurrentUnits[MeanIndex])) return Math.Round(values.Average(), 4);
                    else return Math.Round(measuresInCurrentUnits[MeanIndex], 4);
                }
            }
        }

        public double range
        {
            get
            {
                return max - min;
            }
        }

        protected string GetSpatialReferenceLinearUnit(ISpatialReference isr)
        {
            IProjectedCoordinateSystem ipcs = (IProjectedCoordinateSystem)isr;
            ILinearUnit ilu = ipcs.CoordinateUnit;
            return ilu.Name;
        }

        protected static int AddNewField(IClass fclass, string Name)
        {
            IField nw = new FieldClass();
            IFieldEdit new_field = (IFieldEdit)nw;

            new_field.Name_2 = Name;
            if (Name.IndexOf("UNIT", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                new_field.Type_2 = esriFieldType.esriFieldTypeString;
                new_field.Length_2 = 20;
            }
            else
            {
                new_field.Type_2 = esriFieldType.esriFieldTypeDouble;
                new_field.DefaultValue_2 = double.NaN;
            }
            new_field.IsNullable_2 = true;
            fclass.AddField(new_field);

            return fclass.FindField(Name);
        }

        public void ProcessFeature(IWorkspaceEdit iwe, IFeatureLayer ifl_active, IRow rw)
        {
            double value = double.NaN;

            if (this.LinearUnit == null || this.LinearUnit == "")
            {
                if (fieldTier == 2)
                {
                    LinearUnit = rw.get_Value(GetFieldUnitIndex()).ToString();
                }

                if (this.LinearUnit == null || this.LinearUnit.Trim() == "")
                {
                    LinearUnit = GetSpatialReferenceLinearUnit(((IFeature)rw).Shape.SpatialReference);
                }
            }

            if (LinearUnit.IndexOf("meter", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                if (useArealUnit)
                {
                    currentAreaUnit = esriAreaUnits.esriSquareMeters;
                    LinearUnit = "Square Meters";
                }
                else
                {
                    currentLinearUnit = esriUnits.esriMeters;
                }
            }
            else if (LinearUnit.IndexOf("feet", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                if (useArealUnit)
                {
                    currentAreaUnit = esriAreaUnits.esriSquareFeet;
                    LinearUnit = "Square Feet";
                }
                else
                {
                    currentLinearUnit = esriUnits.esriFeet;
                }
            }
            else if (LinearUnit.IndexOf("acre", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                currentAreaUnit = esriAreaUnits.esriAcres;
                currentLinearUnit = esriUnits.esriUnknownUnits;
                LinearUnit = "Acres";
            }

            if (doReCalcValues || !double.TryParse(rw.get_Value(GetFieldIndex()).ToString(), out value) || value == double.NaN || value == 0.0)
            {
                value = DoMeasure(rw);

                //try writing the (single) measured value to the table
                try
                {
                    IFeature feat = (IFeature)rw;

                    //if we are re-calculating all values, there is no need to start editing on each row
                    if (!doReCalcValues)
                    {
                        if (!iwe.IsBeingEdited())
                            iwe.StartEditing(true);

                        iwe.StartEditOperation();
                    }

                    feat.set_Value(GetFieldIndex(), value);
                    feat.set_Value(GetFieldUnitIndex(), LinearUnit);
                    feat.Store();
                }
                catch (Exception err)
                {
                }
                finally
                {
                    if (!doReCalcValues)
                    {
                        iwe.StopEditOperation();

                        if (iwe.IsBeingEdited())
                            iwe.StopEditing(true);

                        //there may be more than one row that requires editing
                        doReCalcValues = true;
                    }
                }
            }

            values.Add(value);
        }
    }

    class PolylineHelperClass : StatHelperClass
    {
        private int length_field_index = -1;
        private int length_field_unit_index = -1;

        protected override double DoMeasure(IRow rw)
        {
            double value = double.NaN;
            //if the parse did not succeed, or is NaN, try measuring directly
            IFeature feat = (IFeature)rw;
            IPolyline ipl = (IPolyline)feat.Shape;
            value = ipl.Length;
            return value;
        }

        public override void SearchForFields(ITable tab)
        {
            //TIER 1: File/SDE Geodatabase
            length_field_index = tab.FindField("Shape_Length");
            fieldTier = 1;

            //TIER 2: Shapefile with LENGTH fields already present
            if (length_field_index == -1)
            {
                length_field_index = tab.FindField("LENG");
                length_field_unit_index = tab.FindField("L_UNIT");
                fieldTier = 2;
            }

            //TIER 3: Neither present. Need to add field and calculate
            if (length_field_index == -1)
            {
                length_field_index = AddNewField((IClass)tab, "LENG");
                length_field_unit_index = AddNewField((IClass)tab, "L_UNIT");
                fieldTier = 3;
                doReCalcValues = true;
            }
        }

        protected override int GetFieldIndex()
        {
            return length_field_index;
        }

        public override string GetColumnHeadings()
        {
            return "Length";
        }

        protected override int GetFieldUnitIndex()
        {
            return length_field_unit_index;
        }
    }

    class PolygonPerimeter_HelperClass : StatHelperClass
    {
        private int perimeter_field_index = -1;
        private int perimeter_field_unit_index = -1;

        protected override double DoMeasure(IRow rw)
        {
            IFeature feat = (IFeature)rw;
            IPolygon plg = (IPolygon)feat.Shape;
            return plg.Length;
        }

        public override void SearchForFields(ITable tab)
        {
            //TIER 1: File/SDE Geodatabase
            perimeter_field_index = tab.FindField("Shape_Length");
            fieldTier = 1;

            //TIER 2: Shapefile with AREA and PERIMETER fields already present
            if (perimeter_field_index == -1)
            {
                perimeter_field_index = tab.FindField("PERIMETER");
                perimeter_field_unit_index = tab.FindField("P_UNIT");
                fieldTier = 2;
            }

            //TIER 3: Neither present. Need to add field and calculate
            if (perimeter_field_index == -1)
            {
                perimeter_field_index = AddNewField(tab, "PERIMETER");
                perimeter_field_unit_index = AddNewField(tab, "P_UNIT");
                doReCalcValues = true;
                fieldTier = 3;
            }
        }

        public override string GetColumnHeadings()
        {
            return "Perimeter";
        }

        protected override int GetFieldIndex()
        {
            return perimeter_field_index;
        }

        protected override int GetFieldUnitIndex()
        {
            return perimeter_field_unit_index;
        }
    }

    class PolygonArea_HelperClass : StatHelperClass
    {
        private int area_field_index = -1;
        private int area_field_unit_index = -1;

        public override bool useArealUnit
        {
            get
            {
                return true;
            }
        }

        protected override double DoMeasure(IRow rw)
        {
            IFeature feat = (IFeature)rw;
            IArea ia = (IArea)feat.Shape;
            return ia.Area;
        }

        public override void SearchForFields(ITable tab)
        {
            //TIER 1: File/SDE Geodatabase
            area_field_index = tab.FindField("Shape_Area");
            fieldTier = 1;

            //TIER 2: Shapefile with AREA and PERIMETER fields already present
            if (area_field_index == -1)
            {
                area_field_index = tab.FindField("AREA");
                area_field_unit_index = tab.FindField("AR_UNIT");
                fieldTier = 2;
            }

            //TIER 3: Neither present. Need to add field and calculate
            if (area_field_index == -1)
            {
                area_field_index = AddNewField(tab, "AREA");
                area_field_unit_index = AddNewField(tab, "AR_UNIT");
                doReCalcValues = true;
                fieldTier = 3;
            }
        }

        public override string GetColumnHeadings()
        {
            return "Area";
        }

        protected override int GetFieldIndex()
        {
            return area_field_index;
        }

        protected override int GetFieldUnitIndex()
        {
            return area_field_unit_index;
        }
    }
}