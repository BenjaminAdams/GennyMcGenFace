using Microsoft.VisualStudio.Shell.Interop;

namespace GennyMcGenFace.UI
{
    public class StatusBar
    {
        private IVsStatusbar _statusBar;
        private uint _cookie;

        public StatusBar(IVsStatusbar bar)
        {
            _statusBar = bar;
            _cookie = 0;
            Start();
        }

        public void Start()
        {
            // Initialize the progress bar.
            _statusBar.Progress(ref _cookie, 1, "", 0, 0);
        }

        public void End()
        {
            _statusBar.Progress(ref _cookie, 0, "", 0, 0);
        }

        public void Progress(string label, int position, int totalOperationsCount)
        {
            _statusBar.Progress(ref _cookie, 1, label, (uint)position, (uint)totalOperationsCount);
        }
    }
}