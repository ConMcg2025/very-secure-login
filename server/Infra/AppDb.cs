using Infra.Entities;
using LinqToDB;
using LinqToDB.Data;

namespace Infra;

///Linq2DB connection; one instance per request (scoped)
public class AppDb(DataOptions options) : DataConnection(options)
{
    public ITable<UserEntity> Users => this.GetTable<UserEntity>();
}
