namespace AutomovilClub.Backend.Helpers
{
    using AutomovilClub.Backend.Data;
    using AutomovilClub.Backend.Data.Entities;
    using Models;
    using System;
    using System.Threading.Tasks;

    public class ConverterHelper : IConverterHelper
    {
        private readonly DataContext _context;
        private readonly ICombosHelper _combosHelper;

        public ConverterHelper(DataContext context, ICombosHelper combosHelper)
        {
            _context = context;
            _combosHelper = combosHelper;
        }

      

        public async Task<User> ToUserAsync(UserViewModel model, string image, bool isNew)
        {
            return new User()
            {
                Email = model.Email,
                Id = isNew ? Guid.NewGuid().ToString() : model.Id,
                PhoneNumber = model.PhoneNumber,
                Phone = model.PhoneNumber,
                UserName = model.Email,
                Role = model.Role,
                Name = model.Name,
                LastName = model.LastName,
                Active = true,
                AccessFailedCount = 0,
                Create = DateTime.Now,
                EmailConfirmed = true,
                Address = model.Address,
                Mail = model.Email,
                Password = model.Password,
            };
        }

        //public async Task<User> ToUserAsync(NewUserRequest model)
        //{
        //    return new User
        //    {
        //        Email = model.Email,
        //        Id = Guid.NewGuid().ToString(),
        //        Image = String.Empty,
        //        PhoneNumber = model.Phone,
        //        Phone = model.Phone,
        //        UserName = model.Email,
        //        Role = Enums.Role.Usuario,
        //        Name = model.Name,
        //        //LastName = String.Empty,
        //        Active = true,
        //        AccessFailedCount = 0,
        //        Create = DateTime.Now,
        //        EmailConfirmed = true,
        //        Address = String.Empty,
        //        Mail = model.Email,
        //        Password = model.Password,
        //    };
        //}

        public UserViewModel ToUserViewModel(User user)
        {
            return new UserViewModel
            {
                Email = user.Email,
                Id = user.Id,
                //Document = "0111251215",
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                Name = $"{user.Name}",
                LastName = user.LastName,
                Address = user.Address,
                Password = user.Password,
                UserName= user.UserName,
                ConfirmedPassword=user.Password,
            };
        }
    }
}
