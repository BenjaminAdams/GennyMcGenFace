using EnvDTE;
using GennyMcGenFace.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GennyMcGenFace.UI
{
    public class MapperGenUI : BaseUI
    {
        public MapperGenUI(List<CodeClass> classes)
        {
            base.Init(classes);
            _mainForm.Text = "Generate Mapper Unit Test Between 2 Classes";
            //  _mainForm.Height = 850;
            InitTopRightControls();
            InitCombo1();
            // InitCombo2();
            var _classNameCombo2 = new ComboListMatcher()
            {
                Left = 50,
                Top = 110,
                Width = 700,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
                AutoCompleteCustomSource = _dataSource,
                DataSource = _dataSource
            };

            //_mainForm.Controls.Add(new Label() { Left = 50, Top = 85, Text = "ClassName" });
            // _classNameCombo2.SelectedValueChanged += (sender, e) => { GenerateEditorTxt(); };
            _classNameCombo2.TextChanged += (sender, e) => { GenerateEditorTxt(); };
            _mainForm.Controls.Add(_classNameCombo2);

            _editor.Left = 50;
            _editor.Top = 170;
            _editor.Width = 700;
            _editor.Height = 600;

            _mainForm.ShowDialog();
        }

        //public MapperGenUI(List<CodeClass> classes)
        //{
        //    base.Init(classes);
        //    _mainForm.Text = "Generate Mapper Unit Test Between 2 Classes";
        //    _mainForm.Height = 850;
        //    InitTopRightControls();
        //    InitCombo1();
        //    InitCombo2();

        //    _editor.Left = 50;
        //    _editor.Top = 170;
        //    _editor.Width = 700;
        //    _editor.Height = 600;

        //    _mainForm.ShowDialog();
        //}

        protected override void GenerateEditorTxt()
        {
            var promptValue1 = "";
            // var promptValue1 = _classNameCombo1.Text;
            //  if (string.IsNullOrWhiteSpace(promptValue1)) throw new Exception("Class name blank");

            // var promptValue2 = _classNameCombo2.Text;
            //  if (string.IsNullOrWhiteSpace(promptValue2)) throw new Exception("Class name blank");

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue1);
            if (selectedClass == null) throw new Exception("Class not found");

            var generatedCode = CodeGenerator.GenerateClass(selectedClass, _opts);
            _editor.Text = generatedCode;
        }
    }
}