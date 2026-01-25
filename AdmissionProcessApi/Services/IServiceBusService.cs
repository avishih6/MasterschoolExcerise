using AdmissionProcessDAL.Models;

namespace AdmissionProcessApi.Services;

public interface IServiceBusService
{
    Task SendUserRegistrationMessageAsync(User user);
}
