using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.Library.API
{
    public class AuthenticatedUser
    {
        public string AccessToken { get; set; }
        public string UserName { get; set; }
    }
}
