using SkiaSharp;
using static QRDessFree.clsGenereQRCode;
using static QRDessFree.clsGraphicsQRCode;
#if WINDOWS
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.Maui.Controls;
#endif

namespace QRDessFree
{
    public partial class AppPage : ContentPage
    {
        /// <summary>Sauvegarde du iDrawable du graphicsView du QrCode</summary>
        private IDrawable qrDrawable;
        private float pourcentCorrection;
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

            // Etape 0 on lit l'image et on recherche si elle contient des pixels 
            using var input = LoadBitmap(result.FullPath);
            bool isPixelTransparent = RecherchePixelTransparent(input);

            // Pas du QRCode 
            int tailleModuleAvecBord = (clsGenereQRCode.ModulesQRCode.GetLength(0) + 2 * clsGenereQRCode.tailleBordure);

            // On calcule la surface de l'image cible pour qu'elle s'adapte à la taille du QR Code de partage
            int tailleImageQRCode = tailleModuleAvecBord * tailleGraphiqueModuleQrCode();
            int surfaceImagecible = (int)(tailleImageQRCode * tailleImageQRCode * (pourcentCorrection / 200.0));

            // On calcule le coefficient de division de l'image 
            double divImage = Math.Sqrt(1.0 * surfaceImagecible / (input.Width * input.Height));

            // On détermine la taille de la bordure de détourage à partir de la surface du QRCode et de celle de l'image lue
            //int tailleBordDetourage =(int)( 2.0 * Math.Sqrt(((double)input.Width * (double)input.Height) / surface));
            int tailleBordDetourage;
            if (divImage < 1) tailleBordDetourage = (int)Math.Round(2.0 / divImage);
            else              tailleBordDetourage = 2;
            if (tailleBordDetourage < 2) tailleBordDetourage = 2;

            // Etape 1 : On conserve à l'image juste un cadre minimum de pixels blancs/transparents
            SKBitmap imageReduite;
            SKBitmap withCadre = AddTransparentBorder(input, isPixelTransparent, tailleBordDetourage);

            // Etape 2 : on réduit l'image pour améliorer la performance du détourage à la taille cible pour le QR Code de partage (cela permet d'augmenter la qualité de l'image en ne la déformant pas)
            // On recalcule le coefficient de division de l'image 
            divImage = Math.Sqrt(1.0 * surfaceImagecible / (withCadre.Width * withCadre.Height));
            imageReduite = ReduitImage(withCadre, qrCodeView.Width, qrCodeView.Height, divImage);

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
                    double surface = qrCodeView.Width * qrCodeView.Height * (double)pourcentCorrection / 200.0; 
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
        /// <param name="width">largeur de l'image</param>
        /// <param name="height">Hauter de l'image</param>
        /// <returns>Le nom du fichier</returns>
        private async Task<string> SauvegarderQrCodeAsync(int width, int height, bool bTelecharge)
        {
            // Récupérer le drawable dans une variable
            var ddrawable = qrCodeView.Drawable;
            string filePath;
            if (bTelecharge) filePath = Path.Combine(FileSystem.AppDataDirectory, "QRCode" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg");
            else filePath = Path.Combine(FileSystem.CacheDirectory, "QRCode" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".jpg");

            clsGraphicsQRCode.SaveDrawingToJpg(filePath, tailleBordure, saveSKBitmap);

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
                    var path = await SauvegarderQrCodeAsync((int)qrCodeView.WidthRequest, (int)qrCodeView.HeightRequest, false);
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Partager le QR Code",
                        File = new ShareFile(path)
                    });
                }
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


#if WINDOWS


public static class FilePickerWorkaround
{


    public static async Task<string?> PickImageFileAsync(Microsoft.Maui.Controls.Window mauiWindow)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var platformView = mauiWindow.Handler?.PlatformView;
        if (platformView is not Microsoft.UI.Xaml.Window nativeWindow)
            throw new InvalidOperationException("Fenêtre native invalide.");

        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Handle de fenêtre nul.");

        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }



}
#endif

    }

}
