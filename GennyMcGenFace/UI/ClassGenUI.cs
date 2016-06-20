using EnvDTE;
using FastColoredTextBoxNS;
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
            _editor.Language = Language.CSharp;
        }
    }
}