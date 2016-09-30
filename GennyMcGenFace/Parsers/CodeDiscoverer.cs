using EnvDTE;
using EnvDTE80;
using FastColoredTextBoxNS;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GennyMcGenFace.Parsers
{
    //another way we could possible get all the classes https://github.com/PombeirP/T4Factories/blob/master/T4Factories.Testbed/CodeTemplates/VisualStudioAutomationHelper.ttinclude#L177

    /// <summary>
    /// Get information about the current project the extension is installed on
    /// </summary>
    public static class CodeDiscoverer
    {
        /// <summary>
        /// loads all classes in solution
        /// </summary>
        /// <param name="projects">All projects in the solution</param>
        /// <param name="editor">The textbox to display progress</param>
        /// <param name="withProperties">True if you only want classes with properties only.  False if you want classes with Functions</param>
        /// <returns></returns>
        public static List<CodeClass> ClassSearch(EnvDTE.Projects projects, FastColoredTextBox editor)
        {
            var projs = CodeDiscoverer.Projects();
            var foundClasses = new List<CodeClass>();

            editor.Text = "Loading projects\r\n";

            foreach (var proj in projs)
            {
                if (proj == null) continue;
                editor.AppendText("\r\n" + proj.Name);

                if (proj.ProjectItems == null || proj.CodeModel == null) continue;
                // var timer = new Stopwatch();
                // timer.Start();
                var projectItems = GetProjectItems(proj.ProjectItems).Where(v => v.Name.Contains(".cs"));

                // foundClasses.AddRange(projectItems.Where(c => c.FileCodeModel != null).SelectMany(x => x.FileCodeModel.CodeElements.OfType<CodeNamespace>().SelectMany(xx => xx.Members.OfType<CodeClass>())));

                Parallel.ForEach(projectItems, (c) =>
                {
                    if (c == null || c.FileCodeModel == null) return;

                    //foundClasses.AddRange(c.FileCodeModel.CodeElements.OfType<CodeNamespace>().SelectMany(x => x.Members.OfType<CodeClass>()));

                    foreach (var ns in c.FileCodeModel.CodeElements.OfType<CodeNamespace>())
                    {
                        foreach (var member in ns.Members.OfType<CodeClass>())
                        {
                            if (member == null || member.Kind != vsCMElement.vsCMElementClass) continue;
                            foundClasses.Add(member);
                        }
                    }
                });

                //timer.Stop();

                // editor.AppendText("\r\n" + proj.Name + "- " + timer.ElapsedMilliseconds + "ms");
            }

            if (foundClasses == null || foundClasses.Count == 0) throw new Exception("Could not find any classes");
            foundClasses.Sort((x, y) => x.FullName.CompareTo(y.FullName));
            return foundClasses;
        }

        public static bool IsValidPublicProperty(CodeElement member)
        {
            try
            {
                var asProp = member as CodeProperty;

                if (asProp != null && member.Kind == vsCMElement.vsCMElementProperty && asProp.Setter != null &&asProp.Access == vsCMAccess.vsCMAccessPublic && asProp.Setter.Access == vsCMAccess.vsCMAccessPublic)
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

        public static bool HasOnePublicProperty(CodeClass selectedClass)
        {
            foreach (CodeElement member in selectedClass.Members.OfType<CodeElement>())
            {
                if (IsValidPublicProperty(member) == false) continue;

                return true;
            }

            return false;
        }

        public static bool HasOneFunction(CodeClass selectedClass)
        {
            foreach (CodeFunction member in selectedClass.Members.OfType<CodeFunction>())
            {
                if (member.Kind == vsCMElement.vsCMElementFunction) return true;
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