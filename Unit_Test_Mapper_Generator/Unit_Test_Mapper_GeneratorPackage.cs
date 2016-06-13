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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using IServiceProvider = System.IServiceProvider;
using TextSelection = EnvDTE.TextSelection;

//good setup tutorial http://www.diaryofaninja.com/blog/2014/02/18/who-said-building-visual-studio-extensions-was-hard

namespace BenjaminAdams.Unit_Test_Mapper_Generator
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
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidUnit_Test_Mapper_GeneratorPkgString)]
    public sealed class Unit_Test_Mapper_GeneratorPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public Unit_Test_Mapper_GeneratorPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            //// Add our command handlers for menu (commands must exist in the .vsct file)
            //OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            //if (null != mcs)
            //{
            //    // Create the command for the menu item.
            //    CommandID menuCommandID = new CommandID(GuidList.guidUnit_Test_Mapper_GeneratorCmdSet, (int)PkgCmdIDList.cmdidMyCommand);
            //    MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);

            //    mcs.AddCommand(menuItem);

            //    // Create the command for the tool window
            //    //CommandID toolwndCommandID = new CommandID(GuidList.guidUnit_Test_Mapper_GeneratorCmdSet, (int)PkgCmdIDList.cmdidMyTool);
            //    //MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
            //    //mcs.AddCommand(menuToolWin);
            //}

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidUnit_Test_Mapper_GeneratorCmdSet, (int)PkgCmdIDList.cmdidMyCommand);

                // WE COMMENT OUT THE LINE BELOW
                // MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );

                // AND REPLACE IT WITH A DIFFERENT TYPE
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;

                mcs.AddCommand(menuItem);
            }
        }

        private void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            //  var t = sender.GetType();
            // var tmp = (OleMenuCommand)sender;

            //   DTE.ActiveWindow.Selection.ActivePoint.CodeElement(vsCMElement.vsCMElementFunction);

            var dte = GetService(typeof(SDTE)) as DTE2;
            if (dte.SelectedItems.Count <= 0) return;

            List<CodeClass> foundClasses = new List<CodeClass>();
            List<CodeFunction> foundMethod = new List<CodeFunction>();
            CodeElements elementsInDocument = dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements;
            RecursiveClassSearch(dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, foundClasses);
            RecursiveMethodSearch(dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, foundMethod);

            //  List<Type> types = GetAllTypes();
            // CodeClassExample(dte);
            // AttributesExample(dte);
            CodeElementExample(dte);

            foreach (SelectedItem selectedItem in dte.SelectedItems)
            {
                if (selectedItem.ProjectItem == null) return;
                var projectItem = selectedItem.ProjectItem;
                var fullPathProperty = projectItem.Properties.Item("FullPath");
                if (fullPathProperty == null) return;
                var fullPath = fullPathProperty.Value.ToString();
                var test = string.Format("Required '{0}'.", fullPath);
            }

            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                // start by assuming that the menu will not be shown
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                IVsHierarchy hierarchy = null;
                uint itemid = VSConstants.VSITEMID_NIL;

                if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
                // Get the file path
                string itemFullPath = null;
                ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
                var transformFileInfo = new FileInfo(itemFullPath);

                // then check if the file is named 'web.config'
                bool isWebConfig = string.Compare("web.config", transformFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0;

                // if not leave the menu hidden
                if (!isWebConfig) return;

                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }

        private static void RecursiveClassSearch(CodeElements elements, List<CodeClass> foundClasses)
        {
            foreach (CodeElement codeElement in elements)
            {
                if (codeElement is CodeClass)
                {
                    foundClasses.Add(codeElement as CodeClass);
                }
                RecursiveClassSearch(codeElement.Children, foundClasses);
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

        //from https://msdn.microsoft.com/en-us/library/envdte.textpoint.codeelement.aspx
        public void CodeElementExample(DTE2 dte)
        {
            var members = "";
            // Before running this example, open a code document from a project
            // and place the insertion point anywhere inside the source code.
            try
            {
                TextSelection sel = (TextSelection)dte.ActiveDocument.Selection;
                TextPoint pnt = (TextPoint)sel.ActivePoint;
                //  var asdasdasdasdd = sel.ActivePoint.CodeElement[vsCMElement.vsCMElementClass];
                TextSelection sel2 = (TextSelection)dte.ActiveDocument.Selection;

                CodeType sel3 = (CodeType)dte.ActiveDocument.Selection;
                foreach (CodeElement elem in sel3.Members)
                {
                    members = members + (elem.Name + "\n");
                }
                //CodeFunction fun = (CodeFunction)sel2.ActivePoint.get_CodeElement(vsCMElement.vsCMElementFunction);

                // var types = GetAvailableTypes(dte, this, true);

                //EnvDTE.CodeParameter fun = sel2.ActivePoint.CodeElement[vsCMElement.vsCMElementParameter] as EnvDTE.CodeParameter;
                //var typppeee = fun.Name + "'s return type is " + fun.Type.AsFullName;

                EnvDTE.TextSelection ts = dte.ActiveWindow.Selection as EnvDTE.TextSelection;
                if (ts == null)
                    return;

                //var codeParam5 = ts.ActivePoint.CodeElement[vsCMElement.vsCMElementParameter];
                //var child = codeParam5.Children;
                //var ttasda345sdasd = codeParam5.InfoLocation;

                //var objCodeCls = (CodeClass)ts.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);

                //var objTextSel = (TextSelection)dte.ActiveDocument.Selection;
                //var objCodeCls = (CodeClass)objTextSel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);
                //var xxxx = objTextSel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);
                // var objCodeCls = (CodeParameter)objTextSel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementParameter);
                // var objCodeCls = (CodeClass)ts.ActivePoint.CodeElement[vsCMElement.vsCMElementParameter] as EnvDTE.CodeClass;

                //foreach (CodeElement elem in objCodeCls.Members)
                //{
                //    members = members + (elem.Name + "\n");
                //}

                //foreach (CodeElement elem in objCodeCls.Attributes)
                //{
                //    members = members + (elem.Name + "\n");
                //}

                //foreach (CodeElement elem in objCodeCls.Collection)
                //{
                //    members = members + (elem.Name + "\n");
                //}

                //EnvDTE.CodeParameter codeParam2 = ts.ActivePoint.CodeElement[vsCMElement.vsCMElementParameter] as EnvDTE.CodeParameter;
                //if (codeParam2 == null)
                //    return;

                //var ttttt = codeParam2.Type;
                //var tttftt = codeParam2.Kind;
                ////var xxx = codeParam2.Type;
                //var et = codeParam2.Type.ElementType;
                //var etk = codeParam2.Type.TypeKind;

                // Type tttt = GetTypeByName(dte, this, codeParam2.Type.AsFullName);

                //EnvDTE.CodeParameter codeParam = ts.ActivePoint.CodeElement[vsCMElement.vsCMElementParameter] as EnvDTE.CodeParameter;
                //if (codeParam == null)
                //    return;

                //  Type t = GetTypeByName(dte, (Package)this, fun.Type.AsFullName);

                //YAY do a foreach here
                //  DoSomethingRecursively(t);

                // Discover every code element containing the insertion point.
                string elems = "";
                vsCMElement scopes = 0;

                foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
                {
                    CodeElement elem = pnt.get_CodeElement(scope);

                    if (elem != null)
                    {
                        elems += elem.Name + " (" + scope.ToString() + ")\n";

                        var tysadasdpe = elem.FullName;
                        var asdasdfdfdasd = elem.Language;
                    }
                }

                var asdasdasd = "The following elements contain the insertion point:\n\n" + elems;
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message);
                var asdasdasdasd = ex;
            }
        }

        public void AttributesExample(DTE2 dte)
        {
            // Before running this example, open a code document from a project
            // and place the insertion point inside a class definition.
            try
            {
                // Retrieve the CodeClass at the insertion point.
                TextSelection sel = (TextSelection)dte.ActiveDocument.Selection;
                CodeClass cls = (CodeClass)sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);

                // Enumerate the CodeClass's attributes.
                string attrs = "";
                foreach (CodeAttribute attr in cls.Attributes)
                {
                    attrs += attr.Name + "(" + attr.Value + ")" + "\r\n";
                }

                var namexxx = cls.Name + " has the following attributes:" + "\r\n\r\n" + attrs;
            }
            catch (Exception ex)
            {
                var asdasdsad = ex;
            }
        }

        public void CodeClassExample(DTE2 dte)
        {
            // Before running this example, open a code document from a
            // project and place the insertion point inside a class definition.
            try
            {
                TextSelection objTextSel;
                CodeClass objCodeCls;
                objTextSel = (TextSelection)dte.ActiveDocument.Selection;
                objCodeCls = (CodeClass)objTextSel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);
                // Add comments to CodeClass objCodeClass - notice change in code document.
                objCodeCls.Comment = "Comments for the CodeClass object.";
                // Access top-level object through the CodeClass object
                // and return the file name of that top-level object.
                var filename = objCodeCls.DTE.FileName;
                // Get the language used to code the CodeClass object - returns a GUID.
                var lang = objCodeCls.Language;

                // Get a collection of elements contained by the CodeClass object.
                string members = "Member Elements of " + objCodeCls.Name + ": \n";
                foreach (CodeElement elem in objCodeCls.Members)
                {
                    members = members + (elem.Name + "\n");
                }
                var taseasdsads = members;
            }
            catch (Exception ex)
            {
                var asdasdsad = ex;
            }
        }

        //from http://stackoverflow.com/a/25747905/2747782
        private System.Type GetTypeByName(EnvDTE80.DTE2 dte, Package package, string name)
        {
            System.IServiceProvider servProv = package as System.IServiceProvider;
            DynamicTypeService typeService = servProv.GetService(typeof(Microsoft.VisualStudio.Shell.Design.DynamicTypeService)) as DynamicTypeService;

            IVsSolution sln = servProv.GetService(typeof(IVsSolution)) as IVsSolution;

            IVsHierarchy hier;

            sln.GetProjectOfUniqueName(dte.ActiveDocument.ProjectItem.ContainingProject.UniqueName, out hier);

            return typeService.GetTypeResolutionService(hier).GetType(name, true);
        }

        private string GetModuleType(Project project, CodeClass codeClass)
        {
            IVsSolution solution = (IVsSolution)GetService(typeof(IVsSolution));
            DynamicTypeService typeResolver = (DynamicTypeService)GetService(typeof(DynamicTypeService));

            IVsHierarchy hierarchy = null;
            solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

            var typeResolutionService = typeResolver.GetTypeResolutionService(hierarchy);

            return typeResolutionService.GetType(codeClass.FullName).AssemblyQualifiedName;
        }

        public IEnumerable<Type> GetAvailableTypes(EnvDTE80.DTE2 dte, Package package, bool includeReferences)
        {
            System.IServiceProvider servProv = package as System.IServiceProvider;
            DynamicTypeService typeService = (DynamicTypeService)servProv.GetService(typeof(DynamicTypeService));
            Debug.Assert(typeService != null, "No dynamic type service registered.");

            IVsSolution sln = servProv.GetService(typeof(IVsSolution)) as IVsSolution;

            IVsHierarchy hier;

            sln.GetProjectOfUniqueName(dte.ActiveDocument.ProjectItem.ContainingProject.UniqueName, out hier);

            // IVsHierarchy hier = VsHelper.GetCurrentHierarchy(provider);
            Debug.Assert(hier != null, "No active hierarchy is selected.");

            ITypeDiscoveryService discovery = typeService.GetTypeDiscoveryService(hier);

            HashSet<Type> availableTypes = new HashSet<Type>();
            foreach (Type type in discovery.GetTypes(typeof(object), includeReferences))
            {
                // We will never allow non-public types selection, as it's terrible practice.
                if (type.IsPublic)
                {
                    if (!availableTypes.Contains(type))
                    {
                        availableTypes.Add(type);
                    }
                }
            }

            return availableTypes;
        }

        public List<Type> GetAllTypes()
        {
            var trs = GetTypeDiscoveryService();
            var types = trs.GetTypes(typeof(object), true /*excludeGlobalTypes*/);
            var result = new List<Type>();
            foreach (Type type in types)
            {
                if (type.IsPublic)
                {
                    if (!result.Contains(type))
                        result.Add(type);
                }
            }
            return result;
        }

        private ITypeDiscoveryService GetTypeDiscoveryService()
        {
            var dte = GetService<EnvDTE.DTE>();
            var typeService = GetService<DynamicTypeService>();
            var solution = GetService<IVsSolution>();
            IVsHierarchy hier;
            var projects = dte.ActiveSolutionProjects as Array;
            var currentProject = projects.GetValue(0) as Project;
            solution.GetProjectOfUniqueName(currentProject.UniqueName, out hier);
            return typeService.GetTypeDiscoveryService(hier);
        }

        private T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public static string DoSomethingRecursively(Type T)
        {
            // if (input == null || IsPrimitiveType(input) == true) return input;
            // if (CheckList<T>() == true) return input;

            if (T.BaseType != null && T.BaseType.ToString() == "Newtonsoft.Json.Linq.JToken") //prevent stackoverflow if its this type
            {
                return "";
            }

            foreach (PropertyInfo prp in T.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanRead && x.CanWrite))
            {
                var tmp = prp;

                //var propValue = prp.GetValue(input, null);

                // var elems = propValue as IList;

                //    if (elems != null)
                //    {
                //        //check nested arrays
                //        var items = prp.GetValue(input, null) as IList;
                //        if (items != null)
                //        {
                //            foreach (var item in items)
                //            {
                //                MaskSensitiveData(item.GetType(), item);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        // MaskObject(prp, input);

                //        if (prp.PropertyType.Assembly == input.GetType().Assembly)
                //        {
                //            //check nested classes
                //            MaskSensitiveData(prp.PropertyType, prp.GetValue(input, null));
                //        }
                //    }
                //}

                //return input;
            }
            return null;
        }

        //private void MyToolWindow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    OleMenuCommandService mcs = this._parent.mcs;
        //    if (null != mcs)
        //    {
        //        CommandID menuID = new CommandID(
        //            GuidList.guidButtonGroupCmdSet,
        //            PkgCmdIDList.ColorMenu);
        //        Point p = this.PointToScreen(e.GetPosition(this));
        //        mcs.ShowContextMenu(menuID, (int)p.X, (int)p.Y);
        //    }
        //}

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                // start by assuming that the menu will not be shown
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                IVsHierarchy hierarchy = null;
                uint itemid = VSConstants.VSITEMID_NIL;

                if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
                // Get the file path
                string itemFullPath = null;
                ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
                var transformFileInfo = new FileInfo(itemFullPath);

                // then check if the file is named 'web.config'
                bool isWebConfig = string.Compare("web.config", transformFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0;

                // if not leave the menu hidden
                if (!isWebConfig) return;

                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;

            string promptValue = Prompt.ShowDialog("Test", "123");

            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            //           0,
            //           ref clsid,
            //           "Unit_Test_Mapper_Generator",
            //           string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
            //           string.Empty,
            //           0,
            //           OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //           OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            //           OLEMSGICON.OLEMSGICON_QUERY,
            //           0,        // false
            //           out result));
        }

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            var classList = new AutoCompleteStringCollection() { "val1", "val2" };

            //Assembly myAssembly = Assembly.GetExecutingAssembly();

            //foreach (Type t in myAssembly.GetTypes())
            //{
            //    classList.Add(t.Name);
            //}

            Assembly ass = Assembly.ReflectionOnlyLoad("Api");
            foreach (Type t in ass.GetTypes())
            {
                classList.Add(t.Name);
            }

            //foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            //{
            //    if (a == null) continue;

            //    try
            //    {
            //        foreach (Type t in a.GetTypes())
            //        {
            //            classList.Add(t.AssemblyQualifiedName);
            //        }
            //    }
            //    catch
            //    {
            //        //
            //    }
            //}

            var classNameCombo1 = new ComboBox()
            {
                Left = 50,
                Top = 50,
                Width = 400,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                AutoCompleteCustomSource = classList
            };

            Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
            // TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(classNameCombo1);

            //  prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? classNameCombo1.Text : "";
        }
    }
}