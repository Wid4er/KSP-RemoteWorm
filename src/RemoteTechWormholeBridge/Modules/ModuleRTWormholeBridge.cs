using KSP.Localization;

namespace RemoteTechWormholeBridge
{
    public sealed class ModuleRTWormholeBridge : PartModule
    {
        [KSPField(
            isPersistant = true,
            guiActive = true,
            guiActiveEditor = true,
            guiName = "#LOC_RTWB_bridgeEnabled")]
        [UI_Toggle(
            enabledText = "#LOC_RTWB_enabled",
            disabledText = "#LOC_RTWB_disabled")]
        public bool bridgeEnabled = true;

        [KSPField(isPersistant = true)]
        public int channel;

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_RTWB_moduleTitle");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            DiagnosticController.RequestRefresh();
        }
    }
}
