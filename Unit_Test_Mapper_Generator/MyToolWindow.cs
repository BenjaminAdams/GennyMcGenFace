using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace BenjaminAdams.Unit_Test_Mapper_Generator
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("f9884568-b61b-4376-9aa5-3c1687a95bf4")]
    public class MyToolWindow : ToolWindowPane
    {
        public OleMenuCommandService mcs;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        //public MyToolWindow() :
        //    base(null)
        //{
        //    // Set the window title reading it from the resources.
        //    this.Caption = Resources.ToolWindowTitle;
        //    // Set the image that will appear on the tab of the window frame
        //    // when docked with an other window
        //    // The resource ID correspond to the one defined in the resx file
        //    // while the Index is the offset in the bitmap strip. Each image in
        //    // the strip being 16x16.
        //    this.BitmapResourceID = 301;
        //    this.BitmapIndex = 1;

        //    // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
        //    // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
        //    // the object returned by the Content property.
        //    base.Content = new MyControl();

        //    mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
        //    base.Content = new MyControl();
        //}

        private MyToolWindow _parent;

        //public MyToolWindow(MyToolWindow parent)
        //{
        //    InitializeComponent();
        //    _parent = parent;

        //    OleMenuCommandService mcs = this._parent.mcs;
        //    if (null != mcs)
        //    {
        //        // Create an alias for the command set guid.
        //        Guid g = GuidList.guidTWShortcutMenuCmdSet;

        //        // Create the command IDs.
        //        var red = new CommandID(g, PkgCmdIDList.cmdidRed);
        //        var yellow = new CommandID(g, PkgCmdIDList.cmdidYellow);
        //        var blue = new CommandID(g, PkgCmdIDList.cmdidBlue);

        //        // Add a command for each command ID.
        //        mcs.AddCommand(new MenuCommand(ChangeColor, red));
        //        mcs.AddCommand(new MenuCommand(ChangeColor, yellow));
        //        mcs.AddCommand(new MenuCommand(ChangeColor, blue));
        //    }
        //}

        //private void ChangeColor(object sender, EventArgs e)
        //{
        //    var mc = sender as MenuCommand;

        //    switch (mc.CommandID.ID)
        //    {
        //        case PkgCmdIDList.cmdidRed:
        //            MyToolWindow.Background = Brushes.Red;
        //            break;

        //        case PkgCmdIDList.cmdidYellow:
        //            MyToolWindow.Background = Brushes.Yellow;
        //            break;

        //        case PkgCmdIDList.cmdidBlue:
        //            MyToolWindow.Background = Brushes.Blue;
        //            break;
        //    }
        //}
    }
}