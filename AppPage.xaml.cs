using Microsoft.Maui.Graphics;
using SkiaSharp;
using static QRDessFree.clsGenereQRCode;
using static QRDessFree.clsGraphicsQRCode;

namespace QRDessFree
{
    public partial class AppPage : ContentPage
    {
        /// <summary>Sauvegarde du pourcentage de correction</summary>
        private float pourcentCorrection;

        public AppPage()
        {
            InitializeComponent();
        }

        /// <summary>Permet d'incoporer une image au QRCode</summary>
        private async void OnIncorporeClicked(object sender, EventArgs e)
        {

            try

            {
                if (qrCodeView.Drawable == null)
                {
                    await DisplayAlert("Incorporation d'image", "Veuillez d'abord générer un QR Code.", "OK");
                    return;
                }

                // L'utilisateur choisit une image
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Choisissez une image pour incorporer au centre du QRCode",
                    FileTypes = FilePickerFileType.Images
                });

                // S'il n'a pas choisi d'image, on ne fait rien
                if (result == null) return;

                string sRetour = await TraiteImage(result.FullPath, qrCodeView, pourcentCorrection);

                if (sRetour != "")
                {
                    // Affichage message d'erreur
                    await DisplayAlert("Partage du QR Code", sRetour, "OK");
                }

                return;

            }

            catch (Exception ex)
            {
                // Affichage message d'erreur
                await DisplayAlert("Exception", "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}", "OK");
            }
        }


        /// <summary>Déclenche la génération du QRCode</summary>
        private async void  OnGenerateQRCodeClicked(object sender, EventArgs e)
        {
            try
            {

                // Récupére le texte saisi et aménage les retours à la ligne
                string texte = txtInput.Text;
                texte = texte.Replace("\r\n","\n").Replace("\r","\n");

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

                //Reset l'image à incorporer
                saveSKBitmap = null;
                pourcentCorrection = 0;

                // Récupérer la taille de bordure
                int imagesize = 0;

                // On réinitialise le message d'erreur
                messageErreur = "";
                // Générer le QR Code et l'afficher
                IDrawable qrDrawable = clsGraphicsQRCode.GenereImageQRCode(texte, correctionLevel, tailleBordure, ref imagesize, ref pourcentCorrection);
                if (qrDrawable == null)
                {
                    if (messageErreur != "")
                    {
                        // Affichage message d'erreur
                        await DisplayAlert("Exception", messageErreur, "OK");
                        return;
                    }
                    else
                    {
                        await DisplayAlert("Génération du QR Code", "La chaine à générer est trop longue pour la capacité d'un QR Code. Essayez de réduire le niveau de correction", "OK");
                        qrCodeView.Drawable = null;
                        qrCodeView.Invalidate();
                        return;
                    }
                }
                qrCodeView.WidthRequest = imagesize;
                qrCodeView.HeightRequest = imagesize;
                
                // Réinitialiser le message d'erreur
                messageErreur = "";
                // On dessine le QR Code dans la vue
                qrCodeView.Drawable = qrDrawable;
                qrCodeView.Invalidate(); // Redessiner la vue
                if (messageErreur != "")
                {
                    // Affichage message d'erreur
                    await DisplayAlert("Génération du QR Code", messageErreur, "OK");
                    qrCodeView.Drawable = null;
                    qrCodeView.Invalidate();
                    return;
                }

            }

            catch (Exception ex) {
                // Affichage message d'erreur
                await DisplayAlert("Exception", "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}", "OK");
            }
        }

        /// <summary>On enregistre l'image dans un fichier</summary>
        /// <param name="width">largeur de l'image</param>
        /// <param name="height">Hauter de l'image</param>
        /// <returns>Le nom du fichier</returns>
        private async Task<string> SauvegarderQrCodeAsync(int width, int height, bool bTelecharge)
        {

            try
            { 
                // Récupérer le drawable dans une variable
                var ddrawable = qrCodeView.Drawable;
                string filePath;
                if (bTelecharge) filePath = Path.Combine(FileSystem.AppDataDirectory, "QRCode" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg");
                else filePath = Path.Combine(FileSystem.CacheDirectory, "QRCode" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg");

                string sRetour=await clsGraphicsQRCode.SaveDrawingToJpg(filePath, saveSKBitmap);
                if (sRetour != "")
                {
                    // Affichage message d'erreur
                    await DisplayAlert("Partage du QR Code", sRetour, "OK");
                    return "";
                }
                else
                {
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                // Affichage message d'erreur
                await DisplayAlert("Exception", "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}", "OK");
                return "";
            }

        }

        /// <summary>Partage du QRCode après l'avoir sauvegardé dans un fichier</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnShareQrCodeClicked(object sender, EventArgs e)
        {
            try

            {
                // On vérifie que le QR Code a été généré
                if (qrCodeView.Drawable != null)
                {
                    // On sauvegarde le QR Code dans un fichier
                    var path = await SauvegarderQrCodeAsync((int)qrCodeView.WidthRequest, (int)qrCodeView.HeightRequest, false);
                    // Si pas d'erreur lors de la génération du fichier, on le partage
                    if (path != "") { 
                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Partager le QR Code",
                            File = new ShareFile(path)
                        });
                    }
                }
                //  Si pas de QR Code généré, on affiche un message
                else await DisplayAlert("Partage d'un QR Code", "Veuillez générer le QR Code", "OK");
            }

            catch (Exception ex)
            {
                // Affichage message d'erreur
                await DisplayAlert("Exception", "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}", "OK");
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
                             "• Incorporer une image au centre en cliquant sur le QR Code (optionnel).\n" +
                             "• Partager.";
            string bouton = "OK";

            await DisplayAlert(titre, message, bouton);
        }



    }

}
