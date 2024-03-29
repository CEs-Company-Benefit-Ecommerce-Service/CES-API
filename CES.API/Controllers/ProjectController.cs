﻿using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private IProjectServices _projectServices;
        private readonly IHttpContextAccessor _contextAccessor;
        public ProjectController(IProjectServices projectServices, IHttpContextAccessor contextAccessor)
        {
            _projectServices = projectServices;
            _contextAccessor = contextAccessor;
        }
        [HttpGet]
        public IActionResult Gets([FromQuery] PagingModel pagingModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Gets(pagingModel);
            return StatusCode((int)result.Code, result);
        }
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Get(id).Result;
            return StatusCode((int)result.Code, result);
        }
        [SwaggerOperation(summary: "Create project", description: "Status: 0 - InActive, 1 - Active")]
        [HttpPost]
        public IActionResult CreateProject([FromBody] ProjectRequestModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Create(requestModel).Result;
            return StatusCode((int)result.Code, result);
        }
        [HttpPut("{id}")]
        public IActionResult UpdateProject(Guid id, [FromBody] ProjectRequestModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Update(id, requestModel).Result;
            return StatusCode((int)result.Code, result);
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Delete(id).Result;
            return StatusCode((int)result.Code, result);
        }
        [SwaggerOperation(summary: "Add member to project")]
        [HttpPost("members")]
        public IActionResult AddProjectMember([FromBody] List<ProjectMemberRequestModel> requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.AddEmployee(requestModel).Result;
            return StatusCode((int)result.Code, result);
        }
        [SwaggerOperation(summary: "Remove member of project")]
        [HttpDelete("members/remove")]
        public IActionResult RemoveProjectMember([FromBody] List<ProjectMemberRequestModel> requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.RemoveEmployee(requestModel).Result;
            return StatusCode((int)result.Code, result);
        }
    }
}
