using EnvDTE;
using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Language = FastColoredTextBoxNS.Language;

namespace GennyMcGenFace
{
    public class TMP
    {
        public string tmp { get; set; }
    }

    public class Prompt
    {
        private readonly List<CodeClass> _classes;
        private int lastCaretPos = 0;

        private ComboListMatcher _classNameCombo1 = new ComboListMatcher()
        {
            Left = 50,
            Top = 50,
            Width = 600,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        private FastColoredTextBox _editor = new FastColoredTextBox
        {
            Left = 50,
            Top = 90,
            Width = 600,
            Height = 500,
        };

        public Prompt(List<CodeClass> classes)
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
                Width = 700,
                Height = 640,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            var textLabel = new Label() { Left = 50, Top = 25, Text = "ClassName" };
            _classNameCombo1.SelectedValueChanged += (sender, e) => { ChangeSelectedClass(); };

            prompt.Controls.Add(_classNameCombo1);
            prompt.Controls.Add(_editor);
            prompt.Controls.Add(textLabel);
            prompt.ShowDialog();
        }

        private void ChangeSelectedClass()
        {
            var promptValue = _classNameCombo1.Text;
            if (promptValue == "") return;

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue);
            if (selectedClass == null) throw new Exception("Class not found");

            var generatedCode = CodeGenerator.GenerateClass(selectedClass);
            _editor.Text = generatedCode;
            //_editor.ProcessAllLines();
        }

        private void AddStyles()
        {
            _editor.Language = Language.CSharp;
            //_editor.SyntaxHighlighter= new SyntaxHighlighter(){};
            //_editor.Settings.Comment = "//";
            //_editor.Settings.KeywordColor = Color.Blue;
            //_editor.Settings.CommentColor = Color.Green;
            //_editor.Settings.StringColor = Color.DarkRed;
            //_editor.Settings.IntegerColor = Color.DarkOrange;
            //_editor.Settings.EnableStrings = true;
            //_editor.Settings.EnableIntegers = true;
            //_editor.Settings.Keywords.AddRange(new string[] { "break", "case", "catch", "false", "interface","namespace", "new", "null", "object", "private", "protected", "public", "return", "true", "try", });
            //_editor.Settings.Keywords.AddRange(new string[] { "var", "datetime", "bool", "byte", "char", "class", "const", "decimal", "double", "enum", "float", "int", "long", "static", "string", "void" });
            //_editor.CompileKeywords();
            //_editor.ProcessAllLines();
        }
    }
}