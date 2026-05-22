using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CostBIM.Services
{
    public class SelectEvent : IExternalEventHandler
    {
        public string TargetElementIdStr { get; set; } = string.Empty;

        public void Execute(UIApplication app)
        {
            if (app == null || string.IsNullOrEmpty(TargetElementIdStr)) return;
            
            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;

            Document doc = uidoc.Document;

            try
            {
#if REVIT2024 || true
                // In Revit 2024+, ElementId is represented by standard Int64 parsing
                if (long.TryParse(TargetElementIdStr, out long idVal))
                {
                    var id = new ElementId((int)idVal);
                    uidoc.Selection.SetElementIds(new List<ElementId> { id });
                    
                    // Center the Revit view on the highlighted element
                    uidoc.ShowElements(id);
                }
#else
                if (int.TryParse(TargetElementIdStr, out int idValOld))
                {
                    var id = new ElementId(idValOld);
                    uidoc.Selection.SetElementIds(new List<ElementId> { id });
                    uidoc.ShowElements(id);
                }
#endif
            }
            catch (Exception)
            {
                // Suppress exception when selection fails or view switches
            }
        }

        public string GetName() => "CostBIM_SelectEvent";
    }
}
