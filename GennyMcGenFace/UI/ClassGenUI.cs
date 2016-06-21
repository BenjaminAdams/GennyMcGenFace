using EnvDTE;
using FastColoredTextBoxNS;
using GennyMcGenFace.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GennyMcGenFace.UI
{
    public class ClassGenUI : BaseUI
    {
        public ClassGenUI(List<CodeClass> classes)
        {
            base.Init(classes);
            _mainForm.ShowDialog();

            _editor.Left = 50;
            _editor.Top = 90;
            _editor.Width = 700;
            _editor.Height = 600;
        }

        protected override void Redraw()
        {
            var promptValue = _classNameCombo1.Text;
            if (promptValue == "") return;

            var selectedClass = _classes.FirstOrDefault(x => x.FullName == promptValue);
            if (selectedClass == null) throw new Exception("Class not found");

            var generatedCode = CodeGenerator.GenerateClass(selectedClass, _opts);
            _editor.Text = generatedCode;
        }
    }
}