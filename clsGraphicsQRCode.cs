using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;
using System.IO;

namespace QRDessFree
{
    /// <summary>Fonctions pour la mise en forme graphique du QRCode</summary>
    public static partial class clsGraphicsQRCode
    {
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

        /// <summary>Réduit la taille de l'image à une taille permettant de faire les détourages sans temps d'attente pour l'utilisateur</summary>
        /// <param name="input">Image à redimensionner</param>
        /// <param name="width">Largeur de l'image</param>
        /// <param name="height">hauteur de l'image</param>
        /// <param name="surface">Surface de l'image en pixels</param>
        /// <returns>Renvoie l'image réduite</returns>
        public static SKBitmap ReduitImage(SKBitmap input, double width, double height, int surface)
        {

            // On calcule la taille de l'image cible en fonction du pourcentage des bits de correction du QRCode
            double divImage = Math.Sqrt(1.0 * surface / (input.Width * input.Height));
            int scaledWidth = (int)(input.Width * divImage);
            int scaledHeight = (int)(input.Height * divImage);

            // On teste la validité de l'image cible
            if (input == null || scaledWidth < 2 || scaledHeight < 2)
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
            //int croppedWidth = maxX - minX + 1;
            //int croppedHeight = maxY - minY + 1;

            var cropped = new SKBitmap(croppedWidth, croppedHeight);
            using (var canvas = new SKCanvas(cropped))
            {
                // fond blanc transparent
                if (isPixelTransparent) canvas.Clear(new SKColor(255, 255, 255, 0));
                else canvas.Clear(new SKColor(255, 255, 255, 255));

                    // On dessine l'image avec la bordure
                var srcRect = new SKRectI(minX, minY, maxX + 1, maxY + 1);
                var destRect = new SKRectI(borderSize, borderSize,  croppedWidth-borderSize, croppedHeight-borderSize);
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

        /// <summary>Regénère le QRCode dans un canvas en fusionnant l'image le cas échéant et l'enregistre dans un fichier jpeg pour le partager</summary>
        /// <param name="filePath">Fichier dans lequel enregistrer le QRCode</param>
        /// <param name="width">Largeur du QRCode</param>
        /// <param name="height">Hauteur du QRCode</param>
        /// <param name="tailleBordure">Taille de la bordure à tracer (en multiple de la taille d'un pixel du QRCode)</param>
        /// <param name="pourcentCorrection">Pourcentage de correction fonction du niveau de correction (permet de savoir quelle surface accorder à l'image à incoprorer)</param>
        /// <param name="saveSKBitmap">Image à incorporer dans le QRCode</param>
        public static async void SaveDrawingToJpg(string filePath, int width, int height, int tailleBordure, int pourcentCorrection, SKBitmap saveSKBitmap)
        {

            // On supprime le fichier temporaire s'il existe déjà
            SupprimerAnciensFichiersQRCode("QRCode*.jpg");

            // on crée un canvas pour le tracé
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            // On trace le QRCode en le fusionnant avec l'image
            drawQRCode(canvas, width, height,  tailleBordure, pourcentCorrection, saveSKBitmap);

            // On enregistre le QRCode dans un fichier jpeg
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }

        /// <summary>Dessine le QRCode et le cas échéant l'image à incoprorer</summary>
        /// <param name="canvas">ICanvas conteneur du QRCode</param>
        /// <param name="width">Largeur du QRCode</param>
        /// <param name="height">Hauteur du QRCode</param>
        /// <param name="tailleBordure">Taille de la bordure à tracer (en multiple de la taille d'un pixel du QRCode)</param>
        /// <param name="pourcentCorrection">Pourcentage de correction fonction du niveau de correction (permet de savoir quelle surface accorder à l'image à incoprorer)</param>
        public static void drawQRCode(ICanvas canvas, float width, float height, Microsoft.Maui.Graphics.IImage sourceImage, int tailleBordure, int pourcentCorrection)
        {
            // Dessiner l'image de fond en utilisant la même méthode que pour l'image de qrCodeView
            int size = CLSGenereQRCode.ModulesQRCode.GetLength(0);
            int iEntourage = tailleBordure * CLSGenereQRCode.iPas;

            // Fond blanc
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(0, 0, width, height);

            // Dessiner chaque module du QR Code
            canvas.FillColor = Colors.Black;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (CLSGenereQRCode.ModulesQRCode[i, j] == 1)
                    {
                        float x1 = iEntourage + i * CLSGenereQRCode.iPas;
                        float y1 = iEntourage + j * CLSGenereQRCode.iPas;
                        canvas.FillRectangle(x1, y1, CLSGenereQRCode.iPas, CLSGenereQRCode.iPas);
                    }
                }
            }

            // Si une image est fournie, on l'incorpore dans le QR Code
            if (sourceImage != null)
            {
                // Coefficient de division de l'image pour l'adapter à la taille du QR Code
                double surface = width * height * (Double)pourcentCorrection / 200.0;
                double divImage = Math.Sqrt(surface / (sourceImage.Width * sourceImage.Height));

                // Taille réduite de l'image
                double scaledWidth = (double)sourceImage.Width * divImage;
                double scaledHeight = (double)sourceImage.Height * divImage;

                //Position de l'image
                double x = (double)(width / 2 - scaledWidth / 2);
                double y = (double)(height / 2 - scaledHeight / 2);

                canvas.DrawImage(sourceImage, (int)x, (int)y, (float)scaledWidth, (float)scaledHeight);
            }

        }

        /// <summary>Dessine le QRCode et le cas échéant l'image à incoprorer</summary>
        /// <param name="canvas">Canvas conteneur du QRCode</param>
        /// <param name="width">Largeur du QRCode</param>
        /// <param name="height">Hauteur du QRCode</param>
        /// <param name="tailleBordure">Taille de la bordure à tracer (en multiple de la taille d'un pixel du QRCode)</param>
        /// <param name="pourcentCorrection">Pourcentage de correction fonction du niveau de correction (permet de savoir quelle surface accorder à l'image à incoprorer)</param>
        /// <param name="saveSKBitmap">Image à incorporer dans le QRCode</param>
        public static void drawQRCode(SKCanvas canvas, float width, float height, int tailleBordure, int pourcentCorrection, SKBitmap saveSKBitmap)
        {

            // Variables d'entrée (à adapter selon ton contexte)
            int size = CLSGenereQRCode.ModulesQRCode.GetLength(0);
            int iEntourage = tailleBordure * CLSGenereQRCode.iPas;

            // Crée un SKPaint blanc pour le fond
            var paintFond = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };

            // Remplir le fond blanc
            canvas.DrawRect(0, 0, width, height, paintFond);

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
                    if (CLSGenereQRCode.ModulesQRCode[i, j] == 1)
                    {
                        float x1 = iEntourage + i * CLSGenereQRCode.iPas;
                        float y1 = iEntourage + j * CLSGenereQRCode.iPas;
                        canvas.DrawRect(x1, y1, CLSGenereQRCode.iPas, CLSGenereQRCode.iPas, paintNoir);
                    }
                }
            }

            // Si elle existe, on dessine l'image à incorporer au centre du QRCode
            if (saveSKBitmap!=null) canvas.DrawBitmap(saveSKBitmap, destRect((int)width, (int)height, saveSKBitmap, pourcentCorrection));

        }

        /// <summary>Calcul de la position et de la taille de l'image cible pour l'incorporer dans le QR Code</summary>
        /// <param name="width">Largeur du QR Code</param>
        /// <param name="height">Hauteur du QR Code</param>
        /// <param name="sourceImage">Image à incorporer dans le QR Code</param>
        /// <param name="pourcentCorrection">Pourcentage de bits de correction du QRCode</param>
        /// <returns></returns>
        public static SKRect destRect(int width, int height, SKBitmap sourceImage, int pourcentCorrection)
        {
            // On calcule le facteur de division de l'image permettant de couvrir la moitié de la surface des bits de correction (on ne tient pas compte des bits de mise en forme)
            double surface = (double)width * height * (double)pourcentCorrection / 200.0;
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
            private Microsoft.Maui.Graphics.IImage _sourceImage;
            private int _pourcentCorrection, _tailleBordure;
            private byte[,] _modulesQRCode;

            /// <summary>Sauvegardedans la classe des paramètres du tracé</summary>
            /// <param name="sourceImage">Image à tracer</param>
            /// <param name="pourcentCorrection">Pourcentage de correction du QRCode</param>
            /// <param name="modulesQRCode">Table des bits du QR Code</param>
            /// <param name="tailleBordure">Taille de la bordure autour du QRCode</param>
            public ImageDrawable(Microsoft.Maui.Graphics.IImage sourceImage, int pourcentCorrection, byte[,] modulesQRCode, int tailleBordure)
            {
                _sourceImage = sourceImage;
                _pourcentCorrection = pourcentCorrection;
                _modulesQRCode = modulesQRCode;
                _tailleBordure = tailleBordure;
            }

            /// <summary>Appel de la fonction de dessin du QRCode</summary>
            /// <param name="canvas">ICanvas du tracé</param>
            /// <param name="dirtyRect">Rectangle de tracé</param>
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {

                drawQRCode(canvas, dirtyRect.Width, dirtyRect.Height, _sourceImage, _tailleBordure, _pourcentCorrection);
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
        public static ImageDrawable GenereImageQRCode(string Texte, string Correction, int TailleBordure, ref int ImageSize, ref int pourcentCorrection)
        {
            // Initialisation des tables de travail
            CLSGenereQRCode.InitTables();
            // Génération du tableau de bytes du QRCode
            bool bOK = true;
            int nbBitsCorrection = 0;   
            CLSGenereQRCode.ModulesQRCode = CLSGenereQRCode.GenereQRCode(Texte, Correction, ref bOK, ref nbBitsCorrection);
            if (bOK != true) return null;

            // Récupération de la largeur de l'écran pour adapter la résolution du QR Code
            var largeurDip = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            int TailleQrCode = (CLSGenereQRCode.ModulesQRCode.GetLength(0) + 2 * TailleBordure);
            CLSGenereQRCode.iPas = (int)largeurDip / TailleQrCode;

            // la résolution maximum est 10
            if (CLSGenereQRCode.iPas >7) CLSGenereQRCode.iPas = 7;

            // La résolution minimum est 2
            if (CLSGenereQRCode.iPas < 2) CLSGenereQRCode.iPas = 2; // afficher un message pour suggérer de dimunuer la correction si elle n'est pas au minimum
            ImageSize = TailleQrCode * CLSGenereQRCode.iPas;
            pourcentCorrection = (int)(100.0 * nbBitsCorrection / ((float)TailleQrCode * (float)TailleQrCode));
            return new ImageDrawable(null, pourcentCorrection, CLSGenereQRCode.ModulesQRCode, TailleBordure);

        }


    }
}
