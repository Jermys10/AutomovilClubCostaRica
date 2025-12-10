namespace AutomovilClub.Backend.Data
{
    using AutomovilClub.Backend.Enums;
    using Entities;
    using Helpers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System.Linq;
    using System.Threading.Tasks;

    public class SeeDb
    {
        private readonly DataContext _context;
        private readonly IUserHelper _userHelper;
        public SeeDb(DataContext context, IUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }


        private async Task CheckUserAsync(string firstName, string lastName, string email, string phoneNumber, string address, Role userType)
        {
            User user = await _userHelper.GetUserAsync(email);
            if (user == null)
            {
                user = new User()
                {
                    Email = email,
                    Name = $"{firstName} {lastName}",
                    PhoneNumber = phoneNumber,
                    UserName = email,
                    Role = userType,
                    Active = true,
                    Address = address
                };

                await _userHelper.AddUserAsync(user, "123456");
                await _userHelper.AddUserToRoleAsync(user, userType.ToString());

                string token = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
                await _userHelper.ConfirmEmailAsync(user, token);
            }
        }

        private async Task CheckRolesAsycn()
        {
            await _userHelper.CheckRoleAsync(Role.Administrador.ToString());
            await _userHelper.CheckRoleAsync(Role.Usuario.ToString());

        }

        public async Task SeedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            await CheckConfigurationAsync();
            await CheckRolesAsycn();
            await CheckUserAsync("Donovan", "Jarquin", "djarquin@ticomanager.com", "60829377", "Los Angeles", Role.Administrador);
            await CheckUserAsync("Tico", "Manager", "admin@ticomanager.com", "71728892", "Grecia", Role.Administrador);
        }

        private async Task CheckConfigurationAsync()
        {
            var configurationExist = await _context.Configurations.AnyAsync();
            if (!configurationExist)
            {
                Configuration configuration = new Configuration()
                {
                   MessagePermiso = $"<h1>Solicitud de permiso internacional</h1>" +
                                       $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitud = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",
                   MessagePermisoAdmin = $"<h1>Solicitud de permiso internacional</h1>" +
                                        $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitudAdmin = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",
                   MessagePermisoApproved = $"<h1>Solicitud de permiso internacional</h1>" +
                                              $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitudApproved = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",
                   ValidateAge = 1,
                   MessagePermisoAdminAprobado = $"<h1>Solicitud de permiso internacional</h1>" +
                                              $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitudAdminAprobado = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",
                   MessagePermisoAdminRejection = $"<h1>Solicitud de permiso internacional</h1>" +
                                              $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitudAdminRejection = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",
                   MessagePermisoRejection = $"<h1>Solicitud de permiso internacional</h1>" +
                                                 $"La solicitud de permiso internacional de conducción ha sido recibida correctamente.",
                   MessageSolicitudRejection = $"<h1>Solicitud de licencia deportiva</h1>" +
                   $"La solicitud de licencia deportiva ha sido recibida correctamente.",


                    MessageMembresia = $"<h1>Solicitud Membresia</h1>" +
                   $"La solicitud de membresia ha sido recibida correctamente.",

                    MessageMembresiaAdmin= $"<h1>Solicitud Membresia</h1>" +
                   $"La solicitud de membresia ha sido recibida correctamente.",
                    MessageMembresiaAdminApproved = $"<h1>Solicitud Membresia Aprobado</h1>" +
                   $"La solicitud de membresia ha sido aprobada correctamente.",

                    MessageMembresiaAdminRejection = $"<h1>Solicitud Membresia Rechazada</h1>" +
                   $"La solicitud de membresia ha sido Rechazada.",

                    ValidateCourseCertificate = false,
                   ValidateElectrocardiogram = false,
                   ValidateIdentification = false,
                   ValidateLicence = false,
                   ValidateMedicalExam = false,
                   ValidatePhoto = false
                };

                _context.Configurations.Add(configuration);
                await _context.SaveChangesAsync();
                
            }
        }

    }
}
