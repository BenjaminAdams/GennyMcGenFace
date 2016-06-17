using EnvDTE;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GennyMcGenFace
{
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

        private Scintilla _editor = new Scintilla()
        {
            Left = 50,
            Top = 90,
            Width = 600,
            Height = 500,
            Lexer = Lexer.Cpp
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
        }

        private void AddStyles()
        {
            _editor.StyleResetDefault();
            _editor.Styles[Style.Default].Font = "Consolas";
            _editor.Styles[Style.Default].Size = 10;
            _editor.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            _editor.Styles[Style.Cpp.Default].ForeColor = Color.Silver;
            _editor.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0); // Green
            _editor.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0); // Green
            _editor.Styles[Style.Cpp.CommentLineDoc].ForeColor = Color.FromArgb(128, 128, 128); // Gray
            _editor.Styles[Style.Cpp.Number].ForeColor = Color.Olive;
            _editor.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
            _editor.Styles[Style.Cpp.Word2].ForeColor = Color.Blue;
            _editor.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.Verbatim].ForeColor = Color.FromArgb(163, 21, 21); // Red
            _editor.Styles[Style.Cpp.StringEol].BackColor = Color.Pink;
            _editor.Styles[Style.Cpp.Operator].ForeColor = Color.Purple;
            _editor.Styles[Style.Cpp.Preprocessor].ForeColor = Color.Maroon;
            _editor.Lexer = Lexer.Cpp;

            _editor.IndentationGuides = IndentView.LookBoth;

            // Set the keywords
            _editor.SetKeywords(0, "abstract as base break case catch checked continue default delegate do else event explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null object operator out override params private protected public readonly ref return sealed sizeof stackalloc switch this throw true try typeof unchecked unsafe using virtual while");
            _editor.SetKeywords(1, "var datetime bool byte char class const decimal double enum float int long sbyte short static string struct uint ulong ushort void");
        }
    }
}