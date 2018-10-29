﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microting.eFormApi.BasePn.Infrastructure.Database.Base;

namespace eFormAPI.Web.Infrastructure.Database.Entities
{
    public class MenuItem : BaseEntity
    {
        [StringLength(250)] 
        public string Name { get; set; }

        public int Position { get; set; }

        public int? ParentId { get; set; }
        public MenuItem Parent { get; set; }

        public List<MenuItem> ChildItems
            = new List<MenuItem>();
    }
}