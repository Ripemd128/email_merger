using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailMerge
{ 
    public class Person: IEqualityComparer<Person>
    {
        public string Nome;
        public string Cognome;
        public string Email;
        public string Paese;

        public bool Equals(Person x, Person y)
        {
            return x.Email.Equals(y.Email);
        }

        public int GetHashCode(Person obj)
        {
            return Email.GetHashCode();
        }

        public override string ToString()
        {
            return Email + ";" + ";" + Nome + ";" + Cognome + ";" + ";" + ";" + ";" + ";" + ";" + ";" + ";" + ((Paese.Length > 2) ? "" : Paese);
        }
    }

    public partial class Form1 : Form
    {
        Dictionary<string, Dictionary<string,List<Person>>> csvFiles;
        OpenFileDialog csvdir;

        public Form1()
        {
            InitializeComponent();
            csvFiles = new Dictionary<string, Dictionary<string, List<Person>>>();
            csvdir = new OpenFileDialog();          
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                csvdir.Filter = "Csv files (.csv)|*.csv";
                csvdir.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                csvdir.Multiselect = true;
                csvdir.Title = "Select csv files...";

                if (csvdir.ShowDialog() == DialogResult.OK)
                {
                    if (csvdir.FileNames.Count() == 0)
                        return;

                    progressBar1.Minimum = 0;
                    progressBar1.Value = 0;
                    progressBar1.Maximum = csvdir.FileNames.Count();
                    progressBar1.Step = 1;

                    await LoadFileAsync(csvdir.FileNames);

                    button2.Enabled = csvFiles.Count > 0;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"Errore",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private async Task LoadFileAsync(string[] files)
        {
            await Task.Run(() => 
            {
                //Foreach file selected
                foreach (var file in files)
                {
                    using (StreamReader fr = new StreamReader(file))
                    {
                        string[] header = fr.ReadLine().Split(',', ';');
                        string[] line;
                        List<Person> contatti = new List<Person>();
                        Dictionary<string,List<Person>> rubrica = new Dictionary<string, List<Person>>();

                        int PosNome = (header.Any(el => el.ToLowerInvariant().Trim('"').Equals("nome"))) ? Array.IndexOf(header, "Nome") : -1;
                        int PosCognome = (header.Any(el => el.ToLowerInvariant().Trim('"').Equals("cognome"))) ? Array.IndexOf(header, "Cognome") : -1;
                        int PosEmail = (header.Any(el => el.ToLowerInvariant().Contains("mail") || el.ToLowerInvariant().Contains("posta")))
                            ? Array.IndexOf(header, header.First(el => el.ToLowerInvariant().Equals("specifica") || el.ToLowerInvariant().Contains("posta") || el.ToLowerInvariant().Contains("email")))
                            : -1;

                        int PosNazione = (header.Any(el => el.ToLowerInvariant().Contains("nazione")))
                            ? Array.IndexOf(header, header.First(el => el.ToLowerInvariant().Contains("nazione")))
                            : -1;

                        //file gestionale in lingua
                        if (header[0].ToLowerInvariant() == "idnazione")
                        {
                            while ((line = fr.ReadLine()?.Split(';')) != null)
                            {
                                if (line.Length == header.Length)
                                {
                                    if (line[PosEmail].Contains('@') && line[PosEmail].Contains('.'))
                                    {
                                        contatti.Add(new Person()
                                        {
                                            Paese = (PosNazione >= 0) ? line[PosNazione].Trim('"').Trim()  : "",
                                            Nome = (PosNome >= 0) ? line[PosNome].Trim('"') : "",
                                            Cognome = (PosCognome >= 0) ? line[PosCognome].Trim('"') : "",
                                            Email = (PosEmail >= 0) ? line[PosEmail].Trim('"') : "",
                                        });
                                    }
                                }
                            }

                            if(file.ToLower().Contains("italiano"))
                                rubrica.Add("it", contatti.Distinct().ToList());
                            else if (file.ToLower().Contains("francese"))
                                rubrica.Add("fr", contatti.Distinct().ToList());
                            else if (file.ToLower().Contains("tedesco"))
                                rubrica.Add("de", contatti.Distinct().ToList());
                            else
                                rubrica.Add("all", contatti.Distinct().ToList());
                        }
                        else//tutti gli altri file
                        {
                            while ((line = fr.ReadLine()?.Split(',')) != null)
                            {
                                if (line.Length == header.Length)
                                {
                                    if (line[PosEmail].Contains('@') && line[PosEmail].Contains('.') && !line[PosEmail].ToLower().EndsWith("guest.booking.com"))
                                    {
                                        contatti.Add(new Person()
                                        {
                                            Paese = (PosNazione >= 0) ? line[PosNazione].Trim('"').Trim() : "",
                                            Nome = (PosNome >= 0) ? line[PosNome].Trim('"') : "",
                                            Cognome = (PosCognome >= 0) ? line[PosCognome].Trim('"') : "",
                                            Email = (PosEmail >= 0) ? line[PosEmail].Trim('"') : "",
                                        });
                                    }
                                }
                            }

                            rubrica.Add("it", contatti.Where(x => x.Paese.ToUpper() == "I" || x.Paese.ToUpper() == "IT").Distinct().ToList());
                            rubrica.Add("fr", contatti.Where(x => x.Paese.ToUpper() == "F" || x.Paese.ToUpper() == "FR").Distinct().ToList());
                            rubrica.Add("de", contatti.Where(x => x.Paese.ToUpper() == "D" || x.Paese.ToUpper() == "DE" || x.Paese.ToUpper() == "A" || x.Paese.ToUpper() == "AT").Distinct().ToList());
                            rubrica.Add("all", contatti.Except(rubrica["it"].Union(rubrica["fr"]).Union(rubrica["de"]).ToList()).ToList());
                        }

                        csvFiles.Add(Path.GetFileName(file), rubrica);

                        BeginInvoke(new Action(() => 
                        {
                            dataGridView1.Rows.Add(new object[] { Path.GetFileName(file), contatti?.Count() });
                        }));
                    }

                    BeginInvoke(new Action(() =>
                    {
                        progressBar1.PerformStep();
                        Application.DoEvents();
                    }));  
                }
            });
           
        }

        private void button2_Click(object sender, EventArgs e)
        {

            List<Person> lista_it = new List<Person>();
            List<Person> lista_fr = new List<Person>();
            List<Person> lista_de = new List<Person>();
            List<Person> lista_all = new List<Person>();

            //Ciclo sul dizionario contenente i file e accumulo i dizionari delle 4 lingue
            foreach (var item in csvFiles)
            {
                if (item.Value.TryGetValue("it", out List<Person> it))
                    lista_it = lista_it.Union(it).ToList();

                if (item.Value.TryGetValue("fr", out List<Person> fr))
                    lista_fr = lista_fr.Union(fr).ToList();

                if (item.Value.TryGetValue("de", out List<Person> de))
                    lista_de = lista_de.Union(de).ToList();

                if (item.Value.TryGetValue("all", out List<Person> all))
                    lista_all = lista_all.Union(all).ToList();
            }

            StampaRubrica(ref lista_it, ref lista_fr, ref lista_de, ref lista_all);

            MessageBox.Show("Ok file stampati!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StampaRubrica(ref List<Person> it, ref List<Person> fr, ref List<Person> de, ref List<Person> all)
        {
            string header = "Email;Azienda;Nome;Cognome;Lingua;DataInizioSoggiono;DataFineSoggiorno;Soddisfazione;Ospiti;Bambini;Citta;Paese";

            //Crea directory splitted
            DirectoryInfo splitted = new DirectoryInfo(Path.GetDirectoryName(csvdir.FileNames?.First()) + @"\Splitted\");
            if (!splitted.Exists)
                splitted.Create();

            //it
            using (StreamWriter fw = new StreamWriter(Path.Combine(splitted.FullName, $"Email_it_{it.Count()}.csv")))
            {
                fw.WriteLine(header);
                it.ForEach( person => fw.WriteLine(person.ToString()));
            }

            //fr
            using (StreamWriter fw = new StreamWriter(Path.Combine(splitted.FullName, $"Email_fr_{fr.Count()}.csv")))
            {
                fw.WriteLine(header);
                fr.ForEach(person => fw.WriteLine(person.ToString()));
            }

            //de
            using (StreamWriter fw = new StreamWriter(Path.Combine(splitted.FullName, $"Email_de_{de.Count()}.csv")))
            {
                fw.WriteLine(header);
                de.ForEach(person => fw.WriteLine(person.ToString()));
            }

            //all
            using (StreamWriter fw = new StreamWriter(Path.Combine(splitted.FullName, $"Email_all_{all.Count()}.csv")))
            {
                fw.WriteLine(header);
                all.ForEach(person => fw.WriteLine(person.ToString()));
            }
        }
    }
}
