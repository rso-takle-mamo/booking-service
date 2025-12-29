using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingService.Api.Services.Interfaces;
using BookingService.Api.Exceptions;

namespace BookingService.Api.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController(IUserContextService userContextService) : ControllerBase
{
    protected Guid GetUserId() => userContextService.GetUserId();

    protected Guid? GetTenantId() => userContextService.GetTenantId();

    protected bool IsCustomer() => userContextService.IsCustomer();

    protected string GetUserRole() => userContextService.GetRole();

    protected void ValidateCustomerAccess() => userContextService.ValidateCustomerAccess();
}