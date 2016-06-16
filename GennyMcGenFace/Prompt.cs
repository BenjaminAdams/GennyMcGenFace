using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GennyMcGenFace.GennyMcGenFace
{
    public class Prompt
    {
        private readonly List<CodeClass> _classes;

        private ComboListMatcher _classNameCombo1 = new ComboListMatcher()
        {
            Left = 50,
            Top = 50,
            Width = 600,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        private RichTextBox _editor = new RichTextBox()
        {
            Left = 50,
            Top = 120,
            Width = 600,
            Height = 500,
            AcceptsTab = true,
            Multiline = true
        };

        public Prompt(List<CodeClass> classes)
        {
            _classes = classes;
            var dataSource = BuildAutoCompleteSource();
            _classNameCombo1.AutoCompleteCustomSource = dataSource;
            _classNameCombo1.DataSource = dataSource;
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
                Height = 700,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            var textLabel = new Label() { Left = 50, Top = 20, Text = "ClassName" };

            // Button confirmation = new Button() { Text = "Ok", Left = 550, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            // confirmation.Click += (sender, e) => { prompt.Close(); };

            _classNameCombo1.SelectedValueChanged += (sender, e) => { ChangeSelectedClass(); };

            prompt.Controls.Add(_classNameCombo1);
            prompt.Controls.Add(_editor);
            // prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            // prompt.AcceptButton = confirmation;
            prompt.ShowDialog();
            //return prompt.ShowDialog() == DialogResult.OK ? classNameCombo1.Text : "";
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
    }
}