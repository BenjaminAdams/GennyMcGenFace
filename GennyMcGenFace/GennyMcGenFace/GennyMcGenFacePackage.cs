using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

using Microsoft.VisualStudio.Shell;

using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using stdole;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

//using System.Windows.Documents;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

//good setup tutorial http://www.diaryofaninja.com/blog/2014/02/18/who-said-building-visual-studio-extensions-was-hard

namespace Genny.GennyMcGenFace
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
    // This attribute registers a tool window exposed by this package.
    // [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidUnit_Test_Mapper_GeneratorPkgString)]
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

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidUnit_Test_Mapper_GeneratorCmdSet, (int)PkgCmdIDList.cmdidMyCommand);

                // WE COMMENT OUT THE LINE BELOW
                // MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );

                // AND REPLACE IT WITH A DIFFERENT TYPE
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                //  menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;

                mcs.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (dte.SelectedItems.Count <= 0) return;

            var foundClasses = GetClasses(dte);

            var promptValue = Prompt.ShowDialog(foundClasses);
            if (promptValue == "") return;

            var selectedClass = (CodeClass)foundClasses.First(x => x.FullName == promptValue);

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

    public static class Prompt
    {
        public static string ShowDialog(List<CodeClass> classes)
        {
            Form prompt = new Form()
            {
                Width = 700,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            var classList = new AutoCompleteStringCollection();

            foreach (var t in classes)
            {
                classList.Add(t.FullName);
            }

            var classNameCombo1 = new ComboBox()
            {
                Left = 50,
                Top = 50,
                Width = 600,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                AutoCompleteCustomSource = classList,
                DataSource = classList
            };

            // classNameCombo1.TextUpdate

            //classNameCombo1.TextUpdate += (sender, e) =>
            //{
            //    // var tmpDataSource = classList.Where(x =>x.);
            //    // classNameCombo1.DataSource = tmpDataSource;

            //    string item = classNameCombo1.Text;
            //    item = item.ToLower();
            //    classNameCombo1.Items.Clear();
            //    List<string> list = new List<string>();
            //    for (int i = 0; i < classList.Count; i++)
            //    {
            //        if (classList[i].ToLower().Contains(item))
            //            list.Add(classList[i]);
            //    }
            //    if (item != String.Empty)
            //        foreach (string str in list)
            //            classNameCombo1.Items.Add(str);
            //    else
            //        classNameCombo1.Items.AddRange(classList);
            //    classNameCombo1.SelectionStart = item.Length;
            //    classNameCombo1.DroppedDown = true;
            //};

            Label textLabel = new Label() { Left = 50, Top = 20, Text = "ClassName" };
            // TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 450, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(classNameCombo1);

            //  prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? classNameCombo1.Text : "";
        }
    }

    public static class SolutionProjects
    {
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