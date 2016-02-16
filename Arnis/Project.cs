﻿using System.Collections.Generic;

namespace Arnis
{
    public class Project
    {
        public string Name { get; set; }
        public string Location { get; set; }

        public List<ProjectDependency> Dependencies { get; set; } = new List<ProjectDependency>();
    }
}