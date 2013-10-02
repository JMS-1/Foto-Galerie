using System.Windows;


namespace Galerie_Creator
{
    /// <summary>
    /// Das Hauptfenster der Anwendung.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Erstellt ein neues Fenster.
        /// </summary>
        public MainWindow()
        {
            // Load designer stuff (XAML)
            InitializeComponent();

            // Attach model
            DataContext = new ViewModel();
        }
    }
}
