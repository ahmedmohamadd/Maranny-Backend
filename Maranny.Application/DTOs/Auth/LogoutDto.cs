using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maranny.Application.DTOs.Auth
{
    public class LogoutDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}