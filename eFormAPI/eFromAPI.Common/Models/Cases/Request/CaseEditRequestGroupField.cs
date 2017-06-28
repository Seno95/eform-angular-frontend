﻿using System.Collections.Generic;

namespace eFromAPI.Common.Models.Cases.Request
{
    public class CaseEditRequestGroupField
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int DisplayOrder { get; set; }
        public List<CaseEditRequestField> Fields { get; set; }
    }
}