using MauiApp1;
using Microsoft.Maui.Controls;




namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Configure la navigation
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
