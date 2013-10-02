using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;


namespace Galerie_Creator
{
    /// <summary>
    /// Das logische Modell der Anwendung.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Alle unterstützten Bildformate.
        /// </summary>
        private static readonly Dictionary<string, ImageFormat> _SupportedFileFormats =
            new Dictionary<string, ImageFormat>( StringComparer.InvariantCultureIgnoreCase ) 
            { 
                { ".jpg", ImageFormat.Jpeg },
                { ".jpeg", ImageFormat.Jpeg },
                { ".bmp", ImageFormat.Bmp },
                { ".gif", ImageFormat.Gif },
                { ".png", ImageFormat.Png },
            };

        /// <summary>
        /// Enthält alle notwendigen Informationen für das Erstellen einer neuen
        /// Web Galerie.
        /// </summary>
        private class OperationContext
        {
            /// <summary>
            /// Der Name des Unterverzeichnisses für die Bilder.
            /// </summary>
            private const string _Pictures = "pics";

            /// <summary>
            /// Der Name des Unterverzeichnisses für die Stilbeschreibung.
            /// </summary>
            private const string _Styles = "content";

            /// <summary>
            /// Der Name des Unterverzeichnisses für den Programmcode.
            /// </summary>
            private const string _Code = "scripts";

            /// <summary>
            /// Das Wurzelverzeichnis für die neue Galerie.
            /// </summary>
            private readonly string m_targetRoot;

            /// <summary>
            /// Das Wurzelverzeichnis mit den Bildern.
            /// </summary>
            private readonly string m_pictureRoot;

            /// <summary>
            /// Das Referenzverzeichnis.
            /// </summary>
            private readonly string m_refRoot;

            /// <summary>
            /// Das Differenzverzeichnis.
            /// </summary>
            private readonly string m_deltaRoot;

            /// <summary>
            /// Der Name der Galerie.
            /// </summary>
            public string Title { get; private set; }

            /// <summary>
            /// Erstellt eine neue Umgebung.
            /// </summary>
            /// <param name="model">Die zugehörigen Anwendungsdaten.</param>
            public OperationContext( ViewModel model )
            {
                // Read out
                m_pictureRoot = model.PictureDirectory;
                m_refRoot = model.ReferenceDirectory;
                m_targetRoot = model.WebDirectory;
                m_deltaRoot = model.DeltaDirectory;
                Title = model.Title;

                // Check mode
                if (string.IsNullOrEmpty( m_refRoot ))
                    m_deltaRoot = null;
                else if (!Directory.Exists( m_refRoot ))
                    m_deltaRoot = null;
                else if (!string.IsNullOrEmpty( m_deltaRoot ))
                    Directory.CreateDirectory( m_deltaRoot );

                // Create all
                Directory.CreateDirectory( m_targetRoot );
                Directory.CreateDirectory( Path.Combine( m_targetRoot, _Pictures ) );
                Directory.CreateDirectory( Path.Combine( m_targetRoot, _Styles ) );
                Directory.CreateDirectory( Path.Combine( m_targetRoot, _Code ) );
            }

            /// <summary>
            /// Meldet alle Bilddateien.
            /// </summary>
            public IEnumerable<string> PictureFiles { get { return Directory.EnumerateFiles( m_pictureRoot ); } }

            /// <summary>
            /// Legt eine neue Codedatei an.
            /// </summary>
            /// <param name="fileName">Der Name der Datei.</param>
            /// <param name="fileContents">Der Inhalt der Datei.</param>
            public void WriteScript( string fileName, byte[] fileContents )
            {
                // Forward
                WriteFile( _Code, fileName, fileContents );
            }

            /// <summary>
            /// Legt eine neue Codedatei an.
            /// </summary>
            /// <param name="fileName">Der Name der Datei.</param>
            /// <param name="fileContents">Der Inhalt der Datei.</param>
            public void WriteStyle( string fileName, byte[] fileContents )
            {
                // Forward
                WriteFile( _Styles, fileName, fileContents );
            }

            /// <summary>
            /// Legt eine neue HTML Datei an.
            /// </summary>
            /// <param name="fileName">Der Name der Datei.</param>
            /// <param name="fileContents">Der Inhalt der Datei.</param>
            public void WritePage( string fileName, byte[] fileContents )
            {
                // Forward
                WriteFile( string.Empty, fileName, fileContents );
            }

            /// <summary>
            /// Legt eine neue Steuerdatei an.
            /// </summary>
            /// <param name="fileName">Der Name der Datei.</param>
            /// <param name="fileContents">Der Inhalt der Datei.</param>
            public void WritePicture( string fileName, byte[] fileContents )
            {
                // Forward
                WriteFile( _Pictures, fileName, fileContents );
            }

            /// <summary>
            /// Legt eine neue Datei an.
            /// </summary>
            /// <param name="directory">Das Verzeichnis, in dem die Datei angelegt werden soll.</param>
            /// <param name="fileName">Der Name der Datei.</param>
            /// <param name="fileContents">Der Inhalt der Datei.</param>
            private void WriteFile( string directory, string fileName, byte[] fileContents )
            {
                // Process
                File.WriteAllBytes( Path.Combine( Path.Combine( m_targetRoot, directory ), fileName ), fileContents );

                // See if we do delta processing
                if (string.IsNullOrEmpty( m_deltaRoot ))
                    return;

                // Get paths
                var refPath = Path.Combine( Path.Combine( m_refRoot, directory ), fileName );
                var deltaDir = Path.Combine( m_deltaRoot, directory );
                var deltaPath = Path.Combine( deltaDir, fileName );

                // No such file
                if (!File.Exists( refPath ))
                    return;

                // Load the old stuff
                var refData = File.ReadAllBytes( refPath );

                // Compare
                if (refData.Length == fileContents.Length)
                    if (Enumerable.Range( 0, refData.Length ).All( i => refData[i] == fileContents[i] ))
                    {
                        // May want to clean up
                        if (File.Exists( deltaPath ))
                            File.Delete( deltaPath );

                        // Skip
                        return;
                    }

                // Create path and store
                Directory.CreateDirectory( deltaDir );
                File.WriteAllBytes( deltaPath, fileContents );
            }
        }

        /// <summary>
        /// Der volle Pfad zum Quellverzeichnis.
        /// </summary>
        public string PictureDirectory
        {
            get
            {
                // Report
                return Properties.Settings.Default.PictureDirectory;
            }
            set
            {
                // Test
                if (StringComparer.Ordinal.Equals( PictureDirectory, value ))
                    return;

                // Remember
                Properties.Settings.Default.PictureDirectory = value;

                // Persist
                Properties.Settings.Default.Save();

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Der volle Pfad zum Zielverzeichnis.
        /// </summary>
        public string WebDirectory
        {
            get
            {
                // Report
                return Properties.Settings.Default.WebDirectory;
            }
            set
            {
                // Test
                if (StringComparer.Ordinal.Equals( WebDirectory, value ))
                    return;

                // Remember
                Properties.Settings.Default.WebDirectory = value;

                // Persist
                Properties.Settings.Default.Save();

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Der volle Pfad zum Referenzverzeichnis.
        /// </summary>
        public string ReferenceDirectory
        {
            get
            {
                // Report
                return Properties.Settings.Default.ReferenceDirectory;
            }
            set
            {
                // Test
                if (StringComparer.Ordinal.Equals( ReferenceDirectory, value ))
                    return;

                // Remember
                Properties.Settings.Default.ReferenceDirectory = value;

                // Persist
                Properties.Settings.Default.Save();

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Der volle Pfad zum Differenzverzeichnis.
        /// </summary>
        public string DeltaDirectory
        {
            get
            {
                // Report
                return Properties.Settings.Default.DeltaDirectory;
            }
            set
            {
                // Test
                if (StringComparer.Ordinal.Equals( DeltaDirectory, value ))
                    return;

                // Remember
                Properties.Settings.Default.DeltaDirectory = value;

                // Persist
                Properties.Settings.Default.Save();

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Die zu verwendende Überschrift.
        /// </summary>
        public string Title
        {
            get
            {
                // Report
                return Properties.Settings.Default.Title;
            }
            set
            {
                // Test
                if (StringComparer.Ordinal.Equals( Title, value ))
                    return;

                // Remember
                Properties.Settings.Default.Title = value;

                // Persist
                Properties.Settings.Default.Save();

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Die aktuelle Fortschrittsanzeige.
        /// </summary>
        private int m_progress = 0;

        /// <summary>
        /// Die aktuelle Fortschrittsanzeige.
        /// </summary>
        public int Progress
        {
            get
            {
                // Report
                return m_progress;
            }
            set
            {
                // Test
                if (m_progress == value)
                    return;

                // Remember
                m_progress = value;

                // Report
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Wird ausgelöst, wenn sich eine Eigenschaft verändert hat.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Meldet, dass sich eine Eigenschaft verändert hat.
        /// </summary>
        /// <param name="propertyName"></param>
        private void FirePropertyChanged( [CallerMemberName] string propertyName = null )
        {
            // Fire
            var sink = PropertyChanged;
            if (sink != null)
                sink( this, new PropertyChangedEventArgs( propertyName ) );

            // May change commands
            m_process.FireCanExecuteChanged();
        }

        /// <summary>
        /// Ein Befehl zur Veränderung des Bilderverzeichnisses.
        /// </summary>
        public ICommand SelectPictureDirectory { get; private set; }

        /// <summary>
        /// Ein Befehl zur Veränderung des Zielverzeichnisses.
        /// </summary>
        public ICommand SelectWebDirectory { get; private set; }

        /// <summary>
        /// Ein Befehl zur Veränderung des Differenzverzeichnisses.
        /// </summary>
        public ICommand SelectDeltaDirectory { get; private set; }

        /// <summary>
        /// Ein Befehl zur Veränderung des Referenzverzeichnisses.
        /// </summary>
        public ICommand SelectReferenceDirectory { get; private set; }

        /// <summary>
        /// Erstellt die Galerie.
        /// </summary>
        private CommandProxy m_process;

        /// <summary>
        /// Erstellt die Galerie.
        /// </summary>
        public ICommand Process { get { return m_process; } }

        /// <summary>
        /// Führt die eigentliche Verarbeitung aus.
        /// </summary>
        private readonly BackgroundWorker m_worker = new BackgroundWorker { WorkerReportsProgress = true };

        /// <summary>
        /// Erstellt ein neue Datenverwaltung.
        /// </summary>
        public ViewModel()
        {
            // Set commands
            SelectReferenceDirectory = new CommandProxy( OnSetReferencePath );
            SelectPictureDirectory = new CommandProxy( OnSetSourcePath );
            SelectDeltaDirectory = new CommandProxy( OnSetDeltaPath );
            SelectWebDirectory = new CommandProxy( OnSetTargetPath );
            m_process = new CommandProxy( OnProcess, TestProcess );

            // Configure worker
            m_worker.RunWorkerCompleted += ProcessFinished;
            m_worker.ProgressChanged += ReportProgress;
            m_worker.DoWork += CreateWebDirectory;
        }

        /// <summary>
        /// Wird aufgerufen, wenn sich der Fortschritt verändert hat.
        /// </summary>
        /// <param name="sender">Wird ignoriert.</param>
        /// <param name="e">Die aktuelle Fortschrittsanzeige.</param>
        void ReportProgress( object sender, ProgressChangedEventArgs e )
        {
            // Change 
            Progress = e.ProgressPercentage;
        }

        /// <summary>
        /// Erstellt das Zielverzeichnis.
        /// </summary>
        /// <param name="sender">Die Steuereinheit.</param>
        /// <param name="e">Enthält die Arbeitsumgebung.</param>
        void CreateWebDirectory( object sender, DoWorkEventArgs e )
        {
            // Attach to environment
            var context = (OperationContext) e.Argument;
            var worker = (BackgroundWorker) sender;

            // Copy app
            context.WriteScript( "jquery-2.0.3.min.js", Properties.Files.jQuery );
            context.WriteScript( "galerie.js", Properties.Files.ScriptCode );
            context.WriteStyle( "galerie.css", Properties.Files.StyleSheet );
            context.WritePage( "index.html", Properties.Files.HomePage );

            // List of all file names
            var pictureFiles =
                context
                    .PictureFiles
                    .Where( path => _SupportedFileFormats.ContainsKey( Path.GetExtension( path ) ) )
                    .ToList();

            // Start progress
            worker.ReportProgress( 0 );

            // Sort
            pictureFiles.Sort( StringComparer.InvariantCultureIgnoreCase );

            // Configuration
            var config = new StringBuilder();

            // Starter
            config.AppendFormat( "{{\"title\":\"{0}\",\"images\":[", context.Title.Replace( "\"", "\\\"" ) );

            // Pictures
            config.Append( string.Join( ",", pictureFiles.Select( path => string.Format( "\"{0}\"", Path.GetFileName( path ).Replace( "\"", "\\\"" ) ) ) ) );

            // Trailer
            config.Append( "]}" );

            // Store
            context.WritePicture( "galerie.txt", Encoding.UTF8.GetBytes( config.ToString() ) );

            // Process all source files
            for (var i = 0; i < pictureFiles.Count; i++)
            {
                // Report
                worker.ReportProgress( (int) Math.Truncate( 100m * i / pictureFiles.Count ) );

                // Target name
                var sourcePath = pictureFiles[i];
                var pictureFile = Path.GetFileName( sourcePath );
                var source = File.ReadAllBytes( sourcePath );

                // Copy over
                context.WritePicture( pictureFile, source );

                // Load as image
                using (var original = new MemoryStream( source, false ))
                using (var image = Image.FromStream( original ))
                {
                    // For now...
                    const decimal thumbMax = 200m;

                    // Get bounds
                    var width = (decimal) image.Width;
                    var height = (decimal) image.Height;

                    // Get scale
                    if (width > height)
                    {
                        // Scale
                        height = thumbMax * height / width;
                        width = thumbMax;
                    }
                    else
                    {
                        // Scale
                        width = thumbMax * width / height;
                        height = thumbMax;
                    }

                    // Create new bitmap
                    using (var thumb = new Bitmap( (int) Math.Round( width ), (int) Math.Round( height ) ))
                    {
                        // Scale
                        using (var g = Graphics.FromImage( thumb ))
                            g.DrawImage( image, 0, 0, thumb.Width, thumb.Height );

                        // Store
                        using (var scratch = new MemoryStream())
                        {
                            // Write to buffer
                            thumb.Save( scratch, _SupportedFileFormats[Path.GetExtension( pictureFile )] );

                            // Write to disk
                            context.WritePicture( "tn_" + pictureFile, scratch.ToArray() );
                        }
                    }
                }
            }

            // Done
            worker.ReportProgress( 100 );
        }

        /// <summary>
        /// Wird aufgerufen, sobald die Erstellung beendet wurde.
        /// </summary>
        /// <param name="sender">Wird ignoriert.</param>
        /// <param name="e">Informationen zur Ausführung.</param>
        void ProcessFinished( object sender, RunWorkerCompletedEventArgs e )
        {
            // Just refresh
            m_process.FireCanExecuteChanged();
        }

        /// <summary>
        /// Fordert eine neues Zielverzeichnis an.
        /// </summary>
        private void OnSetTargetPath()
        {
            // Attach to dialog
            using (var dir = new FolderBrowserDialog())
            {
                // Configure
                dir.SelectedPath = WebDirectory;
                dir.ShowNewFolderButton = true;
                dir.Description = "Bitte das Zielverzeichnis auswählen";

                // Process
                if (dir.ShowDialog() == DialogResult.OK)
                    WebDirectory = dir.SelectedPath;
            }
        }

        /// <summary>
        /// Fordert eine neues Differenzverzeichnis an.
        /// </summary>
        private void OnSetDeltaPath()
        {
            // Attach to dialog
            using (var dir = new FolderBrowserDialog())
            {
                // Configure
                dir.SelectedPath = DeltaDirectory;
                dir.ShowNewFolderButton = true;
                dir.Description = "Bitte das Differenzverzeichnis auswählen";

                // Process
                if (dir.ShowDialog() == DialogResult.OK)
                    DeltaDirectory = dir.SelectedPath;
            }
        }

        /// <summary>
        /// Fordert eine neues Bilderverzeichnis an.
        /// </summary>
        private void OnSetSourcePath()
        {
            // Attach to dialog
            using (var dir = new FolderBrowserDialog())
            {
                // Configure
                dir.SelectedPath = PictureDirectory;
                dir.ShowNewFolderButton = false;
                dir.Description = "Bitte das Bilderverzeichnis auswählen";

                // Process
                if (dir.ShowDialog() == DialogResult.OK)
                    PictureDirectory = dir.SelectedPath;
            }
        }

        /// <summary>
        /// Fordert eine neues Referenzverzeichnis an.
        /// </summary>
        private void OnSetReferencePath()
        {
            // Attach to dialog
            using (var dir = new FolderBrowserDialog())
            {
                // Configure
                dir.SelectedPath = ReferenceDirectory;
                dir.ShowNewFolderButton = false;
                dir.Description = "Bitte das Referenzverzeichnis auswählen";

                // Process
                if (dir.ShowDialog() == DialogResult.OK)
                    ReferenceDirectory = dir.SelectedPath;
            }
        }

        /// <summary>
        /// Meldet, ob eine Ausführung möglich ist.
        /// </summary>
        /// <returns>Gesetzt, wenn eine Ausführung möglich ist.</returns>
        private bool TestProcess()
        {
            // Busy
            if (m_worker.IsBusy)
                return false;

            // Check source path
            if (string.IsNullOrEmpty( PictureDirectory ))
                return false;
            if (!Directory.Exists( PictureDirectory ))
                return false;

            // Check target path
            if (string.IsNullOrEmpty( WebDirectory ))
                return false;

            // Check title
            if (string.IsNullOrEmpty( Title ))
                return false;

            // Yepp
            return true;
        }

        /// <summary>
        /// Führt die Analyse aus.
        /// </summary>
        private void OnProcess()
        {
            // Start worker
            m_worker.RunWorkerAsync( new OperationContext( this ) );

            // Just refresh
            m_process.FireCanExecuteChanged();
        }
    }
}
