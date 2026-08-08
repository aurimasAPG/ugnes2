// iOS haptics bridge. UIFeedbackGenerator runs off the main render path, so these
// cost the frame budget nothing. Kept to the three canonical flavors.
#import <UIKit/UIKit.h>

extern "C" {

void _hv_hapticImpact(int strength)
{
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackStyle style = strength >= 2 ? UIImpactFeedbackStyleHeavy
                                    : strength == 1 ? UIImpactFeedbackStyleMedium
                                                    : UIImpactFeedbackStyleLight;
        UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [gen prepare];
        [gen impactOccurred];
    }
}

void _hv_hapticSuccess(void)
{
    if (@available(iOS 10.0, *)) {
        UINotificationFeedbackGenerator *gen = [[UINotificationFeedbackGenerator alloc] init];
        [gen prepare];
        [gen notificationOccurred:UINotificationFeedbackTypeSuccess];
    }
}

void _hv_hapticTick(void)
{
    if (@available(iOS 10.0, *)) {
        UISelectionFeedbackGenerator *gen = [[UISelectionFeedbackGenerator alloc] init];
        [gen prepare];
        [gen selectionChanged];
    }
}

}
