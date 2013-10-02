using System;
using System.Windows.Input;


namespace Galerie_Creator
{
    /// <summary>
    /// Ein einfacher Befehl.
    /// </summary>
    public class CommandProxy : ICommand
    {
        /// <summary>
        /// Der auszuführende Code.
        /// </summary>
        private readonly Action m_action;

        /// <summary>
        /// Methode zur Prüfung, ob eine Ausführung möglich ist.
        /// </summary>
        private readonly Func<bool> m_canExecute;

        /// <summary>
        /// Erstellt einen neuen Befehl.
        /// </summary>
        /// <param name="action">Der auszuführende Code.</param>
        /// <param name="canExecute">Prüft, ob eine Ausführung möglich ist.</param>
        public CommandProxy( Action action, Func<bool> canExecute = null )
        {
            // Remember
            m_canExecute = canExecute;
            m_action = action;
        }

        /// <summary>
        /// Meldet, dass sich der Ausführungsstatus geändert hat.
        /// </summary>
        public void FireCanExecuteChanged()
        {
            // Fire
            var sink = CanExecuteChanged;
            if (sink != null)
                sink( this, EventArgs.Empty );
        }

        /// <summary>
        /// Meldet den Ausführungsstatus.
        /// </summary>
        /// <param name="parameter">Ein beliebiger Parameter.</param>
        /// <returns>Gesetzt, wenn der Befhl ausgeführt werden kann.</returns>
        public bool CanExecute( object parameter )
        {
            // Report
            return (m_canExecute == null) || m_canExecute();
        }

        /// <summary>
        /// Wird ausgelöst, wenn sich der Ausführungsstatus verändert hat.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Führt den Befehl aus.
        /// </summary>
        /// <param name="parameter">Ein freier Parameter des Befehls.</param>
        public void Execute( object parameter )
        {
            // Process
            if (CanExecute( parameter ))
                m_action();
        }
    }
}
