using Microsoft.EntityFrameworkCore;

namespace BlazorS7Upload.DB;

public class PSqlComplianceDbContext : DbContext
{
    public PSqlComplianceDbContext(DbContextOptions<PSqlComplianceDbContext> options): base(options){}
}
