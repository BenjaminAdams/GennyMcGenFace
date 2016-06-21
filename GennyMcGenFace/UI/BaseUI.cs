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

        protected ComboListMatcher _classNameCombo1 = new ComboListMatcher()
        {
            Left = 50,
            Top = 50,
            Width = 700,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        protected FastColoredTextBox _editor = new FastColoredTextBox
        {
            Left = 50,
            Top = 90,
            Width = 700,
            Height = 600,
        };

        protected NumericUpDown _wordsTxt = new NumericUpDown()
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

        protected Label _classNameCombo1Lbl = new Label() { Left = 50, Top = 25, Text = "ClassName" };
        protected Label _wordsLbl = new Label() { Left = 615, Top = 17, AutoSize = true, Text = "Words in strings" };
        protected Label _intLengthLbl = new Label() { Left = 480, Top = 17, AutoSize = true, Text = "Number size" };

        protected NumericUpDown _intLengthTxt = new NumericUpDown()
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

        protected AutoCompleteStringCollection BuildAutoCompleteSource()
        {
            var classList = new AutoCompleteStringCollection();
            foreach (var t in _classes)
            {
                classList.Add(t.FullName);
            }

            return classList;
        }

        protected void Init(List<CodeClass> classes)
        {
            _classes = classes;
            var dataSource = BuildAutoCompleteSource();
            _classNameCombo1.AutoCompleteCustomSource = dataSource;
            _classNameCombo1.DataSource = dataSource;
            _editor.Language = Language.CSharp;

            _mainForm = new Form()
            {
                Width = 800,
                Height = 740,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };
            _mainForm.Controls.Add(_classNameCombo1Lbl);

            _classNameCombo1.SelectedValueChanged += (sender, e) => { Redraw(); };
            _wordsTxt.ValueChanged += (sender, e) => { ChangeWordsInStr(); };
            _intLengthTxt.ValueChanged += (sender, e) => { ChangeIntLength(); };

            _mainForm.Controls.Add(_classNameCombo1Lbl);
            _mainForm.Controls.Add(_intLengthLbl);
            _mainForm.Controls.Add(_wordsLbl);

            _mainForm.Controls.Add(_wordsTxt);
            _mainForm.Controls.Add(_intLengthTxt);
            _mainForm.Controls.Add(_classNameCombo1);
            _mainForm.Controls.Add(_editor);
        }

        protected void ChangeWordsInStr()
        {
            _opts.WordsInStrings = _wordsTxt.Value;
            Redraw();
        }

        protected void ChangeIntLength()
        {
            _opts.IntLength = _intLengthTxt.Value;
            Redraw();
        }

        protected virtual void Redraw()
        {
            throw new Exception("Must override Redraw");
        }
    }
}