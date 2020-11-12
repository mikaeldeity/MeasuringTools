using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace MeasuringTools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class MeasureMultiplePoints : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            TransactionGroup tg = new TransactionGroup(doc);

            tg.Start();

            Transaction t1 = new Transaction(doc, "Measure");

            List<XYZ> points = new List<XYZ>();                

            bool stop = false;

            int count = 0;

            while (!stop)
            {
                Reference pointref = null;
                try
                {
                    pointref = uidoc.Selection.PickObject(ObjectType.PointOnElement, "Pick points");
                }
                catch
                {
                    stop = true;
                }
                if(pointref != null)
                {
                    XYZ p = pointref.GlobalPoint;
                    if (p == null) { return Result.Failed; }
                    points.Add(p);
                    t1.Start();
                    DrawCircles(doc, p);
                    t1.Commit();
                    count++;
                    if (count > 1)
                    {
                        t1.Start();
                        DrawLine(doc, points[count - 2], points[count - 1]);
                        t1.Commit();
                    }
                }                
            }

            DisplayUnit dunits = doc.DisplayUnitSystem;

            double distance = 0;

            for(int i = 0; i < points.Count -1; i++)
            {
                distance += points[i].DistanceTo(points[i + 1]);
            }

            if (dunits == DisplayUnit.METRIC)
            {
                distance = Math.Round(UnitUtils.ConvertFromInternalUnits(distance, DisplayUnitType.DUT_METERS), 3);
                TaskDialog.Show("Measure Point to Point", "Total distance: " + distance.ToString() + " m");
            }
            else
            {
                distance = Math.Round(distance, 3);
                TaskDialog.Show("Measure Point to Point", "Total distance: " + distance.ToString() + " ft");
            }

            tg.RollBack();

            return Result.Succeeded;
        }
        private ModelCurve[] DrawCircles(Document doc, XYZ point)
        {
            Arc c11 = Arc.Create(point, 0.05, 0, 2*Math.PI, XYZ.BasisX, XYZ.BasisY);
            Arc c12 = Arc.Create(point, 0.05, 0, 2*Math.PI, XYZ.BasisX, XYZ.BasisZ);
            Arc c13 = Arc.Create(point, 0.05, 0, 2*Math.PI, XYZ.BasisY, XYZ.BasisZ);
            Plane pl11 = Plane.CreateByOriginAndBasis(point, XYZ.BasisX, XYZ.BasisY);
            Plane pl12 = Plane.CreateByOriginAndBasis(point, XYZ.BasisX, XYZ.BasisZ);
            Plane pl13 = Plane.CreateByOriginAndBasis(point, XYZ.BasisY, XYZ.BasisZ);
            SketchPlane sk11 = SketchPlane.Create(doc, pl11);
            SketchPlane sk12 = SketchPlane.Create(doc, pl12);
            SketchPlane sk13 = SketchPlane.Create(doc, pl13);

            ModelCurve[] circles = new ModelCurve[3];

            if (doc.IsFamilyDocument)
            {
                ModelCurve cc1 = doc.FamilyCreate.NewModelCurve(c11, sk11);
                ModelCurve cc2 = doc.FamilyCreate.NewModelCurve(c12, sk12);
                ModelCurve cc3 = doc.FamilyCreate.NewModelCurve(c13, sk13);

                circles[0] = cc1;
                circles[1] = cc2;
                circles[2] = cc3;
            }
            else
            {
                ModelCurve cc1 = doc.Create.NewModelCurve(c11, sk11);
                ModelCurve cc2 = doc.Create.NewModelCurve(c12, sk12);
                ModelCurve cc3 = doc.Create.NewModelCurve(c13, sk13);

                circles[0] = cc1;
                circles[1] = cc2;
                circles[2] = cc3;
            }

            return circles;
        }
        private ModelCurve DrawLine(Document doc, XYZ point1,XYZ point2)
        {
            Line ln = Line.CreateBound(point1, point2);

            XYZ p3;

            if (ln.Direction.Z != 1)
            {
                p3 = new XYZ(point1.X, point1.Y, point1.Z + 1);
            }
            else
            {
                p3 = new XYZ(point1.X + 2, point1.Y, point1.Z);
            }

            Plane pl = Plane.CreateByThreePoints(point1, point2, p3);
            SketchPlane sk = SketchPlane.Create(doc, pl);

            if (doc.IsFamilyDocument)
            {
                ModelCurve ml = doc.FamilyCreate.NewModelCurve(ln, sk);
                return ml;
            }
            else
            {
                ModelCurve ml = doc.Create.NewModelCurve(ln, sk);
                return ml;
            }
        }
    }
}
