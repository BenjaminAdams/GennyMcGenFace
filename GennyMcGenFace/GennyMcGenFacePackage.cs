using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace GennyMcGenFace
{
    //Hosted at https://visualstudiogallery.msdn.microsoft.com/7079720a-e403-4322-9842-d44673776664
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidGennyMcGenFacePkgString)]
    public sealed class GennyMcGenFacePackage : Package
    {
        protected override void Initialize()
        {
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

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (dte.SelectedItems.Count <= 0) return;

            var foundClasses = GetClasses(dte);
            if (foundClasses == null || foundClasses.Count == 0) throw new Exception("Must have at least one class in your solution with at least one public property");

            var dialog = new Prompt(foundClasses);
            dialog.ShowDialog();
        }

        private List<CodeClass> GetClasses(DTE2 dte)
        {
            List<CodeClass> foundClasses = new List<CodeClass>();
            var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));
            CodeDiscoverer.ClassSearch(dte.Solution.Projects, foundClasses, statusBar);
            foundClasses.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return foundClasses;
        }
    }
}