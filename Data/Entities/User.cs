
namespace AutomovilClub.Backend.Data.Entities
{
    using Microsoft.AspNetCore.Identity;
    using Enums;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Xml.Linq;
    using System.ComponentModel.DataAnnotations.Schema;

    public class User : IdentityUser
    {
        [Display(Name = "Nombre")]
        public string? Name { get; set; }

        [Display(Name = "Apellidos")]
        public string? LastName { get; set; }

        [Display(Name = "Teléfono")]
        public string? Phone { get; set; }
            
        [Display(Name = "Email")]
        public string? Mail { get; set; }

        [Display(Name = "Dirección")]
        [MaxLength(100, ErrorMessage = "El campo {0} no puede tener más de {1} carácteres.")]
        public string? Address { get; set; }

        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Display(Name = "Activo")]
        public bool Active { get; set; }

        [Display(Name = "Eliminado")]
        public bool IsDelete { get; set; }

        [Display(Name = "Creación")]
        public DateTime Create { get; set; }

        [Display(Name = "Perfil")]
        public Role Role { get; set; }


        public virtual ICollection<RequestVirtualSportsOfficialLicenses>? RequestVirtualSportsOfficialLicenses { get; set; }

        public virtual ICollection<RequestLicenceSportInternational>? RequestLicenceSportInternationals { get; set; }

        public virtual ICollection<RequestLicenceSport>? RequestLicenceSports { get; set; }
        
        public virtual ICollection<RequestLicenceConcursanteSport>? RequestLicenceConcursanteSports { get; set; }
        
        public virtual ICollection<RequestAssociateMembership>? RequestAssociateMemberships { get; set; }
    }
}
