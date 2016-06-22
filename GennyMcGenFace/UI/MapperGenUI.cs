using EnvDTE;
using GennyMcGenFace.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GennyMcGenFace.UI
{
    public class MapperGenUI : BaseUI
    {
        protected ComboBox _classNameCombo2 = new ComboListMatcher
        {
            Left = 50,
            Top = 110,
            Width = 700,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };

        public MapperGenUI(List<CodeClass> classes)
        {
            base.Init(classes);
            _mainForm.Text = "Generate Mapper Unit Test Between 2 Classes";
            //  _mainForm.Height = 850;
            InitTopRightControls();
            InitCombo1();
            InitCombo2();

            _editor.Left = 50;
            _editor.Top = 170;
            _editor.Width = 700;
            _editor.Height = 600;

            _mainForm.Shown += GenerateEditorTxt;
            _mainForm.ShowDialog();
        }

        protected override void GenerateEditorTxt(object sender, EventArgs e)
        {
            var promptValue1 = _classNameCombo1.Text;
            if (string.IsNullOrWhiteSpace(promptValue1)) throw new Exception("Class name blank");

            var promptValue2 = _classNameCombo2.Text;
            if (string.IsNullOrWhiteSpace(promptValue2)) throw new Exception("Class name blank");

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue1);
            if (selectedClass == null) throw new Exception("Class not found");

            var generatedCode = ClassGenerator.GenerateClassStr(selectedClass, _opts);
            _editor.Text = generatedCode;
        }

        protected void InitCombo2()
        {
            _classNameCombo2.AutoCompleteCustomSource = _dataSource;
            _classNameCombo2.DataSource = _dataSource;

            _mainForm.Controls.Add(new Label() { Left = 50, Top = 85, Text = "ClassName" });
            _mainForm.Controls.Add(_classNameCombo2);
            _classNameCombo2.SelectionChangeCommitted += GenerateEditorTxt;
        }
    }
}