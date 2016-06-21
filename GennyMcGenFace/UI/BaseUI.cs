using EnvDTE;
using FastColoredTextBoxNS;
using GennyMcGenFace.Models;
using GennyMcGenFace.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Language = FastColoredTextBoxNS.Language;

namespace GennyMcGenFace.UI
{
    public class BaseUI
    {
        protected List<CodeClass> _classes;
        protected static GenOptions _opts = new GenOptions();
        protected Form _mainForm;
        protected FastColoredTextBox _editor = new FastColoredTextBox();
        protected AutoCompleteStringCollection _dataSource;

        protected ComboBox _classNameCombo1 = new ComboListMatcher
        {
            Left = 50,
            Top = 50,
            Width = 700,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        protected void Init(List<CodeClass> classes)
        {
            _classes = classes;
            _editor.Language = Language.CSharp;
            _dataSource = BuildAutoCompleteSource();

            _mainForm = new Form()
            {
                Width = 800,
                Height = 740,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            _mainForm.Controls.Add(_editor);
        }

        protected AutoCompleteStringCollection BuildAutoCompleteSource()
        {
            var classList = new AutoCompleteStringCollection();
            foreach (var t in _classes)
            {
                classList.Add(t.FullName);
            }

            return classList;
        }

        protected void InitTopRightControls()
        {
            var wordsTxt = new NumericUpDown()
            {
                Width = 50,
                Height = 50,
                Top = 15,
                Left = 700,
                Value = _opts.WordsInStrings,
                Increment = 1,
                Maximum = 9,
                Minimum = 0
            };

            var intLengthTxt = new NumericUpDown()
            {
                Width = 50,
                Height = 50,
                Top = 15,
                Left = 550,
                Value = _opts.IntLength,
                Increment = 1,
                Maximum = 9,
                Minimum = 0
            };

            var wordsLbl = new Label() { Left = 615, Top = 17, AutoSize = true, Text = "Words in strings" };
            var intLengthLbl = new Label() { Left = 480, Top = 17, AutoSize = true, Text = "Number size" };

            _mainForm.Controls.Add(intLengthLbl);
            _mainForm.Controls.Add(wordsLbl);
            _mainForm.Controls.Add(wordsTxt);
            _mainForm.Controls.Add(intLengthTxt);

            wordsTxt.ValueChanged += ChangeWordsInStr;
            intLengthTxt.ValueChanged += ChangeIntLength;
        }

        protected void InitCombo1()
        {
            _classNameCombo1.AutoCompleteCustomSource = _dataSource;
            _classNameCombo1.DataSource = _dataSource;
            _mainForm.Controls.Add(new Label() { Left = 50, Top = 25, Text = "ClassName" });
            _mainForm.Controls.Add(_classNameCombo1);
            //_classNameCombo1.SelectionChangeCommitted += GenerateEditorTxt;
            _classNameCombo1.SelectedIndexChanged += GenerateEditorTxt;
        }

        protected void ChangeWordsInStr(object sender, EventArgs e)
        {
            var txtBox = sender as NumericUpDown;
            if (txtBox == null) throw new Exception("Unable to change value");
            _opts.WordsInStrings = txtBox.Value;
            GenerateEditorTxt(null, null);
        }

        protected void ChangeIntLength(object sender, EventArgs e)
        {
            var txtBox = sender as NumericUpDown;
            if (txtBox == null) throw new Exception("Unable to change value");
            _opts.IntLength = txtBox.Value;
            GenerateEditorTxt(null, null);
        }

        protected virtual void GenerateEditorTxt(object sender, EventArgs e)
        {
            throw new Exception("Must override Redraw");
        }
    }
}