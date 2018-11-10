using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Auth;

namespace app.ServiceModel
{
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    [Route("/register")]
    [Route("/register/{Email}/{Password}")]
    public class Register : IReturn<RegisterResponse>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterResponse
    {
        public string Result { get; set; }
    }

    [Route("/users")]
    [Route("/users/{Id}")]
    public class UsersReq : IReturn<RegisterResponse>
    {
        public string Id { get; set; }
    }

    public class UsersResponse
    {
        public object Users { get; set; }
    }
}
