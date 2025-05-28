using Foundation;
using UIKit;

namespace QRDessFree;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();


    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);

        // Définir la couleur de fond de la status bar (simulée)
        var statusBarFrame = UIApplication.SharedApplication.StatusBarFrame;
        var statusBarView = new UIView(statusBarFrame)
        {
            BackgroundColor = UIColor.FromRGB(143, 143, 122) 
        };

        UIApplication.SharedApplication.KeyWindow?.AddSubview(statusBarView);

        return result;
    }


}
