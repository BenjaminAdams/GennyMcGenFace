using EnvDTE;
using EnvDTE80;
using GennyMcGenFace.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.UI
{
    public class UnitTestGenUI : BaseUI
    {
        private DTE2 _dte;

        public UnitTestGenUI(List<CodeClass> classes, DTE2 dte)
        {
            base.Init(classes);

            _dte = dte;

            _mainForm.Text = "Generate Unit Test for a Class";
            InitTopRightControls();
            InitCombo1();

            _editor.Left = 50;
            _editor.Top = 90;
            _editor.Width = 900;
            _editor.Height = 600;

            _mainForm.Shown += GenerateEditorTxt;
            _mainForm.ShowDialog();
        }

        protected override void GenerateEditorTxt(object sender, EventArgs e)
        {
            var promptValue1 = _classNameCombo1.Text;
            if (string.IsNullOrWhiteSpace(promptValue1)) throw new Exception("Class name blank");

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue1);
            if (selectedClass == null) throw new Exception("Class not found");
            _editor.Text = "Loading...";
      
            var genner = new UnitTestGenerator(selectedClass, _dte);
            _editor.Text = genner.Gen(selectedClass, _opts);
        }
    }
}