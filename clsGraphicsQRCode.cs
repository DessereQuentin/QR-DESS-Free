using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;

namespace QRDessFree
{
    /// <summary>Fonctions pour la mise en forme graphique du QRCode</summary>
    public static partial class clsGraphicsQRCode
    {
        /// <summary>taux de division de l'image à insérer dans un QRCode par rapport au pourcentageCorrection</summary>
        /// <remarks>Permet de garder 60% des modules de correction</remarks>
        public static double tauxDivisionImage = 250.0;

        /// <summary>Sauvegarde de l'image détourée pour la réutiliser lors du partage</summary>
        public static SKBitmap saveSKBitmap=null;

        /// <summary>Message d'erreur à remonter au thread principal</summary>
        public static string messageErreur = "";

        /// <summary>Détourage de l'image remplace les pixels proches du blanc ou transparent par des pixels transparents Jusqu'à rencontrer des pixels ne répondant pas à cette condition</summary>
        /// <param name="inputBitmap">Image à traitée</param>
        /// <remarks>On inclut les pixels transparent dans l'algorithme même s'il n'y a pas besoin de les remplacer pour assurer la continuité du détourage</remarks>
        /// <returns>Renvoie l'image transformée</returns>
        public static SKBitmap RemoveWhiteBorder(SKBitmap inputBitmap)
        {
            int width = inputBitmap.Width;
            int height = inputBitmap.Height;

            // Création de l'image cible
            var output = new SKBitmap(width, height, true);

            // Copie des pixels
            inputBitmap.CopyTo(output);

            bool[,] visited = new bool[width, height];
            Queue<(int x, int y)> queue = new();

            // Condition de détourage
            bool IsWhite(SKColor color) => color.Red >= 250 && color.Green >= 250 && color.Blue >= 250;

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height || visited[x, y]) return;

                SKColor color = output.GetPixel(x, y);
                if (IsWhite(color))
                {
                    queue.Enqueue((x, y));
                    visited[x, y] = true;
                }
            }

            // Enqueue les bords
            for (int x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (int y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            // BFS flood fill
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                var color = output.GetPixel(x, y);
                output.SetPixel(x, y, new SKColor(color.Red, color.Green, color.Blue, 0)); // transparent

                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            return output;
        }

        /// <summary>Recherche si l'image contient des pixels de transparencecomplète </summary>
        /// <param name="input">Image à explorer</param>
        /// <returns>true si l'image contient des pixels transparents, false sinon</returns>
        public static bool RecherchePixelTransparent(SKBitmap input)
        {

            int width = input.Width;
            int height = input.Height;


            // On boucle sur tous les pixels de l'image
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // On vérifie si le pixel est transparent et si oui on retourne true
                    if (input.GetPixel(x, y).Alpha == 0) return true;
                }
            }

            return false; // Si on n'a pas trouvé de pixel alpha, on retourne false
        }


        /// <summary>Entoure le contour de l'image réélle (hors pixel de transparence) d'une bordure blanche pour qu'elle apparaisse bien sur le QR Code</summary>
        /// <param name="input">Image à traiter</param>
        /// <param name="outlineThickness">Taille de la bordure</param>
        /// <returns>Renvoie l'image traitée</returns>
        public static SKBitmap AddWhiteOutline(SKBitmap input, int outlineThickness)
        {
            int width = input.Width;
            int height = input.Height;

            var mask = new SKBitmap(width, height, SKColorType.Alpha8, SKAlphaType.Premul);

            // Étape 1 : créer un masque alpha (1 = visage, 0 = fond)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte alpha = input.GetPixel(x, y).Alpha;
                    mask.SetPixel(x, y, new SKColor(0, 0, 0, alpha > 0 ? (byte)255 : (byte)0));
                }
            }

            // Étape 2 : dilater le masque (agrandir le contour)
            var outline = new SKBitmap(width, height, SKColorType.Alpha8, SKAlphaType.Premul);

            using (var canvas = new SKCanvas(outline))
            {
                var paint = new SKPaint
                {
                    ImageFilter = SKImageFilter.CreateDilate(outlineThickness, outlineThickness),
                    BlendMode = SKBlendMode.Src
                };
                canvas.DrawBitmap(mask, 0, 0, paint);
            }

            // Étape 3 : enlever l'intérieur du masque (on ne veut que le contour)
            var outlineOnly = new SKBitmap(width, height, SKColorType.Alpha8, SKAlphaType.Premul);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte dilated = outline.GetPixel(x, y).Alpha;
                    byte original = mask.GetPixel(x, y).Alpha;
                    byte bresult = (byte)(dilated > 0 && original == 0 ? 255 : 0);
                    outlineOnly.SetPixel(x, y, new SKColor(0, 0, 0, bresult));
                }
            }

            // Étape 4 : créer un nouveau bitmap avec le fond transparent + contour blanc + image
            var result = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);

                // Dessine le contour blanc
                using var outlinePaint = new SKPaint
                {
                    Color = SKColors.White,
                    BlendMode = SKBlendMode.SrcOver
                };
                canvas.DrawBitmap(outlineOnly, 0, 0, outlinePaint);

                // Dessine l’image d’origine
                canvas.DrawBitmap(input, 0, 0);
            }

            return result;
        }

        /// <summary>Réduit la taille de l'image à sa taille cible pour le partage, basée sur une taille de QRCode proche de 1000x1000</summary>
        /// <param name="input">Image à redimensionner</param>
        /// <param name="width">Largeur de l'image</param>
        /// <param name="height">hauteur de l'image</param>
        /// <param name="divImage">Facteur de redimensionnement de l'image</param>
        /// <returns>Renvoie l'image réduite</returns>
        public static SKBitmap ReduitImage(SKBitmap input, double width, double height, double divImage)
        {
            // On réduit la taille de l'image cible en fonction du pourcentage des bits de correction du QRCode
            //double divImage = Math.Sqrt(1.0 * surface / (input.Width * input.Height
            int scaledWidth, scaledHeight;
            if (divImage < 1)
            {
                scaledWidth = (int)(input.Width * divImage);
                scaledHeight = (int)(input.Height * divImage);
            }
            else return input; // pas de réduction si l'image source est plus petite que le calcul de l'image cible

            // On teste la validité de l'image cible
            if (scaledWidth < 1 || scaledHeight < 1)
                throw new ArgumentException("Le bitmap d'origine est invalide ou trop petit.");

            // Nouvelle taille avec même format que l'original (y compris alpha)
            var resizedInfo = new SKImageInfo(scaledWidth, scaledHeight, input.ColorType, input.AlphaType);

            // Resize avec haute qualité
            SKBitmap resizedBitmap = input.Resize(resizedInfo, SKSamplingOptions.Default);

            if (resizedBitmap == null)
                throw new Exception("Échec du redimensionnement de l'image.");

            return resizedBitmap;

        }

        /// <summary>Ajout si nécessaire un bord transparent autour de l'image</summary>
        /// <param name="input">Image à traiter</param>
        /// <param name="borderSize">Taille en pixels du bord à ajouter</param>
        /// <param name="isPixelTransparent">Indique si l'image contient des pixels transparent</param>
        /// <returns>Renvoie l'image avec la bordure ajoutée</returns>
        public static SKBitmap AddTransparentBorder(SKBitmap input, bool isPixelTransparent, int borderSize)
        {
            int width = input.Width;
            int height = input.Height;
            bool bDetoure;

            // Déterminer les limites du contenu visible (non transparent/blanc)
            int minX = width, minY = height, maxX = 0, maxY = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = input.GetPixel(x, y);
                    if (isPixelTransparent) {bDetoure = (pixel.Alpha > 0);}
                    else                    {bDetoure = !(pixel.Red >= 250 && pixel.Green >= 250 && pixel.Blue >= 250);}

                    //if (pixel.Alpha > 0)
                    if (bDetoure) 
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Rien de visible, on retourne l’image telle quelle
            if (minX >= maxX || minY >= maxY)
            return input; 

            int croppedWidth = maxX - minX + 1 + 2 * borderSize;
            int croppedHeight = maxY - minY + 1 + 2 * borderSize;

            var cropped = new SKBitmap(croppedWidth, croppedHeight);
            using (var canvas = new SKCanvas(cropped))
            {
                // fond blanc transparent
                if (isPixelTransparent) canvas.Clear(new SKColor(255, 255, 255, 0));
                else canvas.Clear(new SKColor(255, 255, 255, 255));

                    // On dessine l'image à l'emplacement permettant d'avoir la bordure et sans déformation
                var srcRect = new SKRectI(minX, minY, maxX + 1, maxY + 1);
                var destRect = new SKRectI(borderSize, borderSize, borderSize + srcRect.Width, borderSize + srcRect.Height);
                canvas.DrawBitmap(input, srcRect, destRect);
            }


            return cropped;
        }

        /// <summary>Ecriture d'un SKBitmap dans un .png</summary>
        /// <param name="bitmap">SKBitmap à écrire</param>
        /// <param name="path">Nom complet du fichier</param>
        public static void SaveBitmap(SKBitmap bitmap, string path)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }

        /// <summary>Lecture d'un fichier image dans un SKBitmap</summary>
        /// <param name="path">Nom complet du fichier</param>
        /// <returns>Renvoie le SKBitmap</returns>
        public static SKBitmap LoadBitmap(string path)
        {
            using var stream = File.OpenRead(path);
            return SKBitmap.Decode(stream);
        }
        /// <summary>Calcule d'un module de QRCode en fonction de la taille écran </summary>
        public static void tailleEcranModuleQRCode()
        {
            var largeurDip = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            int TailleQrCode = (clsGenereQRCode.ModulesQRCode.GetLength(0) + 2 * clsGenereQRCode.tailleBordure);
            clsGenereQRCode.iPas = (int)largeurDip / TailleQrCode;

            // la résolution maximum est 10
            if (clsGenereQRCode.iPas > 7) clsGenereQRCode.iPas = 7;

            // La résolution minimum est 2
            if (clsGenereQRCode.iPas < 2) clsGenereQRCode.iPas = 2; 
        }

        /// <summary>Calcule de la taille graphique d'un module de QRCode</summary>
        /// <returns>Taille du module</returns>
        public static int tailleGraphiqueModuleQrCode()
        {
            return (int)Math.Round(1000.0 / (clsGenereQRCode.ModulesQRCode.GetLength(0) + 2 * clsGenereQRCode.tailleBordure));
        }

        /// <summary>Ouvre une image choisit par l'utilisateur et la détoure pour l'afficher au centre du QR Code</summary>
        /// <param name="filePath">Fichier à ouvrir</param>
        /// <param name="qrCodeView">Graphics view dans lequel afficher le QR Code avec l'image incoporée</param>
        /// <param name="pourcentCorrection">pourcentage de modules de correction du QR Code </param>
        /// <returns></returns>
        public static async Task<string> TraiteImage(string filePath, Microsoft.Maui.Controls.GraphicsView qrCodeView, float pourcentCorrection)
        {
            try
            {

                SKBitmap withBorder = null;

                // Etape 0 on lit l'image et on recherche si elle contient des pixels 
                using var input = LoadBitmap(filePath);
                bool isPixelTransparent = RecherchePixelTransparent(input);

                // Pas du QRCode 
                int tailleModuleAvecBord = (clsGenereQRCode.ModulesQRCode.GetLength(0) + 2 * clsGenereQRCode.tailleBordure);

                // On calcule la surface de l'image cible pour qu'elle s'adapte à la taille du QR Code de partage
                int tailleImageQRCode = tailleModuleAvecBord * tailleGraphiqueModuleQrCode();
                int surfaceImagecible = (int)(tailleImageQRCode * tailleImageQRCode * (pourcentCorrection / tauxDivisionImage));

                // On calcule le coefficient de division de l'image 
                double divImage = Math.Sqrt(1.0 * surfaceImagecible / (input.Width * input.Height));

                // On détermine la taille de la bordure de détourage à partir de la surface du QRCode et de celle de l'image lue
                //int tailleBordDetourage =(int)( 2.0 * Math.Sqrt(((double)input.Width * (double)input.Height) / surface));
                int tailleBordDetourage;
                if (divImage < 1) tailleBordDetourage = (int)Math.Round(2.0 / divImage);
                else tailleBordDetourage = 2;
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
                        double surface = qrCodeView.Width * qrCodeView.Height * (double)pourcentCorrection / tauxDivisionImage;
                        tailleBordDetourage = (int)(2.0 * Math.Sqrt(((double)imageReduite.Width * (double)imageReduite.Height) / surface));
                        if (tailleBordDetourage < 2) tailleBordDetourage = 2;

                        // Étape 4 : on ajoute une bordure blanche autour de l'image 
                        withBorder = AddWhiteOutline(transparent, tailleBordDetourage);

                        // Etape 5 : on enregistre le résultat dans le cache
                        string cachePath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
                        saveSKBitmap = withBorder;

                        // On calcule la taille d'un module à l'écran
                        tailleEcranModuleQRCode();

                        // On affecte le résultat fusionné à la vue graphique
                        qrCodeView.Drawable = new ImageDrawable(ConvertSKBitmapToMauiImage(saveSKBitmap), pourcentCorrection, clsGenereQRCode.iPas);
                        qrCodeView.Invalidate();

                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                return "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}";
            }

        }

        /// <summary>Regénère le QRCode dans un canvas en fusionnant l'image le cas échéant et l'enregistre dans un fichier jpeg pour le partager</summary>
        /// <param name="filePath">Fichier dans lequel enregistrer le QRCode</param>
        /// <param name="saveSKBitmap">Image à incorporer dans le QRCode</param>
        public static async Task<string> SaveDrawingToJpg(string filePath, SKBitmap saveSKBitmap)
        {
            try
            {

                // On supprime le fichier temporaire s'il existe déjà
                SupprimerAnciensFichiersQRCode("QRCode*.jpg");

                // on crée un canvas pour le tracé
                int size = tailleGraphiqueModuleQrCode() * (2 * clsGenereQRCode.tailleBordure + clsGenereQRCode.ModulesQRCode.GetLength(0));
                using var bitmap = new SKBitmap(size, size);
                using var canvas = new SKCanvas(bitmap);

                // On trace le QRCode en le fusionnant avec l'image
                drawQRCode(canvas, size, saveSKBitmap, tailleGraphiqueModuleQrCode(), 1.0);

                // On enregistre le QRCode dans un fichier jpeg
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                return "";
            }
            catch (Exception ex)
            {
                return "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}";
            }
        }

        /// <summary>Dessine le QRCode et le cas échéant l'image à incorporer pour le partager ou l'afficher à l'écran</summary>
        /// <param name="canvas">Canvas conteneur du QRCode</param>
        /// <param name="sizeQRCode">Taille du QR Code (nombre de pixels du côté du carré)</param>
        /// <param name="saveSKBitmap">Image à incorporer dans le QRCode</param>
        /// <param name="tailleModule">Taille d'un module du QR Code (en pixels)</param>
        /// <param name="divImage">Facteur de réduction de l'image pour affichage écran (=1 pour le partage)</param>
        public static void drawQRCode(SKCanvas canvas, int sizeQRCode,  SKBitmap saveSKBitmap, int tailleModule, double divImage)
        {

            int size = clsGenereQRCode.ModulesQRCode.GetLength(0);
            int iEntourage = clsGenereQRCode.tailleBordure * tailleModule;

            // Crée un SKPaint blanc pour le fond
            var paintFond = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };

            // Remplir le fond blanc
            canvas.DrawRect(0, 0, sizeQRCode, sizeQRCode, paintFond);

            // Préparer le pinceau noir pour les modules
            var paintNoir = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };

            // Dessiner chaque module du QR Code
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (clsGenereQRCode.ModulesQRCode[i, j] == 1)
                    {
                        float x1 = iEntourage + i * tailleModule;
                        float y1 = iEntourage + j * tailleModule;
                        canvas.DrawRect(x1, y1, tailleModule, tailleModule, paintNoir);
                    }
                }
            }

            // Création et renvoi du rectangle de destination pour l'image
            if (saveSKBitmap != null)
            {

                Double scaledWidth = saveSKBitmap.Width * divImage;
                Double scaledHeight = saveSKBitmap.Height * divImage;
                Double x = (sizeQRCode - scaledWidth) / 2.0;
                Double y = (sizeQRCode - scaledHeight) / 2.0;

                var Rect = new SKRect((float)x, (float)y, (float)(x + scaledWidth), (float)(y + scaledHeight));
                canvas.DrawBitmap(saveSKBitmap, Rect);
            }

        }

        /// <summary>Calcul de la position et de la taille de l'image cible pour l'incorporer dans le QR Code</summary>
        /// <param name="width">Largeur du QR Code</param>
        /// <param name="height">Hauteur du QR Code</param>
        /// <param name="sourceImage">Image à incorporer dans le QR Code</param>
        /// <param name="pourcentCorrection">Pourcentage de bits de correction du QRCode</param>
        /// <returns></returns>
        public static SKRect destRect(int width, int height, SKBitmap sourceImage, float pourcentCorrection)
        {
            // On calcule le facteur de division de l'image permettant de couvrir la moitié de la surface des bits de correction (on ne tient pas compte des bits de mise en forme)
            double surface = (double)width * height * (double)pourcentCorrection / tauxDivisionImage;
            double divImage = Math.Sqrt(surface / (sourceImage.Width * sourceImage.Height));

            // Calcul de la position et de la taille de l'image cible 
            double scaledWidth = sourceImage.Width * divImage;
            double scaledHeight = sourceImage.Height * divImage;
            double x = width / 2.0 - scaledWidth / 2.0;
            double y = height / 2.0 - scaledHeight / 2.0;

            // Création et renvoi du rectangle de destination pour l'image
            var Rect = new SKRect((float)x, (float)y, (float)(x + scaledWidth), (float)(y + scaledHeight));
            return Rect;

        }
        
        /// <summary>Interface de tracer du QRCode</summary>
        public class ImageDrawable : IDrawable
        {
            private Microsoft.Maui.Graphics.IImage _cibleImage;

            /// <summary>Sauvegardedans la classe des paramètres du tracé</summary>
            /// <param name="sourceImage">Image à tracer</param>
            /// <param name="pourcentCorrection">Pourcentage de correction du QRCode</param>
            public ImageDrawable(Microsoft.Maui.Graphics.IImage sourceImage, float pourcentCorrection, int iPas)
            {

                // Allocation de l'image
                int size = iPas * (2 * clsGenereQRCode.tailleBordure + clsGenereQRCode.ModulesQRCode.GetLength(0));
                using var bitmap = new SKBitmap(size, size);
                using var canvas = new SKCanvas(bitmap);

                // Coefficient de division de l'image pour l'adapter à la taille du QR Code
                double surface = size * size * (Double)pourcentCorrection / tauxDivisionImage;
                double divImage = 0 ;
                if (saveSKBitmap != null) divImage = Math.Sqrt(surface / (saveSKBitmap.Width * saveSKBitmap.Height));

                // On trace le QRCode en le fusionnant avec l'image
                drawQRCode(canvas, size, saveSKBitmap, iPas, divImage);

                _cibleImage = ConvertSKBitmapToMauiImage(bitmap);

            }

            /// <summary>Appel de la fonction de dessin du QRCode</summary>
            /// <param name="canvas">ICanvas du tracé</param>
            /// <param name="dirtyRect">Rectangle de tracé</param>
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
            try {

                    canvas.DrawImage(_cibleImage, 0, 0, dirtyRect.Width, dirtyRect.Height);

            }
            catch (Exception ex)
            {
                    messageErreur = "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n";

            }

    }
}


        /// <summary>Nettoie les fichiers dans le cache avant de créer le fichier temporaire pour le partage</summary>
        /// <param name="protofilename">Prototype des fichiers à supprimer</param>
        public static void SupprimerAnciensFichiersQRCode(string protofilename)
        {
            // Chemin du cache de l'application
            string cachePath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;

            // Récupère tous les fichiers du cache dont le nom commence par "qr" et finit par ".png"
            var fichiersQRCode = Directory.GetFiles(cachePath, protofilename);

            // Supprime chaque fichier trouvé
            foreach (var fichier in fichiersQRCode)
            {
                try { File.Delete(fichier); }
                catch { }
            }
        }

        /// <summary>Convertit un SKBitmap en IImage</summary>
        /// <param name="bitmap">Bitmap à convertir</param>
        /// <returns>Renvoie le bitmap sous forme de IImage</returns>
        public static Microsoft.Maui.Graphics.IImage ConvertSKBitmapToMauiImage(SKBitmap bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            return PlatformImage.FromStream(stream);
        }

        /// <summary>Génération de l'image du QRCode</summary>
        /// <param name="Texte">Texte à intégrer dans le QRCode</param>
        /// <param name="Correction">Type de correction à appliquer</param>
        /// <param name="TailleBordure">Taille du bord blanc du QRCode en multiple de la taille d'un module</param>
        /// <param name="ImageSize">Taille du QR Code</param>
        /// <param name="pourcentCorrection">Proportion des bits de correction</param>
        /// <returns>Renvoie l'image du QRCode sous forme de BitMap</returns>
        public static ImageDrawable GenereImageQRCode(string Texte, string Correction, int TailleBordure, ref int ImageSize, ref float pourcentCorrection)
        {
            try
            {

                // Initialisation des tables de travail
                clsGenereQRCode.InitTables();
                // Génération du tableau de bytes du QRCode
                bool bOK = true;
                int nbBitsCorrection = 0;   
                clsGenereQRCode.ModulesQRCode = clsGenereQRCode.GenereQRCode(Texte, Correction, ref bOK, ref nbBitsCorrection);
                if (bOK != true) return null;

                // Récupération de la largeur de l'écran pour adapter la résolution du QR Code
                tailleEcranModuleQRCode();
                int TailleQrCode = (clsGenereQRCode.ModulesQRCode.GetLength(0) + 2 * TailleBordure);
                ImageSize = TailleQrCode * clsGenereQRCode.iPas;
                pourcentCorrection = (int)(100.0 * nbBitsCorrection / ((float)TailleQrCode * (float)TailleQrCode));
                return new ImageDrawable(null, pourcentCorrection, clsGenereQRCode.iPas);

            }
            catch (Exception ex)
            {
                messageErreur = "Votre application QR Dess Free a rencontré un problème, veuillez contacter le développeur en lui communiquant le message ci-dessous :\n\n"
                   + $"{ex}";
                return null;
            }

        }


    }
}
