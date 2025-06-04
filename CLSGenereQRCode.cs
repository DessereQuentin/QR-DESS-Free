using Microsoft.Maui.Graphics.Platform;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using SkiaSharp;
using System.Text;

namespace QRDessFree
{
    /// <summary>Fonctions pour la génération de QRCode</summary>
    public static partial class clsGenereQRCode
    {
        /// <summary>Chaine représentant tous les caractères alphanumérique. Leur poisition dans la chaine donne leur code.</summary>
        public static string CharAlphaNum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        public static string CharNum = "0123456789";

        public static int iPas = 7; // Taille d'un "pixel" du QR Code, affecté à la taille par défaut

        public static int tailleBordure = 1;

        /// <summary>Table des modulos des puissances de 2 du Gallois Field</summary>
        public static byte[] LogTableGF256 = new byte[256];

        /// <summary>Table inversée des modulos des puissances de 2 du Gallois Field</summary>
        public static byte[] AntiLogTableGF256 = new byte[256];

        /// <summary>Modulo du Gallois Field</summary>
        public static int GFModulo = 285;

        /// <summary>Nombres de bits à ajouter en fin de la chaine complète de codage du QRcode (y compris Codewords de correction) en fonction du n° de la version</summary>
        public static byte[] RemainderBits = {0, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0};

        /// <summary>Masque des informations de format</summary>
        public static byte[] MaskFormatInformation = { 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0 };

        /// <summary>Pattern à appliquer aux trois coins repère</summary>
        public static byte[,] MaskPatternCorner = new byte[,] 
         { {1, 1, 1, 1, 1, 1, 1, 0}, { 1, 0, 0, 0, 0, 0, 1, 0 }, { 1, 0, 1, 1, 1, 0, 1, 0 }, { 1, 0, 1, 1, 1, 0, 1, 0 },
           {1, 0, 1, 1, 1, 0, 1, 0}, { 1, 0, 0, 0, 0, 0, 1, 0 }, { 1, 1, 1, 1, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 0, 0 }};

        /// <summary>Pattern à appliquer aux trois coins repère</summary>
        public static byte[,] MaskPatternAlign = new byte[,]
         { {1, 1, 1, 1, 1}, { 1, 0, 0, 0, 1}, { 1, 0, 1, 0, 1}, { 1, 0, 0, 0, 1}, {1, 1, 1, 1, 1} };

        /// <summary>Position des patterns d'alignement</summary>
        public static byte[,] PosPatternAlign = new byte[,]
        {   {0, 0, 0, 0, 0, 0, 0}, {6, 18, 0, 0, 0, 0, 0}, {6, 22, 0, 0, 0, 0, 0}, {6, 26, 0, 0, 0, 0, 0}, {6, 30, 0, 0, 0, 0, 0},
            {6, 34, 0, 0, 0, 0, 0}, {6, 22, 38, 0, 0, 0, 0}, {6, 24, 42, 0, 0, 0, 0}, {6, 26, 46, 0, 0, 0, 0}, {6, 28, 50, 0, 0, 0, 0},
            {6, 30, 54, 0, 0, 0, 0}, {6, 32, 58, 0, 0, 0, 0}, {6, 34, 62, 0, 0, 0, 0}, {6, 26, 46, 66, 0, 0, 0}, {6, 26, 48, 70, 0, 0, 0},
            {6, 26, 50, 74, 0, 0, 0}, {6, 30, 54, 78, 0, 0, 0}, {6, 30, 56, 82, 0, 0, 0}, {6, 30, 58, 86, 0, 0, 0}, {6, 34, 62, 90, 0, 0, 0},
            {6, 28, 50, 72, 94, 0, 0}, {6, 26, 50, 74, 98, 0, 0}, {6, 30, 54, 78, 102, 0, 0}, {6, 28, 54, 80, 106, 0, 0},
            {6, 32, 58, 84, 110, 0, 0}, {6, 30, 58, 86, 114, 0, 0}, {6, 34, 62, 90, 118, 0, 0}, {6, 26, 50, 74, 98, 122, 0},
            {6, 30, 54, 78, 102, 126, 0}, {6, 26, 52, 78, 104, 130, 0}, {6, 30, 56, 82, 108, 134, 0}, {6, 34, 60, 86, 112, 138, 0},
            {6, 30, 58, 86, 114, 142, 0}, {6, 34, 62, 90, 118, 146, 0}, {6, 30, 54, 78, 102, 126, 150}, {6, 24, 50, 76, 102, 128, 154},
            {6, 28, 54, 80, 106, 132, 158}, {6, 32, 58, 84, 110, 136, 162}, {6, 26, 54, 82, 110, 138, 166}, {6, 30, 58, 86, 114, 142, 170}
        };

        public static byte[] PatternScore3 = { 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1 };

        /// <summary> Matrice des modules du QRCode à générer</summary>
        public static byte[,] ModulesQRCode= { { 0 } };

        /// <summary>Elements déscriptif d'une Version de QRCode pour une correction spécifique</summary>
        public partial struct VersionStruct
        {
            /// <summary>Version de QRCode</summary>
            public byte Version;
            /// <summary>Niveau de correction du QRCode</summary>
            public string ErrorCorrectionLevel;
            /// <summary>Capacité en caractères pour la version et la correction mode Numérique</summary>
            public int NumericModeCapacity;
            /// <summary>Capacité en caractères pour la version et la correction mode Alphanumérique</summary>
            public int AlphaNumericModeCapacity;
            /// <summary>Capacité en caractères pour la version et la correction mode Byte</summary>
            public int ByteModeCapacity;
            /// <summary>Capacité en caractères pour la version et la correction mode Kanji</summary>
            public int KanjiModeCapacity;
            /// <summary>Nombre de bits pour stocker la longueur du texte en mode Numérique</summary>
            public int NumericModeLength;
            /// <summary>Nombre de bits pour stocker la longueur du texte en mode Alphanumérique</summary>
            public int AlphaNumericModeLength;
            /// <summary>Nombre de bits pour stocker la longueur du texte en mode Byte</summary>
            public int ByteModeLength;
            /// <summary>Nombre de bits pour stocker la longueur du texte en mode Kanji</summary>
            public int KanjiModeLength;
            /// <summary>Nombre d'octets data</summary>
            public int DataCodeWords;
            /// <summary>Nombre d'octets correction</summary>
            public int ECCodeWords;
            /// <summary>Nombre de blocs du groupe 1</summary>
            public int NBBlocksGroup1;
            /// <summary>Nombre d'octets data par bloc du groupe 1</summary>
            public int DataCodewordsInEachBlocksGroup1;
            /// <summary>Nombre de blocs du groupe 2</summary>
            public int NBBlocksGroup2;
            /// <summary>Nombre d'octets data par bloc du groupe 2</summary>
            public int DataCodewordsInEachBlocksGroup2;


        }

        /// <summary>Table des données pour les versions/corrections de QR Code</summary>
        public static VersionStruct[] tbVersion;

        /// <summary>Valeurs possibles pour les modes d'encodage d'un QR Code</summary>
        public enum ModeEnum : int
        {   NumericMode = 0,
            AlphaNumericMode = 1,
            ByteMode = 2,
            KanjiMode = 3
        }

        /// <summary>Structure de données d'un bloc</summary>        
        public partial struct DataBlockStruct
        {   /// <summary>Table des data</summary>
            public byte[] DataCodewords;
            /// <summary>Table des corrections</summary>
            public byte[] ECCodewords;
        }

        /// <summary>Structure de données d'un bloc</summary>        
        public partial struct GroupBlockStruct
        {   /// <summary>Données des différents groupes</summary>
            public DataBlockStruct[] Group ;
        }

        /// <summary>Coefficients d'un polynome diviseur au rang n </summary>        
        public partial struct DividerPolynomeStruct
        {   /// <summary>Table des coefficients</summary>
            public byte[] Coef;
        }

        /// <summary>Table des polynomes diviseurs pour les différentes tailles de bytes de correction</summary>
        public static DividerPolynomeStruct[] DividerPolynome;

        /// <summary>Chaine représentant le 1er byte pour compléter la chaine de données du QRCode</summary>
        public static string PaddingString1 = "11101100";

        /// <summary>Chaine représentant le 2ème byte pour compléter la chaine de données du QRCode</summary>
        public static string PaddingString2 = "00010001";

        /// <summary>Table des puissances de 2</summary>
        public static int[] PowerOf2;

        /// <summary>Génération de la matrice des modules du QRCode</summary>
        /// <param name="Texte">Texte à intégrer dans le QRCode</param>
        /// <param name="Correction">Type de correction à appliquer</param>
        /// <param name="bOK">Indique si la génération s'est bien passée</param>
        /// <param name="nbBitsCorrection">Pourcentage de bits de correction</param>
        /// <returns>Renvoie les bits dans un tableau de byte (un byte par bit ayant la valeur 0 ou 1)</returns>
        public static byte[,] GenereQRCode(string Texte, string Correction, ref bool bOK, ref int nbBitsCorrection)
        {

            ModeEnum nm =DetermineMode(Texte);
            int i, j, k;
            int Version=0;
            string s;
            switch (nm)
            {   case ModeEnum.NumericMode:      s = GenereNumQRCode(Texte, Correction, ref Version);      break;
                case ModeEnum.AlphaNumericMode: s = GenereAlphaNumQRCode(Texte, Correction, ref Version); break;
                case ModeEnum.ByteMode:         s = GenereByteQRCode(Texte, Correction, ref Version);     break;
                case ModeEnum.KanjiMode:        s = GenereKanjiQRCode(Texte, Correction, ref Version);    break;
                default:                        s = "";                                                   break; 
            }

            if (Version == -1) { bOK = false; return null; }; // La chaine est trop longue pour le type de QRCode à générer

            // On complète si nécessaire la chaine avec les quatre zéros de fin de chaine 
            int nbBits = tbVersion[Version].DataCodeWords * 8;
            s += "0000";

            // si cela fait dépasser la taille maximum de la chaine on tronque, sinon on complète si nécessaire pour avoir des bytes complets 
            if (s.Length > nbBits)
            { s = s.Substring(0, nbBits); }
            else
            {
                i = s.Length / 8 * 8;
                if (i != s.Length)
                {
                    i = i + 8;
                    for (j = s.Length + 1; j <= i; j++) s += "0";
                }
            }

            // On complète la chaine si nécessaire avec les octets de complétude
            bool b = true;
            var loopTo1 = tbVersion[Version].DataCodeWords;
            for (i = (int)Math.Round(s.Length / 8d + 1d); i <= loopTo1; i++)
            {
                if (b) s += PaddingString1; else s += PaddingString2;
                b = !b;
            }

            byte[] DataCodewords = clsGenereQRCode.GenereDataCodewords(s);
            GroupBlockStruct[] QRCodeDataAndCorrections= null;
            byte[] CorrectionCodeWords = clsGenereQRCode.GenereLitAllCodewords(ref DataCodewords, Version, ref QRCodeDataAndCorrections, ref nbBitsCorrection);

            /* string ss = ConvertBytesString(CorrectionCodeWords);  ---- pour test ----*/ 

            // Size contient la taille en modules du QRCode
            int iVersion = tbVersion[Version].Version;
            int NBReserved = 0;
            int Size= 21 + 4 * (tbVersion[Version].Version - 1);
            byte[,] Reserved = new byte[Size, Size];

            byte[,] ModulesQRCode = EncodeQRCode(CorrectionCodeWords, ref Reserved, tbVersion[Version].Version - 1, ref NBReserved);
            byte[,] QRCodeMasked, QRCodeSaveMasked = null;
            int MinScore = int.MaxValue;
            int Score, iScore = 0;

            for (k = 0; k <= 7; k++)
            {   QRCodeMasked = GenereQRCodeMasked(ModulesQRCode, Reserved, k);
                
                // On calcule et positionne les informations de format (correction et mask) 
                byte[] BytesFormat = GenereFormatInformation(15, k, Correction);
                for (j = 0; j <= 6; j++)
                {   QRCodeMasked[8, Size - 1 - j] = BytesFormat[j];
                    if (j < 6) QRCodeMasked[j, 8] = BytesFormat[j]; else QRCodeMasked[j + 1, 8] = BytesFormat[j];
                }
                for (j = 0; j <= 7; j++)
                {   QRCodeMasked[Size - 8 + j, 8] = BytesFormat[j + 7];
                    if (j <= 1) QRCodeMasked[8, 8 - j] = BytesFormat[j + 7]; else QRCodeMasked[8, 8 - j - 1] = BytesFormat[j + 7];
                }

                // Si le QR code est de taille supérieure ou égale à 7 on ajoute les informations de version 
                if (iVersion >= 7)
                {   BytesFormat = GenereFormatInformation(18, iVersion);
                    for (i = 0; i < 6; i++) for (j = 0; j < 3; j++) 
                    {   QRCodeMasked[Size - 11 + j, i] = BytesFormat[17 - (i * 3 + j)];
                        QRCodeMasked[i, Size - 11 + j] = BytesFormat[17 - (i * 3 + j)]; 
                    }
                }

                // On calcul le score lié au masque s'il est meilleur que les précédents, on le conserve 
                Score = CalculScoreMask(QRCodeMasked);
                if (Score < MinScore) { MinScore = Score; QRCodeSaveMasked = (byte[,])QRCodeMasked.Clone(); iScore = k;}
            }


            return QRCodeSaveMasked;
        }

        /// <summary>Calcul le score d'un QRCode avec un masque appliqué</summary>
        /// <param name="b">QR Code masqué à évaluer</param>
        /// <returns>Renvoie la valeur du score</returns>
        public static int CalculScoreMask(byte[,] b)
        {
            int i, j, k, Score=0, Cumul;
            bool bOK;
            int Size = b.GetLength(0);

            // Score 1 : recherche des series supérieures ou égales à 5 en colonne 
            for (i = 0; i<Size; i++)
            {   Cumul = 1;
                for (j = 1; j < Size; j++)
                {   if (b[i, j] == b[i, j - 1]) Cumul += 1; 
                    else 
                    {   if (Cumul >= 5) Score +=3 + Cumul - 5; 
                        Cumul = 1; 
                    } 
                }
                if (Cumul >= 5) Score += 3 + Cumul - 5;
            }

            // Score 1 : recherche des series supérieures ou égales à 5 en ligne 
            for (j = 0; j < Size; j++)
            {   Cumul = 1;
                for (i = 1; i < Size; i++)
                {   if (b[i, j] == b[i - 1, j]) Cumul += 1;
                    else
                    {   if (Cumul >= 5) Score += 3 + Cumul - 5;
                        Cumul = 1;
                    }
                }
                if (Cumul >= 5) Score += 3 + Cumul - 5;
            }

            // Score 2 : recherche des carrés 2x2 de même couleur
            for (i = 1; i < Size; i++)
            {   for (j = 1; j < Size; j++)
                { if (b[i, j] == b[i - 1, j] & b[i, j] == b[i, j - 1] & b[i, j] == b[i - 1, j - 1]) Score += 3; }
            }

            // Score 3 : recherche patterns 00001011101 et 10111010000 en colonne
            for (i = 0; i < Size; i++)
            {   for (j = 0; j <= Size - 11; j++)
                {   bOK = false;
                    for (k = 0; k < 11; k++) if (b[i, j + k] != PatternScore3[k]) { bOK = true; break; }
                    if (!bOK) Score += 40;
                    bOK = false;
                    for (k = 0; k < 11; k++) if (b[i, j + k] != PatternScore3[10 - k]) { bOK = true; break; }
                    if (!bOK) Score += 40;
                }
            }

            // Score 3 : recherche patterns 00001011101 et 10111010000 en ligne
            for (j = 0; j < Size; j++)
            {   for (i = 0; i <= Size - 11; i++)
                {
                    bOK = false;
                    for (k = 0; k < 11; k++) if (b[i + k, j] != PatternScore3[k]) { bOK = true; break; }
                    if (!bOK) Score += 40;
                    bOK = false;
                    for (k = 0; k < 11; k++) if (b[i + k, j] != PatternScore3[10 - k]) { bOK = true; break; }
                    if (!bOK) Score += 40;
                }
            }

            // Score 4 : calcul basé sur le pourcentage de modules noirs
            k = 0;
            for (i = 0; i < Size - 1; i++) for (j = 0; j <= Size - 1; j++) k += b[i, j];
            int Ratio =(int)(100f* (float)k / (float)(Size * Size));
            i = (Ratio / 5) * 5;
            j = (i + 5) -50;
            if (j < 0) j = -j;
            i -= 50;
            if (i < 0) i = -i;
            if (i < j) i = i / 5; else i = j / 5;
            Score += i * 10;

            return Score;
        }

        /// <summary>Encodage de la matrice du QRCode avec les patterns et les données</summary>
        /// <param name="b">Bits (Bytes à 0 ou 1) de données du QR Code</param>
        /// <param name="Version">Version du QRCode</param>
        /// <param name="BitControle">Permet de marquer les patterns et les emplacements réservés pour ne pas y mettre de données et ne pas appliquer de masque</param>
        /// <param name="NBReserved">Nombre d'emplacements réservés dans le QRCode</param>
        /// <returns>Renvoie le tableau de bits (Bytes à 0 ou 1) du QR Code prêt à imprimer</returns>
        public static byte[,] EncodeQRCode(byte[] b,ref byte[,] BitControle, int Version, ref int NBReserved)
        {
            int i, j, k, l, ii, jj;

          
            // Size contient la taille en modules du QRCode
            int Size = 21 + 4 * (Version);
            
            // QRCode contient tous les bits du QRCode à imprimer
            byte[,] QRCode = new byte[Size, Size];

            BitControle = new byte[Size, Size];

            // Placement des patterns de coin
            for (i = 0; i < 8; i++)
            {   for (j = 0; j < 8; j++)
                {   // On marque les emplacements traités pour ne pas y mettre des données
                    BitControle[i, j] = 1; 
                    BitControle[i, Size - j - 1] = 1;
                    BitControle[Size - i - 1, j] = 1;
                    QRCode[i, j] = MaskPatternCorner[i,j];
                    QRCode[i, Size - j - 1] = MaskPatternCorner [i, j];
                    QRCode[Size - i -1, j] = MaskPatternCorner[i, j];
                }
            }

            // Placement des patterns d'alignement
            for (i = 0; i < 7; i++)
            {   ii = PosPatternAlign[Version, i];
                if (ii == 0) break;
                for (j = 0; j < 7; j++)
                {   jj = PosPatternAlign[Version, j];
                    if (jj == 0) break;
                    if (!((ii == 6 & jj==6) | (ii == 6 & jj == Size - 7) | (jj == 6 & ii == Size - 7)))
                    {   for (k= -2; k <=2; k++)
                        {   for (l = -2; l <= 2; l++)
                            {   QRCode[ii + k, jj + l] = MaskPatternAlign[k+2, l+2];
                                BitControle[ii + k, jj + l] = 1 ;
            }   }   }   }   }


            // Placement des timing patterns et du dark module
            for (i = 8; i < Size - 8; i += 2) { QRCode[i, 6] = 1; QRCode[6, i] = 1; }
            for (i = 8; i < Size - 8; i++) { BitControle[i, 6] = 1; BitControle[6, i] = 1; }
            QRCode[8, Size - 8] = 1; BitControle[8, Size - 8] = 1;


            // Réservation des zones de format et de version
            for (i = 0; i <= 8; i++) { BitControle[i, 8] = 1; BitControle[8, i] = 1;}
            for (i = 0; i <= 7; i++) { BitControle[Size - 1 - i, 8] = 1; BitControle[8, Size - 1 - i] = 1;}
            if (Version >=6)
            {   for (i = 0; i < 6; i++) for (j = 0; j < 3; j++) { BitControle[Size - j - 9, i] = 1; BitControle[i, Size - j - 9] = 1; }
            }

            // Vérification du comptage des bits de données
            NBReserved = 0;
            for (i = 0; i < Size; i++)  for (j = 0; j < Size; j++) NBReserved += BitControle[i, j];

            // Lorsque b est null on se contente de la réservation des patterns et des zones de version
            if (b == null) return QRCode;
            if ((Size * Size - NBReserved - b.Length) != 0) throw new Exception("Problème de taille du QRCode");


            // Placement des données
            LectureEcritureDonnees(b, BitControle , QRCode);
          
            return QRCode;
        }

        /// <summary>Détermine le mode de codage du QRCode approprié en fonction du contenu de la chaine de texte</summary>
        /// <param name="Texte">Texte à intégrer dans le QRCode</param>
        /// <returns>Renvoie le mode de codage à utiliser</returns>
        public static ModeEnum DetermineMode(string Texte)
        {
            int i, j;
            bool b;
            /// On determine le mode d'encodage en fonction du contenu de la chaine

            // Si présence uniquement de caractères numériques, on utilise le mode numérique
            b = true;
            for (i = 0; i < Texte.Length; i++)
            {
                j = (int)Texte[i];
                if (j < 48 | j > 57) { b = false; break; }
            }
            if (b) return ModeEnum.NumericMode;

            /// Si présence uniquement de caractères Alphanumériques, on utilise le mode Alphanumériques
            b = true;
            for (i = 0; i < Texte.Length; i++)
            {
                if (CharAlphaNum.IndexOf(Texte[i]) < 0) { b = false; break; }
            }
            if (b) return ModeEnum.AlphaNumericMode;

            // On convertit la chaine en kanji pour tester le mode kanji
            Encoding shiftJis = Encoding.GetEncoding("shift_jis");
            byte[] sjisBytes = shiftJis.GetBytes(Texte);

            //Si présence uniquement de caractères Kanji, on utilise le mode Kanji
            b = true;
            for (i = 0; i < Texte.Length; i++)
            {
                if (!IsKanji(Texte[i])) { b = false; break; }
            }
            if (b) return ModeEnum.KanjiMode;

            // Sinon on utilise le mode byte
            return ModeEnum.ByteMode;
        }
        
        /// <summary>Lecture/écriture des données du QRCode aux emplacements non réservés</summary>
        /// <param name="b">Table des données incluant les données de correction</param>
        /// <param name="Reserved">Emplacement réservés (pattern, informations, ...)</param>
        /// <param name="QRCode">Tableau des cellules du QRCode</param>
        /// <param name="Lecture">Indique si les données sont lus (Lecture=true) ou écrites dans le QRCode (Lecture=false)</param>
        /// <remarks>Le placement s'effectue en balayant de haut en bas et de bas en haut par paires de colonne</remarks>
        public static void LectureEcritureDonnees(byte[] b, byte[,] Reserved, byte[,] QRCode,  Boolean Lecture=false )
        {
            // Placement des données
            bool bSwitch = false, bVersHaut = true;
            int i, ii, j, k, Size = QRCode.GetLength(0);
            i = Size - 1; j = Size - 1; ii = i; k = 0;
            while (k < b.Length)
            {
                if (Reserved[ii, j] == 0) 
                {   { if (Lecture) b[k] = QRCode[ii, j]; else QRCode[ii, j] = b[k]; }
                    k += 1; 
                }
                if (bVersHaut) { if (bSwitch) { j -= 1; ii = i; } else ii = i - 1; }
                else { if (bSwitch) { j += 1; ii = i; } else ii = i - 1; }
                // Alternance de colonne
                bSwitch = !bSwitch;
                //Changement de sens
                if (j < 0) { bVersHaut = false; j = 0; i -= 2; ii = i; if (i == 6) i = 5; ii = i; }
                if (j >= Size) { bVersHaut = true; j = Size - 1; i -= 2; ii = i; }
            }

        }


        /// <summary>Génération à partir d'un texte alphanumérique de la chaine binaire représentant la partie data du QRCode</summary>
        /// <param name="Texte">Texte alphanumérique à intégrer dans le QRCode</param>
        /// <param name="Correction">Type de correction à appliquer</param>
        /// <returns>Renvoie la chaine binaire</returns>
        public static string GenereAlphaNumQRCode(string Texte, string Correction, ref int Version)
        {
            int i, j, k1, k2, l, iCode;

            //Conversion de la chaine alphanumérique texte en une chaine binaire 
            string sCode = "";
            string sCode1;

            Texte = Texte.ToUpper();
            j = 0; iCode = 0;
            // on boucle tant qu'il reste des caractères dans la chaine en traitant les caractères deux par deux puis le dernier s'il en reste un
            while (j <= Texte.Length - 1)
            {
                k1 = CharAlphaNum.IndexOf(Texte[j]);
                sCode1 = "";
                if (j < Texte.Length - 1)    // tant qu'il reste au moins 2 caractères alphanumériques, on les combine pour les coder sur 11 positions binaires 
                {
                    k2 = CharAlphaNum.IndexOf(Texte[j + 1]);
                    l = k1 * 45 + k2;
                    for (i = 0; i <= 10; i++) if ((PowerOf2[i] & l) > 0) sCode1 = "1" + sCode1; else sCode1 = "0" + sCode1;
                    iCode = iCode + 11;
                    j += 2;
                }
                else   // lorsqu'il qu'il ne reste plus qu'un caractère alphanumérique, on le code sur 6 positions binaires 
                {
                    l = k1;
                    for (i = 0; i <= 5; i++)
                        if ((PowerOf2[i] & l) > 0) sCode1 = "1" + sCode1; else sCode1 = "0" + sCode1;
                    j += 1;
                }

                // on ajoute le couple de caractères encodés à la chaine de codage
                sCode += sCode1;
            }

            // On ajoute les informations de mode alphanumérique et de longueur de la chaine 
            Version = IndiceVersion(Correction, ModeEnum.AlphaNumericMode, Texte.Length);
            if (Version == -1) return ""; // Si la version est -1, c'est que le texte est trop long pour le mode alphanumérique

            sCode = "0010" + LongueurQRCode(ModeEnum.AlphaNumericMode, Version, Texte.Length) + sCode;

            return sCode;
        }

        /// <summary>Génération à partir d'un texte numérique de la chaine binaire représentant la partie data du QRCode</summary>
        /// <param name="Texte">Texte numérique à intégrer dans le QRCode</param>
        /// <param name="Correction">Type de correction à appliquer</param>
        /// <returns>Renvoie la chaine binaire</returns>
        public static string GenereNumQRCode(string Texte, string Correction, ref int Version)

        {
            int i, j, k1, k2, k3, l, iCode, lCode;
            //Conversion de la chaine numérique texte en une chaine binaire 
            string sCode = "";
            string sCode1;
            Texte = Texte.ToUpper();
            j = 0; iCode = 0; lCode = 0; l = 0;

            // on boucle tant qu'il reste des caractères dans la chaine en traitant les caractères 3 par 3 puis les deux derniers ou le dernier
            while (j <= Texte.Length - 1)
            {

                // On transforme les caractères numériques en entier
                k1 = CharNum.IndexOf(Texte[j]);
                if (j + 1 < Texte.Length) k2 = CharNum.IndexOf(Texte[j + 1]); else k2 = 0;
                if (j + 2 < Texte.Length) k3 = CharNum.IndexOf(Texte[j + 2]); else k3 = 0;

                // On initialise la chaine de codage du groupe de caractère
                sCode1 = "";

                // En fonction de la taille du groupe (3 pour un groupe courant, 2 ou 1 pour le groupe de fin de chaine),
                // Ou du fait que le groupe commence par 1 ou 2 zéros
                // on calcule la valeur numérique du groupe traité et on affecte la longueur de la chaine à générer 
                if       (j < Texte.Length - 2) { l = k3 + 10 * k2 + 100 * k1; lCode = 10; j += 3; }
                else if (j == Texte.Length - 2) { l = k2 + 10 * k1;            lCode = 7;  j += 2; }
                else if (j == Texte.Length - 1) { l = k1;                      lCode = 4;  j += 1; }

                // On code le nombre obtenu en bits
                for (i = 0; i < lCode; i++) if (((long)Math.Round(Math.Pow(2d, i)) & l) > 0L) sCode1 = "1" + sCode1; else sCode1 = "0" + sCode1;

                // On incrémente le compteur de bits générés
                iCode += lCode;

                // On concatène à la chaine de codage
                sCode += sCode1;
            }

            // On ajoute les informations de mode numérique et de longueur de la chaine 
            Version = IndiceVersion(Correction, ModeEnum.NumericMode, Texte.Length);

            if (Version == -1) return ""; // Si la version est -1, c'est que le texte est trop long pour le mode numérique

            sCode = "0001" + LongueurQRCode(ModeEnum.NumericMode, Version, Texte.Length) + sCode;

            return sCode;
        }

        /// <summary>Génération à partir d'un texte non alphanumérique de la chaine binaire représentant la partie data du QRCode</summary>
        /// <param name="texte">Texte à intégrer dans le QRCode</param>
        /// <param name="sCorrection">Type de correction à appliquer</param>
        /// <returns>Renvoie la chaine binaire</returns>
        public static string GenereByteQRCode(string Texte, string Correction, ref int Version)
        {
            //on fabrique un tableau de bytes avec les caractères UTF8 multibytes de la chaine traitée
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Texte);

            string sCode = "";
            int i, j, k;

            // On boucle sur tous les bytes pour les coder
            for (i = 0; i < bytes.Length; i++)
            {
                k = bytes[i];
                for (j = 7; j >= 0; j--) if ((PowerOf2[j] & k) > 0) sCode += "1"; else sCode += "0";
            }

            // On ajoute les informations de mode numérique et de longueur de la chaine 
            Version = IndiceVersion(Correction, ModeEnum.ByteMode, bytes.Length);
            if (Version == -1) return ""; // Si la version est -1, c'est que le texte est trop long pour le mode byte

            sCode = "0100" + LongueurQRCode(ModeEnum.ByteMode, Version, bytes.Length) + sCode;
            return sCode;
        }

        /// <summary> Détermine si un caractère est kanji</summary>
        /// <param name="c">Caractère à tester</param>
        /// <returns>Vrai si le caractère est kanji, false sinon</returns>
        public static bool IsKanji(char c)
        {
            // On déclare un objet encoding pour l'encodage en kanji
            Encoding shiftJis = Encoding.GetEncoding("shift_jis");

            // on transforme le caractère en kanji et on le stocke dans un tableau d'octets
            byte[] bytes = shiftJis.GetBytes(new char[] { c });

            // si le caractère ne fait pas deux octets, il n'est pas kanji
            if (bytes.Length != 2) return false;

            // on stocke le caractère Kanji dans un entier 
            int value = (bytes[0] << 8) | bytes[1];

            // On teste que le caratère est dans les plages de caractères Kanji
            // 0x8140 à 0x9FFC  et  0xE040 à 0xEBBF
            return (value >= 0x8140 && value <= 0x9FFC) ||
                   (value >= 0xE040 && value <= 0xEBBF);
        }

        /// <summary>Génération à partir d'un texte Kanji de la chaine binaire représentant la partie data du QRCode</summary>
        /// <param name="Texte">Texte numérique à intégrer dans le QRCode</param>
        /// <param name="Correction">Type de correction à appliquer</param>
        /// <param name="Version">Version du QR Code (taille)</param>
        /// <returns>Renvoie la chaine binaire</returns>
        public static string GenereKanjiQRCode(string Texte, string Correction, ref int Version)
        {
            string sCode = "";
            Encoding shiftJis = Encoding.GetEncoding("shift_jis");

            int i, j, k, k1, k2;

            for (i = 0; i < Texte.Length; i++)
            {
                byte[] sjisBytes = shiftJis.GetBytes(Texte[i].ToString());

                k = 256 * sjisBytes[0] + sjisBytes[1];
                if (k >= 0x8140 & k <= 0x9FFC) k -= 0x8140;
                else if (k >= 0xE040 & k <= 0xEBBF) k -= 0xC140;
                k1 = k / PowerOf2[8]; k2 = k - k1 * PowerOf2[8];
                k = (k1 * 0xC0) + k2;
                for (j = 12; j >= 0; j--) if ((PowerOf2[j] & k) > 0) sCode += "1"; else sCode += "0";
            }

            // On ajoute les informations de mode numérique et de longueur de la chaine 
            Version = IndiceVersion(Correction, ModeEnum.ByteMode, Texte.Length);
            if (Version == -1) return ""; // Si la version est -1, c'est que le texte est trop long pour le mode Kanji

            sCode = "1000" + LongueurQRCode(ModeEnum.ByteMode, Version, Texte.Length) + sCode;
            return sCode;
        }

        /// <summary>Initialisation des différentes tables</summary>
        public static void InitTables()
        {

            InitPowerOf2();
            InitVersions();
            InitLogTables();
            GenerePolynomeDiviseur();

            // Nécessaire pour permettre l'encodage étendu, ce qui permet d'encoder en mode Kanji 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        }

        /// <summary>Initialisation des tables LogGF256 et AntilogGF256</summary>
        public static void InitLogTables()
        {
            int i, j;
            j = 1;
            for (i = 0; i < 256; i++)
            { if (j > 255) j = j ^ GFModulo;
                AntiLogTableGF256[i] = (byte)j;
                if (j > 1) LogTableGF256[j] = (byte)i;
                j *= 2;
            }
        }

        /// <summary>Initialisation de la table des informations de version/correction</summary>
        public static void InitVersions()
        {
            var strCapacity = new string[]
            {
                "1;L;41;25;17;10;10;9;8;8;19;7;1;19;;", "1;M;34;20;14;8;10;9;8;8;16;10;1;16;;", "1;Q;27;16;11;7;10;9;8;8;13;13;1;13;;", "1;H;17;10;7;4;10;9;8;8;9;17;1;9;;",
                "2;L;77;47;32;20;10;9;8;8;34;10;1;34;;", "2;M;63;38;26;16;10;9;8;8;28;16;1;28;;", "2;Q;48;29;20;12;10;9;8;8;22;22;1;22;;", "2;H;34;20;14;8;10;9;8;8;16;28;1;16;;",
                "3;L;127;77;53;32;10;9;8;8;55;15;1;55;;", "3;M;101;61;42;26;10;9;8;8;44;26;1;44;;", "3;Q;77;47;32;20;10;9;8;8;34;18;2;17;;", "3;H;58;35;24;15;10;9;8;8;26;22;2;13;;",
                "4;L;187;114;78;48;10;9;8;8;80;20;1;80;;", "4;M;149;90;62;38;10;9;8;8;64;18;2;32;;", "4;Q;111;67;46;28;10;9;8;8;48;26;2;24;;", "4;H;82;50;34;21;10;9;8;8;36;16;4;9;;",
                "5;L;255;154;106;65;10;9;8;8;108;26;1;108;;", "5;M;202;122;84;52;10;9;8;8;86;24;2;43;;", "5;Q;144;87;60;37;10;9;8;8;62;18;2;15;2;16", "5;H;106;64;44;27;10;9;8;8;46;22;2;11;2;12",
                "6;L;322;195;134;82;10;9;8;8;136;18;2;68;;", "6;M;255;154;106;65;10;9;8;8;108;16;4;27;;", "6;Q;178;108;74;45;10;9;8;8;76;24;4;19;;", "6;H;139;84;58;36;10;9;8;8;60;28;4;15;;",
                "7;L;370;224;154;95;10;9;8;8;156;20;2;78;;", "7;M;293;178;122;75;10;9;8;8;124;18;4;31;;", "7;Q;207;125;86;53;10;9;8;8;88;18;2;14;4;15", "7;H;154;93;64;39;10;9;8;8;66;26;4;13;1;14",
                "8;L;461;279;192;118;10;9;8;8;194;24;2;97;;", "8;M;365;221;152;93;10;9;8;8;154;22;2;38;2;39", "8;Q;259;157;108;66;10;9;8;8;110;22;4;18;2;19", "8;H;202;122;84;52;10;9;8;8;86;26;4;14;2;15",
                "9;L;552;335;230;141;10;9;8;8;232;30;2;116;;", "9;M;432;262;180;111;10;9;8;8;182;22;3;36;2;37", "9;Q;312;189;130;80;10;9;8;8;132;20;4;16;4;17", "9;H;235;143;98;60;10;9;8;8;100;24;4;12;4;13",
                "10;L;652;395;271;167;12;11;16;10;274;18;2;68;2;69", "10;M;513;311;213;131;12;11;16;10;216;26;4;43;1;44", "10;Q;364;221;151;93;12;11;16;10;154;24;6;19;2;20", "10;H;288;174;119;74;12;11;16;10;122;28;6;15;2;16",
                "11;L;772;468;321;198;12;11;16;10;324;20;4;81;;", "11;M;604;366;251;155;12;11;16;10;254;30;1;50;4;51", "11;Q;427;259;177;109;12;11;16;10;180;28;4;22;4;23", "11;H;331;200;137;85;12;11;16;10;140;24;3;12;8;13",
                "12;L;883;535;367;226;12;11;16;10;370;24;2;92;2;93", "12;M;691;419;287;177;12;11;16;10;290;22;6;36;2;37", "12;Q;489;296;203;125;12;11;16;10;206;26;4;20;6;21", "12;H;374;227;155;96;12;11;16;10;158;28;7;14;4;15",
                "13;L;1022;619;425;262;12;11;16;10;428;26;4;107;;", "13;M;796;483;331;204;12;11;16;10;334;22;8;37;1;38", "13;Q;580;352;241;149;12;11;16;10;244;24;8;20;4;21", "13;H;427;259;177;109;12;11;16;10;180;22;12;11;4;12",
                "14;L;1101;667;458;282;12;11;16;10;461;30;3;115;1;116", "14;M;871;528;362;223;12;11;16;10;365;24;4;40;5;41", "14;Q;621;376;258;159;12;11;16;10;261;20;11;16;5;17", "14;H;468;283;194;120;12;11;16;10;197;24;11;12;5;13",
                "15;L;1250;758;520;320;12;11;16;10;523;22;5;87;1;88", "15;M;991;600;412;254;12;11;16;10;415;24;5;41;5;42", "15;Q;703;426;292;180;12;11;16;10;295;30;5;24;7;25", "15;H;530;321;220;136;12;11;16;10;223;24;11;12;7;13",
                "16;L;1408;854;586;361;12;11;16;10;589;24;5;98;1;99", "16;M;1082;656;450;277;12;11;16;10;453;28;7;45;3;46", "16;Q;775;470;322;198;12;11;16;10;325;24;15;19;2;20", "16;H;602;365;250;154;12;11;16;10;253;30;3;15;13;16",
                "17;L;1548;938;644;397;12;11;16;10;647;28;1;107;5;108", "17;M;1212;734;504;310;12;11;16;10;507;28;10;46;1;47", "17;Q;876;531;364;224;12;11;16;10;367;28;1;22;15;23", "17;H;674;408;280;173;12;11;16;10;283;28;2;14;17;15",
                "18;L;1725;1046;718;442;12;11;16;10;721;30;5;120;1;121", "18;M;1346;816;560;345;12;11;16;10;563;26;9;43;4;44", "18;Q;948;574;394;243;12;11;16;10;397;28;17;22;1;23", "18;H;746;452;310;191;12;11;16;10;313;28;2;14;19;15",
                "19;L;1903;1153;792;488;12;11;16;10;795;28;3;113;4;114", "19;M;1500;909;624;384;12;11;16;10;627;26;3;44;11;45", "19;Q;1063;644;442;272;12;11;16;10;445;26;17;21;4;22", "19;H;813;493;338;208;12;11;16;10;341;26;9;13;16;14",
                "20;L;2061;1249;858;528;12;11;16;10;861;28;3;107;5;108", "20;M;1600;970;666;410;12;11;16;10;669;26;3;41;13;42", "20;Q;1159;702;482;297;12;11;16;10;485;30;15;24;5;25", "20;H;919;557;382;235;12;11;16;10;385;28;15;15;10;16",
                "21;L;2232;1352;929;572;12;11;16;10;932;28;4;116;4;117", "21;M;1708;1035;711;438;12;11;16;10;714;26;17;42;;", "21;Q;1224;742;509;314;12;11;16;10;512;28;17;22;6;23", "21;H;969;587;403;248;12;11;16;10;406;30;19;16;6;17",
                "22;L;2409;1460;1003;618;12;11;16;10;1006;28;2;111;7;112", "22;M;1872;1134;779;480;12;11;16;10;782;28;17;46;;", "22;Q;1358;823;565;348;12;11;16;10;568;30;7;24;16;25", "22;H;1056;640;439;270;12;11;16;10;442;24;34;13;;",
                "23;L;2620;1588;1091;672;12;11;16;10;1094;30;4;121;5;122", "23;M;2059;1248;857;528;12;11;16;10;860;28;4;47;14;48", "23;Q;1468;890;611;376;12;11;16;10;614;30;11;24;14;25", "23;H;1108;672;461;284;12;11;16;10;464;30;16;15;14;16",
                "24;L;2812;1704;1171;721;12;11;16;10;1174;30;6;117;4;118", "24;M;2188;1326;911;561;12;11;16;10;914;28;6;45;14;46", "24;Q;1588;963;661;407;12;11;16;10;664;30;11;24;16;25", "24;H;1228;744;511;315;12;11;16;10;514;30;30;16;2;17",
                "25;L;3057;1853;1273;784;12;11;16;10;1276;26;8;106;4;107", "25;M;2395;1451;997;614;12;11;16;10;1000;28;8;47;13;48", "25;Q;1718;1041;715;440;12;11;16;10;718;30;7;24;22;25", "25;H;1286;779;535;330;12;11;16;10;538;30;22;15;13;16",
                "26;L;3283;1990;1367;842;12;11;16;10;1370;28;10;114;2;115", "26;M;2544;1542;1059;652;12;11;16;10;1062;28;19;46;4;47", "26;Q;1804;1094;751;462;12;11;16;10;754;28;28;22;6;23", "26;H;1425;864;593;365;12;11;16;10;596;30;33;16;4;17",
                "27;L;3517;2132;1465;902;14;13;16;12;1468;30;8;122;4;123", "27;M;2701;1637;1125;692;14;13;16;12;1128;28;22;45;3;46", "27;Q;1933;1172;805;496;14;13;16;12;808;30;8;23;26;24", "27;H;1501;910;625;385;14;13;16;12;628;30;12;15;28;16",
                "28;L;3669;2223;1528;940;14;13;16;12;1531;30;3;117;10;118", "28;M;2857;1732;1190;732;14;13;16;12;1193;28;3;45;23;46", "28;Q;2085;1263;868;534;14;13;16;12;871;30;4;24;31;25", "28;H;1581;958;658;405;14;13;16;12;661;30;11;15;31;16",
                "29;L;3909;2369;1628;1002;14;13;16;12;1631;30;7;116;7;117", "29;M;3035;1839;1264;778;14;13;16;12;1267;28;21;45;7;46", "29;Q;2181;1322;908;559;14;13;16;12;911;30;1;23;37;24", "29;H;1677;1016;698;430;14;13;16;12;701;30;19;15;26;16",
                "30;L;4158;2520;1732;1066;14;13;16;12;1735;30;5;115;10;116", "30;M;3289;1994;1370;843;14;13;16;12;1373;28;19;47;10;48", "30;Q;2358;1429;982;604;14;13;16;12;985;30;15;24;25;25", "30;H;1782;1080;742;457;14;13;16;12;745;30;23;15;25;16",
                "31;L;4417;2677;1840;1132;14;13;16;12;1843;30;13;115;3;116", "31;M;3486;2113;1452;894;14;13;16;12;1455;28;2;46;29;47", "31;Q;2473;1499;1030;634;14;13;16;12;1033;30;42;24;1;25", "31;H;1897;1150;790;486;14;13;16;12;793;30;23;15;28;16",
                "32;L;4686;2840;1952;1201;14;13;16;12;1955;30;17;115;;", "32;M;3693;2238;1538;947;14;13;16;12;1541;28;10;46;23;47", "32;Q;2670;1618;1112;684;14;13;16;12;1115;30;10;24;35;25", "32;H;2022;1226;842;518;14;13;16;12;845;30;19;15;35;16",
                "33;L;4965;3009;2068;1273;14;13;16;12;2071;30;17;115;1;116", "33;M;3909;2369;1628;1002;14;13;16;12;1631;28;14;46;21;47", "33;Q;2805;1700;1168;719;14;13;16;12;1171;30;29;24;19;25", "33;H;2157;1307;898;553;14;13;16;12;901;30;11;15;46;16",
                "34;L;5253;3183;2188;1347;14;13;16;12;2191;30;13;115;6;116", "34;M;4134;2506;1722;1060;14;13;16;12;1725;28;14;46;23;47", "34;Q;2949;1787;1228;756;14;13;16;12;1231;30;44;24;7;25", "34;H;2301;1394;958;590;14;13;16;12;961;30;59;16;1;17",
                "35;L;5529;3351;2303;1417;14;13;16;12;2306;30;12;121;7;122", "35;M;4343;2632;1809;1113;14;13;16;12;1812;28;12;47;26;48", "35;Q;3081;1867;1283;790;14;13;16;12;1286;30;39;24;14;25", "35;H;2361;1431;983;605;14;13;16;12;986;30;22;15;41;16",
                "36;L;5836;3537;2431;1496;14;13;16;12;2434;30;6;121;14;122", "36;M;4588;2780;1911;1176;14;13;16;12;1914;28;6;47;34;48", "36;Q;3244;1966;1351;832;14;13;16;12;1354;30;46;24;10;25", "36;H;2524;1530;1051;647;14;13;16;12;1054;30;2;15;64;16",
                "37;L;6153;3729;2563;1577;14;13;16;12;2566;30;17;122;4;123", "37;M;4775;2894;1989;1224;14;13;16;12;1992;28;29;46;14;47", "37;Q;3417;2071;1423;876;14;13;16;12;1426;30;49;24;10;25", "37;H;2625;1591;1093;673;14;13;16;12;1096;30;24;15;46;16",
                "38;L;6479;3927;2699;1661;14;13;16;12;2702;30;4;122;18;123", "38;M;5039;3054;2099;1292;14;13;16;12;2102;28;13;46;32;47", "38;Q;3599;2181;1499;923;14;13;16;12;1502;30;48;24;14;25", "38;H;2735;1658;1139;701;14;13;16;12;1142;30;42;15;32;16",
                "39;L;6743;4087;2809;1729;14;13;16;12;2812;30;20;117;4;118", "39;M;5313;3220;2213;1362;14;13;16;12;2216;28;40;47;7;48", "39;Q;3791;2298;1579;972;14;13;16;12;1582;30;43;24;22;25", "39;H;2927;1774;1219;750;14;13;16;12;1222;30;10;15;67;16",
                "40;L;7089;4296;2953;1817;14;13;16;12;2956;30;19;118;6;119", "40;M;5596;3391;2331;1435;14;13;16;12;2334;28;18;47;31;48", "40;Q;3993;2420;1663;1024;14;13;16;12;1666;30;34;24;34;25", "40;H;3057;1852;1273;784;14;13;16;12;1276;30;20;15;61;16"
            };
            int i;
            string[] s;
            tbVersion = new VersionStruct[strCapacity.Length];
            for (i = 0; i <= strCapacity.Length - 1; i++)
            {
                s = strCapacity[i].Split(";");
                tbVersion[i].Version = (byte)Math.Round(Conversion.Val(s[0]));
                tbVersion[i].ErrorCorrectionLevel = s[1];
                tbVersion[i].NumericModeCapacity = (int)Math.Round(Conversion.Val(s[2]));
                tbVersion[i].AlphaNumericModeCapacity = (int)Math.Round(Conversion.Val(s[3]));
                tbVersion[i].ByteModeCapacity = (int)Math.Round(Conversion.Val(s[4]));
                tbVersion[i].KanjiModeCapacity = (int)Math.Round(Conversion.Val(s[5]));
                tbVersion[i].NumericModeLength = (int)Math.Round(Conversion.Val(s[6]));
                tbVersion[i].AlphaNumericModeLength = (int)Math.Round(Conversion.Val(s[7]));
                tbVersion[i].ByteModeLength = (int)Math.Round(Conversion.Val(s[8]));
                tbVersion[i].KanjiModeLength = (int)Math.Round(Conversion.Val(s[9]));
                tbVersion[i].DataCodeWords = (int)Math.Round(Conversion.Val(s[10]));
                tbVersion[i].ECCodeWords = (int)Math.Round(Conversion.Val(s[11]));
                tbVersion[i].NBBlocksGroup1 = (int)Math.Round(Conversion.Val(s[12]));
                tbVersion[i].DataCodewordsInEachBlocksGroup1 = (int)Math.Round(Conversion.Val(s[13]));
                tbVersion[i].NBBlocksGroup2 = (int)Math.Round(Conversion.Val(s[14]));
                tbVersion[i].DataCodewordsInEachBlocksGroup2 = (int)Math.Round(Conversion.Val(s[15]));
            }

        }

        /// <summary>Determine le rang dans la table des versions en fonction de la longueur du contenu, du niveau de correction attendu et du mode de stockage </summary>
        /// <param name="Correction">Niveau de correction attendu</param>
        /// <param name="Mode">Mode de stockage du texte</param>
        /// <param name="Longueur">Longueur du texte</param>
        /// <returns>Rang de l'entrée dans la table des versions, -1 si la longueur est trop grande pour la version la plus grande</returns>
        public static int IndiceVersion(string Correction, ModeEnum Mode, int Longueur)
        {
            int i; int l;

            // On cherche la version minimum permettant de gérer le texte nécessaire, c'est à dire celle gérant une longueur juste au dessus de la longueur nécessaire
            for (i = 0; i < tbVersion.Length; i++)
            {
                if (tbVersion[i].ErrorCorrectionLevel == Correction)
                {
                    switch (Mode)
                    {   case ModeEnum.NumericMode: { l = tbVersion[i].NumericModeCapacity; break; }
                        case ModeEnum.AlphaNumericMode: { l = tbVersion[i].AlphaNumericModeCapacity; break; }
                        case ModeEnum.ByteMode: { l = tbVersion[i].ByteModeCapacity; break; }
                        case ModeEnum.KanjiMode: { l = tbVersion[i].KanjiModeCapacity; break; }
                        default: { l = tbVersion[i].AlphaNumericModeCapacity; break; }
                    }
                    if (l >= Longueur) break;
                }
            }

            // Si la taille demandée dépasse la capacité du plus grand QRCode on renvoie -1 sinon on renvoie la version trouvée
            if (i < tbVersion.Length) return i; else return -1;
        }

        /// <summary>Génération de la chaine de longueur du QRCode dont la taille dépend de la version/correction</summary>
        /// <param name="Mode">Mode de codage des caractères du QRCode</param>
        /// <param name="Version">Version du QRCode permettant de stocker la chaine de données</param>
        /// <param name="Longueur">Longueur de la chaine de données</param>
        /// <returns>Chaine binaire contenant la longueur du texte à coder</returns>
        public static string LongueurQRCode(ModeEnum Mode, int Version, int Longueur)
        {
            int i, iCapacity = default;
            string s = "";
            // on détermine la longueur de la chaine pour la version/correction du QRCode en fonction du mode d'encodage de la chaine
            switch (Mode)
            {
                case ModeEnum.NumericMode: { iCapacity = tbVersion[Version].NumericModeLength; break; }
                case ModeEnum.AlphaNumericMode: { iCapacity = tbVersion[Version].AlphaNumericModeLength; break; }
                case ModeEnum.ByteMode: { iCapacity = tbVersion[Version].ByteModeLength; break; }
                case ModeEnum.KanjiMode: { iCapacity = tbVersion[Version].KanjiModeLength; break; }
            }

            // on boucle sur toutes les positions binaires de la longueur pour générer la chaine de longueur
            for (i = 0; i <= iCapacity - 1; i++) if ((PowerOf2[i] & Longueur) > 0) s = "1" + s; else s = "0" + s;
            return s;
        }

        /// <summary>Génère tous les polynomes diviseurs du premier au 30ème degré (longuer maximum d'une chaine de correction)</summary>
        public static void GenerePolynomeDiviseur()
        {
            int i, j;
            byte[,] coef;
            DividerPolynome = new DividerPolynomeStruct[30];
            DividerPolynome[0].Coef = new byte[2];

            DividerPolynome[0].Coef[0] = 0;
            DividerPolynome[0].Coef[1] = 0;
            for (i = 1; i < 30; i++)
            {
                coef = new byte[i + 1, 2];
                for (j = 0; j <= i; j++) { coef[j, 0] = DividerPolynome[i - 1].Coef[j]; coef[j, 1] = (byte)(((int)DividerPolynome[i - 1].Coef[j] + i) % 255); }
                DividerPolynome[i].Coef = new byte[i + 2];
                DividerPolynome[i].Coef[0] = 0; DividerPolynome[i].Coef[i + 1] = coef[i, 1];
                for (j = 0; j < i; j++) DividerPolynome[i].Coef[j + 1] = AddTwoAlpha(coef[j + 1, 0], coef[j, 1]);
            }
        }

        public static byte AddTwoAlpha(byte alpha1, byte alpha2)
        { return LogTableGF256[AntiLogTableGF256[alpha1] ^ AntiLogTableGF256[alpha2]];
        }


        /// <summary>En écriture, génère tous les Codewords de correction d'erreur pour tous les blocs de tous les groupes et renvoie la chaine de bits finale du QRCode
        ///          En lecture, récupère les Codewords de data et de correction d'erreur pour tous les blocs de tous les groupes dans DataWords pour les data et en retour de la fonction pour les blocs d'erreurs</summary>
        /// <param name="Datawords">Data Codewords du QRCode en ecriture, toutes les Data (CodeWords+ECWords) en lecture)</param>
        /// <param name="Version">Indice de la version du QRCode dans la table des versions</param>
        /// <param name="Lecture">Indique si les données sont à ecrire dans le QRCode ou à lire</param>
        /// <returns>Table complète des bits du QRCode (complété avec les bits à zéro restant=Remainder Bits)</returns>
        public static byte[] GenereLitAllCodewords(ref byte[] Datawords, int Version, ref GroupBlockStruct[] QRCodeDataAndCorrections, ref int nbBitsCorrection, bool Lecture = false)
        {

            int[] iSizeGroup ={ tbVersion[Version].NBBlocksGroup1, tbVersion[Version].NBBlocksGroup2 };
            int[] iSizeBlocksGroup = { tbVersion[Version].DataCodewordsInEachBlocksGroup1, tbVersion[Version].DataCodewordsInEachBlocksGroup2 };
            int ECCodeWords = tbVersion[Version].ECCodeWords;
            int i, j, k, l, n;

            QRCodeDataAndCorrections= new GroupBlockStruct[2];

            /// Génération de tous les Codewords de correction d'erreur pour tous les groupes et tous leurs blocs
            l = 0;
            for (i = 0; i < 2; i++)
            {   QRCodeDataAndCorrections[i].Group = new DataBlockStruct[iSizeGroup[i]];
                for (j = 0; j < iSizeGroup[i]; j++)
                {   
                    // On comptabilise les bits de correction
                    nbBitsCorrection+= ECCodeWords * 8;
                    // On initialise les blocs de données et de correction du groupe
                    QRCodeDataAndCorrections[i].Group[j].DataCodewords = new byte[iSizeBlocksGroup[i]];
                    QRCodeDataAndCorrections[i].Group[j].ECCodewords = new byte[ECCodeWords];
                    
                    /// On initialise la table des Codewords du bloc du groupe en écriture
                    if (!Lecture) {
                        for (k=0; k < iSizeBlocksGroup[i]; k++)
                        {   QRCodeDataAndCorrections[i].Group[j].DataCodewords[k] = Datawords[l];
                            l += 1;
                        }
                        QRCodeDataAndCorrections[i].Group[j].ECCodewords = GenereCorrectionCodewords(QRCodeDataAndCorrections[i].Group[j].DataCodewords, Version);
                    }
                }
            }

            /// Entrelacement des blocs de données, on lit/écrit une donnée dans chaque bloc des deux groupes jusqu'à épuisement
            int iSize = (iSizeBlocksGroup[0] + ECCodeWords) * iSizeGroup[0] + (iSizeBlocksGroup[1] + ECCodeWords) * iSizeGroup[1];
            byte[] AllBytes = new byte[8 * iSize + RemainderBits[tbVersion[Version].Version - 1]];
            l = 0;
            for (i = 0; i < Math.Max(iSizeBlocksGroup[0], iSizeBlocksGroup[1]); i++)        
            {   for (j = 0; j < 2; j++)
                {   for (k = 0; k < iSizeGroup[j]; k++)
                    {   if (i < iSizeBlocksGroup[j]) 
                        {   for (n = 7; n >= 0; n--)
                            {   if (Lecture) QRCodeDataAndCorrections[j].Group[k].ECCodewords[i] += (byte)(PowerOf2[n] * AllBytes[l]);
                                else
                                { 
                                    if ((byte)(PowerOf2[n] & (int)QRCodeDataAndCorrections[j].Group[k].DataCodewords[i]) > 0) AllBytes[l] = 1; else AllBytes[l] = 0;
                                    l += 1;
                                }
                            }
                        }
                    }
                }
            }

            /// Entrelacement des blocs de correction, on lit/écrit une donnée de correction dans chaque bloc des deux groupes jusqu'à épuisement
            for (i = 0; i < ECCodeWords; i++)
            {   for (j = 0; j < 2; j++)
                {   for (k = 0; k < iSizeGroup[j]; k++)
                    {   for (n = 7; n >= 0; n--) 
                        {   if (Lecture) QRCodeDataAndCorrections[j].Group[k].ECCodewords[i] += (byte)(PowerOf2[n] * AllBytes[l]);
                            else
                            {   if ((byte)(PowerOf2[n] & (int)QRCodeDataAndCorrections[j].Group[k].ECCodewords[i]) > 0) AllBytes[l] = 1; else AllBytes[l] = 0;
                                l += 1;
                            }
                        }
                    }
                }
            }

            /// Ajout des bits de complétion
            if (!Lecture) for (i = 0; i < RemainderBits[tbVersion[Version].Version - 1]; i++) { AllBytes[l] = 0; l += 1; }
 
            return AllBytes;
        }

        /// <summary>Génére les Codewords de correction d'erreur d'un block de Codewords de data</summary>
        /// <param name="Datawords">Table des datas</param>
        /// <param name="Version">rang de la Version/Correction du QRCode permettant de connaitre le nombre de Codewords à générer</param>
        /// <returns>Tableau des Codewords de correction</returns>
        /// <remarks>Les Codewords d'erreur sont obtenus en divisant le polynome dont les coefficients sont les Codewords de donnée
        /// par le polynome diviseur de rang le nombre de Codewords d'erreur</remarks>
        public static byte[] GenereCorrectionCodewords(byte[] Datawords, int Version)
        {
            int i, ip;
            int NbCorrectionCodeWords = tbVersion[Version].ECCodeWords;
            byte[] CorrectionCodewords = new byte[NbCorrectionCodeWords];
            byte[] PolynomData = new byte[NbCorrectionCodeWords + Datawords.Length];
            byte[] PolynomCorrection = new byte[NbCorrectionCodeWords + Datawords.Length];
            byte[] PolynomCorrectionByte = new byte[NbCorrectionCodeWords + Datawords.Length];

            // On initialise le polynome à diviser, dont les coefficients sont les données du bloc et dont la puissance maximale est
            // la somme des données du bloc et de correction -1
            for (i = 0; i < Datawords.Length; i++) PolynomData[i] = Datawords[i];

            // On boucle pour diviser par le polynome diviseur autant de fois qu'il y a de données dans le bloc
            for (ip = 0; ip < Datawords.Length; ip++)
            {

                if (PolynomData[ip] != 0) // Si lors de la division, le coefficient de la puissance la lus haute restante est à zéro, on saute l'itération
                {

                    for (i = 0; i <= NbCorrectionCodeWords; i++)
                    {
                        // On multiplie le polynome diviseur par le plus haut coef du polynome à diviser : cela revient à cumuler les puissances Alpha (modulo 255 pour rester sur un byte)
                        PolynomCorrection[i] = (byte)((DividerPolynome[NbCorrectionCodeWords - 1].Coef[i] + LogTableGF256[PolynomData[ip]]) % 255);
                        // On calcule ensuite les coefficients du polynome en effectuant les puissances Alpha
                        PolynomCorrectionByte[i] = AntiLogTableGF256[PolynomCorrection[i]];
                    }
                    //On soustrait les deux polynomes pour conserver le reste
                    for (i = 0 + ip; i <= NbCorrectionCodeWords + ip; i++) PolynomData[i] = (byte)(PolynomData[i] ^ PolynomCorrectionByte[i - ip]);
                }
            }

            // Les coefficients du polynome obtenu comme reste de la division sont les bytes d'erreur
            for (i = 0; i < NbCorrectionCodeWords; i++) CorrectionCodewords[i] = PolynomData[i + Datawords.Length];

            return CorrectionCodewords;
        }

        /// <summary>Génère le tableau de Codewords d'une chaine de caractères représentant des bits</summary>
        /// <param name="Codewords">Chaine de bits représentant les Codewords</param>
        /// <returns>Tableau de Codewords</returns>
        public static byte[] GenereDataCodewords(string Codewords)
        {   int i, j, k;
            byte[] b = new byte[Codewords.Length/8];
            j = 0; k = 0;
            for (i = 0; i < Codewords.Length; i++)
            {   if (Codewords.Substring(i, 1) == "1") b[k] += (byte)PowerOf2[7-j];
                if (j == 7) { j = 0; k += 1; } else j += 1;
            }
            return b;
        }

        /// <summary>Génère le masque de rang RgMask du QRCode encodé</summary>
        /// <param name="b">QR code encodé</param>
        /// <param name="Reserved">Zone réservée (valeurs à 1 dans le tableau)</param>
        /// <param name="RgMask">Rang du masque à appliquer</param>
        /// <returns>QRCode encodé avec le masque appliqué</returns>
        public static byte[,] GenereQRCodeMasked(byte[,] b, byte[,] Reserved, int RgMask)
        {
            int i, j;
            int Size = b.GetLength(0);
            byte [,] bMasked = new byte[Size, Size];

            for (i=0; i< Size; i++) 
            {  for (j = 0; j < Size; j++)
                {    bMasked[i, j] = b[i, j];
                    if (Reserved[i, j] == 0) // on applique pas le masque pas les zones réservées
                    {   switch (RgMask)
                        {   case 0: if ((i + j) % 2 == 0)                       bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 1: if (j % 2 == 0)                             bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 2: if (i % 3 == 0)                             bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 3: if ((i + j) % 3 == 0)                       bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 4: if (((i / 3) + (j / 2)) % 2 == 0)           bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 5: if (((i * j) % 2) + ((i * j) % 3) == 0)     bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 6: if ((((i * j) % 2) + ((i * j) % 3)) % 2 == 0) bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            case 7: if ((((i + j) % 2) + ((i * j) % 3)) % 2 == 0) bMasked[i, j] = (byte)(1 - b[i, j]); break;
                            default: break;
            }   }   }   }
            return bMasked;
        }

        /// <summary>Initialise la table des 16 permières puissances de 2</summary>
        public static void InitPowerOf2()
        {   int i;
            PowerOf2 = new int[16];
            PowerOf2[0] = 1;
            for (i = 1; i < 16; i++) PowerOf2[i] = 2 * PowerOf2[i - 1]; 
        }

        /// <summary>Conversion d'un tableau de bytes contenant des bits dans une chaine de caractères</summary>
        /// <param name="b">Tableau de bits à convertir</param>
        /// <returns>Chaine d'une longueur multiple de 8 contenant des "0" et des "1"</returns>
        public static string ConvertBytesString(byte[] b)
        {
            string sCode = "";
            for (int i = 0; i <b.Length; i++) { if ( b[i]== 0) sCode += "0"; else sCode += "1"; }
            return sCode;
        }

        /// <summary>Génération des informations de format (15 bits) et pour les QRCode de version >= 7 de version (18 bits)</summary>
        /// <param name="Correction">Niveau de correction du QRCode (informations de format), non renseigné sinon</param>
        /// <param name="Data">Rang du masque à appliquer (si informations de formats) vs Version du QRCode (informations de version)</param>
        /// <param name="LengthTypeInfo">Longueur totale de la table de bytes à constituer : =15 si informations de format, =18 si informations de version </param>
        /// <returns>tableau de bytes contenant les bits des informations de format (15 bits), ou de version (18 bits) suivant l'appel effectué </returns>
        public static byte[] GenereFormatInformation(int LengthTypeInfo, int Data, string Correction="")
        {
            byte[] Mask18 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] Mask;
            byte[] Format15 = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            byte[] Format18 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] Format;
            byte[] PolynomCorrection15 = {1, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0};
            byte[] PolynomCorrection18 = { 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0};
            byte[] PolynomCorrection;

            // Initialisations en fonction de la taille de la chaine d'informations à renvoyer
            int SizeCorrection = 2 * LengthTypeInfo / 3;
            int SizeData = LengthTypeInfo - SizeCorrection;
            if (LengthTypeInfo == 15)
            {   Format = Format15;
                Mask = MaskFormatInformation;
                PolynomCorrection = PolynomCorrection15;
                switch (Correction)
                {   case "L": Format[0] = 0; Format[1] = 1; break;
                    case "M": Format[0] = 0; Format[1] = 0; break;
                    case "Q": Format[0] = 1; Format[1] = 1; break;
                    case "H": Format[0] = 1; Format[1] = 0; break;
                }
                Format[2] = (byte) ((Data & 4) / 4); Format[3] = (byte)((Data & 2) / 2); Format[4] = (byte)(Data & 1);
            }
            else
            {
                Format = Format18;
                Mask = Mask18;
                PolynomCorrection = PolynomCorrection18;
                Format[0] = (byte)((Data & 32) / 32); Format[1] = (byte)((Data & 16) / 16); Format[2] = (byte)((Data & 8)/8);
                Format[3] = (byte)((Data & 4) / 4);   Format[4] = (byte)((Data & 2) / 2);   Format[5] = (byte)(Data & 1) ;
            }

            byte[] FormatSave= (byte[])Format.Clone();
            int i, j;

            while (Format.Length> SizeCorrection) 
            {   // on décale les bits vers la gauche pour y amener le premier bit à 1 en préservant une longueur minimum de 10
                for (i = 0; i < Format.Length; i++) if (Format[i] == 1) break;
                if ((Format.Length - i) < SizeCorrection) i = Format.Length - SizeCorrection;
                for (j = i; j < Format.Length; j++) Format[j - i] = Format[j];
                Array.Resize(ref Format, Format.Length - i);
                if (Format.Length > SizeCorrection) 
                {for (i = 0; i < Format.Length; i++) Format[i] = (byte)(Format[i] ^ PolynomCorrection[i]); 
                }
            }

            // On récupère les bits de données puis les bits de correction
            // On applique le masque pour les informations de format, pour la version le mask avec que des zeros ne change aucun bit
            for (i = 0; i < LengthTypeInfo; i++)
            {   if (i < SizeData)   FormatSave[i] = (byte)(FormatSave[i] ^ Mask[i]);
                else         FormatSave[i] = (byte)(Format[i - SizeData] ^ Mask[i]);
            }
            return FormatSave;
        }

        public static string Hello_World1Q = "00100000010110110000101101111000110100010111001011011100010011010100001101000000111011000001000111101100";

        public static string Exemple5MIn = "0100001101010101010001101000011001010111001001100101010111000010"+
                                           "0111011100110010000001100001001000000110011001110010011011110110"+
                                           "1111011001000010000001110111011010000110111100100000011100100110"+
                                           "0101011000010110110001101100011110010010000001101011011011100110"+
                                           "1111011101110111001100100000011101110110100001100101011100100110"+
                                           "0101001000000110100001101001011100110010000001110100011011110111"+
                                           "011101100101011011000010000001101001011100110010"+"00010000"+ "11101100"+
                                           "000100011110110000010001111011000001000111101100";
        public static string Exempe5MOut = "01000011111101101011011001000110010101011111011011100110111101110100011001000010111101110111011010000110000001110111011101010110010101110111011000110010110000100010011010000110000001110000011001010101111100100111011010010111110000100000011110000110001100100111011100100110010101110001000000110010010101100010011011101100000001100001011001010010000100010001001011000110000001101110110000000110110001111000011000010001011001111001001010010111111011000010011000000110001100100001000100000111111011001101010101010111100101001110101111000111110011000111010010011111000010110110000010110001000001010010110100111100110101001010110101110011110010100100110000011000111101111011011010000101100100111111000101111100010010110011101111011111100111011111001000100001111001011100100011101110011010101111100010000110010011000010100010011010000110111100001111111111011101011000000111100110101011001001101011010001101111010101001001101111000100010000101000000010010101101010001101101100100000111010000110100011111100000010000001101111011110001100000010110010001001111000010110001101111011000000000";

        public static byte[,] TestScore = { {1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1},
                                            {1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1},
                                            {1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1},
                                            {1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1},
                                            {0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                                            {0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 1},
                                            {0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1},
                                            {0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 1, 1, 0, 0, 0},
                                            {0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 1, 0},
                                            {1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 1},
                                            {0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 1},
                                            {1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 1, 0, 0},
                                            {1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 0},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0},
                                            {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1},
                                            {1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 0, 1, 1},
                                            {1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1}};




    }
}
