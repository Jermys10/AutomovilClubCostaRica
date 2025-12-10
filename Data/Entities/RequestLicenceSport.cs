using AutomovilClub.Backend.Enums;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace AutomovilClub.Backend.Data.Entities
{
    public class RequestLicenceSport
    {

        private static IConfiguration _configuration;

        public static IConfiguration Configuration
        {
            get { return _configuration; }
            set { _configuration = value; }
        }

        [Key]
        public int RequestLicenceSportId { get; set; }

        [Display(Name = "Identificación")]
        public string Identification { get; set; }

        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Mail { get; set; }

        [Display(Name = "Número Telefónico")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Número de Licencia Deportiva")]
        public string? LicenceNumber { get; set; }

        [Display(Name = "Examen Médico ACCR")]
        public string? MedicalExam { get; set; }

        public string MedicalExamFullPath => string.IsNullOrEmpty(MedicalExam)
      ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
      : $"{Configuration["ImageSettings:ImageUrl"]}/{MedicalExam.Substring(2)}";

        public bool MedicalExamApproved { get; set; } = false;

        [Display(Name = "Electrocardiograma")]
        public string? Electrocardiogram { get; set; }

        public string ElectrocardiogramFullPath => string.IsNullOrEmpty(Electrocardiogram)
     ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
     : $"{Configuration["ImageSettings:ImageUrl"]}/{Electrocardiogram.Substring(2)}";

        public bool ElectrocardiogramApproved { get; set; } = false;

        [Display(Name = "Certificado de Curso ACCR")]
        public string? CourseCertificate { get; set; }

        public string CourseCertificateFullPath => string.IsNullOrEmpty(CourseCertificate)
? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
: $"{Configuration["ImageSettings:ImageUrl"]}/{CourseCertificate.Substring(2)}";

        public bool CourseCertificateApproved { get; set; } = false;

        [Display(Name = "Foto Reciente")]
        public string? Photo { get; set; }

        public string PhotoFullPath  => string.IsNullOrEmpty(Photo)
            ? $"{Configuration["ImageSettings:ImageUrl"]}/img/noimage.png"
            : $"{Configuration["ImageSettings:ImageUrl"]}/{Photo.Substring(2)}";

        [Display(Name = "País de Nacimiento")]
        public int CountryId { get; set; }

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

        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? BirthDay { get; set; } = null;

        [Display(Name = "Tipo de Licencia")]
        public LicenceType LicenceType { get; set; }

        [Display(Name = "Nombre Corto")]
        public string? ShortName { get; set; }

        [Display(Name = "Grádo")]
        public string? Grade { get; set; } = null;

        [Display(Name = "Expedicion")]
        [DataType(DataType.Date)]
        public DateTime? Expedition { get; set; } = null;

        [Display(Name = "Expiración")]
        [DataType(DataType.Date)]
        public DateTime? Expiration { get; set; } = null;

        [Display(Name = "Vista Corregida")]
        public bool VistaCorregida { get; set; } = false;

        [Display(Name = "Supervision Medica")]
        public bool SupervisionMedica { get; set; } = false;

        [Display(Name = "Consentimiento WADB")]
        public bool ConsentimientoWADB { get; set; } = false;

        [Display(Name = "FIA MS")]
        [DataType(DataType.Date)]
        public DateTime? FiaMS { get; set; } = null;

        public virtual Country? Country { get; set; }

        public string? UserId { get; set; }

        public virtual User? User { get; set; }

        public virtual ICollection<Note>? Notes { get; set; }

        public virtual ICollection<Rejection>? Rejections { get; set; }


        public RequestLicenceSport()
        {
            Create = DateTime.Now;
            Modify = DateTime.Now;
            Approved = null;

            FullApproved = false;
            MedicalExamApproved= false;
            ElectrocardiogramApproved = false;
            CourseCertificateApproved = false;
        }
    }
}
