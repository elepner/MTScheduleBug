using System;

namespace MT.Scheduling.Messages
{
    public interface CreateUserCommand
    {
        string FirstName { get; set; }
        string LastName { get; set; }
    }

    public interface UserVerified
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        int Timeout { get; set; }
    }
}
