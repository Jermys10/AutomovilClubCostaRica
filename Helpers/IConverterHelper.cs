namespace AutomovilClub.Backend.Helpers
{
    using AutomovilClub.Backend.Data.Entities;
    using Models;
    
    using System.Threading.Tasks;

    public interface IConverterHelper
    {

        Task<User> ToUserAsync(UserViewModel model, string image, bool isNew);

        //Task<User> ToUserAsync(NewUserRequest model);

        UserViewModel ToUserViewModel(User user);

        //Data.Entities.System ToSystem(SystemViewModel model);

        //Organ ToOrgan(OrganViewModel model);

        //Category ToCategory(CategoryViewModel model);

        //Pharmaceutical ToPharmaceutical(PharmaceuticalViewModel model);

        //Brand ToBrand(BrandViewModel model);

        //Drug ToDrug(DrugViewModel model);

        //PharmaceuticalViewModel ToPharmaceuticalViewModel(Pharmaceutical pharmaceutical);

        //CategoryViewModel ToCategoryViewModel(Category model);
    }
}
