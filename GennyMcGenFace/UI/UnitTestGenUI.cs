using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GennyMcGenFace.UI
{
    public class UnitTestGenUI : BaseUI
    {
        private DTE2 _dte;

        public UnitTestGenUI(List<CodeClass> classes, DTE2 dte)
        {
            base.Init(classes);

            _dte = dte;

            _mainForm.Text = "Generate Unit Test for a Class";
            InitTopRightControls();
            InitCombo1();

            _editor.Left = 50;
            _editor.Top = 90;
            _editor.Width = 900;
            _editor.Height = 600;

            // _mainForm.Shown += GenerateEditorTxt;
            _editor.Text = @"╔╦╦╦╦╦╦╦╦╦╦╦╦╗
╠╬╬╬╬╬╬╬╬╬╬╬╬╣
╠╬╬╬╬╬╬╬╬╬╬╬╬╣
╠╬╬█╬╬╬╬╬╬█╬╬╣
╠╬╬╬╬╬╬╬╬╬╬╬╬╣
╠╬╬╬╬╬╬╬╬╬╬╬╬╣
╠╬█╬╬╬╬╬╬╬╬█╬╣
╠╬██████████╬╣
╠╬╬╬╬╬╬╬╬╬╬╬╬╣
╚╩╩╩╩╩╩╩╩╩╩╩╩╝

Welcome, Please select a class.

";
            _mainForm.ShowDialog();
        }

        protected override async void GenerateEditorTxt(object sender, EventArgs e)
        {
            DisableUIStuff();
            var promptValue1 = _classNameCombo1.Text;
            if (string.IsNullOrWhiteSpace(promptValue1)) ShowError("Class name blank");

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue1);
            if (selectedClass == null) ShowError("Class not found");
            // _editor.Text = "Loading...\r\n";
            _editor.Text = @"        . . . . o o o o o
               _____      o       ____________
      ____====  ]OO|_n_n__][.     |Generating|
     [________]_|__|________)<    |Unit Tests|
      oo    oo  'oo OOOO-| oo\\_  ~~~~~|~~~~~~
  +--+--+--+--+--+--+--+--+-$1-+--+--+--+--+

";

            var genner = new UnitTestGenerator(selectedClass, _dte, _editor);

            _editor.Text = await genner.Gen(selectedClass, _opts);
            EnableUIStuff();
        }

        private void EnableUIStuff()
        {
            _wordsTxt.ReadOnly = false;
            _intLengthTxt.ReadOnly = false;
            _wordsTxt.Enabled = true;
            _intLengthTxt.Enabled = true;

            _classNameCombo1.Enabled = true;
        }

        private void DisableUIStuff()
        {
            _wordsTxt.ReadOnly = true;
            _intLengthTxt.ReadOnly = true;
            _wordsTxt.Enabled = false;
            _intLengthTxt.Enabled = false;

            _classNameCombo1.Enabled = false;
        }

        private void ShowError(string msg)
        {
            EnableUIStuff();
            throw new Exception(msg);
        }
    }
}