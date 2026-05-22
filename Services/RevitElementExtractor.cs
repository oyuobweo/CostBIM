using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using CostBIM.Models;

namespace CostBIM.Services
{


    public class RevitElementExtractor
    {
        // Safe Constants for Unit Conversion (Feet to Metric)
        private const double FeetToMeters = 0.3048;
        private const double SqFeetToSqMeters = 0.09290304;
        private const double CuFeetToCuMeters = 0.028316846592;

        private static bool IsMepCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return false;
            string lower = categoryName.ToLower();
            // 영문 및 국문 MEP 관련 카테고리 식별
            return lower.Contains("duct") || 
                   lower.Contains("pipe") || 
                   lower.Contains("terminal") || 
                   lower.Contains("accessory") ||
                   lower.Contains("fitting") ||
                   lower.Contains("기계") || 
                   lower.Contains("배관") || 
                   lower.Contains("덕트") ||
                   lower.Contains("장비") ||
                   lower.Contains("conduit") ||
                   lower.Contains("cable") ||
                   lower.Contains("조명") ||
                   lower.Contains("소방") ||
                   lower.Contains("스프링클러");
        }

        public static ParameterSchema ScanAvailableParameters(Document doc)
        {
            var schema = new ParameterSchema();
            if (doc == null) return schema;

            View activeView = doc.ActiveView;
            if (activeView == null || activeView.ViewType != ViewType.ThreeD) return schema;

            var collector = new FilteredElementCollector(doc, activeView.Id)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent();

            var builtInSet = new SortedSet<string>();
            var projectSet = new SortedSet<string>();
            var sharedSet = new SortedSet<string>();

            int count = 0;
            foreach (Element elem in collector)
            {
                if (elem == null || elem.Category == null) continue;
                if (elem.Category.CategoryType != CategoryType.Model) continue;
                if (elem is ImportInstance || elem is Group) continue;

                // 🌟 BuiltIn 및 카테고리 명칭 기반 정밀 블랙리스트 필터링 (불필요 요소 원천 차단)
                string catName = elem.Category.Name;
                if (string.IsNullOrEmpty(catName)) continue;

                string lowerCat = catName.ToLower();
                if (lowerCat.Contains("뷰") || 
                    lowerCat.Contains("카메라") || 
                    lowerCat.Contains("단면") || 
                    lowerCat.Contains("section") || 
                    lowerCat.Contains("참조") || 
                    lowerCat.Contains("reference") ||
                    lowerCat.Contains("그리드") || 
                    lowerCat.Contains("grid") || 
                    lowerCat.Contains("레벨") || 
                    lowerCat.Contains("level") ||
                    lowerCat.Contains("센터라인") || 
                    lowerCat.Contains("centerline") || 
                    lowerCat.Contains("center line") ||
                    lowerCat.Contains("범위") || 
                    lowerCat.Contains("scope") ||
                    lowerCat.Contains("분석") || 
                    lowerCat.Contains("analytical"))
                {
                    continue;
                }

                // 🌟 [물리적 기하 실체 검증] 실제로 3D 공간 상에 면적/체적 형상을 가지는 실체(Solid)인지 정밀 판별 (상세 수준 Fine으로 기하 검증 수행)
                if (!HasValidPhysicalGeometry(elem))
                {
                    continue;
                }

                // 1) Instance Parameters
                foreach (Parameter param in elem.Parameters)
                {
                    CategorizeAndAdd(param, builtInSet, projectSet, sharedSet, schema.GroupMap);
                }

                // 2) Type Parameters
                ElementId typeId = elem.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                {
                    Element typeElem = doc.GetElement(typeId);
                    if (typeElem != null)
                    {
                        foreach (Parameter param in typeElem.Parameters)
                        {
                            CategorizeAndAdd(param, builtInSet, projectSet, sharedSet, schema.GroupMap);
                        }
                    }
                }

                count++;
                if (count > 500) break; // Scan limit for performance
            }

            schema.BuiltIn = builtInSet.ToList();
            schema.Project = projectSet.ToList();
            schema.Shared = sharedSet.ToList();

            return schema;
        }

        private static long GetElementIdValue(ElementId id)
        {
            if (id == null) return -1;
            
            // 1) Revit 2024+ "Value" 속성 검색 (long 리턴)
            var propValue = typeof(ElementId).GetProperty("Value");
            if (propValue != null)
            {
                var valObj = propValue.GetValue(id);
                return valObj != null ? Convert.ToInt64(valObj) : -1;
            }
            
            // 2) Revit 2023 이하 "IntegerValue" 속성 검색 (int 리턴)
            var propIntegerValue = typeof(ElementId).GetProperty("IntegerValue");
            if (propIntegerValue != null)
            {
                var valObj = propIntegerValue.GetValue(id);
                return valObj != null ? Convert.ToInt64(valObj) : -1;
            }
            
            return -1;
        }

        private static string GetParameterGroupName(Parameter param)
        {
            if (param == null || param.Definition == null) return "기타";
            
            try
            {
                // 1) Revit 2024+ (GroupId / ForgeTypeId) 검출
                var propGroupId = typeof(InternalDefinition).GetProperty("GroupId");
                if (propGroupId != null)
                {
                    var groupIdObj = propGroupId.GetValue(param.Definition);
                    if (groupIdObj != null)
                    {
                        var methodGetLabel = typeof(LabelUtils).GetMethod("GetLabelForGroup", new Type[] { groupIdObj.GetType() });
                        if (methodGetLabel != null)
                        {
                            var label = methodGetLabel.Invoke(null, new object[] { groupIdObj }) as string;
                            if (!string.IsNullOrEmpty(label)) return label;
                        }
                    }
                }
            }
            catch { }

            try
            {
                // 2) Revit 2023 이하 및 하위 호환성 (ParameterGroup)
                var propParamGroup = typeof(Definition).GetProperty("ParameterGroup");
                if (propParamGroup != null)
                {
                    var groupObj = propParamGroup.GetValue(param.Definition);
                    if (groupObj != null)
                    {
                        // JIT TypeLoadException을 방지하기 위해 BuiltInParameterGroup 타입을 동적으로 로드합니다.
                        var typeBuiltInParamGroup = typeof(Definition).Assembly.GetType("Autodesk.Revit.DB.BuiltInParameterGroup");
                        if (typeBuiltInParamGroup != null)
                        {
                            var methodGetLabel = typeof(LabelUtils).GetMethod("GetLabelFor", new Type[] { typeBuiltInParamGroup });
                            if (methodGetLabel != null)
                            {
                                var label = methodGetLabel.Invoke(null, new object[] { groupObj }) as string;
                                if (!string.IsNullOrEmpty(label)) return label;
                            }
                        }
                    }
                }
            }
            catch { }

            return "기타";
        }

        private static void CategorizeAndAdd(Parameter param, SortedSet<string> builtIn, SortedSet<string> project, SortedSet<string> shared, Dictionary<string, string> groupMap)
        {
            if (param == null || param.Definition == null || !param.HasValue) return;
            string name = param.Definition.Name;
            if (string.IsNullOrEmpty(name)) return;
 
            // Skip internal Revit GUID-like names or double underscore system parameters
            if (name.StartsWith("__") || name.StartsWith("AP_") || name.StartsWith("ExtEvent")) return;
 
            // 🌟 파라미터 한글 그룹명 추출 및 매핑 추가
            if (!groupMap.ContainsKey(name))
            {
                groupMap[name] = GetParameterGroupName(param);
            }

            if (param.IsShared)
            {
                shared.Add(name);
            }
            else if (GetElementIdValue(param.Id) > 0)
            {
                project.Add(name);
            }
            else
            {
                builtIn.Add(name);
            }
        }

        public static List<ExtractedElement> ExtractVisibleElements(Document doc, List<string> customParamNames)
        {
            var resultList = new List<ExtractedElement>();

            if (doc == null) return resultList;

            // Get Active View
            View activeView = doc.ActiveView;
            if (activeView == null || activeView.ViewType != ViewType.ThreeD)
            {
                throw new InvalidOperationException("현재 활성화된 뷰가 3D 뷰가 아닙니다. 3D 뷰를 활성화한 후 다시 실행해주세요.");
            }

            // Gather all physical visible elements in the active 3D view
            var collector = new FilteredElementCollector(doc, activeView.Id)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent();

            foreach (Element elem in collector)
            {
                // Filter out non-physical elements
                if (elem == null || elem.Category == null) continue;
                
                // 1) Filter by CategoryType: MUST be a Model category (excludes Section Box, Scope Box, Levels, Viewer, etc.)
                if (elem.Category.CategoryType != CategoryType.Model) continue;

                // 2) Exclude imported CAD links or system grouping helper classes
                if (elem is ImportInstance || elem is Group) continue;

                // 3) BuiltIn 및 카테고리 명칭 기반 정밀 블랙리스트 필터링 (비물리 참조 요소 원천 차단)
                string catName = elem.Category.Name;
                if (string.IsNullOrEmpty(catName)) continue;

                // 실무에서 불필요한 참조용 범주 키워드 차단 (센터라인, 단면상자 등)
                string lowerCat = catName.ToLower();
                if (lowerCat.Contains("뷰") || 
                    lowerCat.Contains("카메라") || 
                    lowerCat.Contains("단면") || 
                    lowerCat.Contains("section") || 
                    lowerCat.Contains("참조") || 
                    lowerCat.Contains("reference") ||
                    lowerCat.Contains("그리드") || 
                    lowerCat.Contains("grid") || 
                    lowerCat.Contains("레벨") || 
                    lowerCat.Contains("level") ||
                    lowerCat.Contains("센터라인") || 
                    lowerCat.Contains("centerline") || 
                    lowerCat.Contains("center line") ||
                    lowerCat.Contains("범위") || 
                    lowerCat.Contains("scope") ||
                    lowerCat.Contains("분석") || 
                    lowerCat.Contains("analytical"))
                {
                    continue;
                }

                // 4) 🌟 [물리적 기하 실체 검증] 실제로 3D 공간 상에 면적/체적 형상을 가지는 실체(Solid)인지 정밀 판별 (상세 수준 Fine으로 기하 검증 수행)
                if (!HasValidPhysicalGeometry(elem))
                {
                    continue;
                }

                // Try to get element type and family
                string familyName = "Generic";
                string typeName = elem.Name;

                ElementId typeId = elem.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                {
                    Element typeElem = doc.GetElement(typeId);
                    if (typeElem is ElementType elemType)
                    {
                        typeName = elemType.Name;
                        familyName = elemType.FamilyName;
                    }
                }

                // Initialize model
                var model = new ExtractedElement
                {
                    Id = elem.Id.ToString(),
                    Guid = elem.UniqueId,
                    Category = catName,
                    Family = familyName,
                    Type = typeName,
                    Workset = "-" // 기본 수집에서 원천 배제하여 필요 시에만 필터링 되도록 설정
                };

                // 2) Extract and load ONLY the custom requested parameters (supporting both Instance and Type, and Project / Shared parameters)
                if (customParamNames != null)
                {
                    foreach (string paramName in customParamNames)
                    {
                        if (string.IsNullOrEmpty(paramName)) continue;

                        // 🌟 작업세트(Workset)는 사용자가 켰을 때만 수집하도록 패치
                        string lower = paramName.ToLower();
                        if (lower == "workset" || lower == "작업세트")
                        {
                            string wsName = doc.GetWorksetTable().GetWorkset(elem.WorksetId)?.Name ?? "Shared";
                            model.Workset = wsName;
                            model.CustomParameters[paramName] = wsName;
                            continue;
                        }

                        // Lookup on Instance element first
                        Parameter param = elem.LookupParameter(paramName);

                        // If not found, lookup on ElementType (Type Parameter)
                        if (param == null || !param.HasValue)
                        {
                            ElementId elemTypeId = elem.GetTypeId();
                            if (elemTypeId != ElementId.InvalidElementId)
                            {
                                Element typeElem = doc.GetElement(elemTypeId);
                                param = typeElem?.LookupParameter(paramName);
                            }
                        }

                        if (param != null && param.HasValue)
                        {
                            string valStr = param.StorageType switch
                            {
                                StorageType.Double => Math.Round(param.AsDouble(), 3).ToString(),
                                StorageType.Integer => param.AsInteger().ToString(),
                                StorageType.String => param.AsString(),
                                StorageType.ElementId => doc.GetElement(param.AsElementId())?.Name ?? param.AsElementId().ToString(),
                                _ => param.AsValueString() ?? ""
                            };

                            if (string.IsNullOrEmpty(valStr))
                            {
                                valStr = param.AsValueString() ?? "";
                            }

                            // Revit 매개변수가 비어있거나 "Undefined", "None" 등의 미지정 문자열을 반환하는 경우 칼같이 '-'로 치환
                            if (string.IsNullOrWhiteSpace(valStr) || 
                                valStr.Trim().Equals("Undefined", StringComparison.OrdinalIgnoreCase) || 
                                valStr.Trim().Equals("<Undefined>", StringComparison.OrdinalIgnoreCase) || 
                                valStr.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
                            {
                                valStr = "-";
                            }

                            model.CustomParameters[paramName] = valStr;
                        }
                        else
                        {
                            // If parameter has no value or does not exist, show empty rather than throwing
                            model.CustomParameters[paramName] = "-";
                        }
                    }
                }

                resultList.Add(model);
            }

            return resultList;
        }

        // Helper: Safe Parameter Get as Double
        private static double? GetDoubleParameter(Element elem, BuiltInParameter bip)
        {
            Parameter param = elem.get_Parameter(bip);
            if (param != null && param.HasValue && param.StorageType == StorageType.Double)
            {
                return param.AsDouble();
            }
            return null;
        }

        private static double? GetDoubleParameter(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param != null && param.HasValue && param.StorageType == StorageType.Double)
            {
                return param.AsDouble();
            }
            return null;
        }

        // Helper: Safe Parameter Get as String
        private static string? GetStringParameter(Element elem, BuiltInParameter bip)
        {
            Parameter param = elem.get_Parameter(bip);
            if (param != null && param.HasValue)
            {
                return param.StorageType == StorageType.String ? param.AsString() : param.AsValueString();
            }
            return null;
        }

        private static string? GetStringParameter(Element elem, string paramName)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param != null && param.HasValue)
            {
                return param.StorageType == StorageType.String ? param.AsString() : param.AsValueString();
            }
            return null;
        }

        /// <summary>
        /// 객체가 실제 수량 산출 대상이 되는 물리적 3D 형상(Solid)을 가지고 있는지 정밀 판별합니다.
        /// </summary>
        private static bool HasValidPhysicalGeometry(Element elem)
        {
            if (elem == null) return false;

            try
            {
                // 뷰 상세 수준을 Fine으로 하여 기하 정보를 안전하게 조회 (렉 방지 및 ComputeReferences 비활성화, MEP 피팅의 3D 형상 정확히 검출)
                var options = new Options
                {
                    DetailLevel = ViewDetailLevel.Fine,
                    ComputeReferences = false
                };

                GeometryElement geomElem = elem.get_Geometry(options);
                if (geomElem == null) return false;

                return CheckGeometryForValidSolid(geomElem);
            }
            catch
            {
                // 기하 정보 획득 시 오류가 발생하는 특수 객체들은 안전하게 예외처리하여 스킵 방지 (기본 포함)
                return true;
            }
        }

        /// <summary>
        /// GeometryElement 내부를 재귀적으로 순회하며 유효한 체적과 면을 가진 Solid 실체가 존재하는지 확인합니다.
        /// </summary>
        private static bool CheckGeometryForValidSolid(IEnumerable<GeometryObject> geomObjects)
        {
            if (geomObjects == null) return false;

            foreach (GeometryObject geomObj in geomObjects)
            {
                if (geomObj is Solid solid)
                {
                    // 체적이나 면이 유효하게 존재하는 실질적인 Solid인지 판별
                    if (solid.Volume > 0.0001 && solid.Faces.Size > 0)
                    {
                        return true;
                    }
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    // 패밀리 인스턴스 등의 내포된 지오메트리 재귀 순회 검사 (Instance 및 Symbol 백업)
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    if (instGeom != null && CheckGeometryForValidSolid(instGeom))
                    {
                        return true;
                    }
                    
                    GeometryElement symGeom = geomInst.GetSymbolGeometry();
                    if (symGeom != null && CheckGeometryForValidSolid(symGeom))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
