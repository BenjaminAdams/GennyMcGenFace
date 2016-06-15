using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GennyMcGenFace.GennyMcGenFace
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidGennyMcGenFacePkgString)]
    public sealed class GennyMcGenFacePackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public GennyMcGenFacePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidGennyMcGenFaceCmdSet, (int)PkgCmdIDList.cmdGennyGenClass);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        #endregion Package Members

        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (dte.SelectedItems.Count <= 0) return;

            var foundClasses = GetClasses(dte);

            var promptValue = Prompt.ShowDialog(foundClasses);
            if (promptValue == "") return;

            var selectedClass = foundClasses.FirstOrDefault(x => x.FullName == promptValue);
            if (selectedClass == null) throw new Exception("Class not found");

            foreach (CodeElement member in selectedClass.Members)
            {
                if (IsValidPublicMember(member) == false) continue;

                var fullName = member.FullName;
                var fieldName = member.Name;
                var type = member.Language;
            }

            //foreach (CodeElement member in selectedClass.Attributes)
            //{
            //    if (member.IsCodeType == false) continue;

            //    var tmp = member.FullName;
            //}

            //foreach (CodeElement member in selectedClass.Children)
            //{
            //    if (member.IsCodeType == false) continue;

            //    var tmp = member.FullName;
            //}
        }

        private static bool IsValidPublicMember(CodeElement member)
        {
            if (member.Kind == vsCMElement.vsCMElementProperty)
            {
                return ((CodeProperty)member).Access == vsCMAccess.vsCMAccessPublic;
            }
            else
            {
                return false;
            }
        }

        private List<CodeClass> GetClasses(DTE2 dte)
        {
            List<CodeClass> foundClasses = new List<CodeClass>();

            ClassSearch(dte.Solution.Projects, foundClasses);
            foundClasses.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return foundClasses;
        }

        //loads all classes in solution
        private void ClassSearch(EnvDTE.Projects projects, List<CodeClass> foundClasses)
        {
            var projs = SolutionProjects.Projects();

            var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));

            var i = 0;
            foreach (var proj in projs)
            {
                i++;
                if (proj == null) continue;
                statusBar.Progress("Loading Classes for Project: " + proj.Name, i, projs.Count);

                if (proj.ProjectItems == null || proj.CodeModel == null) continue;

                var allClasses = GetProjectItems(proj.ProjectItems).Where(v => v.Name.Contains(".cs"));

                foreach (var c in allClasses)
                {
                    var eles = c.FileCodeModel;
                    if (eles == null) continue;
                    foreach (var ele in eles.CodeElements)
                    {
                        if (ele is EnvDTE.CodeNamespace)
                        {
                            var ns = ele as EnvDTE.CodeNamespace;

                            foreach (var property in ns.Members)
                            {
                                //var member = property as CodeType;
                                var member = property as CodeClass;
                                if (member == null)
                                    continue;

                                if (member.Kind != vsCMElement.vsCMElementClass) continue;

                                if (HasOnePublicMember(member))
                                {
                                    foundClasses.Add(member);
                                }
                            }
                        }
                    }
                }
            }

            statusBar.End();
        }

        private static Task<List<CodeClass>> IterateProjects(IList<Project> projs, List<CodeClass> foundClasses)
        {
            return Task.FromResult(foundClasses);
        }

        private static bool HasOnePublicMember(CodeClass selectedClass)
        {
            foreach (CodeElement member in selectedClass.Members)
            {
                if (IsValidPublicMember(member) == false) continue;

                return true;
            }

            return false;
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
    }
}