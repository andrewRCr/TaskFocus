namespace TaskFocusAPI.Models
{
    public record UserRegistrationModel(string FirstName,
                                        string LastName,
                                        string Email,
                                        string Password);
}
