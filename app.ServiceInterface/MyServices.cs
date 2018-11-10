using System;
using ServiceStack;
using ServiceStack.OrmLite;
using app.ServiceModel;
using ServiceStack.Logging;
using ServiceStack.Auth;
using ServiceStack.Data;
using System.Collections.Generic;

namespace app.ServiceInterface
{
    
    public class MyServices : Service
    {
        private ILog log = LogManager.GetLogger(typeof(MyServices));

        private IAuthRepository authRepository;
        private IDbConnectionFactory usersRepository;

        public MyServices(IAuthRepository authRepository, IDbConnectionFactory usersRepository)
        {
            this.authRepository = authRepository;
            this.usersRepository = usersRepository;
        }
        [Authenticate]
        public object Any(Hello request)
        {
            IAuthSession session = GetSession();
            log.Debug(session.UserAuthId);
            log.Debug(session.Email);
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }

        public object Any(ServiceModel.Register request)
        {
            var user = request.ConvertTo<UserAuth>();
            
            authRepository.CreateUserAuth(user, request.Password);
            return new ServiceModel.RegisterResponse { Result = $"Hello, {request.Email}!" };
        }
        public object Get(UsersReq request)
        {
            var data = new List<UserAuth>();
            using (var db = usersRepository.Open())
            {
                //Create the PortalTempUser POCO table if it doesn't already exist
                //db.CreateTableIfNotExists<PortalTempUser>();
                data = db.Select<UserAuth>();
            }
            return new UsersResponse { Users = data };
        }
    }
}
