using System.Linq;
using UnityEngine;

namespace LMCore.ProcGen
{
    public static class OccupationList
    {
        private static string[] UncommonOccupations = new string[]
        {
"Blogger",
"Book Coach",
"Polygraph",
"Scenic designer",
"Charge artist",
        };

        private static string[] Occupations = new string[] {
"Author",
"Copy Editor",
"Creative Consultant",
"Freelancer",
"Ghostwriter",
"Infopreneur",
"Investigative Journalist",
"Journalist",
"Literary Editor",
"Novelist",
"Poet",
"Author",
"Screenwriter",
"Songwriter",
"Speechwriter",
"Technical writer",
"Choreographer",
"Composer",
"Costume designer",
"Director",
"Dramaturge",
"Fight Director",
"Intimacy Coordinator",
"Lighting Designer",
"Make-up Artist",
"Music director",
"Playwright",
"Producer",
"Scenographer",
"Set designer",
"Sound designer",
"Actor",
"Audio engineer",
"Backstage",
"Carpenter",
"Master carpenter",
"Dancer",
"Electrician",
"Fight Director",
"Hair and wig designer",
"Lighting technician",
"Musician",
"Painters",
"Production manager",
"Property master",
"Publicist",
"Sound Engineer",
"Sound Technician",
"Stagehand",
"Stage manager",
"Assistant Stage Manager",
"Technical director",
"Theatrical technician",
"Wardrobe supervisor",
"Artistic director",
"Company Manager",
"Costume Shop Manager",
"Production Manager",
"Cabin Crew",
"Janitor",
"Light Board Operator",
"Literary Manager",
"Marketing Director",
"Music Director",
"Public Relations Director",
"Spotlight Operator",
"Stage crew",
"Technical Director",
"Theater manager",
"Ticketing agent",
"Usher",
"Wardrobe Crew",
"Acrobat",
"Actor",
"Athlete",
"Circus Performer",
"Clown",
"Comedian",
"Dancer",
"Drag queen",
"Drag king",
"DJ",
"Filmmaker",
"Host",
"Illusionist",
"Internet celebrity",
"Online streamer",
"Musician",
"Painter",
"Performer",
"Photographer",
"Podcaster",
"Professional wrestler",
"Radio personality",
"Singer",
"Street performer",
"Stunt performer",
"TV celebrity",
"Choreographer",
"Dancer",
"Backup dancer",
"Exotic dancer",
"Taxi driver",
"Animator",
"Architect",
"Baker",
"Comedian",
"Concept Artist",
"Curator",
"Event Planner",
"Fashion Designer",
"Floral Designer",
"Game Designer",
"Graphic Designer",
"Hairstylist",
"Illustrator",
"Potter",
"Sculptor",
"Tattoo Artist",
"Video Game Designer",
"Arborist",
"Auto Mechanic",
"Construction Worker",
"Factory Worker",
"Foreman",
"Mechanic",
"Miller",
"Plumber",
"Welder",
"Woodworker",
"Smith",
"Blacksmith",
"Silversmith",
"Goldsmith",
"Jeweler",
"CNC Operator",
"Manager",
"Engineer",
"Scientist",
"Miner",
"Union Representative",
"Truck Driver",
"Mechanical Engineer",
"Chemical Engineer",
"Industrial Engineer",
"Civil Engineer",
"Guard",
"Station Agent",
"Senior Station Master",
"Junior Station master",
"Porter",
"Dispatcher",
"Freight Conductor",
"Navigator",
"Bookbinder",
"Glover",
"Hatter",
"Leatherworker",
"Sailmaker",
"Shoemaker",
"Tailor",
"Taxidermist",
"Upholsterer",
"Bus Driver",
"Chauffeur",
"Delivery Man",
"Gig Worker",
"Ambulance Driver",
"Tram Driver",
"Pilot",
"Computer Operator",
"Computer Scientist",
"Data Analyst",
"IT Consultant",
"Network Analyst",
"Programmer",
"Scrum master",
"Security Engineer",
"Software Analyst",
"Software Architect",
"Support Technician",
"System Administrator",
"Video Game Developer",
"Anesthesiologist",
"Nurse",
"Cardiologist",
"Dentist",
"Dental Hygienist",
"Dental Assistant",
"Dental Technician",
"Dermatologist",
"Dietitian",
"Paramedic",
"Endocrinologist",
"Gastroenterologist",
"Genetic Counsellor",
"Geriatrician",
"Haematologist",
"Dialysis Technician",
"Neurologist",
"Audiologist",
"Neuropsychologist",
"Oncologist",
"Radiation Therapist",
"Pathologist",
"General Practitioner",
"Pharmacist",
"Pediatrician",
"Neonatal Nurse",
"Pediatric Nurse",
"Psychiatrist",
"Psychologist",
"Social Worker",
"Sport Psychologist",
"Chiropractor",
"Yoga Instructor",
"Massage Therapist",
"Radiologist",
"Obstetrician",
"Gynaecologist",
"Midwife",
"Urologist",
"Biologist",
"Botanist",
"Herpetologist",
"Microbiologist",
"Neuroscientist",
"Physician",
"Veterinarian",
"Zoologist",
"Mathematician",
"Actuary",
"Statistician",
"Forensic Scientist",
"Inventor",
"Archaeologist",
"Astronaut",
"Astronomer",
"Chemist",
"Geographer",
"Naturalist",
"Oceanographer",
"Paleontologist",
"Physicist",
"Economist",
"Historian",
"Linguist",
"Political scientist",
"Sociologist",
"Urban Planner",
"Agent Provocateur",
"Bodyguard",
"Park Ranger",
"Prison Officer",
"Prison Warden",
"Private Investigator",
"Military Police",
"Sheriff",
};

        public static string[] AllOccupations => Occupations.Concat(UncommonOccupations).ToArray();

        public static string GetRandomOccupation() => Occupations[Random.Range(0, Occupations.Length)];
    }
}