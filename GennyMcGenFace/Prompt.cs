using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GennyMcGenFace.GennyMcGenFace
{
    public static class Prompt
    {
        public static string ShowDialog(List<CodeClass> classes)
        {
            Form prompt = new Form()
            {
                Width = 700,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pick a class",
                StartPosition = FormStartPosition.CenterScreen
            };

            var classList = new AutoCompleteStringCollection();
            foreach (var t in classes)
            {
                classList.Add(t.FullName);
            }

            var classNameCombo1 = new ComboListMatcher()
            {
                Left = 50,
                Top = 50,
                Width = 600,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                AutoCompleteCustomSource = classList,
                DataSource = classList
            };

            Label textLabel = new Label() { Left = 50, Top = 20, Text = "ClassName" };
            Button confirmation = new Button() { Text = "Ok", Left = 550, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(classNameCombo1);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? classNameCombo1.Text : "";
        }
    }
}