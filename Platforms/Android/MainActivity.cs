using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace QRDessFree;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.SensorPortrait )]
public class MainActivity : MauiAppCompatActivity
{

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Modifier la couleur de la status bar
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#8F8F7A"));
    }


}
