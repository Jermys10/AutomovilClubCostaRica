namespace AutomovilClub.Backend.Data.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Note
    {
        [Key]
        public int NoteId { get; set; }

        [Display(Name = "Nota")]
        public string Text { get; set; }

        [Display(Name ="Creado")]
        public DateTime Create { get; set; }

        public int? RequestLicenceSportId { get; set; }

        public int? RequestLicenceSportInternationalId { get; set; }

        public int? RequestAssociateMembershipId { get; set; }

        public int? RequestVirtualSportsOfficialLicensesId { get; set; }

        public virtual RequestLicenceSport? RequestLicenceSport { get; set; }

        public virtual RequestLicenceSportInternational? RequestLicenceSportInternational { get; set; }

        public virtual RequestAssociateMembership? RequestAssociateMembership { get; set; }

        public virtual RequestVirtualSportsOfficialLicenses? RequestVirtualSportsOfficialLicenses { get; set; }
    }
}
