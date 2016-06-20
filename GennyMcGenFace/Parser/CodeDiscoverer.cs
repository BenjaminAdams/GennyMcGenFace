using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using StatusBar = GennyMcGenFace.UI.StatusBar;

namespace GennyMcGenFace.Parser
{
    /// <summary>
    /// Get information about the current project the extension is installed on
    /// </summary>
    public static class CodeDiscoverer
    {
        //loads all classes in solution
        public static List<CodeClass> ClassSearch(EnvDTE.Projects projects, StatusBar statusBar)
        {
            var projs = CodeDiscoverer.Projects();
            var foundClasses = new List<CodeClass>();

            var i = 0;
            foreach (var proj in projs)
            {
                i++;
                if (proj == null) continue;
                statusBar.Progress("Loading Classes for Project: " + proj.Name, i, projs.Count);

                if (proj.ProjectItems == null || proj.CodeModel == null) continue;

                var projectItems = GetProjectItems(proj.ProjectItems).Where(v => v.Name.Contains(".cs"));

                foreach (var c in projectItems)
                {
                    var eles = c.FileCodeModel;
                    if (eles == null) continue;

                    foreach (var ns in eles.CodeElements.OfType<CodeNamespace>())
                    {
                        foreach (var member in ns.Members.OfType<CodeClass>())
                        {
                            if (member == null || member.Kind != vsCMElement.vsCMElementClass)
                                continue;

                            if (HasOnePublicMember(member))
                            {
                                foundClasses.Add(member);
                            }
                        }
                    }
                }
            }

            if (foundClasses == null || foundClasses.Count == 0) throw new Exception("Could not find any classes");
            foundClasses.Sort((x, y) => x.FullName.CompareTo(y.FullName));
            statusBar.End();
            return foundClasses;
        }

        public static bool IsValidPublicMember(CodeElement member)
        {
            try
            {
                var asProp = member as CodeProperty;

                if (asProp != null && member.Kind == vsCMElement.vsCMElementProperty && asProp.Setter != null && asProp.Access == vsCMAccess.vsCMAccessPublic)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool HasOnePublicMember(CodeClass selectedClass)
        {
            foreach (CodeElement member in selectedClass.Members.OfType<CodeElement>())
            {
                if (IsValidPublicMember(member) == false) continue;

                return true;
            }

            return false;
        }

        public static bool IsPublic(this CodeElement codeElement)
        {
            if (codeElement is CodeType)
                return ((CodeType)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            if (codeElement is CodeProperty)
                return ((CodeProperty)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            if (codeElement is CodeFunction)
                return ((CodeFunction)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            if (codeElement is CodeVariable)
                return ((CodeVariable)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            if (codeElement is CodeStruct)
                return ((CodeStruct)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            if (codeElement is CodeDelegate)
                return ((CodeDelegate)codeElement).Access == vsCMAccess.vsCMAccessPublic;
            return false;
        }

        //from http://www.wwwlicious.com/2011/03/29/envdte-getting-all-projects-html/
        public static IList<Project> Projects()
        {
            Projects projects = GetActiveIDE().Solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        public static IEnumerable<ProjectItem> GetProjectItems(EnvDTE.ProjectItems projectItems)
        {
            foreach (EnvDTE.ProjectItem item in projectItems)
            {
                yield return item;

                if (item.SubProject != null)
                {
                    foreach (EnvDTE.ProjectItem childItem in GetProjectItems(item.SubProject.ProjectItems))
                        yield return childItem;
                }
                else
                {
                    foreach (EnvDTE.ProjectItem childItem in GetProjectItems(item.ProjectItems))
                        yield return childItem;
                }
            }
        }

        public static void RecursiveMethodSearch(CodeElements elements, List<CodeFunction> foundMethod)
        {
            foreach (CodeElement codeElement in elements)
            {
                if (codeElement is CodeFunction)
                {
                    foundMethod.Add(codeElement as CodeFunction);
                }
                RecursiveMethodSearch(codeElement.Children, foundMethod);
            }
        }

        private static DTE2 GetActiveIDE()
        {
            // Get an instance of currently running Visual Studio IDE.
            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            return dte2;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }
    }
}