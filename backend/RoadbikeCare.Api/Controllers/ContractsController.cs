using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;
using RoadbikeCare.Api.Services;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalUser")]
[Route("api/contracts")]
public sealed class ContractsController(RoadbikeCareDbContext db, RoadbikeCarePersistenceService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetContracts(CancellationToken cancellationToken)
    {
        IQueryable<Contract> contracts = ContractQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            contracts = contracts.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        return Ok((await contracts.ToListAsync(cancellationToken)).Select(ContractView));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetContract(int id, CancellationToken cancellationToken)
    {
        IQueryable<Contract> contracts = ContractQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            contracts = contracts.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        Contract? contract = await contracts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return contract is null ? NotFound() : Ok(ContractView(contract));
    }

    [HttpPost]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateContract(CreateContractRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateContractAsync(input, cancellationToken),
            item => Created($"/api/contracts/{item.Id}", ContractView(item)));
}
