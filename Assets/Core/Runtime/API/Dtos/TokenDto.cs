using System.Collections.Generic;

namespace Nebula.Runtime.API.Dtos
{
    public class TokenDto
    {
        /// <summary>
        /// JWT token body
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Roles of this user
        /// </summary>
        public List<string> Roles { get; set; }
    }
}