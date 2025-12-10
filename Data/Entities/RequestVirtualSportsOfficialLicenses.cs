using AutomovilClub.Backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace AutomovilClub.Backend.Data.Entities
{
    public class RequestVirtualSportsOfficialLicenses
    {
        private static IConfiguration _configuration;

        public static IConfiguration Configuration
        {
            get { return _configuration; }
            set { _configuration = value; }
        }

        [Key]
        public int RequestVirtualSportsOfficialLicensesId { get; set; }

        [Display(Name = "Identificación")]
        public string Identification { get; set; }

        [Display(Name = "Nombre")]
        public string Name { get; set; }


        [Display(Name = "Nombre Corto")]
        public string? ShortName { get; set; }

        [Display(Name = "Número Telefónico")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Mail { get; set; }

        [Display(Name = "Foto Reciente")]
        public string? Photo { get; set; }

        public string PhotoFullPath => string.IsNullOrEmpty(Photo)
            ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
            : $"{Configuration["ImageSettings:ImageUrl"]}/{Photo.Substring(2)}";

        //degree
        [Display(Name = "Grado")]
        public Degree Degree { get; set; }

        [Display(Name = "Número de Licencia Deportiva")]
        public string? LicenceNumber { get; set; }

        [Display(Name = "Rol")]
        public Rol Rol { get; set; }

        [Display(Name = "Confirma que acepta el código de conducta y ética")]
        public bool SignedCodeOfConductAndEthics { get; set; } = false;

        [Display(Name = "Creado")]
        public DateTime? Create { get; set; } = DateTime.Now;

        [Display(Name = "Modificado")]
        public DateTime? Modify { get; set; } = DateTime.Now;

        [Display(Name = "Aprobado")]
        public DateTime? Approved { get; set; } = null;

        [Display(Name = "Rejection")]
        public DateTime? Rejection { get; set; } = null;

        public bool FullApproved { get; set; } = false;

        public bool FullRejection { get; set; } = false;

        [Display(Name = "Grádo")]
        public string? Grade { get; set; } = null;

        [Display(Name = "Expedicion")]
        [DataType(DataType.Date)]
        public DateTime? Expedition { get; set; } = null;

        [Display(Name = "Expiración")]
        [DataType(DataType.Date)]
        public DateTime? Expiration { get; set; } = null;

        public string? UserId { get; set; }

        public virtual User? User { get; set; }

        public virtual ICollection<Note>? Notes { get; set; }

        public virtual ICollection<Rejection>? Rejections { get; set; }

        public RequestVirtualSportsOfficialLicenses()
        {
            SignedCodeOfConductAndEthics = false;

        }
    }
}
