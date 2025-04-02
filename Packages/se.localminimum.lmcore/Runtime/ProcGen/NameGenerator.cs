using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.ProcGen
{
    public static class NameGenerator
    {
        private static string GetWeightedChoice(string[] options, int[] weights)
        {
            var weightedOptions = options.Zip(weights, (l, w) => Enumerable.Repeat(l, w)).SelectMany(l => l).ToArray();
            return weightedOptions[Random.Range(0, weightedOptions.Length)];
        }

        #region Language

        private static string[] language = new string[]{
        "Swedish",
        "English",
        "German",
        "Spanish",
    };

        public static string GetRandomLanguage()
        {
            return language[Random.Range(0, language.Length)];
        }

        public static string GetWeightedRandomLanguage()
        {
            return GetWeightedRandomLanguage(new[] { 1, 4, 3, 4 });
        }

        public static string GetWeightedRandomLanguage(int[] weights) => GetWeightedChoice(language, weights);

        #endregion Language

        #region Country

        private static Dictionary<string, string[]> languageToCountries = new Dictionary<string, string[]>() {
        { "Swedish", new string[] { "Sweden" } },
        { "English", new string[] { "Great Britain", "United States", "Australia", "New Zeeland", "Canada" } },
        { "German", new string [] { "Germany", "Austria", "Switzerland"} },
        { "Spanish", new string[] { "Spain", "Argentine", "Mexico", "Bolivia", "Chile", "Peru", "Cuba", "Venezuela", "United States"} },
    };

        private static Dictionary<string, int[]> languageToCountryWeights = new Dictionary<string, int[]>()
    {
        { "Swedish", new int[] { 1 } },
        { "English", new int[] { 2, 4, 2, 1, 2 } },
        { "German", new int[] { 4, 1, 1 } },
        { "Spanish", new int[] { 3, 4, 3, 2, 2, 1, 1, 2, 2 } },
    };

        public static string GetNation(string language)
        {
            var countries = languageToCountries[language];
            return countries[Random.Range(0, countries.Length)];
        }

        public static string GetWeightedNation(string language)
        {
            var countries = languageToCountries[language];
            var weights = languageToCountryWeights[language];
            return GetWeightedChoice(countries, weights);
        }

        #endregion Country

        #region Swedish

        private static string[] SweFirstNames = new string[] {
        "Anna", "Eva", "Maria", "Karin", "Sara", "Christina", "Lena", "Emma", "Kerstin", "Marie", "Malin",
        "Jenny", "Ingrid", "Hanna", "Linda", "Annika", "Susanne", "Elin", "Monica", "Birgitta",
        "Lars", "Mikael", "Anders", "Johan", "Erik", "Per", "Peter", "Thomas", "Karl", "Jan", "Daniel",
        "Fredrik", "Mohammad", "Andreas", "Stefan", "Hans", "Mats", "Marcus", "Mattias", "Magnus"
    };

        private static string[] SweSurnames = new string[] {
        "Andersson", "Johansson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson", "Svensson",
        "Gustafsson", "Pettersson", "Jonsson", "Jansson", "Hansson", "Bengtsson", "Jönsson", "Lindberg",
        "Jakobsson", "Magnusson", "Olofsson", "Lindström", "Lindqvist", "Lindgren", "Axelsson", "Berg"
    };

        private static string GetSwedishName() => $"{SweFirstNames[Random.Range(0, SweFirstNames.Length)]} {SweSurnames[Random.Range(0, SweSurnames.Length)]}";

        #endregion Swedish

        #region English

        private static string[] EngFirstNames = new string[]
        {
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "David", "Elizabeth",
        "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Christopher",
        "Karen", "Charles", "Lisa", "Daniel", "Nancy", "Mathew", "Betty", "Anthony", "Sandra",
        "Mark", "Margaret", "Donald", "Ashley", "Steven", "Kimberly", "Andrew", "Emily",
        "Paul", "Donna", "Joshua", "Michelle", "Keneth", "Carol", "Kevin", "Amanda", "Brian", "Melissa",
        "George", "Deborah", "Timothy", "Stephanie", "Ronald", "Dorothy", "Jason", "Rebecca"
        };

        private static string[] EngSurnames = new string[]
        {
        "Smith", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Davies", "Patel",
        "Robinson", "Wright", "Thompson", "Evans", "Walker", "White", "Roberts",
        "Green", "Hall", "Thomas", "Clarke", "Jackson", "Wood", "Harris", "Edwards", "Turner",
        "Martin", "Cooper", "Hill", "Ward", "Hughes", "Moor", "Clark", "King", "Harrison",
        "Lewis", "Baker", "Lee", "Morris", "Khan", "Scott", "Watson", "Davis", "Parker",
        "Miler", "Anderson", "Young",
        };

        private static string GetEnglishName() => $"{EngFirstNames[Random.Range(0, EngFirstNames.Length)]} {EngSurnames[Random.Range(0, EngSurnames.Length)]}";

        #endregion English

        #region German

        private static string[] GerFirstNames = new string[]
        {
        "Peter", "Michael", "Wolfgang", "Thomas", "Klaus", "Werner", "Manfred",
        "Ursula", "Maria", "Hans", "Heinz", "Andreas", "Jürgen", "Monika", "Helmut",
        "Gerhard", "Petra", "Günter", "Renate", "Helga", "Karin", "Dieter", "Horst",
        "Sabine", "Birgitte", "Josef", "Ingrid", "Elisabeth", "Frank", "Andrea", "Gisela",
        "Walter", "Bernd", "Erika", "Karl", "Christa", "Claudia", "Herbert", "Martin",
        "Birgit", "Christine", "Susanne"
        };

        private static string[] GerSurnames = new string[]
        {
        "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker",
        "Schultz", "Hoffmann", "Schäfer", "Koch", "Bauer", "Richter", "Klein", "Wolf",
        "Schröder", "Neumann", "Schwartz", "Zimmermann", "Braun", "Krüger", "Hofmann",
        "Hartmann", "Lange", "Schmitt", "Werner", "Schmitz", "Krause", "Meier", "Lehmann",
        "Schmid", "Schulze", "Maier", "Köhler", "Herrmann", "König", "Walter", "Mayer",
        "Huber", "Kaiser", "Fuchs", "Peters", "Lang", "Scholtz", "Möller"
        };

        private static string GetGermanName() => $"{GerFirstNames[Random.Range(0, GerFirstNames.Length)]} {GerSurnames[Random.Range(0, GerSurnames.Length)]}";

        #endregion German

        #region Spanish

        private static string[] EspFirstNames = new string[]
        {
        "Maria", "Jose", "Juan", "Francisco", "Antonio", "Ana", "Manuel", "Miguel", "Carmen",
        "David", "Luis", "Carlos", "Jesus", "Javier", "Pedro", "Daniel", "Isabel", "Jose-Antonio",
        "Rosa", "Angel", "Laura", "Alejandro", "Jose-Luis", "Josefa", "Francisco-Javier", "Rafael",
        "Ana-Maria", "Christina", "Marta", "Pablo", "Maria-Pilar", "Jorge", "Fernando",
        "Jose-Manuel", "Francisca", "Miguel-Angel", "Antonia", "Alberto", "Sergio", "Lucia",
        "Jose-Maria", "Dolores", "Maria-Isabel", "Maria-Jose", "Elena", "Sara", "Paula",
        "Diego", "Pilar", "Victor", "Raquel"
        };

        private static string[] EspSurnames = new string[]
        {
        "Gracía", "Fernández", "González", "Rodrígues", "López", "Martínez", "Sánchez", "Pérez",
        "Martin", "Gómez", "Ruiz", "Hernández", "Jiménez", "Díaz", "Álvarez", "Moreno", "Muñoz",
        "Alonso", "Gutiérrez", "Romero", "Navarro", "Torres", "Domínguez", "Gil", "Vázquez",
        "Serrano", "Ramos", "Blanco", "Sanz", "Castro", "Suárez", "Ortega", "Rubio", "Molina",
        "Delgado", "Ramírez", "Morales", "Ortiz", "Marin", "Iglesias", "Flores", "Cruz", "Reyes",
        "Jiménez", "Mendoza", "Aguilar", "Castillo", "Herrera", "Medina", "Vargas", "Méndez",
        "Guzmán", "Juárez", "Rojas", "Luna", "Peña", "Rosario", "Santana", "Rivera", "De León"
        };

        private static string GetSpanishName() => $"{EspFirstNames[Random.Range(0, EspFirstNames.Length)]} {EspSurnames[Random.Range(0, EspSurnames.Length)]} {EspSurnames[Random.Range(0, EspSurnames.Length)]}";

        #endregion Spanish

        public static string GetName(string language)
        {
            if (language == "Swedish")
            {
                return GetSwedishName();
            }
            else if (language == "English")
            {
                return GetEnglishName();
            }
            else if (language == "German")
            {
                return GetGermanName();
            }
            else if (language == "Spanish")
            {
                return GetSpanishName();
            }

            throw new System.NotImplementedException($"Don't know {language} names");
        }
    }
}