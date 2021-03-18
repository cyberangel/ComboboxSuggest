using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class ComboboxSuggest : Form
    {
        private readonly System.Timers.Timer _keypressTimer;
        private delegate void KeyUpCallback();
        private int _delayTime = 500; // načtení návrhů je zpožděno o počet milisekund určených touto vlastností
        private Keys _posledniStisknutaKlavesa;

        public ComboboxSuggest()
        {
            InitializeComponent();

            // nastavit timer stisku klávesy
            _keypressTimer = new System.Timers.Timer();
            _keypressTimer.Elapsed += OnTimedEvent;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            NactiNavrhy("");

            comboBox1.KeyUp += comboBox1_KeyUp;
        }

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            _keypressTimer.Stop();
            Invoke(new KeyUpCallback(KeyUp_Akce));
        }

        private void KeyUp_Akce()
        {
            try
            {
                var cbo = comboBox1;
                cbo.KeyUp -= comboBox1_KeyUp;
                var cboText = cbo.Text;

                NactiNavrhy(cboText);

                // zobrazit uživateli nově odfiltrovaný seznam
                if (cbo.Items.Count != 1
                    && _posledniStisknutaKlavesa != Keys.Enter)
                    cbo.DroppedDown = true; // toto přepíše text v kombu, takže to musíme vrátit zpět

                // binding dat vymaže text, takže musíme vrátit uživatelem zadaný text zpět
                cbo.Text = cboText;
                cbo.SelectionStart = cboText.Length; // vrátíme kurzor zpět, kde byl

                if (cbo.Items.Count == 1)
                    cbo.DroppedDown = false;

                cbo.KeyUp += comboBox1_KeyUp;
            }
            catch { }
        }

        private void comboBox1_KeyUp(object sender, KeyEventArgs e)
        {
            _posledniStisknutaKlavesa = e.KeyCode;

            // ignorovat pohyb v kombu
            if (e.KeyCode == Keys.Down
                || e.KeyCode == Keys.Up
                || e.KeyCode == Keys.Left
                || e.KeyCode == Keys.Right
                || e.Control)
                return;

            // pokud je nastaveno zpoždění, zpozdit handling změněného textu
            if (_delayTime > 0)
            {
                _keypressTimer.Interval = _delayTime;
                _keypressTimer.Start();
            }
            else
                KeyUp_Akce();
        }

        private readonly object _locker = new object();

        private void NactiNavrhy(string zacatek)
        {
            lock (_locker)
            {
                comboBox1.Items.Clear();

                string connStr = ConfigurationManager.ConnectionStrings["WindowsFormsApp1.Properties.Settings.Myconn"].ConnectionString;
                using (var conn = new SqlConnection(connStr))
                {
                    string sql = "SELECT TOP 100 nazev FROM Firmy WHERE nazev LIKE @zacatek+'%' ORDER BY nazev";
                    using (var comm = new SqlCommand(sql,
                                                     conn))
                    {
                        comm.Parameters.AddWithValue("zacatek",
                                                     zacatek);

                        conn.Open();
                        var reader = comm.ExecuteReader();
                        while (reader.Read())
                        {
                            comboBox1.Items.Add(reader["nazev"].ToString());
                        }
                    }

                    conn.Close();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _keypressTimer.Elapsed -= OnTimedEvent;
            comboBox1.KeyUp -= comboBox1_KeyUp;
        }
    }
}
