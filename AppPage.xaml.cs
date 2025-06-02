using SkiaSharp;
using static QRDessFree.CLSGenereQRCode;
using static QRDessFree.clsGraphicsQRCode;

namespace QRDessFree
{
    public partial class AppPage : ContentPage
    {
        /// <summary>Sauvegarde du iDrawable du graphicsView du QrCode</summary>
        private IDrawable qrDrawable;
        private int pourcentCorrection;
        private int tailleBordure = 1;
        private int nbBitCorrection = 0; // Nombre de bits de correction du QR Code
        private SKBitmap saveSKBitmap; // Sauvegarde de l'image détourée pour la réutiliser lors du partage

        public AppPage()
        {
            InitializeComponent();

        }
        /// <summary>Incorporation au QRCODE d'une image choisie par l'utilisateur</summary>
        /// <returns>O si aucune image choisie, 1 sinon</returns>
        private async Task<int> IncorporeImage()
        {
            SKBitmap withBorder;
            // L'utilisateur choisit une image
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choisissez une image pour incorporer au centre du QRCode",
                FileTypes = FilePickerFileType.Images
            });

            // S'il n'a pas choisi d'image, on ne fait rien
            if (result == null) return 0;

            // On sauve le nom du fichier pour pouvoir le réouvrir ultérieurement lors du partage
            string filenameImage = result.FullPath; //"C:/Temp/YamahaVide.png";

            // Etape 0 on lit l'image et on recherche si elle contient des pixels 
            using var input = LoadBitmap(filenameImage);
            bool isPixelTransparent = RecherchePixelTransparent(input);

            // On détermine la taille de la bordure de détourage à partir de la surface du QRCode et de celle de l'image lue
            double surface = qrCodeView.Width * qrCodeView.Height * (double)pourcentCorrection / 200.0; // On conserve la moitié des bits de correction
            int tailleBordDetourage =(int)( 2.0 * Math.Sqrt(((double)input.Width * (double)input.Height) / surface));
            if (tailleBordDetourage < 2) tailleBordDetourage = 2;

            // Etape 1 : On conserve à l'image juste un cadre minimum de pixels blancs/transparents
            var withCadre = AddTransparentBorder(input, isPixelTransparent, tailleBordDetourage);

            // Etape 2 : on réduit l'image à sa taille cible pour améliorer la performance du détourage 
            SKBitmap imageReduite;
            if (withCadre.Width * withCadre.Height > 100000) imageReduite = ReduitImage(withCadre, qrCodeView.Width, qrCodeView.Height, 100000);
            else imageReduite = withCadre;

            using (imageReduite)
            {
                SKBitmap transparent;

                // Étape 3 : si aucun pixel de transparence, suppression du fond blanc
                if (isPixelTransparent == false) transparent = RemoveWhiteBorder(imageReduite);
                else transparent = imageReduite;

                // Suite du traitement de l'image
                using (transparent)
                {
                    // On recalcule la taille de la bordure sur l'image réduite
                    tailleBordDetourage = (int)(2.0 * Math.Sqrt(((double)imageReduite.Width * (double)imageReduite.Height) / surface));
                    if (tailleBordDetourage < 2) tailleBordDetourage = 2;

                    // Étape 4 : on ajoute une bordure blanche autour de l'image 
                    withBorder = AddWhiteOutline(transparent, tailleBordDetourage);

                    // Etape 5 : on enregistre le résultat dans le cache
                    string cachePath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
                    saveSKBitmap = withBorder;

                }
            }

            // On affecte le résultat fusionné à la vue graphique
            qrDrawable = new ImageDrawable(ConvertSKBitmapToMauiImage(withBorder), pourcentCorrection, ModulesQRCode, tailleBordure);
            qrCodeView.Drawable = qrDrawable;
            qrCodeView.Invalidate();

            return 1;
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
                int retour = await IncorporeImage();
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

                //Reset l'image à incorporer
                saveSKBitmap = null;
                pourcentCorrection = 0;

                // Récupérer la taille de bordure
                int imagesize = 0;

                // Générer le QR Code et l'afficher
                qrDrawable = clsGraphicsQRCode.GenereImageQRCode(texte, correctionLevel, tailleBordure, ref imagesize, ref pourcentCorrection);
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

            catch (Exception ex) {
                // Affichage message d'erreur
                await DisplayAlert("Exception", "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}", "OK");
            }
        }

        /// <summary>On enregistre l'image dans un fichier</summary>
        /// <param name="drawable">objet drawable contenant l'image</param>
        /// <param name="width">largeur de l'image</param>
        /// <param name="height">Hauter de l'image</param>
        /// <returns>Le nom du fichier</returns>
        private async Task<string> SauvegarderQrCodeAsync(int width, int height)
        {
            // Récupérer le drawable dans une variable
            var ddrawable = qrCodeView.Drawable;
            string filePath = Path.Combine(FileSystem.CacheDirectory, "QRCode"+ DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".jpg");

            clsGraphicsQRCode.SaveDrawingToJpg(filePath, (int) qrCodeView.Width, (int)qrCodeView.Height, tailleBordure,pourcentCorrection, saveSKBitmap);

            return filePath;
        }

        /// <summary>Partage du QRCode après l'avoir sauvegardé dans un fichier</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnShareQrCodeClicked(object sender, EventArgs e)
        {
            try

            {
                if (qrCodeView.Drawable != null)
                {
                    var path = await SauvegarderQrCodeAsync((int)qrCodeView.WidthRequest, (int)qrCodeView.HeightRequest);
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Partager le QR Code",
                        File = new ShareFile(path)
                    });
                }
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
