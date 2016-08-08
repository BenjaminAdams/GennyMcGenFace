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
        protected List<CodeClass> _classes;

        public UnitTestGenUI(DTE2 dte)
        {
            _editor.Left = 50;
            _editor.Top = 90;
            _editor.Width = 900;
            _editor.Height = 600;
            _dte = dte;
            base.Init();

            InitTopRightControls();
            ShowLoadingCombo();
            // _mainForm.Text = "Generate Unit Test for a Class";

            _mainForm.Shown += LoadClasses;

            _mainForm.ShowDialog();
        }

        protected override async void GenerateEditorTxt(object sender, EventArgs e)
        {
            DisableUIStuff();
            var promptValue1 = _classNameCombo1.Text;

            _classNameCombo1.DroppedDown = false;

            if (string.IsNullOrWhiteSpace(promptValue1)) ShowError("Class name blank");

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue1);
            if (selectedClass == null) ShowError("Class not found");

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

        private void LoadClasses(object sender, EventArgs e)
        {
            DisableUIStuff();

            var worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += LoadClassBackground;
            worker.RunWorkerCompleted += LoadClassDone;

            worker.RunWorkerAsync();
        }

        private void LoadClassBackground(object sender, EventArgs e)
        {
            _classes = CodeDiscoverer.ClassSearch(_dte.Solution.Projects, _editor, false);

            _dataSource = BuildAutoCompleteSource();
        }

        private void LoadClassDone(object sender, EventArgs e)
        {
            if (_classes == null || _classes.Any() == false)
            {
                _editor.Text = "Could not find any projects, do you have a solution open?";
                return;
            }

            EnableUIStuff();
            _mainForm.Controls.Remove(_loadingMsgCombo);
            InitCombo1();

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
            // _classNameCombo1.DroppedDown = false; //The dropdown was open for some reason
            // _classNameCombo1.Focus();
        }

        private void EnableUIStuff()
        {
            _wordsTxt.ReadOnly = false;
            _intLengthTxt.ReadOnly = false;
            _wordsTxt.Enabled = true;
            _intLengthTxt.Enabled = true;

            if (_classNameCombo1 != null)
            {
                _classNameCombo1.Enabled = true;
            }
        }

        private void DisableUIStuff()
        {
            _wordsTxt.ReadOnly = true;
            _intLengthTxt.ReadOnly = true;
            _wordsTxt.Enabled = false;
            _intLengthTxt.Enabled = false;

            if (_classNameCombo1 != null)
            {
                _classNameCombo1.Enabled = false;
            }
        }

        private void ShowError(string msg)
        {
            EnableUIStuff();
            throw new Exception(msg);
        }

        protected AutoCompleteStringCollection BuildAutoCompleteSource()
        {
            var classList = new AutoCompleteStringCollection();
            for (var i = 0; i < _classes.Count; i++)
            {
                var t = _classes[i];
                classList.Add(t.FullName);
            }

            return classList;
        }

        protected void ShowLoadingCombo()
        {
            _loadingMsgCombo = new ComboListMatcher
            {
                Left = 50,
                Top = 50,
                Width = 900,
                DroppedDown = false,
                Enabled = false,
                DataSource = new AutoCompleteStringCollection() { "Loading..." }
            };

            _mainForm.Controls.Add(_loadingMsgCombo);
        }
    }
}