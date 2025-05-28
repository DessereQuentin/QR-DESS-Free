using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using QRDessFree;
using System.IO;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui;
using static QRDessFree.CLSGenereQRCode;

namespace QRDessFree
{
    public partial class AppPage : ContentPage
    {
        /// <summary>Sauvegarde du iDrawable du graphicsView du QrCode</summary>
        private IDrawable qrDrawable;
        private int PourcentCorrection;
        private int tailleBordure = 1;
        private Microsoft.Maui.Graphics.IImage imageAIncorporer;
        private string saveFilenameAIncorporer;
        public AppPage()
        {
            InitializeComponent();

        }
        /// <summary>Incorporation au QRCODE d'une image choisie par l'utilisateur</summary>
        /// <returns>O si aucune image choisie, 1 sinon</returns>
        private async Task<int> IncorporeImage()
        {
            // L'utilisateur choisit une image
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choisissez une image pour incorporer au centre du QRCode",
                FileTypes = FilePickerFileType.Images
            });

            // S'il n'a pas choisi d'image, on ne fait rien
            if (result == null) return 0;

            // On sauve le nom du fichier pour pouvoir le réouvrir ultérieuremnt lors du partage
            saveFilenameAIncorporer = result.FullPath;

            //On ouvre le fichier
            using var stream = await result.OpenReadAsync();
            imageAIncorporer = PlatformImage.FromStream(stream);

            // On affecte le résultat fusionné à la vue graphique
            qrDrawable = new ImageDrawable(imageAIncorporer, PourcentCorrection, ModulesQRCode, tailleBordure);
            qrCodeView.Drawable = qrDrawable;
            qrCodeView.Invalidate();

            return 1;
        }

        /// <summary>Permet d'incoporer une image au QRCode</summary>
        private async void OnIncorporeClicked(object sender, EventArgs e)
        {
           

           
            if (qrCodeView.Drawable == null)
            {
                await DisplayAlert("Incorporation d'image", "Veuillez d'abord générer un QR Code.", "OK");
                return;
            }
            int retour = await IncorporeImage();
 
        }

        /// <summary>Déclenche la génération du QRCode</summary>
        private async void  OnGenerateQRCodeClicked(object sender, EventArgs e)
        {
            // Récupérer le texte saisi
            string texte = txtInput.Text;

            // Vérifier qu'un texte est bien entré
            if (string.IsNullOrWhiteSpace(texte))
            {
                await DisplayAlert("Génération du QR Code", "Veuillez entrer un texte à encoder.", "OK");
                qrCodeView.Drawable = null;
                qrCodeView.Invalidate(); 
                return;
            }

            // Récupérer le niveau de correction sélectionné
            string correctionLevel = "L"; // Valeur par défaut
            if (rbM.IsChecked) correctionLevel = "M";
            else if (rbQ.IsChecked) correctionLevel = "Q";
            else if (rbH.IsChecked) correctionLevel = "H";

            //Conserver le taux de correction
            switch (correctionLevel)
            {
                case "L": PourcentCorrection = 7; break;
                case "M": PourcentCorrection = 15; break;
                case "Q": PourcentCorrection = 25; break;
                case "H": PourcentCorrection = 30; break;
            }

            //Reset l'image à incorporer
            imageAIncorporer = null;
            saveFilenameAIncorporer = "";

            // Récupérer la taille de bordure
            int imagesize = 0;

            // Générer le QR Code et l'afficher
            qrDrawable = CLSGenereQRCode.GenereImageQRCode(texte, correctionLevel, tailleBordure, ref imagesize, PourcentCorrection);
            if (qrDrawable == null)
            {
                await DisplayAlert("Génération du QR Code", "La chaine à générer est trop longue pour la capacité d'un QR Code. Essayez de réduire le niveau de correction", "OK");
                qrCodeView.Drawable = null;
                qrCodeView.Invalidate();
                return;
            }
            qrCodeView.WidthRequest = imagesize;
            qrCodeView.HeightRequest = imagesize;
            qrCodeView.Drawable = qrDrawable;
            qrCodeView.Invalidate(); // Redessiner la vue

        }

        /// <summary>On enregistre l'image dans un fichier</summary>
        /// <param name="drawable">objet drawable contenant l'image</param>
        /// <param name="width">largeur de l'image</param>
        /// <param name="height">Hauter de l'image</param>
        /// <returns>Le nom du fichier</returns>
        private async Task<string> SauvegarderQrCodeAsync(IDrawable drawable, int width, int height)
        {
            // Récupérer le drawable dans une variable
            var ddrawable = qrCodeView.Drawable;
            string filePath = Path.Combine(FileSystem.CacheDirectory, "QRCode"+ DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".jpg");

            CLSGenereQRCode.SaveDrawingToJpg(filePath, (int) qrCodeView.Width, (int)qrCodeView.Height, saveFilenameAIncorporer, tailleBordure,PourcentCorrection);

            return filePath;
        }

        /// <summary>Partage du QRCode après l'avoir sauvegardé dans un fichier</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnShareQrCodeClicked(object sender, EventArgs e)
        {
            if (qrCodeView.Drawable != null)
            {
                var path = await SauvegarderQrCodeAsync(qrCodeView.Drawable, (int)qrCodeView.WidthRequest, (int)qrCodeView.HeightRequest);
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Partager le QR Code",
                    File = new ShareFile(path)
                });
            }
        }


       /// <summary>Affichage du texte d'aide</summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
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
