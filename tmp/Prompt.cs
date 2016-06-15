using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Genny.GennyMcGenFace
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
                Text = "Pick a classxx",
                StartPosition = FormStartPosition.CenterScreen
            };

            // var classList = new AutoCompleteStringCollection();
            var classList = new List<string>();
            foreach (var t in classes)
            {
                classList.Add(t.FullName);
            }

            //var classNameCombo1 = new ComboListMatcher()
            //{
            //    Left = 50,
            //    Top = 50,
            //    Width = 600,
            //    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            //    AutoCompleteSource = AutoCompleteSource.CustomSource,
            //    AutoCompleteCustomSource = classList,
            //    DataSource = classList
            //};

            var classNameCombo1 = new AutoCompleteTextBox(classList.ToArray())
            {
                Left = 50,
                Top = 50,
                Width = 600
            };

            // classNameCombo1.TextUpdate

            //classNameCombo1.TextUpdate += (sender, e) =>
            //{
            //    // var tmpDataSource = classList.Where(x =>x.);
            //    // classNameCombo1.DataSource = tmpDataSource;

            //    string item = classNameCombo1.Text;
            //    item = item.ToLower();
            //    classNameCombo1.Items.Clear();
            //    List<string> list = new List<string>();
            //    for (int i = 0; i < classList.Count; i++)
            //    {
            //        if (classList[i].ToLower().Contains(item))
            //            list.Add(classList[i]);
            //    }
            //    if (item != String.Empty)
            //        foreach (string str in list)
            //            classNameCombo1.Items.Add(str);
            //    else
            //        classNameCombo1.Items.AddRange(classList);
            //    classNameCombo1.SelectionStart = item.Length;
            //    classNameCombo1.DroppedDown = true;
            //};

            Label textLabel = new Label() { Left = 50, Top = 20, Text = "ClassNamexx" };
            // TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 450, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(classNameCombo1);

            //  prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? classNameCombo1.Text : "";
        }
    }
}