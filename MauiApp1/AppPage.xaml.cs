using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using MauiApp1;
using System.IO;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using Microsoft.Maui.Devices;

namespace MauiApp1
{
    public partial class AppPage : ContentPage
    {
        private IDrawable qrDrawable;

        public AppPage()
        {
            InitializeComponent();

        }

        private void OnGenerateQRCodeClicked(object sender, EventArgs e)
        {
            // Récupérer le texte saisi
            string texte = txtInput.Text;

            // Vérifier qu'un texte est bien entré
            if (string.IsNullOrWhiteSpace(texte))
            {
                DisplayAlert("Erreur", "Veuillez entrer un texte à encoder.", "OK");
                return;
            }

            // Récupérer le niveau de correction sélectionné
            string correctionLevel = "L"; // Valeur par défaut
            if (rbM.IsChecked) correctionLevel = "M";
            else if (rbQ.IsChecked) correctionLevel = "Q";
            else if (rbH.IsChecked) correctionLevel = "H";

            // Récupérer la taille de bordure
            int tailleBordure = 1;
            int imagesize = 0;

            // Générer le QR Code et l'afficher
            qrDrawable = CLSGenereQRCode.GenereImageQRCode(texte, correctionLevel, tailleBordure, ref imagesize);
            qrCodeView.WidthRequest = imagesize;
            qrCodeView.HeightRequest = imagesize;
            qrCodeView.Drawable = qrDrawable;
            qrCodeView.Invalidate(); // Redessiner la vue
        }

        // on enregistre l'image dans un fichier
        private async Task<string> SauvegarderQrCodeAsync(IDrawable drawable, int width, int height)
        {
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            // Convertir Drawable MAUI en dessin SkiaSharp
            var mauiCanvas = new Microsoft.Maui.Graphics.Skia.SkiaCanvas();
            mauiCanvas.Canvas = canvas;
            drawable.Draw(mauiCanvas, new RectF(0, 0, width, height));

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var filePath = Path.Combine(FileSystem.CacheDirectory, "qrcode.png");

            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);

            return filePath;
        }

        private async void OnShareQrCodeClicked(object sender, EventArgs e)
        {
            if (qrDrawable != null)
            {

                var path = await SauvegarderQrCodeAsync(qrDrawable, (int)qrCodeView.WidthRequest, (int)qrCodeView.HeightRequest);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Partager le QR Code",
                    File = new ShareFile(path)
                });
            }
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            string titre = "Aide - QR Dess Free";
            string message = "• Saisir le texte à encoder.\n" +
                             "• Sélectionner le niveau de correction.\n" +
                             "• Générer le qrCode.\n" +
                             "• Partager.";
            string bouton = "OK";

            await DisplayAlert(titre, message, bouton);
        }

    }

}
