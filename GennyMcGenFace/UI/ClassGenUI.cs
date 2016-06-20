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
    public class ClassGenUI
    {
        private readonly List<CodeClass> _classes;
        private static GenOptions _opts = new GenOptions();

        private ComboListMatcher _classNameCombo1 = new ComboListMatcher()
        {
            Left = 50,
            Top = 50,
            Width = 700,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        private FastColoredTextBox _editor = new FastColoredTextBox
        {
            Left = 50,
            Top = 90,
            Width = 700,
            Height = 600,
        };

        private NumericUpDown _wordsTxt = new NumericUpDown()
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

        private NumericUpDown _intLengthTxt = new NumericUpDown()
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

        public ClassGenUI(List<CodeClass> classes)
        {
            _classes = classes;
            var dataSource = BuildAutoCompleteSource();
            _classNameCombo1.AutoCompleteCustomSource = dataSource;
            _classNameCombo1.DataSource = dataSource;
            AddStyles();
        }

        private AutoCompleteStringCollection BuildAutoCompleteSource()
        {
            var classList = new AutoCompleteStringCollection();
            foreach (var t in _classes)
            {
                classList.Add(t.FullName);
            }

            return classList;
        }

        public void ShowDialog()
        {
            var prompt = new Form()
            {
                Width = 800,
                Height = 740,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            prompt.Controls.Add(new Label() { Left = 50, Top = 25, Text = "ClassName" });

            prompt.Controls.Add(new Label() { Left = 615, Top = 17, AutoSize = true, Text = "Words in strings" });
            prompt.Controls.Add(new Label() { Left = 480, Top = 17, AutoSize = true, Text = "Number size" });

            _classNameCombo1.SelectedValueChanged += (sender, e) => { ChangeSelectedClass(); };
            _wordsTxt.ValueChanged += (sender, e) => { ChangeWordsInStr(); };
            _intLengthTxt.ValueChanged += (sender, e) => { ChangeIntLength(); };

            prompt.Controls.Add(_wordsTxt);
            prompt.Controls.Add(_intLengthTxt);
            prompt.Controls.Add(_classNameCombo1);
            prompt.Controls.Add(_editor);

            prompt.ShowDialog();
        }

        private void ChangeWordsInStr()
        {
            _opts.WordsInStrings = _wordsTxt.Value;
            ChangeSelectedClass();
        }

        private void ChangeIntLength()
        {
            _opts.IntLength = _intLengthTxt.Value;
            ChangeSelectedClass();
        }

        private void ChangeSelectedClass()
        {
            var promptValue = _classNameCombo1.Text;
            if (promptValue == "") return;

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue);
            if (selectedClass == null) throw new Exception("Class not found");

            var generatedCode = CodeGenerator.GenerateClass(selectedClass, _opts);
            _editor.Text = generatedCode;
        }

        private void AddStyles()
        {
            _editor.Language = Language.CSharp;
        }
    }
}