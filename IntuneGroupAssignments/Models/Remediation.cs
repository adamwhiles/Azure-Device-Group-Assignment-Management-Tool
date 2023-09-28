﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntuneGroupAssignments.Models
{
    public class Remediation
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public string ModifiedDate { get; set; }
        public List<Assignment> Assignments { get; set; }

        public Remediation()
        {
            Assignments = new List<Assignment>();
        }
    }
}