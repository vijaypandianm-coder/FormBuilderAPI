using System.Data;

namespace FormBuilderAPI.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}