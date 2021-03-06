using System.Linq;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Infra.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(GamifyTasksContext context)
            : base(context) { }

        public Usuario ObterPorEmailESenha(string email, string senha)
        {
            return Queryable().FirstOrDefault(u => u.Email == email && u.Senha == senha);
        }

        public Usuario ObterPorEmail(string email)
        {
            return Queryable().Where(u => u.Email == email).FirstOrDefault();
        }
    }
}
