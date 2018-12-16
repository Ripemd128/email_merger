using CsvHelper.Configuration;
using System.Collections.Generic;

namespace EmailMerge
{
    public class Person
    {
        private string email;
        private string paese;
        private string citta;
        private string lingua;

        public string Nome { get; set; }
        public string Cognome { get; set; }

        public string Lingua
        {
            get { return lingua; }
            set
            {
                if (value == null)
                {
                    lingua = string.Empty;
                    return;
                }

                lingua = (value.Length > 2) ? string.Empty : value.ToUpper();
            }
        }
        public string Email
        {
            get { return email; }
            set { email = value.ToLower();}
        }

        public string Citta
        {
            get { return citta; }
            set
            {
                if (value == null)
                {
                    citta = string.Empty;
                    return;
                }

                citta = value;
            }
        }

        public string Paese
        {
            get { return paese; }
            set
            {
                if (value == null)
                {
                    paese = string.Empty;
                    return;
                }

                switch(value.Length)
                {
                    case 1:
                        switch(value)
                        {
                            case "A":
                                paese = "AT";
                                break;
                            case "D":
                                paese = "DE";
                                break;
                            case "B":
                                paese = "BE";
                                break;
                            case "H":
                                paese = "HU";
                                break;
                            case "I":
                                paese = "IT";
                                break;
                            case "F":
                                paese = "FR";
                                break;
                            case "E":
                                paese = "ES";
                                break;
                            case "R":
                                paese = "R0";
                                break;

                        }
                        break;
                    case int l when l >= 3:
                        switch (value)
                        {
                            case "ROS":
                                paese = "RU";
                                break;
                            case "MCO":
                                paese = "MC";
                                break;
                            case "USA":
                                paese = "US";
                                break;
                            default:
                                paese = string.Empty;
                                break;
                        }
                        break;
                    default:
                        paese = (value.Length > 2) ? string.Empty : value.ToUpper();
                        break;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return (obj as Person).Email.Equals(Email);
        }

        public override int GetHashCode()
        {
            return Email.GetHashCode();
        }

        public override string ToString()
        {
            return (Email + ";" + ";" + Nome + ";" + Cognome + ";" + Lingua + ";" + ";" + ";" + ";" + ";" + ";" + Citta + ";" + Paese);
        }
    }

    public sealed class PersonMap : ClassMap<Person>
    {
        public PersonMap()
        {      
            Map(m => m.Nome).Name("Nome", "Name");
            Map(m => m.Cognome).Name("Cognome", "Surname");
            Map(m => m.Lingua).Name("Lang", "Lingua");
            Map(m => m.Email).Name("Email", "Indirizzo posta elettronica", "Mail","MAIL");
            Map(m => m.Citta).Name("Citta");
            Map(m => m.Paese).Name("Nazione", "IDNazione", "Country");
        }
    }


}
