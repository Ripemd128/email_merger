using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailMerge
{ 
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
                files.ToList().ForEach(item => 
                {
                    using (CsvReader csv = new CsvReader(new StreamReader(item.ToString())))
                    {
                        Dictionary<string, List<Person>> rubrica = new Dictionary<string, List<Person>>();
                        List<Person> records = new List<Person>();

                        csv.Configuration.MissingFieldFound = null;
                        csv.Configuration.ReadingExceptionOccurred = null;
                        csv.Configuration.BadDataFound = null;
                        csv.Configuration.RegisterClassMap<PersonMap>();
                        csv.Read();
                        csv.ReadHeader();
                        //Tutti i file csv...
                        records = csv.GetRecords<Person>().ToList();
                        //...solo contatti gestionale
                        if (records?.Count == 0)
                        {
                            using (CsvReader csvbis = new CsvReader(new StreamReader(item.ToString())))
                            {
                                csvbis.Configuration.Delimiter = ";";
                                csvbis.Configuration.MissingFieldFound = null;
                                csvbis.Configuration.RegisterClassMap<PersonMap>();
                                csvbis.Read();
                                csvbis.ReadHeader();
                                records = csvbis.GetRecords<Person>().ToList();

                                //Aggiungo tutte le liste divise per lingua
                                if (item.ToLower().Contains("italiano"))
                                    rubrica.Add("it", records.Distinct().ToList());
                                else if (item.ToLower().Contains("francese"))
                                    rubrica.Add("fr", records.Distinct().ToList());
                                else if (item.ToLower().Contains("tedesco"))
                                    rubrica.Add("de", records.Distinct().ToList());
                                else
                                    rubrica.Add("all", records.Distinct().ToList());
                            }
                        }
                        else
                        {

                            List<Person> contatti = records.Where(x => !string.IsNullOrEmpty(x.Email)).Where(x => !x.Email.EndsWith("guest.booking.com")).ToList();

                            //Aggiungo le liste divise per lingua it-IT
                            {
                                var tmp_IT = contatti.GroupBy(x => x.Paese).Where(x => x.Key == "I" || x.Key == "IT").SelectMany(x => x).Distinct();
                                var tmp_end_it = contatti.Where(x => x.Email.EndsWith(".it"));
                                rubrica.Add("it", tmp_IT.Union(tmp_end_it).Distinct().ToList());
                            }

                            //Aggiungo le liste divise per lingua fr-FR
                            {
                                var tmp_FR = contatti.GroupBy(x => x.Paese).Where(x => x.Key == "F" || x.Key == "FR").SelectMany(x => x).Distinct();
                                var tmp_end_fr = contatti.Where(x => x.Email.EndsWith(".fr"));
                                rubrica.Add("fr", tmp_FR.Union(tmp_end_fr).Distinct().ToList());
                            }

                            //Aggiungo le liste divise per lingua de-DE e at-AT
                            {
                                var tmp_DE = contatti.GroupBy(x => x.Paese).Where(x => x.Key == "D" || x.Key == "DE" || x.Key == "A" || x.Key == "AT").SelectMany(x => x).Distinct();
                                var tmp_end_de = contatti.Where(x => x.Email.EndsWith(".de") || x.Email.EndsWith(".at"));
                                rubrica.Add("de", tmp_DE.Union(tmp_end_de).Distinct().ToList());
                            }

                            //Per la lista totale tolgo tutte le liste create precedentemente
                            rubrica.Add("all", contatti.Except(rubrica["it"].Union(rubrica["fr"]).Union(rubrica["de"])).Distinct().ToList());
                        }

                        csvFiles.Add(Path.GetFileName(item), rubrica);
                        BeginInvoke(new Action(() =>
                        {
                            dataGridView1.Rows.Add(new object[] { Path.GetFileName(item), records?.Count() });
                        }));
                    }

                    BeginInvoke(new Action(() =>
                    {
                        progressBar1.PerformStep();
                        Application.DoEvents();
                    }));
                });
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
