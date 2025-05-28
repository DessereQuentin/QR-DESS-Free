using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System.Text.Json;

namespace QRDessFree
{
    public partial class MainPage : ContentPage
    {
        /// <summary>Indique que l'application vient d'être lancée</summary>
        private Boolean isNew=true;
        public MainPage()
        {
            InitializeComponent();

            this.Loaded += async(s, e) =>
            {
                // La page est vraiment visible, l’UI est prête
                if (isNew) await LancerAnimationAsync();
                isNew = false;
            };

        }
        /// <summary>Ouvre la page de l'application</summary>
        /// <returns></returns>
        private async Task LancerAnimationAsync()
        {
            // Facultatif pour être sûr que l'UI est prête
            await Task.Delay(100); 

            //Animation d'ouverture
            int iRotation;

            iRotation = 0;
            QRCodeDESS.HeightRequest = 0;
            QRCodeDESS.WidthRequest = 0;
            QRCodeDESS.IsVisible = true;

            // On fait tourner le QRCode de la page de lancement en le faisant grossir
            for (int i=0; i<360;i++)
            {
                iRotation += 4;
                await QRCodeDESS.RotateTo(iRotation, 1);
                QRCodeDESS.HeightRequest = i/2;
                QRCodeDESS.WidthRequest = i/2;
            }

            // Affichage libellés de bienvenue
            await Label1.FadeTo(1, 500);
            await Label2.FadeTo(1, 500);
            await Label3.FadeTo(1, 500);

            // Affichage boton de lancement
            btnStart.Opacity=1;
        }

        private async void OnOpenAppClicked(object sender, EventArgs e)
        {
            // Ouvre la page de l'application
            await Navigation.PushAsync(new AppPage());
        }

        private async Task AfficheLibelle()
        {
            Label1.IsVisible = true;
            Label2.IsVisible = true;
            Label3.IsVisible = true;
        }





    }
}

