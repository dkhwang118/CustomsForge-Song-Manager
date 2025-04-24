using System;
using System.IO;
using RocksmithToolkitLib;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.Extensions;

namespace CustomsForgeSongManager.LocalTools 
{
    /// <summary>
    /// This class is used to package and unpack PSARC files.
    /// This is the THREAD SAFE version of the original PsarcPackager class.
    /// </summary>
    public class ThreadSafePsarcPackager : IDisposable
    {
        private string packageDir;

        private bool _deleteOnClose;

        public ThreadSafePsarcPackager(bool deleteOnClose = false)
        {
            _deleteOnClose = deleteOnClose;
        }

        public DLCPackageData ReadPackage(string srcPath, bool fixMultiTone = false, bool fixLowBass = false, bool decodeAudio = false)
        {
            bool decodeAudio2 = decodeAudio;
            packageDir = Packer.Unpack(srcPath, Path.GetTempPath(), null, decodeAudio2);
            Platform platform = srcPath.GetPlatform();
            //DLCPackageData dLCPackageData = null;
            if (platform.version == GameVersion.RS2014)
            {
                packageDir = DLCPackageData.DoLikeProject(packageDir);
                return DLCPackageData.LoadFromFolder(packageDir, platform, platform, fixMultiTone, fixLowBass);
            }

            return DLCPackageData.RS1LoadFromFolder(packageDir, platform, convert: false);
        }

        public void WritePackage(string destPath, DLCPackageData packageData, string srcPath = "")
        {
            Platform platform = ((!string.IsNullOrEmpty(srcPath)) ? srcPath.GetPlatform() : destPath.GetPlatform());
            using (ThreadSafeDLCPackageCreator creator = new ThreadSafeDLCPackageCreator())
            {
                creator.Generate(destPath, packageData, platform);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _deleteOnClose && Directory.Exists(packageDir))
            {
                IOExtension.DeleteDirectory(packageDir);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
