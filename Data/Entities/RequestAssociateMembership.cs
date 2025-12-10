using AutomovilClub.Backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace AutomovilClub.Backend.Data.Entities
{
    public class RequestAssociateMembership
    {
        [Key]
        public int RequestAssociateMembershipId { get; set; }

        [Display(Name = "Identificación")]
        public string Identification { get; set; }

        [Display(Name = "Nombre Completo")]
        public string FullName { get; set; }

        [Display(Name = "Nombre Corto")]
        public string? ShortName { get; set; }

        [Display(Name = "Tipo de Asociado")]
        public AssociateType? AssociateType { get; set; } = null;

        [Display(Name = "Número de Licencia")]
        public string? Number { get; set; }

        [Display(Name = "Creado")]
        public DateTime? Create { get; set; } = DateTime.Now;

        [Display(Name = "Modificado")]
        public DateTime? Modify { get; set; } = DateTime.Now;

        [Display(Name = "Aprobado")]
        public DateTime? Approved { get; set; } = null;

        [Display(Name = "Rejection")]
        public DateTime? Rejection { get; set; } = null;

        [Display(Name = "Expedicion")]
        [DataType(DataType.Date)]
        public DateTime? Expedition { get; set; } = null;

        [Display(Name = "Expiración")]
        [DataType(DataType.Date)]
        public DateTime? Expiration { get; set; } = null;

        public bool FullApproved { get; set; } = false;

        public bool FullRejection { get; set; } = false;

        [Display(Name = "Correo Electrónico")]
        public string Mail { get; set; }

        public string? UserId { get; set; }

        public virtual User? User { get; set; }

        public virtual ICollection<Note>? Notes { get; set; }

        public virtual ICollection<Rejection>? Rejections { get; set; }

        public RequestAssociateMembership()
        {
            Create = DateTime.Now;
            Modify = DateTime.Now;
            Approved = null;

            FullApproved = false;
        }
    }
}
