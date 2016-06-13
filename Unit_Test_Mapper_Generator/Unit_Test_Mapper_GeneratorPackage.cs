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

            string promptValue = Prompt.ShowDialog(foundClasses);

            var selectedClass = (CodeClass)foundClasses.First(x => x.FullName == promptValue);

            foreach (CodeElement member in selectedClass.Members)
            {
                if (member.IsCodeType == false) continue;

                var tmp = member.FullName;
            }

            foreach (CodeElement member in selectedClass.Attributes)
            {
                if (member.IsCodeType == false) continue;

                var tmp = member.FullName;
            }

            foreach (CodeElement member in selectedClass.Children)
            {
                if (member.IsCodeType == false) continue;

                var tmp = member.FullName;
            }
        }

        //private void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        //{
        //    //  var t = sender.GetType();
        //    // var tmp = (OleMenuCommand)sender;

        //    //   DTE.ActiveWindow.Selection.ActivePoint.CodeElement(vsCMElement.vsCMElementFunction);

        //    var dte = GetService(typeof(SDTE)) as DTE2;
        //    if (dte.SelectedItems.Count <= 0) return;

        //    var foundClasses = GetClasses(dte);
        //}

        private static List<CodeClass> GetClasses(DTE2 dte)
        {
            List<CodeClass> foundClasses = new List<CodeClass>();
            //List<CodeFunction> foundMethod = new List<CodeFunction>();
            //CodeElements elementsInDocument = dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements;
            RecursiveClassSearch(dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, foundClasses);
            //RecursiveMethodSearch(dte.ActiveDocument.ProjectItem.FileCodeModel.CodeElements, foundMethod);

            return foundClasses;
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
    }

    public static class Prompt
    {
        public static string ShowDialog(List<CodeClass> classes)
        {
            Form prompt = new Form()
            {
                Width = 500,
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
                Width = 400,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                AutoCompleteCustomSource = classList,
                DataSource = classList
            };

            Label textLabel = new Label() { Left = 50, Top = 20, Text = "ClassName" };
            // TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 80, DialogResult = DialogResult.OK };
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