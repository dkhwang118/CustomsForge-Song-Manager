using RocksmithToolkitLib.DLCPackage.AggregateGraph;
using RocksmithToolkitLib.DLCPackage.Manifest.Tone;
using RocksmithToolkitLib.DLCPackage.Manifest;
using RocksmithToolkitLib.DLCPackage.Manifest2014.Header;
using RocksmithToolkitLib.DLCPackage.Manifest2014.Tone;
using RocksmithToolkitLib.DLCPackage.Manifest2014;
using RocksmithToolkitLib.DLCPackage.XBlock;
using RocksmithToolkitLib.DLCPackage;
using Arrangement = RocksmithToolkitLib.DLCPackage.Arrangement;
using RocksmithToolkitLib.Extensions;
using RocksmithToolkitLib.Ogg;
using RocksmithToolkitLib.Properties;
using RocksmithToolkitLib.PSARC;
using RocksmithToolkitLib.Sng;
using RocksmithToolkitLib.Sng2014HSL;
using RocksmithToolkitLib.XML;
using RocksmithToolkitLib.XmlRepository;
using RocksmithToolkitLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X360.IO;
using X360.Other;
using X360.STFS;

namespace CustomsForgeSongManager.LocalTools
{
    public class ThreadSafeDLCPackageCreator : IDisposable
    {
        private static readonly string XBOX_WORKDIR = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "xboxpackage");

        private static readonly string PS3_WORKDIR = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "edat");

        private static readonly string[] PATH_PC = new string[3] { "Windows", "Generic", "_p" };

        private static readonly string[] PATH_MAC = new string[3] { "Mac", "MacOS", "_m" };

        private static readonly string[] PATH_XBOX = new string[3] { "XBox360", "XBox360", "_xbox" };

        private static readonly string[] PATH_PS3 = new string[3] { "PS3", "PS3", "_ps3" };

        private List<string> FILES_XBOX = new List<string>();

        private List<string> FILES_PS3 = new List<string>();

        private List<string> TMPFILES_SNG = new List<string>();

        private List<string> TMPFILES_ART = new List<string>();

        private RocksmithToolkitLib.PSARC.PSARC packPsarc;

        private string dlcName;
        private bool disposedValue;

        private static void DeleteTmpFiles(List<string> files)
        {
            try
            {
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
            }

            files.Clear();
        }

        public string[] GetPathName(Platform platform)
        {
            return platform.platform.GetPathName();
        }

        public string[] GetPathName(GamePlatform gPlatform)
        {
            switch (gPlatform)
            {
                case GamePlatform.Pc:
                    return PATH_PC;
                case GamePlatform.Mac:
                    return PATH_MAC;
                case GamePlatform.XBox360:
                    return PATH_XBOX;
                case GamePlatform.PS3:
                    return PATH_PS3;
                default:
                    throw new InvalidOperationException("Unexpected game platform value");
            }
        }

        public ThreadSafeDLCPackageCreator() 
        {
        }


        public string Generate(string destPath, DLCPackageData info, Platform platform, DLCPackageType dlcType = DLCPackageType.Song, int pnum = -1)
        {
            switch (platform.platform)
            {
                case GamePlatform.XBox360:
                    if (!Directory.Exists(XBOX_WORKDIR))
                    {
                        Directory.CreateDirectory(XBOX_WORKDIR);
                    }

                    break;
                case GamePlatform.PS3:
                    if (!Directory.Exists(PS3_WORKDIR))
                    {
                        Directory.CreateDirectory(PS3_WORKDIR);
                    }

                    break;
            }

            string path = Path.GetFileNameWithoutExtension(destPath).StripPlatformEndName();
            string text = Path.Combine(Path.GetDirectoryName(destPath), path);
            if (platform.version == GameVersion.RS2014)
            {
                text += platform.GetPathName()[2];
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                switch (platform.version)
                {
                    case GameVersion.RS2014:
                        switch (dlcType)
                        {
                            case DLCPackageType.Song:
                                GenerateRS2014SongPsarc(memoryStream, info, platform, pnum);
                                break;
                            case DLCPackageType.Lesson:
                                throw new NotImplementedException("Lesson package type not implemented yet :(");
                            case DLCPackageType.Inlay:
                                GenerateRS2014InlayPsarc(memoryStream, info, platform);
                                break;
                        }

                        break;
                    case GameVersion.RS2012:
                        GenerateRS1Psarcs(memoryStream, info, platform);
                        break;
                    case GameVersion.None:
                        throw new InvalidOperationException("Unexpected game version value");
                }

                switch (platform.platform)
                {
                    case GamePlatform.Pc:
                    case GamePlatform.Mac:
                        switch (platform.version)
                        {
                            case GameVersion.RS2014:
                                {
                                    if (!text.EndsWith(".psarc"))
                                    {
                                        text += ".psarc";
                                    }

                                    using (FileStream destination = File.Create(text))
                                    {
                                        memoryStream.CopyTo(destination);
                                    }

                                    break;
                                }
                            case GameVersion.RS2012:
                                {
                                    if (!text.EndsWith(".dat"))
                                    {
                                        text += ".dat";
                                    }

                                    using (FileStream output = File.Create(text))
                                    {
                                        RijndaelEncryptor.EncryptFile(memoryStream, output, RijndaelEncryptor.DLCKey);
                                    }

                                    break;
                                }
                            default:
                                throw new InvalidOperationException("Unexpected game version value");
                        }

                        break;
                    case GamePlatform.XBox360:
                        if (!text.EndsWith("_xbox"))
                        {
                            text += "_xbox";
                        }

                        text = BuildXBox360Package(text, info, FILES_XBOX, platform.version, dlcType);
                        break;
                    case GamePlatform.PS3:
                        if (!text.EndsWith(".psarc"))
                        {
                            text += ".psarc";
                        }

                        text = EncryptPS3EdatFiles(text, platform);
                        break;
                }
            }

            if (packPsarc != null)
            {
                packPsarc.Dispose();
                packPsarc = null;
            }

            FILES_XBOX.Clear();
            FILES_PS3.Clear();
            DeleteTmpFiles(TMPFILES_SNG);
            if (pnum <= 1)
            {
                DeleteTmpFiles(TMPFILES_ART);
            }

            return text;
        }

        public string BuildXBox360Package(string destPath, DLCPackageData info, IEnumerable<string> xboxFiles, GameVersion gameVersion, DLCPackageType dlcType = DLCPackageType.Song)
        {
            LogRecord logRecord = new LogRecord();
            RSAParams rSAParams = ((info.SignatureType == PackageMagic.CON) ? new RSAParams(new DJsIO(Resources.XBox360_KV, BigEndian: true)) : new RSAParams(StrongSigned.LIVE));
            CreateSTFS createSTFS = new CreateSTFS();
            createSTFS.HeaderData = GetSTFSHeader(info, gameVersion, dlcType);
            foreach (string xboxFile in xboxFiles)
            {
                createSTFS.AddFile(xboxFile, Path.GetFileName(xboxFile));
            }

            STFSPackage sTFSPackage = new STFSPackage(createSTFS, rSAParams, destPath, logRecord);
            if (!sTFSPackage.RebuildPackage(rSAParams))
            {
                throw new InvalidOperationException("Error on create XBox360 package, details: \n" + logRecord.Log);
            }

            sTFSPackage.FlushPackage(rSAParams);
            sTFSPackage.CloseIO();
            IOExtension.DeleteDirectory(XBOX_WORKDIR);
            if (File.Exists(destPath))
            {
                return destPath;
            }

            return string.Empty;
        }

        private HeaderData GetSTFSHeader(DLCPackageData info, GameVersion gameVersion, DLCPackageType dlcType)
        {
            HeaderData headerData = new HeaderData();
            string text = "Custom Package";
            switch (dlcType)
            {
                case DLCPackageType.Song:
                    text = $"{info.SongInfo.SongDisplayName} by {info.SongInfo.Artist}";
                    break;
                case DLCPackageType.Lesson:
                    throw new NotImplementedException("Lesson package type not implemented yet :(");
                case DLCPackageType.Inlay:
                    text = "Custom Inlay by Song Creator";
                    break;
            }

            switch (gameVersion)
            {
                case GameVersion.RS2012:
                    headerData.Title_Package = "Rocksmith";
                    headerData.TitleID = 1431505011u;
                    headerData.PackageImageBinary = Resources.XBox360_DLC_image;
                    break;
                case GameVersion.RS2014:
                    headerData.Title_Package = "Rocksmith 2014";
                    headerData.TitleID = 1431505088u;
                    headerData.PackageImageBinary = Resources.XBox360_DLC_image2014;
                    break;
            }

            headerData.Publisher = $"Song Creator Toolkit for Rocksmith ({ToolkitVersion.RSTKGuiVersion} beta)";
            headerData.Title_Display = text;
            headerData.Description = text;
            headerData.ThisType = PackageType.MarketPlace;
            headerData.ContentImageBinary = headerData.PackageImageBinary;
            headerData.IDTransfer = TransferLock.AllowTransfer;
            if (info.SignatureType == PackageMagic.LIVE)
            {
                foreach (XBox360License xBox360License in info.XBox360Licenses)
                {
                    headerData.AddLicense(xBox360License.ID, xBox360License.Bit, xBox360License.Flag);
                }
            }

            return headerData;
        }

        public string EncryptPS3EdatFiles(string srcPath, Platform platform)
        {
            string text = string.Empty;
            if (Path.GetFileName(srcPath).Contains(" "))
            {
                srcPath = Path.Combine(Path.GetDirectoryName(srcPath), Path.GetFileName(srcPath).Replace(" ", "_"));
            }

            IEnumerable<string> enumerable = from e in Directory.EnumerateFiles(PS3_WORKDIR, "*.*")
                                             where !e.EndsWith(".psarc")
                                             select e;
            foreach (string item in enumerable)
            {
                File.Delete(item);
            }

            if (platform.version == GameVersion.RS2014 && FILES_PS3.Count == 1 && File.Exists(FILES_PS3[0]))
            {
                string sourceFileName = FILES_PS3[0].Clone().ToString();
                FILES_PS3[0] = Path.Combine(Path.GetDirectoryName(FILES_PS3[0]), Path.GetFileName(srcPath));
                if (File.Exists(FILES_PS3[0]))
                {
                    File.Delete(FILES_PS3[0]);
                }

                File.Move(sourceFileName, FILES_PS3[0]);
            }

            string text2 = RijndaelEncryptor.EncryptPS3Edat();
            foreach (string item2 in FILES_PS3)
            {
                if (File.Exists(item2))
                {
                    File.Delete(item2);
                }
            }

            if (platform.version == GameVersion.RS2014)
            {
                string text3 = $"{FILES_PS3[0]}.edat";
                text = $"{srcPath}.edat";
                if (File.Exists(text))
                {
                    File.Delete(text);
                }

                if (File.Exists(text3))
                {
                    File.Move(text3, text);
                }
            }
            else if (Directory.Exists(PS3_WORKDIR))
            {
                IOExtension.MoveDirectory(PS3_WORKDIR, $"{srcPath}_PS3", overwrite: true);
            }

            if (text2.IndexOf("No JDK or JRE is installed on your machine") > 0)
            {
                throw new InvalidOperationException("You need install Java SE 7 (x86) or higher on your machine. The Java path should be in PATH Environment Variable:" + Environment.NewLine + Environment.NewLine + text2);
            }

            if (text2.IndexOf("Encrypt all EDAT files successfully") < 0)
            {
                throw new InvalidOperationException("Rebuilder error, please check if .edat files are created correctly and see output bellow:" + Environment.NewLine + Environment.NewLine + text2);
            }

            return text;
        }

        private void GenerateRS2014SongPsarc(Stream output, DLCPackageData info, Platform platform, int pnum = -1)
        {
            dlcName = info.Name.ToLower();
            packPsarc = new RocksmithToolkitLib.PSARC.PSARC();
            Stream stream = null;
            Stream stream2 = null;
            Stream stream3 = null;
            Stream stream4 = null;
            try
            {
                if (info.ArtFiles == null && File.Exists(info.AlbumArtPath))
                {
                    string albumArtPath = info.AlbumArtPath;
                    string text = albumArtPath.Remove(albumArtPath.Length - 7) + "128.dds";
                    string text2 = albumArtPath.Remove(albumArtPath.Length - 7) + "64.dds";
                    if (File.Exists(text2) && File.Exists(text))
                    {
                        List<DDSConvertedFile> list = new List<DDSConvertedFile>();
                        list.Add(new DDSConvertedFile
                        {
                            sizeX = 64,
                            destinationFile = text2
                        });
                        list.Add(new DDSConvertedFile
                        {
                            sizeX = 128,
                            destinationFile = text
                        });
                        list.Add(new DDSConvertedFile
                        {
                            sizeX = 256,
                            destinationFile = albumArtPath
                        });
                        List<DDSConvertedFile> artFiles = list;
                        info.ArtFiles = artFiles;
                    }
                }

                if (info.ArtFiles == null)
                {
                    List<DDSConvertedFile> artFiles2 = info.ArtFiles;
                    string text3;
                    if (File.Exists(info.AlbumArtPath))
                    {
                        text3 = info.AlbumArtPath;
                    }
                    else
                    {
                        using (MemoryStream memoryStream = new MemoryStream(Resources.albumart2014_256))
                        {
                            text3 = GeneralExtension.GetTempFileName(".dds");
                            memoryStream.WriteFile(text3);
                            TMPFILES_ART.Add(text3);
                        }
                    }

                    List<DDSConvertedFile> list2 = new List<DDSConvertedFile>();
                    list2.Add(new DDSConvertedFile
                    {
                        sizeX = 64,
                        sizeY = 64,
                        sourceFile = text3,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    list2.Add(new DDSConvertedFile
                    {
                        sizeX = 128,
                        sizeY = 128,
                        sourceFile = text3,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    list2.Add(new DDSConvertedFile
                    {
                        sizeX = 256,
                        sizeY = 256,
                        sourceFile = text3,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    artFiles2 = list2;
                    ToDDS(artFiles2);
                    info.ArtFiles = artFiles2;
                }

                foreach (DDSConvertedFile artFile in info.ArtFiles)
                {
                    packPsarc.AddEntry($"gfxassets/album_art/album_{dlcName}_{artFile.sizeX}.dds", new FileStream(artFile.destinationFile, FileMode.Open, FileAccess.Read, FileShare.Read));
                    if (artFile.sizeY != 0)
                    {
                        TMPFILES_ART.Add(artFile.destinationFile);
                    }
                }

                string text4 = string.Empty;
                if (info.Arrangements.Any((RocksmithToolkitLib.DLCPackage.Arrangement arr) => arr.HasCustomFont))
                {
                    text4 = info.Arrangements.Find((RocksmithToolkitLib.DLCPackage.Arrangement arr) => arr.HasCustomFont).LyricsArtPath;
                }

                if (!string.IsNullOrEmpty(text4))
                {
                    packPsarc.AddEntry(string.Format("assets/ui/lyrics/{0}/lyrics_{0}.dds", dlcName), new FileStream(text4, FileMode.Open, FileAccess.Read, FileShare.Read));
                }

                string oggPath = info.OggPath;
                if (!File.Exists(oggPath))
                {
                    throw new InvalidOperationException($"Audio file '{oggPath}' not found.");
                }

                stream = ((platform.IsConsole == oggPath.GetAudioPlatform().IsConsole) ? File.OpenRead(oggPath) : OggFile.ConvertAudioPlatform(oggPath));
                string oggPreviewPath = info.OggPreviewPath;
                stream2 = ((!File.Exists(oggPreviewPath)) ? stream : ((platform.IsConsole == oggPreviewPath.GetAudioPlatform().IsConsole) ? File.OpenRead(oggPreviewPath) : OggFile.ConvertAudioPlatform(oggPreviewPath)));
                stream3 = new MemoryStream(Resources.rsenumerable_root);
                packPsarc.AddEntry("flatmodels/rs/rsenumerable_root.flat", stream3);
                stream4 = new MemoryStream(Resources.rsenumerable_song);
                packPsarc.AddEntry("flatmodels/rs/rsenumerable_song.flat", stream4);
                MemoryStream memoryStream2 = new MemoryStream();
                MemoryStream memoryStream3 = new MemoryStream();
                MemoryStream memoryStream4 = new MemoryStream();
                MemoryStream memoryStream5 = new MemoryStream();
                MemoryStream memoryStream6 = new MemoryStream();
                MemoryStream memoryStream7 = new MemoryStream();
                MemoryStream memoryStream10 = new MemoryStream();
                DisposableCollection<Stream> disposableCollection3 = new DisposableCollection<Stream>();
                DisposableCollection<Stream> disposableCollection2 = new DisposableCollection<Stream>();
                DisposableCollection<Stream> disposableCollection = new DisposableCollection<Stream>();
                MemoryStream memoryStream11 = new MemoryStream();
                MemoryStream memoryStream12 = new MemoryStream();
                GenerateToolkitVersion(memoryStream2, info.ToolkitInfo.PackageAuthor, info.ToolkitInfo.PackageVersion, info.ToolkitInfo.PackageComment, info.ToolkitInfo.PackageRating);
                packPsarc.AddEntry("toolkit.version", memoryStream2);
                if (!platform.IsConsole)
                {
                    GenerateAppId(memoryStream3, info.AppId, platform);
                    packPsarc.AddEntry("appid.appid", memoryStream3);
                }

                if (platform.platform == GamePlatform.XBox360)
                {
                    StreamWriter streamWriter = new StreamWriter(memoryStream4);
                    streamWriter.Write(dlcName);
                    streamWriter.Flush();
                    memoryStream4.Seek(0L, SeekOrigin.Begin);
                    WriteTmpFile(memoryStream4,"PackageList.txt", platform);
                }

                string arg = $"song_{dlcName}";
                string arg2 = SoundBankGenerator2014.GenerateSoundBank(info.Name, stream, memoryStream5, info.Volume, platform);
                packPsarc.AddEntry($"audio/{platform.GetPathName()[0].ToLower()}/{arg}.bnk", memoryStream5);
                packPsarc.AddEntry($"audio/{platform.GetPathName()[0].ToLower()}/{arg2}.wem", stream);
                string arg3 = $"song_{dlcName}_preview";
                float volume = info.PreviewVolume ?? info.Volume;
                dynamic val = SoundBankGenerator2014.GenerateSoundBank(info.Name + "_Preview", stream2, memoryStream6, volume, platform, preview: true, !File.Exists(oggPreviewPath));
                packPsarc.AddEntry($"audio/{platform.GetPathName()[0].ToLower()}/{arg3}.bnk", memoryStream6);
                if (!stream2.Equals(stream))
                {
                    packPsarc.AddEntry(string.Format("audio/{0}/{1}.wem", platform.GetPathName()[0].ToLower(), val), stream2);
                }

                string name = $"{dlcName}_aggregategraph.nt";
                RocksmithToolkitLib.DLCPackage.AggregateGraph2014.AggregateGraph2014 aggregateGraph = new RocksmithToolkitLib.DLCPackage.AggregateGraph2014.AggregateGraph2014(info, platform);
                aggregateGraph.Serialize(memoryStream7);
                memoryStream7.Flush();
                memoryStream7.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry(name, memoryStream7);
                ManifestHeader2014<AttributesHeader2014> manifestHeader = new ManifestHeader2014<AttributesHeader2014>(platform);
                SongPartition songPartition = new SongPartition();
                new SongPartition();
                foreach (Arrangement arrangement2 in info.Arrangements)
                {
                    if (arrangement2.ArrangementType != ArrangementType.ShowLight)
                    {
                        string text5 = songPartition.GetArrangementFileName(arrangement2.ArrangementName, arrangement2.ArrangementType).ToLower();
                        UpdateToneDescriptors(info);
                        GenerateSNG(arrangement2, platform);
                        FileStream fileStream = File.OpenRead(arrangement2.SongFile.File);
                        disposableCollection.Add(fileStream);
                        packPsarc.AddEntry($"songs/bin/{platform.GetPathName()[1].ToLower()}/{dlcName}_{text5}.sng", fileStream);
                        FileStream fileStream2 = File.OpenRead(arrangement2.SongXml.File);
                        disposableCollection.Add(fileStream2);
                        packPsarc.AddEntry($"songs/arr/{dlcName}_{text5}.xml", fileStream2);
                        Manifest2014<Attributes2014> manifest = new Manifest2014<Attributes2014>();
                        Attributes2014 attributes = new Attributes2014(text5, arrangement2, info, platform);
                        Dictionary<string, Attributes2014> dictionary = new Dictionary<string, Attributes2014>();
                        dictionary.Add("Attributes", attributes);
                        Dictionary<string, Attributes2014> value = dictionary;
                        manifest.Entries.Add(attributes.PersistentID, value);
                        MemoryStream memoryStream8 = new MemoryStream();
                        disposableCollection2.Add(memoryStream8);
                        manifest.Serialize(memoryStream8);
                        memoryStream8.Seek(0L, SeekOrigin.Begin);
                        packPsarc.AddEntry(string.Format(platform.IsConsole ? "manifests/songs_dlc/{0}_{1}.json" : "manifests/songs_dlc_{0}/{0}_{1}.json", dlcName, text5), memoryStream8);
                        Dictionary<string, AttributesHeader2014> dictionary2 = new Dictionary<string, AttributesHeader2014>();
                        dictionary2.Add("Attributes", new AttributesHeader2014(attributes));
                        Dictionary<string, AttributesHeader2014> value2 = dictionary2;
                        if (platform.IsConsole)
                        {
                            manifestHeader = new ManifestHeader2014<AttributesHeader2014>(platform);
                            manifestHeader.Entries.Add(attributes.PersistentID, value2);
                            MemoryStream memoryStream9 = new MemoryStream();
                            disposableCollection3.Add(memoryStream9);
                            manifestHeader.Serialize(memoryStream9);
                            memoryStream8.Seek(0L, SeekOrigin.Begin);
                            packPsarc.AddEntry($"manifests/songs_dlc/{dlcName}_{text5}.hson", memoryStream9);
                        }
                        else
                        {
                            manifestHeader.Entries.Add(attributes.PersistentID, value2);
                        }
                    }
                }

                if (!platform.IsConsole)
                {
                    manifestHeader.Serialize(memoryStream10);
                    memoryStream10.Seek(0L, SeekOrigin.Begin);
                    packPsarc.AddEntry(string.Format("manifests/songs_dlc_{0}/songs_dlc_{0}.hsan", dlcName), memoryStream10);
                }

                Arrangement arrangement = info.Arrangements.FirstOrDefault((Arrangement ar) => ar.ArrangementType == ArrangementType.ShowLight);
                if (arrangement != null && arrangement.SongXml.File != null)
                {
                    using (FileStream fileStream3 = File.OpenRead(arrangement.SongXml.File))
                    {
                        fileStream3.CopyTo(memoryStream11);
                    }
                }
                else
                {
                    Showlights showlights = new Showlights();
                    showlights.CreateShowlights(info);
                    if (showlights.ShowlightList.Count <= 1)
                    {
                        throw new InvalidOperationException("<ERROR> Insufficient showlight changes will crash game: " + showlights.ShowlightList.Count);
                    }

                    showlights.Serialize(memoryStream11);
                    string text6 = Path.Combine(Path.GetDirectoryName(info.Arrangements[0].SongXml.File), string.Format("{0}_showlights.xml", "cst"));
                    using (FileStream stream5 = new FileStream(text6, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream11.WriteTo(stream5);
                    }

                    Song2014.WriteXmlComments(text6);
                    using (FileStream fileStream4 = File.OpenRead(text6))
                    {
                        fileStream4.CopyTo(memoryStream11);
                    }
                }

                if (memoryStream11.CanRead && memoryStream11.Length > 0)
                {
                    packPsarc.AddEntry($"songs/arr/{dlcName}_showlights.xml", memoryStream11);
                }

                GameXblock<Entity2014> gameXblock = GameXblock<Entity2014>.Generate2014(info, platform);
                gameXblock.SerializeXml(memoryStream12);
                memoryStream12.Flush();
                memoryStream12.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry($"gamexblocks/nsongs/{dlcName}.xblock", memoryStream12);
                packPsarc.Write(output, !platform.IsConsole);
                WriteTmpFile(output, $"{dlcName}.psarc", platform);

                memoryStream2.Dispose();
                memoryStream3.Dispose();
                memoryStream4.Dispose();
                memoryStream5.Dispose();
                memoryStream6.Dispose();
                memoryStream7.Dispose();
                memoryStream10.Dispose();
                memoryStream11.Dispose();
                memoryStream12.Dispose();
                disposableCollection.Dispose();
                disposableCollection2.Dispose();
                disposableCollection3.Dispose();

            }
            finally
            {
                stream?.Dispose();
                stream2?.Dispose();
                stream3?.Dispose();
                stream4?.Dispose();
                if (pnum <= 1)
                {
                    DeleteTmpFiles(TMPFILES_ART);
                }

                DeleteTmpFiles(TMPFILES_SNG);
            }
        }

        private void GenerateRS2014InlayPsarc(Stream output, DLCPackageData info, Platform platform)
        {
            dlcName = info.Inlay.DLCSixName;
            packPsarc = new RocksmithToolkitLib.PSARC.PSARC();
            Stream stream = null;
            Stream stream2 = null;
            try
            {
                List<DDSConvertedFile> artFiles = info.ArtFiles;
                if (artFiles == null)
                {
                    string text;
                    if (File.Exists(info.Inlay.IconPath))
                    {
                        text = info.Inlay.IconPath;
                    }
                    else
                    {
                        using (MemoryStream memoryStream = new MemoryStream(Resources.cgm_default_icon))
                        {
                            text = Path.ChangeExtension(Path.GetTempFileName(), ".png");
                            memoryStream.WriteFile(text);
                            TMPFILES_ART.Add(text);
                        }
                    }

                    string text2;
                    if (File.Exists(info.Inlay.InlayPath))
                    {
                        text2 = info.Inlay.InlayPath;
                    }
                    else
                    {
                        using (MemoryStream memoryStream2 = new MemoryStream(Resources.cgm_default_inlay))
                        {
                            text2 = GeneralExtension.GetTempFileName(".png");
                            memoryStream2.WriteFile(text2);
                            TMPFILES_ART.Add(text2);
                        }
                    }

                    artFiles = new List<DDSConvertedFile>();
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 64,
                        sizeY = 64,
                        sourceFile = text,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 128,
                        sizeY = 128,
                        sourceFile = text,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 256,
                        sizeY = 256,
                        sourceFile = text,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 512,
                        sizeY = 512,
                        sourceFile = text,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 1024,
                        sizeY = 512,
                        sourceFile = text2,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    ToDDS(artFiles, DLCPackageType.Inlay);
                    info.ArtFiles = artFiles;
                }

                foreach (DDSConvertedFile artFile in info.ArtFiles)
                {
                    if (artFile.sizeX == 1024)
                    {
                        packPsarc.AddEntry($"assets/gameplay/inlay/inlay_{dlcName}.dds", new FileStream(artFile.destinationFile, FileMode.Open, FileAccess.Read, FileShare.Read));
                    }
                    else
                    {
                        packPsarc.AddEntry($"gfxassets/rewards/guitar_inlays/reward_inlay_{dlcName}_{artFile.sizeX}.dds", new FileStream(artFile.destinationFile, FileMode.Open, FileAccess.Read, FileShare.Read));
                    }
                }

                stream = new MemoryStream(Resources.rsenumerable_root);
                packPsarc.AddEntry("flatmodels/rs/rsenumerable_root.flat", stream);
                stream2 = new MemoryStream(Resources.rsenumerable_guitar);
                packPsarc.AddEntry("flatmodels/rs/rsenumerable_guitars.flat", stream2);
                MemoryStream memoryStream3 = new MemoryStream();
                MemoryStream memoryStream4 = new MemoryStream();
                MemoryStream memoryStream5 = new MemoryStream();
                MemoryStream memoryStream6 = new MemoryStream();
                DisposableCollection<Stream> disposableCollection = new DisposableCollection<Stream>();
                MemoryStream memoryStream8 = new MemoryStream();
                MemoryStream memoryStream10 = new MemoryStream();
                MemoryStream memoryStream9 = new MemoryStream();
                GenerateToolkitVersion(memoryStream3);
                packPsarc.AddEntry("toolkit.version", memoryStream3);
                if (!platform.IsConsole)
                {
                    GenerateAppId(memoryStream4, info.AppId, platform);
                    packPsarc.AddEntry("appid.appid", memoryStream4);
                }

                if (platform.platform == GamePlatform.XBox360)
                {
                    StreamWriter streamWriter = new StreamWriter(memoryStream5);
                    streamWriter.Write(dlcName);
                    streamWriter.Flush();
                    memoryStream5.Seek(0L, SeekOrigin.Begin);
                    WriteTmpFile(memoryStream5,"PackageList.txt", platform);
                }

                string name = $"{dlcName}_aggregategraph.nt";
                RocksmithToolkitLib.DLCPackage.AggregateGraph2014.AggregateGraph2014 aggregateGraph = new RocksmithToolkitLib.DLCPackage.AggregateGraph2014.AggregateGraph2014(info, platform, DLCPackageType.Inlay);
                aggregateGraph.Serialize(memoryStream6);
                memoryStream6.Flush();
                memoryStream6.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry(name, memoryStream6);
                InlayAttributes2014 inlayAttributes = new InlayAttributes2014(info);
                Dictionary<string, InlayAttributes2014> dictionary = new Dictionary<string, InlayAttributes2014>();
                dictionary.Add("Attributes", inlayAttributes);
                Dictionary<string, InlayAttributes2014> value = dictionary;
                Manifest2014<InlayAttributes2014> manifest = new Manifest2014<InlayAttributes2014>(DLCPackageType.Inlay);
                manifest.Entries.Add(inlayAttributes.PersistentID, value);
                MemoryStream memoryStream7 = new MemoryStream();
                disposableCollection.Add(memoryStream7);
                manifest.Serialize(memoryStream7);
                memoryStream7.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry(string.Format(platform.IsConsole ? "manifests/songs_dlc/dlc_guitar_{0}.json" : "manifests/songs_dlc_{0}/dlc_guitar_{0}.json", dlcName), memoryStream7);
                Dictionary<string, InlayAttributes2014> dictionary2 = new Dictionary<string, InlayAttributes2014>();
                dictionary2.Add("Attributes", inlayAttributes);
                Dictionary<string, InlayAttributes2014> value2 = dictionary2;
                ManifestHeader2014<InlayAttributes2014> manifestHeader = new ManifestHeader2014<InlayAttributes2014>(platform, DLCPackageType.Inlay);
                manifestHeader.Entries.Add(inlayAttributes.PersistentID, value2);
                manifestHeader.Serialize(memoryStream8);
                memoryStream8.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry(string.Format(platform.IsConsole ? "manifests/songs_dlc/dlc_{0}.hson" : "manifests/songs_dlc_{0}/dlc_{0}.hsan", dlcName), memoryStream8);
                GameXblock<Entity2014> gameXblock = GameXblock<Entity2014>.Generate2014(info, platform, DLCPackageType.Inlay);
                gameXblock.SerializeXml(memoryStream9);
                memoryStream9.Flush();
                memoryStream9.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry($"gamexblocks/nguitars/guitar_{dlcName}.xblock", memoryStream9);
                InlayNif inlayNif = new InlayNif(info);
                inlayNif.Serialize(memoryStream10);
                memoryStream10.Flush();
                memoryStream10.Seek(0L, SeekOrigin.Begin);
                packPsarc.AddEntry($"assets/gameplay/inlay/{dlcName}.nif", memoryStream10);
                packPsarc.Write(output, !platform.IsConsole);
                WriteTmpFile(output,$"{dlcName}.psarc", platform);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                stream?.Dispose();
                stream2?.Dispose();
                DeleteTmpFiles(TMPFILES_ART);
            }
        }

        private void GenerateRS1Psarcs(Stream output, DLCPackageData info, Platform platform)
        {
            packPsarc = new RocksmithToolkitLib.PSARC.PSARC();
            IList<Stream> list = new List<Stream>();
            MemoryStream memoryStream2 = new MemoryStream();
            MemoryStream memoryStream3 = new MemoryStream();
            MemoryStream memoryStream = new MemoryStream();
            MemoryStream memoryStream4 = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream);
            try
            {
                GenerateToolkitVersion(memoryStream2, info.ToolkitInfo.PackageAuthor, info.ToolkitInfo.PackageVersion, info.ToolkitInfo.PackageComment, info.ToolkitInfo.PackageRating);
                packPsarc.AddEntry("toolkit.version", memoryStream2);
                if (platform.platform == GamePlatform.Pc)
                {
                    GenerateAppId(memoryStream3, info.AppId, platform);
                    packPsarc.AddEntry("APP_ID", memoryStream3);
                }

                streamWriter.WriteLine(info.Name);
                GenerateRS1SongPsarc(memoryStream4, info, platform);
                string text = $"{info.Name}.psarc";
                packPsarc.AddEntry(text, memoryStream4);
                WriteTmpFile(memoryStream4, text, platform);
                for (int i = 0; i < info.Tones.Count; i++)
                {
                    RocksmithToolkitLib.DLCPackage.Manifest.Tone.Tone tone = info.Tones[i];
                    bool isTone = true;
                    string text2 = tone.Key.GetValidKey("", isTone);
                    if (string.IsNullOrEmpty(text2))
                    {
                        object obj;
                        if (tone.Name != null)
                        {
                            bool isTone2 = true;
                            obj = tone.Name.GetValidKey("", isTone2);
                        }
                        else
                        {
                            obj = "Default";
                        }

                        text2 = (string)obj;
                    }

                    MemoryStream memoryStream5 = new MemoryStream();
                    GenerateTonePsarc(memoryStream5, text2, tone);
                    string text3 = $"DLC_Tone_{text2}.psarc";
                    packPsarc.AddEntry(text3, memoryStream5);
                    WriteTmpFile(memoryStream5, text3, platform);
                    if (i + 1 != info.Tones.Count)
                    {
                        streamWriter.WriteLine("DLC_Tone_{0}", text2);
                    }
                    else
                    {
                        streamWriter.Write("DLC_Tone_{0}", text2);
                    }

                    list.Add(memoryStream5);
                }

                streamWriter.Flush();
                memoryStream.Seek(0L, SeekOrigin.Begin);
                if (platform.platform != GamePlatform.PS3)
                {
                    string text4 = "PackageList.txt";
                    packPsarc.AddEntry(text4, memoryStream);
                    WriteTmpFile(memoryStream, text4, platform);
                }

                packPsarc.Write(output);
            }
            catch (Exception ex)
            {
                throw new Exception("<ERROR> GenerateRS1Psarcs: " + ex.Message + Environment.NewLine);
            }
            finally
            {
                foreach (Stream item in list)
                {
                    item?.Dispose();
                }
            }
        }

        private void GenerateRS1SongPsarc(Stream output, DLCPackageData info, Platform platform)
        {
            string text = $"Song_{info.Name}";
            Stream stream = null;
            Stream stream2 = null;
            try
            {
                string text2;
                if (File.Exists(info.AlbumArtPath))
                {
                    text2 = info.AlbumArtPath;
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(Resources.albumart))
                    {
                        text2 = GeneralExtension.GetTempFileName(".dds");
                        memoryStream.WriteFile(text2);
                        TMPFILES_ART.Add(text2);
                    }
                }

                List<DDSConvertedFile> artFiles = info.ArtFiles;
                if (artFiles == null)
                {
                    artFiles = new List<DDSConvertedFile>();
                    artFiles.Add(new DDSConvertedFile
                    {
                        sizeX = 512,
                        sizeY = 512,
                        sourceFile = text2,
                        destinationFile = GeneralExtension.GetTempFileName(".dds")
                    });
                    ToDDS(artFiles);
                    info.ArtFiles = artFiles;
                }

                stream = new FileStream(info.ArtFiles[0].destinationFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                string oggPath = info.OggPath;
                if (!File.Exists(oggPath))
                {
                    throw new InvalidOperationException($"Audio file '{oggPath}' not found.");
                }

                stream2 = ((platform.IsConsole == oggPath.GetAudioPlatform().IsConsole) ? File.OpenRead(oggPath) : OggFile.ConvertAudioPlatform(oggPath));
                MemoryStream memoryStream2 = new MemoryStream();
                MemoryStream memoryStream3 = new MemoryStream();
                MemoryStream memoryStream4 = new MemoryStream();
                MemoryStream memoryStream5 = new MemoryStream();
                MemoryStream memoryStream6 = new MemoryStream();
                Stream stream3 = OggFile.ConvertOgg(stream2);
                DisposableCollection<Stream> disposableCollection = new DisposableCollection<Stream>();
                ManifestBuilder manifestBuilder = new ManifestBuilder();
                manifestBuilder.AggregateGraph = new RocksmithToolkitLib.DLCPackage.AggregateGraph.AggregateGraph
                {
                    SoundBank = new SoundBank
                    {
                        File = text + ".bnk"
                    },
                    AlbumArt = new AlbumArt
                    {
                        File = info.AlbumArtPath
                    }
                };
                ManifestBuilder manifestBuilder2 = manifestBuilder;
                foreach (Arrangement arrangement in info.Arrangements)
                {
                    GenerateSNG(arrangement, platform);
                    manifestBuilder2.AggregateGraph.SongFiles.Add(arrangement.SongFile);
                    manifestBuilder2.AggregateGraph.SongXMLs.Add(arrangement.SongXml);
                }

                manifestBuilder2.AggregateGraph.XBlock = new XBlockFile
                {
                    File = info.Name + ".xblock"
                };
                manifestBuilder2.AggregateGraph.Write(info.Name, platform.GetPathName(), platform, memoryStream2);
                memoryStream2.Flush();
                memoryStream2.Seek(0L, SeekOrigin.Begin);
                string value = manifestBuilder2.GenerateManifest(info.Name, info.Arrangements, info.SongInfo, platform);
                StreamWriter streamWriter = new StreamWriter(memoryStream3);
                streamWriter.Write(value);
                streamWriter.Flush();
                memoryStream3.Seek(0L, SeekOrigin.Begin);
                GameXblock<Entity>.Generate(info.Name, manifestBuilder2.Manifest, manifestBuilder2.AggregateGraph, memoryStream4);
                memoryStream4.Flush();
                memoryStream4.Seek(0L, SeekOrigin.Begin);
                string arg = SoundBankGenerator.GenerateSoundBank(info.Name, stream3, memoryStream5, info.Volume, platform);
                memoryStream5.Flush();
                memoryStream5.Seek(0L, SeekOrigin.Begin);
                GenerateSongPackageId(memoryStream6, info.Name);
                RocksmithToolkitLib.PSARC.PSARC pSARC = new RocksmithToolkitLib.PSARC.PSARC();
                pSARC.AddEntry("PACKAGE_ID", memoryStream6);
                pSARC.AddEntry("AggregateGraph.nt", memoryStream2);
                pSARC.AddEntry("Manifests/songs.manifest.json", memoryStream3);
                pSARC.AddEntry($"Exports/Songs/{info.Name}.xblock", memoryStream4);
                pSARC.AddEntry($"Audio/{platform.GetPathName()[0]}/{text}.bnk", memoryStream5);
                pSARC.AddEntry($"Audio/{platform.GetPathName()[0]}/{arg}.ogg", stream3);
                pSARC.AddEntry($"GRAssets/AlbumArt/{manifestBuilder2.AggregateGraph.AlbumArt.Name}.dds", stream);
                foreach (Arrangement arrangement2 in info.Arrangements)
                {
                    if (!File.Exists(arrangement2.SongFile.File) || !File.Exists(arrangement2.SongXml.File))
                    {
                        throw new FileNotFoundException("<ERROR> Can not find required SNG/XML file(s)" + Environment.NewLine);
                    }

                    FileStream fileStream = File.OpenRead(arrangement2.SongXml.File);
                    disposableCollection.Add(fileStream);
                    FileStream fileStream2 = File.OpenRead(arrangement2.SongFile.File);
                    disposableCollection.Add(fileStream2);
                    pSARC.AddEntry($"GR/Behaviors/Songs/{Path.GetFileNameWithoutExtension(arrangement2.SongXml.File)}.xml", fileStream);
                    pSARC.AddEntry($"GRExports/{platform.GetPathName()[1]}/{Path.GetFileNameWithoutExtension(arrangement2.SongFile.File)}.sng", fileStream2);
                }

                pSARC.Write(output);
            }
            catch (Exception ex)
            {
                throw new Exception("<ERROR> GenerateSongPsarcRS1: " + ex.Message + Environment.NewLine);
            }
            finally
            {
                stream?.Dispose();
                stream2?.Dispose();
            }
        }

        private void GeneratePackageList(Stream output, string dlcName)
        {
            StreamWriter streamWriter = new StreamWriter(output);
            streamWriter.WriteLine(dlcName);
            streamWriter.WriteLine("DLC_Tone_{0}", dlcName);
            streamWriter.Flush();
            output.Seek(0L, SeekOrigin.Begin);
        }

        private void GenerateSongPackageId(Stream output, string dlcName)
        {
            StreamWriter streamWriter = new StreamWriter(output);
            streamWriter.Write(dlcName);
            streamWriter.Flush();
            output.Seek(0L, SeekOrigin.Begin);
        }

        private void GenerateTonePsarc(Stream output, string toneKey, RocksmithToolkitLib.DLCPackage.Manifest.Tone.Tone tone)
        {
            RocksmithToolkitLib.PSARC.PSARC pSARC = new RocksmithToolkitLib.PSARC.PSARC();
            MemoryStream memoryStream4 = new MemoryStream();
            MemoryStream memoryStream = new MemoryStream();
            MemoryStream memoryStream2 = new MemoryStream();
            MemoryStream memoryStream3 = new MemoryStream();
            ToneGenerator.Generate(toneKey, tone, memoryStream, memoryStream2, memoryStream3);
            GenerateTonePackageId(memoryStream4, toneKey);
            pSARC.AddEntry($"Exports/Pedals/DLC_Tone_{toneKey}.xblock", memoryStream2);
            int num = tone.PedalList.Where((KeyValuePair<string, Pedal> pedal) => pedal.Value.PedalKey.ToLower().Contains("bass")).Count();
            pSARC.AddEntry((num > 0) ? "Manifests/tone_bass.manifest.json" : "Manifests/tone.manifest.json", memoryStream);
            pSARC.AddEntry("AggregateGraph.nt", memoryStream3);
            pSARC.AddEntry("PACKAGE_ID", memoryStream4);
            pSARC.Write(output);
            memoryStream4.Dispose();
            memoryStream.Dispose();
            memoryStream2.Dispose();
            memoryStream3.Dispose();
        }

        private void GenerateTonePackageId(Stream output, string toneKey)
        {
            StreamWriter streamWriter = new StreamWriter(output);
            streamWriter.Write("DLC_Tone_{0}", toneKey);
            streamWriter.Flush();
            output.Seek(0L, SeekOrigin.Begin);
        }

        public void ToDDS(List<DDSConvertedFile> filesToConvert, DLCPackageType dlcType = DLCPackageType.Song)
        {
            string format = null;
            switch (dlcType)
            {
                case DLCPackageType.Song:
                    format = "-file \"{0}\" -output \"{1}\" -prescale {2} {3} -nomipmap -RescaleBox -dxt1a -overwrite -forcewrite";
                    break;
                case DLCPackageType.Lesson:
                    throw new NotImplementedException("Lesson package type not implemented yet :(");
                case DLCPackageType.Inlay:
                    format = "-file \"{0}\" -output \"{1}\" -prescale {2} {3} -quality_highest -max -dxt5 -nomipmap -alpha -overwrite -forcewrite";
                    break;
            }

            foreach (DDSConvertedFile item in filesToConvert)
            {
                GeneralExtension.RunExternalExecutable(ExternalApps.APP_NVDXT, toolkitRootFolder: true, runInBackground: true, waitToFinish: true, string.Format(format, item.sourceFile, item.destinationFile, item.sizeX, item.sizeY));
            }
        }

        public void GenerateToolkitVersion(Stream output, string packageAuthor = null, string packageVersion = null, string packageComment = null, string packageRating = null, string toolkitVersion = null)
        {
            StreamWriter streamWriter = new StreamWriter(output);
            if (string.IsNullOrEmpty(packageAuthor))
            {
                packageAuthor = ConfigRepository.Instance()["general_defaultauthor"];
            }

            if (string.IsNullOrEmpty(toolkitVersion))
            {
                toolkitVersion = ToolkitVersion.RSTKGuiVersion;
            }

            if (!string.IsNullOrEmpty(toolkitVersion))
            {
                streamWriter.WriteLine("Toolkit version: {0}", toolkitVersion);
            }

            if (!string.IsNullOrEmpty(packageAuthor))
            {
                streamWriter.WriteLine("Package Author: {0}", packageAuthor);
            }

            if (!string.IsNullOrEmpty(packageVersion))
            {
                streamWriter.WriteLine("Package Version: {0}", packageVersion);
            }

            if (!string.IsNullOrEmpty(packageRating))
            {
                streamWriter.WriteLine("Package Rating: {0}", packageRating);
            }

            if (!string.IsNullOrEmpty(packageComment))
            {
                streamWriter.Write("Package Comment: {0}", packageComment);
            }

            streamWriter.Flush();
            output.Seek(0L, SeekOrigin.Begin);
        }

        public void GenerateAppId(Stream output, string appId, Platform platform)
        {
            StreamWriter streamWriter = new StreamWriter(output);
            string text = ((platform.version == GameVersion.RS2012) ? "206113" : "248750");
            streamWriter.Write(appId ?? text);
            streamWriter.Flush();
            output.Seek(0L, SeekOrigin.Begin);
        }

        public void UpdateToneDescriptors(DLCPackageData info)
        {
            foreach (Tone2014 item in info.TonesRS2014)
            {
                if (item == null)
                {
                    continue;
                }

                string text = item.Name.Split('_').Last();
                foreach (ToneDescriptor item2 in ToneDescriptor.List())
                {
                    if (!(item2.ShortName != text))
                    {
                        item.ToneDescriptors.Clear();
                        item.ToneDescriptors.Add(item2.Descriptor);
                        break;
                    }
                }
            }
        }

        public void GenerateSNG(Arrangement arr, Platform platform)
        {
            string text = Path.ChangeExtension(arr.SongXml.File, ".sng");
            switch (platform.version)
            {
                case GameVersion.RS2012:
                    SngFileWriter.Write(arr, text, platform);
                    break;
                case GameVersion.RS2014:
                    {
                        if (arr.Sng2014 == null)
                        {
                            arr.Sng2014 = Sng2014File.ConvertXML(arr.SongXml.File, arr.ArrangementType);
                            if (arr.HasCustomFont)
                            {
                                arr.Sng2014.PopFontPath(dlcName);
                                GlyphDefinitions.WriteToSng(arr.Sng2014, arr.GlyphsXmlPath);
                            }
                        }

                        using (FileStream output = new FileStream(text, FileMode.Create))
                        {
                            arr.Sng2014.WriteSng(output, platform);
                        }

                        break;
                    }
                default:
                    throw new InvalidOperationException("Unexpected game version value");
            }

            if (arr.SongFile == null)
            {
                arr.SongFile = new SongFile
                {
                    File = ""
                };
            }

            arr.SongFile.File = Path.GetFullPath(text);
            TMPFILES_SNG.Add(text);
        }


        private void WriteTmpFile(Stream memoryStream, string fileName, Platform platform)
        {
            if (platform.IsConsole)
            {
                string path = ((platform.platform == GamePlatform.XBox360) ? XBOX_WORKDIR : PS3_WORKDIR);
                string text = Path.Combine(path, fileName);
                memoryStream.WriteFile(text);
                switch (platform.platform)
                {
                    case GamePlatform.XBox360:
                        FILES_XBOX.Add(text);
                        break;
                    case GamePlatform.PS3:
                        FILES_PS3.Add(text);
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (packPsarc != null)
                    {
                        packPsarc.Dispose();
                        packPsarc = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
