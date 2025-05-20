using QRDessFree;
using Microsoft.Maui.Controls;




namespace QRDessFree
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
