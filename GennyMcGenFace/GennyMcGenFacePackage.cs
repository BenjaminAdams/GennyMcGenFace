using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Parsers;
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
        private DTE2 _dte;

        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs == null) throw new Exception("Could not load plugin");

            // _dte = GetService(typeof(SDTE)) as DTE2;
            _dte = GetService(typeof(DTE)) as DTE2;

            // Create the command for the menu item.
            // mcs.AddCommand(new MenuCommand(DisplayGenClassUI, new CommandID(GuidList.guidGennyMcGenFaceCmdSet, (int)PkgCmdIDList.cmdGennyGenClass)));
            //  mcs.AddCommand(new MenuCommand(DisplayGenMapperTestUI, new CommandID(GuidList.guidGennyMcGenFaceCmdSet, (int)PkgCmdIDList.cmdGennyGenMapperTest)));
            mcs.AddCommand(new MenuCommand(DisplayGenUnitTestUI, new CommandID(GuidList.guidGennyMcGenFaceCmdSet, (int)PkgCmdIDList.cmdGennyGenUnitTest)));
        }

        //private void DisplayGenClassUI(object sender, EventArgs e)
        //{
        //    var dte = GetService(typeof(SDTE)) as DTE2;
        //    if (dte == null) throw new Exception("Could not load plugin");

        //    var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));
        //    var foundClasses = CodeDiscoverer.ClassSearch(dte.Solution.Projects, statusBar, true);

        //    var ui = new ClassGenUI(foundClasses, dte);
        //}

        //private void DisplayGenMapperTestUI(object sender, EventArgs e)
        //{
        //    var dte = GetService(typeof(SDTE)) as DTE2;
        //    if (dte == null) throw new Exception("Could not load plugin");
        //    var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));
        //    var foundClasses = CodeDiscoverer.ClassSearch(dte.Solution.Projects, statusBar, true);

        //    var ui = new MapperGenUI(foundClasses);
        //}

        private void DisplayGenUnitTestUI(object sender, EventArgs e)
        {
            //   var dte = GetService(typeof(SDTE)) as DTE2;
            if (_dte == null) throw new Exception("Could not load plugin");
            var statusBar = new StatusBar((IVsStatusbar)GetService(typeof(SVsStatusbar)));
            var foundClasses = CodeDiscoverer.ClassSearch(_dte.Solution.Projects, statusBar, false);

            var ui = new UnitTestGenUI(foundClasses, _dte);
        }
    }
}