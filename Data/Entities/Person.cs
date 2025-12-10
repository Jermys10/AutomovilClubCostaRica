using System.ComponentModel.DataAnnotations;

namespace AutomovilClub.Backend.Data.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }

        public string Identification { get; set; }

        public string District { get; set; }

        public string Expirate { get; set; }

        public string Name { get; set; }

        public string LastName1 { get; set; }

        public string LastName2 { get; set; }
    }
}
