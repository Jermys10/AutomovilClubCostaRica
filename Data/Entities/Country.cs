using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutomovilClub.Backend.Data.Entities
{
    public class Country
    {
        [Key]
        public int CountryId { get; set; }

        [Display(Name = "País")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Código de País")]
        public string CountryCode { get; set; }=string.Empty;

        [Display(Name = "Código de País (3DS)")]
        public string CountryCode3ds { get; set; } = string.Empty;

        [Display(Name="Creación")]
        public DateTime Create { get; set; } = DateTime.Today;

        [Display(Name="Activo")]
        public bool Active { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<RequestLicenceSport>? RequestLicenceSports { get; set; }

        // Relación uno a muchos con RequestLicenceSportInternational para el país de residencia
        public virtual ICollection<RequestLicenceSportInternational>? ResidenceCountries { get; set; }

        // Relación uno a muchos con RequestLicenceSportInternational para el país de domicilio
        public virtual ICollection<RequestLicenceSportInternational>? DomicileCountries { get; set; }

        public Country()
        {
            Name = string.Empty;
            CountryCode = string.Empty;
            Create=DateTime.Today;
            Active = true;
        }
    }
}
