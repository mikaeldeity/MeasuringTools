using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ModellingTools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class PlaceFamilyAlongCurve : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Error", "This tool only works in Project environment.");

                return Result.Cancelled;
            }

            Reference refcurve = null;

            Reference reffamily = null;

            Reference refface = null;

            bool hosted = false;

            try
            {
                reffamily = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilterFamily(), "Select Family");
            }
            catch
            {
                return Result.Cancelled;
            }

            FamilyInstance familyinstance = doc.GetElement(reffamily) as FamilyInstance;

            FamilySymbol symbol = familyinstance.Symbol;

            int behaviour = symbol.Family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR).AsInteger();

            if (behaviour != 5 && behaviour != 0)
            {
                TaskDialog.Show("Error", "This Family cannot be used. Only workplane and face based are permitted.");
                return Result.Failed;
            }

            if (behaviour == 5)
            {
                hosted = true;

                try
                {
                    refface = uidoc.Selection.PickObject(ObjectType.Face, "Select Face");
                }
                catch
                {
                    return Result.Cancelled;
                }
            }

            try
            {
                refcurve = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilterCurve(), "Select Curves");
            }
            catch
            {
                return Result.Cancelled;
            }

            var exportdialog = new ModellingTools.Dialogs.DivisionNumberInputDialog();

            var dialog = exportdialog.ShowDialog();

            if (dialog != DialogResult.OK)
            {
                return Result.Cancelled;
            }

            int divisions = Convert.ToInt32(exportdialog.numericUpDown.Value);

            Transaction t1 = new Transaction(doc, "Place Families along Curve");

            Curve curve = null;

            if(doc.GetElement(refcurve) is ModelCurve)
            {
                ModelCurve modelcurve = doc.GetElement(refcurve) as ModelCurve;

                curve = modelcurve.GeometryCurve;
            }
            else if (doc.GetElement(refcurve) is DetailCurve)
            {
                DetailCurve detailcurve = doc.GetElement(refcurve) as DetailCurve;

                curve = detailcurve.GeometryCurve;
            }

            t1.Start();

            PlacePointsOnCurve(hosted, doc, divisions, curve, symbol, refface);

            t1.Commit();

            return Result.Succeeded;
        }
        internal void PlacePointsOnCurve(bool hosted, Document doc, int divisions, Curve curve, FamilySymbol symbol, Reference host)
        {
            List<XYZ> points = new List<XYZ>();
            List<XYZ> tangents = new List<XYZ>();
            List<XYZ> normals = new List<XYZ>();
            List<double> parameters = new List<double>();
            List<double> angles = new List<double>();
            List<Line> lines = new List<Line>();

            XYZ vert = new XYZ(0, 1, 0);

            int elements = divisions;

            if (curve is Line | curve is Arc)
            {
                if (!curve.IsBound)
                {
                    curve.MakeBound(0, 2 * Math.PI);
                }

                for (int i = 0; i < elements-1; i++)
                {
                    double param = i * (1.0 / (divisions - 1));
                    XYZ point = curve.Evaluate(param, true);
                    parameters.Add(param);
                    points.Add(point);                    
                }

                if (curve.IsBound)
                {
                    points.Add(curve.Evaluate(1, true));
                    parameters.Add(1);
                }
            }
            else
            {
                if (!curve.IsBound)
                {
                    curve.MakeBound(0, 2 * Math.PI);
                    divisions++;
                }

                double stepsize = curve.Length / (divisions - 1);
                double dist = 0.0;

                IList<XYZ> tessellation = curve.Tessellate();

                XYZ p = curve.GetEndPoint(0);

                foreach (XYZ q in tessellation)
                {
                    if (points.Count == 0)
                    {
                        points.Add(p);
                        double param = curve.Project(p).Parameter;
                        parameters.Add(curve.ComputeNormalizedParameter(param));
                        dist = 0.0;
                    }
                    else
                    {
                        dist += p.DistanceTo(q);

                        if (dist >= stepsize)
                        {
                            points.Add(q);
                            double param = curve.Project(q).Parameter;
                            parameters.Add(curve.ComputeNormalizedParameter(param));
                            dist = 0.0;
                        }
                        p = q;
                    }
                }

                if (curve.IsBound)
                {
                    points.Add(curve.Evaluate(1, true));
                    parameters.Add(1);
                }
            }

            for (int i = 0; i < points.Count; i++)
            {
                try
                {
                    if (hosted)
                    {
                        XYZ vector = curve.ComputeDerivatives(parameters[i], true).BasisX;
                        doc.Create.NewFamilyInstance(host, points[i], vector, symbol);
                    }
                    else
                    {
                        doc.Create.NewFamilyInstance(points[i], symbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    }
                }
                catch { }
            }
        }
    }   
}
