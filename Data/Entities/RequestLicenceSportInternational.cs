using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomovilClub.Backend.Data.Entities
{
    public class RequestLicenceSportInternational
    {
        private static IConfiguration _configuration;

        public static IConfiguration Configuration
        {
            get { return _configuration; }
            set { _configuration = value; }
        }

        [Key]
        public int RequestLicenceSportInternationalId { get; set; }

        //[Display(Name = "Identificación")]
        //public string Identification { get; set; }

        [Display(Name = "Nombre Completo")]
        public string Name { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Mail { get; set; }

        [Display(Name = "Número Telefónico")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Dirección")]
        public string  Address { get; set; }

        [Display(Name = "¿Dónde Viaja?")]
        public string Travel { get; set; }

        //[Display(Name = "Número de Licencia Deportiva")]
        //public string? LicenceNumber { get; set; }

     //   [Display(Name = "Examen Médico ACCR")]
     //   public string? MedicalExam { get; set; }

     //   public string MedicalExamFullPath => string.IsNullOrEmpty(MedicalExam)
     // ? $"https://localhost:7169/img/noimage.png"
     // : $"https://localhost:7169/{MedicalExam.Substring(2)}";

     //   public bool MedicalExamApproved { get; set; } = false;

     //   [Display(Name = "Electrocardiograma")]
     //   public string? Electrocardiogram { get; set; }

     //   public string ElectrocardiogramFullPath => string.IsNullOrEmpty(Electrocardiogram)
     //? $"https://localhost:7169/img/noimage.png"
     //: $"https://localhost:7169/{Electrocardiogram.Substring(2)}";

     //   public bool ElectrocardiogramApproved { get; set; } = false;

     //   [Display(Name = "Certificado de Curso ACCR")]
     //   public string? CourseCertificate { get; set; }

     //   public bool CourseCertificateApproved { get; set; } = false;

        [Display(Name = "Documento de identificación (Anverso)")]
        public string? IdentificationFP { get; set; }

        public string IdentificationFPFullPath => string.IsNullOrEmpty(IdentificationFP)
           ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
           : $"{Configuration["ImageSettings:ImageUrl"]}/{IdentificationFP.Substring(2)}";

        [Display(Name = "Documento de identificación (Reverso)")]
        public string? IdentificationTP { get; set; }

        public string IdentificationTPFullPath => string.IsNullOrEmpty(IdentificationTP)
       ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
       : $"{Configuration["ImageSettings:ImageUrl"]}/{IdentificationTP.Substring(2)}";

        [Display(Name = "Licencia costarricense (Anverso)")]
        public string? LicenceFP { get; set; }

        public string LicenceFPFullPath => string.IsNullOrEmpty(LicenceFP)
           ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
           : $"{Configuration["ImageSettings:ImageUrl"]}/{LicenceFP.Substring(2)}";

        [Display(Name = "Licencia costarricense (Reverso)")]
        public string? LicenceTP { get; set; }

        public string LicenceTPFullPath => string.IsNullOrEmpty(LicenceTP)
       ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
       : $"{Configuration["ImageSettings:ImageUrl"]}/{LicenceTP.Substring(2)}";

        [Display(Name = "Foto Reciente")]
        public string? Photo { get; set; }

        public string PhotoFullPath => string.IsNullOrEmpty(Photo)
            ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
            : $"{Configuration["ImageSettings:ImageUrl"]}/{Photo.Substring(2)}";

        [Display(Name = "País de residencia")]
        public int? ResidenceCountryId { get; set; }

        [Display(Name = "Ciudad de Residencia ")]
        [Required]
        public string ResidenceCity { get; set; }

        [Display(Name = "País de Nacimiento")]
        public int DomicileCountryId { get; set; }

        [Display(Name = "Ciudad de Nacimiento ")]
        [Required]
        public string DomicileCity { get; set; }

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

        public string? UserId { get; set; }

        public virtual User? User { get; set; }

        [ForeignKey("ResidenceCountryId")]
        public virtual Country? ResidenceCountry { get; set; }

        [ForeignKey("DomicileCountryId")]
        public virtual Country? DomicileCountry { get; set; }

        public virtual ICollection<Note>? Notes { get; set; }

        public virtual ICollection<Rejection>? Rejections { get; set; }


        public RequestLicenceSportInternational()
        {
            Create = DateTime.Now;
            Modify = DateTime.Now;
            Approved = null;

            FullApproved = false;
            //MedicalExamApproved = false;
            //ElectrocardiogramApproved = false;
            //CourseCertificateApproved = false;
        }
    }
}
