using System;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Data;
using BarangayVillaMTejeroSystem.Forms;

namespace BarangayVillaMTejeroSystem
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            try
            {
                // Creates Data\barangay.db (schema + seed data) on first run,
                // and is a no-op on every run after that.
                DatabaseHelper.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not set up the database:\n\n{ex.Message}",
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new LoginForm());
        }
    }
}
