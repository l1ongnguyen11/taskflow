using System;

namespace TaskFlow.Application.DTOs.Projects;

public class AddProjectMemberRequest
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member";
}
