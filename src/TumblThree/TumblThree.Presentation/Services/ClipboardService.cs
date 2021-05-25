using System;
using System.ComponentModel.Composition;
using System.Windows;
using TumblThree.Applications;
using TumblThree.Applications.Services;
using TumblThree.Domain;

namespace TumblThree.Presentation.Services
{
    [Export(typeof(IClipboardService))]
    internal class ClipboardService : IClipboardService
    {
        private IShellService _shellService;

        public ClipboardService(IShellService shellService)
        {
            _shellService = shellService;
        }

        public void SetText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                Logger.Error($"ClipboardService:SetText: {ex}");
                _shellService.ShowError(new ClipboardContentException(ex), "error setting clipboard content");
            }
        }
    }
}
