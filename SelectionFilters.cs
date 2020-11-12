using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModellingTools
{
    internal sealed class SelectionFilterCurve : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is ModelCurve) return true;

            else if (elem is DetailCurve) return true;

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }

        private int GetCategoryIdAsInteger(Element element)
        {
            return element?.Category?.Id?.IntegerValue ?? -1;
        }
    }
    internal sealed class SelectionFilterFamily : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance) return true;

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }

        private int GetCategoryIdAsInteger(Element element)
        {
            return element?.Category?.Id?.IntegerValue ?? -1;
        }
    }
}
