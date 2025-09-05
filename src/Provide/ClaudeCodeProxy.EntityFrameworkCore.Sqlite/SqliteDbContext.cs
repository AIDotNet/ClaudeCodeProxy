using ClaudeCodeProxy.Core;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.EntityFrameworkCore.Sqlite;

public class SqliteDbContext : MasterDbContext<SqliteDbContext>
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
    {
    }
}