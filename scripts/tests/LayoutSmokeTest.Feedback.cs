using Godot;
using System.Threading.Tasks;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class LayoutSmokeTest
{
    private async Task CheckGainPresentation(Vector2I dimensions)
    {
        var s = ServiceLocator.Instance!;
        s.PlayerActionState!.SetAction("cultivation"); s.PlayerActionState.SetWorkMode(false);
        await Select("CultivationTab");
        var scroll = (ScrollContainer)PageRoot("CultivationScroll"); scroll.ScrollVertical = 0;
        s.InputHookService!.SetPaused(false); s.InputActivityState!.RegisterKeyDown(); await Settle();
        var card = (Control)_tabs.FindChild("HomeGainFeedback", true, false);
        var headline = (Label)_tabs.FindChild("GainHeadline", true, false);
        var hud = _bar.GetNode<Control>("Chrome/GainFeedbackHud");
        Check(card.Size.X <= scroll.Size.X && Inside(_book.GetGlobalRect(), headline.GetGlobalRect()), dimensions + ": immediate gain headline fits visible paper");
        Check(hud.IsVisibleInTree() && Inside(_bar.GetGlobalRect(), hud.GetGlobalRect()), dimensions + ": immediate gain stays in the compact desktop bar");
        Check(Inside(card.GetGlobalRect(), ((Control)_tabs.FindChild("GainReview", true, false)).GetGlobalRect())
            && Inside(card.GetGlobalRect(), ((Control)_tabs.FindChild("GainMotion", true, false)).GetGlobalRect()), dimensions + ": gain actions wrap inside their card");
        var exact = _bar.GetNode<Control>("Chrome/CultivationProgressBar/ExactExperience");
        Check(exact.Size.X <= exact.GetParent<Control>().Size.X && !exact.GetParent<ProgressBar>().ShowPercentage,
            dimensions + ": precise experience fits without a second overlapping percentage");
        var picker = (Control)_tabs.FindChild("HomeActivityPicker", true, false);
        if (!Inside(scroll.GetGlobalRect(), picker.GetGlobalRect()))
        {
            GD.Print($"GAIN LAYOUT: scroll={scroll.GetGlobalRect()} picker={picker.GetGlobalRect()} card={card.GetGlobalRect()} headline={headline.Text}");
            await Capture("gain_overflow");
        }
        Check(Inside(scroll.GetGlobalRect(), picker.GetGlobalRect()), dimensions + ": activity choice remains visible below feedback");
        if (dimensions.X is 1600 or 800) await Capture("gain_input_" + dimensions.X);
        if (dimensions.X == 800)
        {
            s.EventLogState!.AddEvent(EventLogState.CategoryCraft, "回气丹炼制完成 ×1", accentId: EventLogState.AccentHighlight);
            await Settle(); await Capture("small_gain_result");
        }
    }
}
