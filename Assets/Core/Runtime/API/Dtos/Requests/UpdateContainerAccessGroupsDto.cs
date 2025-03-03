using System.Collections.Generic;

namespace Nebula.Core.Runtime.API.Dtos.Requests
{
    public class UpdateContainerAccessGroupsDto
    {
        public List<string> AccessGroups { get; set; }
    }
}