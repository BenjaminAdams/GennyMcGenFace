using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Parser;
using GennyMcGenFace.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using StatusBar = GennyMcGenFace.UI.StatusBar;

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
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs == null) throw new Exception("Could not load plugin");

            // Create the command for the menu item.
            var genClassId = new CommandID(GuidList.guidGennyMcGenFaceCmdSet, (int)PkgCmdIDList.cmdGennyGenClass);
            mcs.AddCommand(new MenuCommand(DisplayGenClassUI, genClassId));
        }

        private void DisplayGenClassUI(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (dte == null) throw new Exception("Could not load plugin");
            if (dte.SelectedItems.Count <= 0) return;

            var foundClasses = GetClasses(dte);

            var dialog = new ClassGenUI(foundClasses);
            dialog.ShowDialog();
        }

        private List<CodeClass> GetClasses(DTE2 dte)
        {
            var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));
            return CodeDiscoverer.ClassSearch(dte.Solution.Projects, statusBar);
        }
    }
}