using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using CostBIM.Models;
using CostBIM.Views;

namespace CostBIM.Services
{
    public class ExtractEvent : IExternalEventHandler
    {
        private MainWindow? _ui;

        public ExtractEvent()
        {
        }

        public void SetWindow(MainWindow ui)
        {
            _ui = ui;
        }

        public void Execute(UIApplication app)
        {
            if (app == null) return;
            var doc = app.ActiveUIDocument?.Document;
            if (doc == null) return;

            try
            {
                // UI가 없는 예외 상황 방지
                if (_ui == null) return;

                // Perform live extraction with selected parameters
                List<ExtractedElement> elements = RevitElementExtractor.ExtractVisibleElements(doc, _ui.CustomParameterNames);

                // Update UI
                _ui.UpdateElementsList(elements);
            }
            catch (Exception ex)
            {
                _ui?.HideLoading();
                _ui?.SetStatus($"❌ 에러: {ex.Message}");
                TaskDialog.Show("CostBIM Error", $"객체 추출 중 에러가 발생했습니다:\n{ex.Message}");
            }
        }

        public string GetName() => "CostBIM_ExtractEvent";
    }
}
