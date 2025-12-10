namespace AutomovilClub.Backend.Data.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Rejection
    {
        [Key]
        public int RejectionId { get; set; }

        [Display(Name ="Motivo")]
        public string Motive { get; set; }

        [Display(Name = "Creado")]
        public DateTime Create { get; set; }

        public int? RequestLicenceSportId { get; set; }

        public int? RequestLicenceSportInternationalId { get; set; }
        
        public int? RequestAssociateMembershipId { get; set; }

        public int? RequestVirtualSportsOfficialLicensesId { get; set; }

        public int? RequestLicenceConcursanteSportId { get; set; }

        public virtual RequestLicenceSport? RequestLicenceSport { get; set; }

        public virtual RequestLicenceConcursanteSport? RequestLicenceConcursanteSport { get; set; }

        public virtual RequestLicenceSportInternational? RequestLicenceSportInternational { get; set; }
        
        public virtual RequestAssociateMembership? RequestAssociateMembership { get; set; }

        public virtual RequestVirtualSportsOfficialLicenses? RequestVirtualSportsOfficialLicenses { get; set; }
    }
}
