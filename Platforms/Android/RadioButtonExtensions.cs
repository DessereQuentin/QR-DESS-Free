#if ANDROID

using Android.Content.Res;
using Microsoft.Maui.Handlers;
using AndroidX.AppCompat.Widget;
using AColor = Android.Graphics.Color;
using Android.Graphics;


namespace QRDessFree.Platforms.Android;

public static class RadioButtonStyling
{
    public static void InitRadioButtonColor()
    {
        RadioButtonHandler.Mapper.AppendToMapping("CustomColor", (handler, view) =>
        {
            if (handler.PlatformView is AppCompatRadioButton nativeRadioButton)
            {
                nativeRadioButton.ButtonTintList =
                    ColorStateList.ValueOf(AColor.ParseColor("#8F8F7A"));
            }
        });
    }
}
#endif