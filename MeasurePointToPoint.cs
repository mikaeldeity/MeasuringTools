using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace MeasuringTools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class MeasurePointToPoint : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                TransactionGroup tg = new TransactionGroup(doc);

                tg.Start();

                Transaction t1 = new Transaction(doc, "Measure");

                t1.Start();

                Reference pointref1 = uidoc.Selection.PickObject(ObjectType.PointOnElement, "Pick a point on an object");
                XYZ p1 = pointref1.GlobalPoint;
                if(p1 == null) { return Result.Failed;}

                DrawCircles(doc, p1);

                t1.Commit();

                t1.Start();

                Reference pointref2 = uidoc.Selection.PickObject(ObjectType.PointOnElement, "Pick a point on an object");
                XYZ p2 = pointref2.GlobalPoint;
                if (p2 == null) { return Result.Failed;}

                DrawCircles(doc, p2);

                t1.Commit();

                t1.Start();

                DrawLine(doc, p1, p2);

                t1.Commit();

                DisplayUnit dunits = doc.DisplayUnitSystem;

                if (dunits == DisplayUnit.METRIC)
                {
                    double distance = Math.Round(UnitUtils.ConvertFromInternalUnits(p1.DistanceTo(p2), DisplayUnitType.DUT_METERS), 3);
                    double distancex = Math.Abs(Math.Round(UnitUtils.ConvertFromInternalUnits(p1.X - p2.X, DisplayUnitType.DUT_METERS), 3));
                    double distancey = Math.Abs(Math.Round(UnitUtils.ConvertFromInternalUnits(p1.Y - p2.Y, DisplayUnitType.DUT_METERS), 3));
                    double distancez = Math.Abs(Math.Round(UnitUtils.ConvertFromInternalUnits(p1.Z - p2.Z, DisplayUnitType.DUT_METERS), 3));
                    TaskDialog.Show("Measure Point to Point", "Distance: " + distance.ToString() + " m" + "\nX: " + distancex + "m" + "\nY: " + distancey + "m" + "\nZ: " + distancez + "m");
                }
                else
                {
                    double distance = Math.Round(p1.DistanceTo(p2), 3);
                    double distancex = Math.Abs(Math.Round(p1.X - p2.X, 3));
                    double distancey = Math.Abs(Math.Round(p1.Y - p2.Y, 3));
                    double distancez = Math.Abs(Math.Round(p1.Z - p2.Z, 3));
                    TaskDialog.Show("Measure Point to Point", "Distance: " + distance.ToString() + " ft" + "\nX: " + distancex + "ft" + "\nY: " + distancey + "ft" + "\nZ: " + distancez + "ft");
                }

                tg.RollBack();

                return Result.Succeeded;
            }
            catch
            {
                return Result.Cancelled;
            }
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
