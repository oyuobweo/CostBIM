using System.Collections.Generic;

namespace CostBIM.Models
{
    /// <summary>
    /// Revit API 의존성이 전혀 없는 순수 매개변수 스키마 데이터 모델
    /// </summary>
    public class ParameterSchema
    {
        public List<string> BuiltIn { get; set; } = new List<string>();
        public List<string> Project { get; set; } = new List<string>();
        public List<string> Shared { get; set; } = new List<string>();
        public Dictionary<string, string> GroupMap { get; set; } = new Dictionary<string, string>();
    }
}
