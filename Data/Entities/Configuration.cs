using System.ComponentModel.DataAnnotations;

namespace AutomovilClub.Backend.Data.Entities
{
    public class Configuration
    {
        [Key]
        public int ConfigurationId { get; set; }

        [Display(Name = "Validar Examen Medico")]
        public bool ValidateMedicalExam { get; set; } = false;

        [Display(Name = "Validar Electrocardiograma")]
        public bool ValidateElectrocardiogram { get; set; } = false;

        [Display(Name = "Validar Certificado de Curso")]
        public bool ValidateCourseCertificate { get; set; } = false;

        [Display(Name = "Validar Foto")]
        public bool ValidatePhoto { get; set; } = false;

        [Display(Name = "Validar Identificación")]
        public bool ValidateIdentification { get; set; } = false;

        [Display(Name = "Validar Licence")]
        public bool ValidateLicence { get; set; } = false;

        [Display(Name = "Años de Validación")]
        public int ValidateAge { get; set; }


        [Display(Name = "Mensaje de Solicitud")]
        public string? MessageSolicitud { get; set; }

        [Display(Name = "Mensaje de Permiso")]
        public string? MessagePermiso { get; set; }

        [Display(Name = "Mensaje de Solicitud Admin")]
        public string? MessageSolicitudAdmin { get; set; }

        [Display(Name = "Mensaje de Permiso Admin")]
        public string? MessagePermisoAdmin { get; set; }

        [Display(Name = "Mensaje de Membresia")]
        public string? MessageMembresia { get; set; }

        [Display(Name = "Mensaje de Membresia Admin")]
        public string? MessageMembresiaAdmin { get; set; }

        [Display(Name = "Mensaje de Oficiales")]
        public string? MessageOficiales { get; set; }

        [Display(Name = "Mensaje de Oficiales Admin")]
        public string? MessageOficialesAdmin { get; set; }


        [Display(Name = "Mensaje de Solicitud Aprobada")]
        public string? MessageSolicitudApproved { get; set; }

        [Display(Name = "Mensaje de Permiso Aprobado")]
        public string? MessagePermisoApproved { get; set; }

        [Display(Name = "Mensaje de Solicitud Aprobado Admin")]
        public string? MessageSolicitudAdminAprobado { get; set; }

        [Display(Name = "Mensaje de Permiso Aprobado Admin")]
        public string? MessagePermisoAdminAprobado { get; set; }

        [Display(Name = "Mensaje de Membresia Aprobado")]
        public string? MessageMembresiaApproved { get; set; }

        [Display(Name = "Mensaje de Membresia Aprobado Admin")]
        public string? MessageMembresiaAdminApproved { get; set; }

        [Display(Name = "Mensaje de Oficiales Aprobado")]
        public string? MessageOficialesApproved { get; set; }

        [Display(Name = "Mensaje de Oficiales Aprobado Admin")]
        public string? MessageOficialesAdminApproved { get; set; }



        [Display(Name = "Mensaje de Solicitud Rechazo")]
        public string? MessageSolicitudRejection{ get; set; }

        [Display(Name = "Mensaje de Permiso Rechazo")]
        public string? MessagePermisoRejection{ get; set; }

        [Display(Name = "Mensaje de Solicitud Rechazo Admin")]
        public string? MessageSolicitudAdminRejection{ get; set; }

        [Display(Name = "Mensaje de Permiso Rechazo Admin")]
        public string? MessagePermisoAdminRejection { get; set; }

        [Display(Name = "Mensaje de Membresia Rechazo")]
        public string? MessageMembresiaRejection { get; set; }

        [Display(Name = "Mensaje de Membresia Rechazo Admin")]
        public string? MessageMembresiaAdminRejection { get; set; }

        [Display(Name = "Mensaje de Oficiales Rechazo")]
        public string? MessageOficialesRejection { get; set; }

        [Display(Name = "Mensaje de Oficiales Rechazo Admin")]
        public string? MessageOficialesAdminRejection { get; set; }

    }
}
