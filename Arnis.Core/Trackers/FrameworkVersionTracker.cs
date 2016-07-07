﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Arnis.Core.Trackers
{
    public class FrameworkVersionTracker: ITracker
    {
        public string Name => this.GetType().Name;
        public string Description { get; } = "Tracks target framework version for each project";

        public FrameworkVersionTracker()
        {
        }

        public TrackerResult Run(string workspace, List<string> skipList)
        {
            var stackReport = new TrackerResult();
            var solutionFiles = Directory.EnumerateFiles(workspace, "*.sln", SearchOption.AllDirectories).ToList();

            solutionFiles.ForEach(s =>
            {
                try
                {
                    var solution = new Solution
                    {
                        Name = Path.GetFileNameWithoutExtension(s),
                        Location = s,
                    };
                    stackReport.Solutions.Add(solution);

                    var solutionFileContent = File.ReadAllText(s);
                    var projRegex = new Regex("Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs)proj)\"", RegexOptions.Compiled);

                    var matches = projRegex.Matches(solutionFileContent).Cast<Match>();
                    var projectFiles = matches.Select(x => x.Groups[2].Value).ToList();

                    projectFiles.ForEach(p =>
                    {
                        try
                        {
                            var project = new Project
                            {
                                Name = Path.GetFileNameWithoutExtension(p),
                                Location = p
                            };
                            solution.Projects.Add(project);

                            string projectFile;
                            if (!Path.IsPathRooted(p))
                            {
                                projectFile = Path.Combine(Path.GetDirectoryName(s), p);
                            }
                            else
                            {
                                projectFile = Path.GetFullPath(p);
                            }

                            if (File.Exists(projectFile))
                            {
                                var xml = XDocument.Load(projectFile);
                                var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

                                var targetFrameworkVersion =
                                    (from l in xml.Descendants(ns + "PropertyGroup")
                                         from i in l.Elements(ns + "TargetFrameworkVersion")
                                         select new
                                         {
                                             targetFrameworkVersion = i.Value
                                         }
                                     ).FirstOrDefault()?
                                    .targetFrameworkVersion.ToString()
                                    .Replace("v",string.Empty);

                                var projectDependencies = new Dependency
                                {
                                    Name = ".NetFramework",
                                    Version = targetFrameworkVersion,
                                    Location = ""
                                };

                                project.Dependencies.Add(projectDependencies);
                            }
                            else
                            {
                                ConsoleEx.Warn($"Missing file: {projectFile}");
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleEx.Error(ex.Message);
                        }

                    });
                }
                catch (Exception ex)
                {
                    ConsoleEx.Error(ex.Message);
                }
            });

            return stackReport;
        }
    }
}
