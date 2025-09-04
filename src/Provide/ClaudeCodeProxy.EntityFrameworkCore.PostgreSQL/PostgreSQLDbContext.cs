using ClaudeCodeProxy.Core;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.EntityFrameworkCore.PostgreSQL;

public class PostgreSQLDbContext : MasterDbContext<PostgreSQLDbContext>
{
    public PostgreSQLDbContext(DbContextOptions<PostgreSQLDbContext> options) : base(options)
    {
    }

}